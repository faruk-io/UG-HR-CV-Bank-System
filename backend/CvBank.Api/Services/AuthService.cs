using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using CvBank.Api.Auth;
using CvBank.Api.Data;
using CvBank.Api.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace CvBank.Api.Services;

public class AuthService(
    CvBankDbContext db,
    IPasswordHasher<User> passwordHasher,
    IConfiguration configuration,
    IAuditWriter auditWriter,
    IWebHostEnvironment environment)
{
    public async Task<LoginResponse?> LoginAsync(LoginRequest request)
    {
        var userName = (request.UserName ?? string.Empty).Trim();

        var user = await db.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.UserName == userName);

        if (user is null || !user.IsActive)
        {
            await auditWriter.WriteAsync(null, userName, "Failed Login", "User", userName, "User not found or inactive");
            return null;
        }

        var verification = passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password ?? string.Empty);
        if (verification == PasswordVerificationResult.Failed)
        {
            await auditWriter.WriteAsync(user.Id, user.FullName, "Failed Login", "User", user.Id.ToString(), "Wrong password");
            return null;
        }

        if (verification == PasswordVerificationResult.SuccessRehashNeeded)
        {
            user.PasswordHash = passwordHasher.HashPassword(user, request.Password!);
        }

        var permissions = await db.RolePermissions
            .Where(p => p.RoleId == user.RoleId)
            .Select(p => p.PermissionCode)
            .ToListAsync();

        user.LastLoginUtc = DateTime.UtcNow;
        await db.SaveChangesAsync();

        var signingKey = ReadSigningKey(configuration, environment.ContentRootPath);
        var expireMinutes = configuration.GetValue("Jwt:ExpireMinutes", 480);
        var expiresAtUtc = DateTime.UtcNow.AddMinutes(expireMinutes);

        var claims = new List<Claim>
        {
            new(AuthClaims.UserId, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(AuthClaims.UserName, user.UserName),
            new(AuthClaims.FullName, user.FullName),
            new(AuthClaims.Role, user.Role.Code)
        };
        claims.AddRange(permissions.Select(p => new Claim(AuthClaims.Permission, p)));

        var token = new JwtSecurityToken(
            issuer: configuration["Jwt:Issuer"],
            audience: configuration["Jwt:Audience"],
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: expiresAtUtc,
            signingCredentials: new SigningCredentials(new SymmetricSecurityKey(signingKey), SecurityAlgorithms.HmacSha256));

        await auditWriter.WriteAsync(user.Id, user.FullName, "User Login", "User", user.Id.ToString(), null);

        return new LoginResponse(
            new JwtSecurityTokenHandler().WriteToken(token),
            user.Id,
            user.FullName,
            user.Role.Code,
            permissions.Distinct().ToArray(),
            expiresAtUtc);
    }

    public static byte[] ReadSigningKey(IConfiguration configuration, string? contentRoot = null)
    {
        var key = configuration["Jwt:SigningKey"];

        if (!string.IsNullOrWhiteSpace(key) && Encoding.UTF8.GetByteCount(key) >= 32)
        {
            return Encoding.UTF8.GetBytes(key);
        }

        return LoadOrCreateFallbackKey(contentRoot);
    }

    private static readonly object KeyFileGate = new();
    private static byte[]? processKey;

    // A missing/short key must never crash startup (post-publish); persist outside published files so tokens survive.
    private static byte[] LoadOrCreateFallbackKey(string? contentRoot)
    {
        lock (KeyFileGate)
        {
            if (processKey is not null)
            {
                return processKey;
            }

            var path = Path.Combine(contentRoot ?? Directory.GetCurrentDirectory(), "App_Data", "jwt-signing.key");

            try
            {
                if (File.Exists(path))
                {
                    var existing = File.ReadAllText(path).Trim();
                    if (Encoding.UTF8.GetByteCount(existing) >= 32)
                    {
                        processKey = Encoding.UTF8.GetBytes(existing);
                        return processKey;
                    }
                }

                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                var generated = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));
                File.WriteAllText(path, generated);
                processKey = Encoding.UTF8.GetBytes(generated);
                return processKey;
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException)
            {
                // Storage unwritable: still start, with a key scoped to this process.
                processKey = RandomNumberGenerator.GetBytes(48);
                return processKey;
            }
        }
    }
}
