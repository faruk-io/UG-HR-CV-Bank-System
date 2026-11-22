using CvBank.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace CvBank.Api.Data;

public class CvBankDbContext : DbContext
{
    public CvBankDbContext(DbContextOptions<CvBankDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<Candidate> Candidates => Set<Candidate>();
    public DbSet<CandidateDocument> CandidateDocuments => Set<CandidateDocument>();
    public DbSet<Concern> Concerns => Set<Concern>();
    public DbSet<Designation> Designations => Set<Designation>();
    public DbSet<JobPosition> JobPositions => Set<JobPosition>();
    public DbSet<InterviewEvaluation> InterviewEvaluations => Set<InterviewEvaluation>();
    public DbSet<EvaluationRating> EvaluationRatings => Set<EvaluationRating>();
    public DbSet<EvaluationDocumentItem> EvaluationDocumentItems => Set<EvaluationDocumentItem>();
    public DbSet<InterviewReference> InterviewReferences => Set<InterviewReference>();
    public DbSet<EvaluatorDetail> EvaluatorDetails => Set<EvaluatorDetail>();
    public DbSet<InterviewResult> InterviewResults => Set<InterviewResult>();
    public DbSet<InterviewApproval> InterviewApprovals => Set<InterviewApproval>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<SystemSetting> SystemSettings => Set<SystemSetting>();
    public DbSet<JoiningRecord> JoiningRecords => Set<JoiningRecord>();
    public DbSet<ResignedRecord> ResignedRecords => Set<ResignedRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(e =>
        {
            e.Property(x => x.FullName).HasMaxLength(200);
            e.Property(x => x.UserName).HasMaxLength(100);
            e.Property(x => x.Email).HasMaxLength(200);
            e.Property(x => x.MobileNumber).HasMaxLength(30);
            e.HasIndex(x => x.UserName).IsUnique();
            e.HasIndex(x => x.MobileNumber);
            e.HasOne(x => x.Role).WithMany(r => r.Users).HasForeignKey(x => x.RoleId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Role>(e =>
        {
            e.Property(x => x.Code).HasMaxLength(50);
            e.Property(x => x.Name).HasMaxLength(100);
            e.HasIndex(x => x.Code).IsUnique();
        });

        modelBuilder.Entity<RolePermission>(e =>
        {
            e.Property(x => x.PermissionCode).HasMaxLength(100);
            e.HasIndex(x => new { x.RoleId, x.PermissionCode }).IsUnique();
            e.HasOne(x => x.Role).WithMany(r => r.Permissions).HasForeignKey(x => x.RoleId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Candidate>(e =>
        {
            e.Property(x => x.CandidateCode).HasMaxLength(20);
            e.Property(x => x.Name).HasMaxLength(200);
            e.Property(x => x.MobileNumber).HasMaxLength(30);
            e.Property(x => x.Email).HasMaxLength(200);
            e.Property(x => x.PresentAddress).HasMaxLength(500);
            e.Property(x => x.CurrentOrganization).HasMaxLength(200);
            e.Property(x => x.CurrentDesignation).HasMaxLength(200);
            e.Property(x => x.DurationInCurrentOrganization).HasMaxLength(100);
            e.Property(x => x.NoticePeriod).HasMaxLength(100);
            e.Property(x => x.Remarks).HasMaxLength(1000);
            e.Property(x => x.PresentSalary).HasPrecision(18, 2);
            e.Property(x => x.ExpectedSalary).HasPrecision(18, 2);
            e.Property(x => x.TotalYearsOfExperience).HasPrecision(5, 2);
            e.HasIndex(x => x.CandidateCode).IsUnique();
            e.HasIndex(x => x.Name);
            e.HasIndex(x => x.MobileNumber);
            e.HasIndex(x => x.Email);
            e.HasIndex(x => x.Status);
            e.HasOne(x => x.AppliedJobPosition).WithMany(p => p.Candidates).HasForeignKey(x => x.AppliedJobPositionId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(x => x.Concern).WithMany().HasForeignKey(x => x.ConcernId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(x => x.CreatedBy).WithMany().HasForeignKey(x => x.CreatedById).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<CandidateDocument>(e =>
        {
            e.Property(x => x.FileName).HasMaxLength(300);
            e.Property(x => x.StoredFileName).HasMaxLength(300);
            e.Property(x => x.ContentType).HasMaxLength(150);
            e.Property(x => x.Remarks).HasMaxLength(500);
            e.HasIndex(x => new { x.CandidateId, x.Category, x.IsCurrent });
            e.HasOne(x => x.Candidate).WithMany(c => c.Documents).HasForeignKey(x => x.CandidateId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.InterviewEvaluation).WithMany().HasForeignKey(x => x.InterviewEvaluationId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.UploadedBy).WithMany().HasForeignKey(x => x.UploadedById).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<Concern>(e => e.Property(x => x.Name).HasMaxLength(200));
        modelBuilder.Entity<Designation>(e => e.Property(x => x.Name).HasMaxLength(200));
        modelBuilder.Entity<JobPosition>(e => e.Property(x => x.Title).HasMaxLength(200));

        modelBuilder.Entity<SystemSetting>(e =>
        {
            e.Property(x => x.Key).HasMaxLength(150);
            e.Property(x => x.Value).HasMaxLength(2000);
            e.Property(x => x.Description).HasMaxLength(500);
            e.HasIndex(x => x.Key).IsUnique();
            e.HasOne(x => x.UpdatedBy).WithMany().HasForeignKey(x => x.UpdatedById).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<AuditLog>(e =>
        {
            e.Property(x => x.Action).HasMaxLength(150);
            e.Property(x => x.EntityType).HasMaxLength(100);
            e.Property(x => x.EntityId).HasMaxLength(50);
            e.Property(x => x.UserDisplayName).HasMaxLength(200);
            e.HasIndex(x => x.OccurredAtUtc);
            e.HasIndex(x => new { x.EntityType, x.EntityId });
            e.HasOne(x => x.User).WithMany(u => u.AuditLogs).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<InterviewEvaluation>(e =>
        {
            e.HasIndex(x => new { x.CandidateId, x.InterviewNumber }).IsUnique();
            e.HasIndex(x => x.InterviewDate);
            e.HasOne(x => x.Candidate).WithMany(c => c.Evaluations).HasForeignKey(x => x.CandidateId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.AppliedJobPosition).WithMany().HasForeignKey(x => x.AppliedJobPositionId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(x => x.Concern).WithMany().HasForeignKey(x => x.ConcernId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(x => x.CreatedBy).WithMany().HasForeignKey(x => x.CreatedById).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<EvaluationRating>(e =>
        {
            e.Property(x => x.DimensionName).HasMaxLength(150);
            e.HasIndex(x => new { x.InterviewEvaluationId, x.DimensionCode }).IsUnique();
            e.HasOne(x => x.InterviewEvaluation).WithMany(v => v.Ratings).HasForeignKey(x => x.InterviewEvaluationId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<EvaluationDocumentItem>(e =>
        {
            e.Property(x => x.ItemName).HasMaxLength(200);
            e.Property(x => x.Details).HasMaxLength(500);
            e.HasIndex(x => new { x.InterviewEvaluationId, x.ItemCode }).IsUnique();
            e.HasOne(x => x.InterviewEvaluation).WithMany(v => v.DocumentItems).HasForeignKey(x => x.InterviewEvaluationId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<InterviewReference>(e =>
        {
            e.Property(x => x.ReferenceName).HasMaxLength(200);
            e.Property(x => x.Designation).HasMaxLength(200);
            e.Property(x => x.Relationship).HasMaxLength(100);
            e.Property(x => x.CompanyAndLocation).HasMaxLength(300);
            e.Property(x => x.Phone).HasMaxLength(50);
            e.HasIndex(x => new { x.InterviewEvaluationId, x.ReferenceNumber }).IsUnique();
            e.HasOne(x => x.InterviewEvaluation).WithMany(v => v.References).HasForeignKey(x => x.InterviewEvaluationId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<EvaluatorDetail>(e =>
        {
            e.Property(x => x.EvaluatorName).HasMaxLength(200);
            e.Property(x => x.Designation).HasMaxLength(200);
            e.Property(x => x.Department).HasMaxLength(200);
            e.Property(x => x.SignatureText).HasMaxLength(200);
            e.HasIndex(x => new { x.InterviewEvaluationId, x.SectionNumber }).IsUnique();
            e.HasOne(x => x.InterviewEvaluation).WithMany(v => v.Evaluators).HasForeignKey(x => x.InterviewEvaluationId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<InterviewResult>(e =>
        {
            e.Property(x => x.Recommendation).HasMaxLength(1000);
            e.Property(x => x.NoticePeriod).HasMaxLength(100);
            e.Property(x => x.Grade).HasMaxLength(50);
            e.Property(x => x.Remarks).HasMaxLength(1000);
            e.Property(x => x.ExpectedSalary).HasPrecision(18, 2);
            e.Property(x => x.SalaryFixed).HasPrecision(18, 2);
            e.HasIndex(x => x.InterviewEvaluationId).IsUnique();
            e.HasIndex(x => x.Decision);
            e.HasOne(x => x.InterviewEvaluation).WithOne(v => v.Result).HasForeignKey<InterviewResult>(x => x.InterviewEvaluationId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.DesignationFixed).WithMany().HasForeignKey(x => x.DesignationFixedId).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<InterviewApproval>(e =>
        {
            e.Property(x => x.AuthorityTitle).HasMaxLength(150);
            e.Property(x => x.Decision).HasMaxLength(50);
            e.Property(x => x.SignatureText).HasMaxLength(200);
            e.HasIndex(x => new { x.InterviewEvaluationId, x.AuthorityNumber }).IsUnique();
            e.HasOne(x => x.InterviewEvaluation).WithMany(v => v.Approvals).HasForeignKey(x => x.InterviewEvaluationId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<JoiningRecord>(e =>
        {
            e.Property(x => x.EmployeeId).HasMaxLength(50);
            e.Property(x => x.Name).HasMaxLength(200);
            e.Property(x => x.Designation).HasMaxLength(200);
            e.Property(x => x.Department).HasMaxLength(200);
            e.Property(x => x.ProbationPeriod).HasMaxLength(100);
            e.Property(x => x.Remarks).HasMaxLength(1000);
            e.Property(x => x.Salary).HasPrecision(18, 2);
            e.Property(x => x.AfterProbationSalary).HasPrecision(18, 2);
            e.Property(x => x.EffectiveSalary).HasMaxLength(50);
            //e.Property(x => x.EffectiveSalary).HasPrecision(18, 2);
            e.HasIndex(x => x.EmployeeId);
            e.HasIndex(x => x.Name);
            e.HasIndex(x => x.DateOfJoining);
            e.HasOne(x => x.Concern).WithMany().HasForeignKey(x => x.ConcernId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(x => x.CreatedBy).WithMany().HasForeignKey(x => x.CreatedById).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<ResignedRecord>(e =>
        {
            e.Property(x => x.EmployeeId).HasMaxLength(50);
            e.Property(x => x.Name).HasMaxLength(200);
            e.Property(x => x.Designation).HasMaxLength(200);
            e.Property(x => x.Department).HasMaxLength(200);
            e.Property(x => x.NoticePeriod).HasMaxLength(100);
            e.Property(x => x.CauseOfResignation).HasMaxLength(500);
            e.Property(x => x.Remarks).HasMaxLength(1000);
            e.Property(x => x.Salary).HasPrecision(18, 2);
            e.HasIndex(x => x.EmployeeId);
            e.HasIndex(x => x.Name);
            e.HasIndex(x => x.ResignDate);
            e.HasOne(x => x.Concern).WithMany().HasForeignKey(x => x.ConcernId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(x => x.CreatedBy).WithMany().HasForeignKey(x => x.CreatedById).OnDelete(DeleteBehavior.SetNull);
        });
    }
}
