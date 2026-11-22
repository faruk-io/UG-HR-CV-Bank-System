namespace CvBank.Api.Models;

public class User
{
    public int Id { get; set; }
    public string FullName { get; set; } = null!;
    public string UserName { get; set; } = null!;
    public string? Email { get; set; }
    public string? MobileNumber { get; set; }
    public string PasswordHash { get; set; } = null!;
    public int RoleId { get; set; }
    public Role Role { get; set; } = null!;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? LastLoginUtc { get; set; }

    public ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();
}

public class Role
{
    public int Id { get; set; }
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;

    public ICollection<RolePermission> Permissions { get; set; } = new List<RolePermission>();
    public ICollection<User> Users { get; set; } = new List<User>();
}

public class RolePermission
{
    public int Id { get; set; }
    public int RoleId { get; set; }
    public Role Role { get; set; } = null!;
    public string PermissionCode { get; set; } = null!;
}

public static class RoleCodes
{
    public const string Executive = "Executive";
    public const string Manager = "Manager";
    public const string SuperAdmin = "SuperAdmin";
}

public static class Permissions
{
    public const string CandidateView = "candidate.view";
    public const string CandidateCreate = "candidate.create";
    public const string CandidateUpdate = "candidate.update";
    public const string CandidateDelete = "candidate.delete";
    public const string CvUpload = "cv.upload";
    public const string CvDownload = "cv.download";
    public const string EvaluationView = "evaluation.view";
    public const string EvaluationGenerate = "evaluation.generate";
    public const string EvaluationUpload = "evaluation.upload";
    public const string EvaluationManage = "evaluation.manage";
    public const string ReportView = "report.view";
    public const string UserManage = "user.manage";
    public const string SettingsManage = "settings.manage";
    public const string ActivityLogView = "activity.log.view";
    public const string DocumentManage = "document.manage";
}
