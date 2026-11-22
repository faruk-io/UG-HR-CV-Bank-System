using CvBank.Api.Auth;
using CvBank.Api.Data;
using CvBank.Api.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CvBank.Api.Services;

public class UserService(
    CvBankDbContext db,
    IPasswordHasher<User> passwordHasher,
    IAuditWriter audit)
{
    public async Task<PagedResult<UserListItemDto>> ListAsync(UserQuery query)
    {
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 5, 100);
        var search = Trim(query.Search);

        var users = db.Users.AsNoTracking().Include(u => u.Role).AsQueryable();

        if (search is not null)
        {
            users = users.Where(u =>
                u.FullName.Contains(search) ||
                u.UserName.Contains(search) ||
                (u.Email != null && u.Email.Contains(search)) ||
                (u.MobileNumber != null && u.MobileNumber.Contains(search)));
        }

        if (query.RoleId is { } roleId)
        {
            users = users.Where(u => u.RoleId == roleId);
        }

        if (query.IsActive is { } isActive)
        {
            users = users.Where(u => u.IsActive == isActive);
        }

        var total = await users.CountAsync();

        var rows = await users
            .OrderBy(u => u.FullName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(u => new UserListItemDto
            {
                Id = u.Id,
                FullName = u.FullName,
                UserName = u.UserName,
                Email = u.Email,
                MobileNumber = u.MobileNumber,
                RoleId = u.RoleId,
                RoleCode = u.Role.Code,
                RoleName = u.Role.Name,
                IsActive = u.IsActive,
                CreatedAtUtc = u.CreatedAtUtc,
                LastLoginUtc = u.LastLoginUtc
            })
            .ToListAsync();

        var counts = await CountActivity(rows.Select(row => row.Id).ToArray());
        foreach (var row in rows)
        {
            row.ActivityCount = counts.GetValueOrDefault(row.Id);
        }

        return new PagedResult<UserListItemDto>(rows, total, page, pageSize);
    }

    public async Task<IReadOnlyList<RoleDto>> RolesAsync()
    {
        var roles = await db.Roles.AsNoTracking().OrderBy(r => r.Id).ToListAsync();
        var permissions = await db.RolePermissions.AsNoTracking().ToListAsync();

        return roles
            .Select(role => new RoleDto(
                role.Id,
                role.Code,
                role.Name,
                permissions.Where(p => p.RoleId == role.Id)
                    .Select(p => p.PermissionCode)
                    .OrderBy(code => code)
                    .ToArray()))
            .ToList();
    }

    public async Task<UserListItemDto> CreateAsync(UserCreateRequest request, SignedInUser actor)
    {
        var fullName = Require(request.FullName, "Full name is required.");
        var userName = Require(request.UserName, "Login name is required.").Replace(" ", "");
        var password = Require(request.Password, "A password is required.");

        ValidatePassword(password);
        await EnsureUserNameFree(userName);

        var role = await db.Roles.AsNoTracking().FirstOrDefaultAsync(r => r.Id == request.RoleId)
            ?? throw new NotFoundException($"Role {request.RoleId} was not found.");

        var user = new User
        {
            FullName = fullName,
            UserName = userName,
            Email = Trim(request.Email),
            MobileNumber = Trim(request.MobileNumber),
            RoleId = role.Id,
            IsActive = request.IsActive,
            CreatedAtUtc = DateTime.UtcNow
        };
        user.PasswordHash = passwordHasher.HashPassword(user, password);

        db.Users.Add(user);
        await db.SaveChangesAsync();

        await audit.WriteAsync(actor.UserId, actor.FullName, "User Create", "User",
            user.Id.ToString(), $"{user.UserName} · {role.Name}{(user.IsActive ? "" : " · created inactive")}");

        return await ShapeAsync(user, role);
    }

    public async Task<UserListItemDto> UpdateAsync(int id, UserUpdateRequest request, SignedInUser actor)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == id)
            ?? throw new NotFoundException("This user no longer exists.");

        var fullName = Require(request.FullName, "Full name is required.");
        var changingOwnAccount = user.Id == actor.UserId;

        if (changingOwnAccount)
        {
            if (request.RoleId != user.RoleId)
            {
                throw new DomainException("You cannot change your own role.");
            }

            if (!request.IsActive)
            {
                throw new DomainException("You cannot deactivate your own account.");
            }
        }

        var role = await db.Roles.AsNoTracking().FirstOrDefaultAsync(r => r.Id == request.RoleId)
            ?? throw new NotFoundException($"Role {request.RoleId} was not found.");

        if (!request.IsActive && await IsLastActiveSuperAdmin(user))
        {
            throw new DomainException("At least one active Super Admin must remain. Create another first.");
        }

        var previousRole = await db.Roles.AsNoTracking().Where(r => r.Id == user.RoleId).Select(r => r.Name).SingleAsync();
        var previousActive = user.IsActive;

        user.FullName = fullName;
        user.Email = Trim(request.Email);
        user.MobileNumber = Trim(request.MobileNumber);
        user.RoleId = role.Id;
        user.IsActive = request.IsActive;

        var notes = new List<string>();

        if (!string.IsNullOrWhiteSpace(request.NewPassword))
        {
            var newPassword = request.NewPassword.Trim();
            ValidatePassword(newPassword);
            user.PasswordHash = passwordHasher.HashPassword(user, newPassword);
            notes.Add("password reset");
        }

        if (previousRole != role.Name)
        {
            notes.Add($"role {previousRole} → {role.Name}");
        }

        if (previousActive != user.IsActive)
        {
            notes.Add(user.IsActive ? "reactivated" : "deactivated");
        }

        await db.SaveChangesAsync();

        await audit.WriteAsync(actor.UserId, actor.FullName, "User Update", "User",
            user.Id.ToString(), $"{user.UserName}{(notes.Count > 0 ? " · " + string.Join(" · ", notes) : "")}");

        return await ShapeAsync(user, role);
    }

    public async Task RemoveAsync(int id, SignedInUser actor)
    {
        var user = await db.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Id == id)
            ?? throw new NotFoundException("This user no longer exists.");

        if (user.Id == actor.UserId)
        {
            throw new DomainException("You cannot delete the account you are signed in with.");
        }

        if (await IsLastActiveSuperAdmin(user))
        {
            throw new DomainException("At least one active Super Admin must remain. Create another first.");
        }

        db.Users.Remove(user);
        await db.SaveChangesAsync();

        await audit.WriteAsync(actor.UserId, actor.FullName, "User Delete", "User",
            id.ToString(), $"{user.UserName} · {user.Role.Name} · their activity log rows are kept");
    }

    public async Task ChangeOwnPasswordAsync(SignedInUser actor, ChangePasswordRequest request)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == actor.UserId)
            ?? throw new NotFoundException("This user no longer exists.");

        var current = request.CurrentPassword ?? string.Empty;
        var replacement = (request.NewPassword ?? string.Empty).Trim();

        if (passwordHasher.VerifyHashedPassword(user, user.PasswordHash, current) == PasswordVerificationResult.Failed)
        {
            await audit.WriteAsync(user.Id, user.FullName, "Failed Password Change", "User", user.Id.ToString(), "current password did not match");
            throw new DomainException("The current password is not correct.");
        }

        ValidatePassword(replacement);

        if (replacement == current)
        {
            throw new DomainException("The new password must be different from the current one.");
        }

        user.PasswordHash = passwordHasher.HashPassword(user, replacement);
        await db.SaveChangesAsync();

        await audit.WriteAsync(user.Id, user.FullName, "Password Change", "User", user.Id.ToString(), "own password changed");
    }

    private async Task<Dictionary<int, int>> CountActivity(int[] userIds)
    {
        if (userIds.Length == 0)
        {
            return [];
        }

        return await db.AuditLogs.AsNoTracking()
            .Where(a => a.UserId != null && userIds.Contains(a.UserId.Value))
            .GroupBy(a => a.UserId!.Value)
            .Select(g => new { UserId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.UserId, x => x.Count);
    }

    private async Task<UserListItemDto> ShapeAsync(User user, Role role)
    {
        var counts = await CountActivity([user.Id]);

        return new UserListItemDto
        {
            Id = user.Id,
            FullName = user.FullName,
            UserName = user.UserName,
            Email = user.Email,
            MobileNumber = user.MobileNumber,
            RoleId = role.Id,
            RoleCode = role.Code,
            RoleName = role.Name,
            IsActive = user.IsActive,
            CreatedAtUtc = user.CreatedAtUtc,
            LastLoginUtc = user.LastLoginUtc,
            ActivityCount = counts.GetValueOrDefault(user.Id)
        };
    }

    private async Task<bool> IsLastActiveSuperAdmin(User user) =>
        user.RoleId == await db.Roles.Where(r => r.Code == RoleCodes.SuperAdmin).Select(r => r.Id).SingleAsync()
        && user.IsActive
        && await db.Users.CountAsync(u => u.RoleId == user.RoleId && u.IsActive) <= 1;

    private async Task EnsureUserNameFree(string userName)
    {
        var upper = userName.ToUpper();
        var taken = await db.Users.AnyAsync(u => u.UserName.ToUpper() == upper);

        if (taken)
        {
            throw new DomainException($"Login name \"{userName}\" is already used.");
        }
    }

    private static void ValidatePassword(string password)
    {
        if (password.Length < 8)
        {
            throw new DomainException("The password must be at least 8 characters.");
        }

        if (!password.Any(char.IsLetter) || !password.Any(char.IsDigit))
        {
            throw new DomainException("The password must contain at least one letter and one digit.");
        }
    }

    private static string? Trim(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string Require(string? value, string message)
    {
        var trimmed = (value ?? string.Empty).Trim();

        if (trimmed.Length == 0)
        {
            throw new DomainException(message);
        }

        return trimmed;
    }
}
