namespace CvBank.Api.Models;

public class Concern
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public bool IsActive { get; set; } = true;
}

public class Designation
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public bool IsActive { get; set; } = true;
}

public class JobPosition
{
    public int Id { get; set; }
    public string Title { get; set; } = null!;
    public bool IsActive { get; set; } = true;

    public ICollection<Candidate> Candidates { get; set; } = new List<Candidate>();
}

public class SystemSetting
{
    public int Id { get; set; }
    public string Key { get; set; } = null!;
    public string? Value { get; set; }
    public string? Description { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    public int? UpdatedById { get; set; }
    public User? UpdatedBy { get; set; }
}

public class AuditLog
{
    public long Id { get; set; }
    public int? UserId { get; set; }
    public User? User { get; set; }
    public string? UserDisplayName { get; set; }
    public string Action { get; set; } = null!;
    public string? EntityType { get; set; }
    public string? EntityId { get; set; }
    public string? Details { get; set; }
    public DateTime OccurredAtUtc { get; set; }
}
