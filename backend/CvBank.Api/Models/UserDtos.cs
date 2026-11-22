using System.ComponentModel.DataAnnotations;

namespace CvBank.Api.Models;

public sealed class UserQuery
{
    public string? Search { get; set; }
    public int? RoleId { get; set; }
    public bool? IsActive { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public sealed class UserListItemDto
{
    public int Id { get; set; }
    public string FullName { get; set; } = null!;
    public string UserName { get; set; } = null!;
    public string? Email { get; set; }
    public string? MobileNumber { get; set; }
    public int RoleId { get; set; }
    public string RoleCode { get; set; } = null!;
    public string RoleName { get; set; } = null!;
    public bool IsActive { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? LastLoginUtc { get; set; }
    public int ActivityCount { get; set; }
}

public class UserCreateRequest
{
    [Required][StringLength(200)] public string FullName { get; set; } = null!;
    [Required][StringLength(100)] public string UserName { get; set; } = null!;
    [EmailAddress][StringLength(200)] public string? Email { get; set; }
    [StringLength(30)] public string? MobileNumber { get; set; }
    [Range(1, 1000)] public int RoleId { get; set; }
    [Required] public string Password { get; set; } = null!;
    public bool IsActive { get; set; } = true;
}

public class UserUpdateRequest
{
    [Required][StringLength(200)] public string FullName { get; set; } = null!;
    [EmailAddress][StringLength(200)] public string? Email { get; set; }
    [StringLength(30)] public string? MobileNumber { get; set; }
    [Range(1, 1000)] public int RoleId { get; set; }
    public bool IsActive { get; set; } = true;

    /// Optional: set only when this save also replaces the password.
    public string? NewPassword { get; set; }
}

public record ChangePasswordRequest([Required] string CurrentPassword, [Required] string NewPassword);

public sealed record RoleDto(int Id, string Code, string Name, IReadOnlyList<string> Permissions);
