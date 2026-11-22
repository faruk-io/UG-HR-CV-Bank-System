using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace CvBank.Api.Auth;

public static class AuthClaims
{
    public const string UserId = "cvb_uid";
    public const string UserName = "cvb_uname";
    public const string FullName = "cvb_name";
    public const string Role = "cvb_role";
    public const string Permission = "cvb_perm";
    public const string PolicyPrefix = "perm:";
}

public class PermissionRequirement : IAuthorizationRequirement
{
    public PermissionRequirement(string code) => Code = code;

    public string Code { get; }
}

public class PermissionHandler : AuthorizationHandler<PermissionRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
    {
        if (context.User.FindAll(AuthClaims.Permission).Any(c => c.Value == requirement.Code))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}

public class PermissionPolicyProvider : IAuthorizationPolicyProvider
{
    private readonly AuthorizationPolicy _defaultPolicy =
        new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build();

    public Task<AuthorizationPolicy> GetDefaultPolicyAsync() => Task.FromResult(_defaultPolicy);

    public Task<AuthorizationPolicy?> GetFallbackPolicyAsync() => Task.FromResult<AuthorizationPolicy?>(null);

    public Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        if (!policyName.StartsWith(AuthClaims.PolicyPrefix, StringComparison.Ordinal))
        {
            return Task.FromResult<AuthorizationPolicy?>(null);
        }

        var code = policyName[AuthClaims.PolicyPrefix.Length..];
        var policy = new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .AddRequirements(new PermissionRequirement(code))
            .Build();

        return Task.FromResult<AuthorizationPolicy?>(policy);
    }
}

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public class PermissionAttribute : AuthorizeAttribute
{
    public PermissionAttribute(string code)
    {
        Policy = AuthClaims.PolicyPrefix + code;
    }
}

public sealed record SignedInUser(int UserId, string UserName, string FullName)
{
    public static SignedInUser From(ClaimsPrincipal user) => new(
        int.Parse(user.FindFirstValue(AuthClaims.UserId) ?? "0"),
        user.FindFirstValue(AuthClaims.UserName) ?? string.Empty,
        user.FindFirstValue(AuthClaims.FullName) ?? string.Empty);
}
