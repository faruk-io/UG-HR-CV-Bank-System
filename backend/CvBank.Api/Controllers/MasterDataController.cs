using CvBank.Api.Auth;
using CvBank.Api.Data;
using CvBank.Api.Models;
using CvBank.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CvBank.Api.Controllers;

[ApiController]
[Route("api/master-data")]
public class MasterDataController(CvBankDbContext db, IAuditWriter audit) : ControllerBase
{
    private SignedInUser Me => SignedInUser.From(User);

    [HttpGet("concerns")]
    [Permission(Permissions.CandidateView)]
    public async Task<ActionResult<IReadOnlyList<MasterDataOption>>> Concerns()
    {
        return Ok(await db.Concerns.Where(x => x.IsActive)
            .OrderBy(x => x.Name)
            .Select(x => new MasterDataOption(x.Id, x.Name))
            .ToListAsync());
    }

    [HttpPost("concerns")]
    [Authorize]
    public async Task<ActionResult<MasterDataOption>> CreateConcern(MasterDataCreateRequest request)
    {
        var name = RequireName(request.Name);

        if (await db.Concerns.AnyAsync(x => x.Name == name))
        {
            throw new DomainException($"Concern \"{name}\" already exists.");
        }

        var entity = new Concern { Name = name };
        db.Concerns.Add(entity);
        await db.SaveChangesAsync();

        await audit.WriteAsync(Me.UserId, Me.FullName, "Concern Create", "Concern", entity.Id.ToString(), name);

        return Ok(new MasterDataOption(entity.Id, entity.Name));
    }

    [HttpGet("designations")]
    [Permission(Permissions.CandidateView)]
    public async Task<ActionResult<IReadOnlyList<MasterDataOption>>> Designations()
    {
        return Ok(await db.Designations.Where(x => x.IsActive)
            .OrderBy(x => x.Name)
            .Select(x => new MasterDataOption(x.Id, x.Name))
            .ToListAsync());
    }

    [HttpPost("designations")]
    [Permission(Permissions.SettingsManage)]
    public async Task<ActionResult<MasterDataOption>> CreateDesignation(MasterDataCreateRequest request)
    {
        var name = RequireName(request.Name);

        if (await db.Designations.AnyAsync(x => x.Name == name))
        {
            throw new DomainException($"Designation \"{name}\" already exists.");
        }

        var entity = new Designation { Name = name };
        db.Designations.Add(entity);
        await db.SaveChangesAsync();

        await audit.WriteAsync(Me.UserId, Me.FullName, "Designation Create", "Designation", entity.Id.ToString(), name);

        return Ok(new MasterDataOption(entity.Id, entity.Name));
    }

    [HttpGet("job-positions")]
    [Permission(Permissions.CandidateView)]
    public async Task<ActionResult<IReadOnlyList<MasterDataOption>>> JobPositions()
    {
        return Ok(await db.JobPositions.Where(x => x.IsActive)
            .OrderBy(x => x.Title)
            .Select(x => new MasterDataOption(x.Id, x.Title))
            .ToListAsync());
    }

    [HttpPost("job-positions")]
    [Authorize]
    public async Task<ActionResult<MasterDataOption>> CreateJobPosition(MasterDataCreateRequest request)
    {
        var name = RequireName(request.Name);

        if (await db.JobPositions.AnyAsync(x => x.Title == name))
        {
            throw new DomainException($"Position \"{name}\" already exists.");
        }

        var entity = new JobPosition { Title = name };
        db.JobPositions.Add(entity);
        await db.SaveChangesAsync();

        await audit.WriteAsync(Me.UserId, Me.FullName, "Job Position Create", "JobPosition", entity.Id.ToString(), name);

        return Ok(new MasterDataOption(entity.Id, entity.Title));
    }

    [HttpGet("manage")]
    [Permission(Permissions.SettingsManage)]
    public async Task<ActionResult<MasterDataManageView>> Manage()
    {
        return Ok(await BuildViewAsync());
    }

    [HttpPut("manage/{kind}/{id:int}")]
    [Permission(Permissions.SettingsManage)]
    public async Task<ActionResult<MasterDataManageView>> Rename(string kind, int id, MasterDataRenameRequest request)
    {
        var list = KindOf(kind);
        var name = RequireName(request.Name);
        var row = await FindRowAsync(list, id);

        if (await NameTakenAsync(list, name, id))
        {
            throw new DomainException($"{LabelOf(list)} \"{name}\" already exists.");
        }

        row.SetName(name);
        await db.SaveChangesAsync();
        await AuditAsync(list, "Rename", id, $"\"{row.Name}\" to \"{name}\"");

        return Ok(await BuildViewAsync());
    }

    [HttpPut("manage/{kind}/{id:int}/active")]
    [Permission(Permissions.SettingsManage)]
    public async Task<ActionResult<MasterDataManageView>> SetActive(string kind, int id, MasterDataActiveRequest request)
    {
        var list = KindOf(kind);
        var row = await FindRowAsync(list, id);

        if (row.IsActive == request.IsActive)
        {
            return Ok(await BuildViewAsync());
        }

        row.SetActive(request.IsActive);
        await db.SaveChangesAsync();
        await AuditAsync(
            list,
            request.IsActive ? "Activate" : "Deactivate",
            id,
            $"\"{row.Name}\" · used by {row.UsedByRecords} record(s)");

        return Ok(await BuildViewAsync());
    }

    private enum MasterDataKind
    {
        Concern,
        Designation,
        JobPosition
    }

    private sealed record EditableRow(
        int Id,
        string Name,
        bool IsActive,
        int UsedByRecords,
        Action<string> SetName,
        Action<bool> SetActive);

    private static MasterDataKind KindOf(string kind) => kind.ToLowerInvariant() switch
    {
        "concerns" => MasterDataKind.Concern,
        "designations" => MasterDataKind.Designation,
        "job-positions" => MasterDataKind.JobPosition,
        _ => throw new DomainException($"Unknown master data list \"{kind}\".")
    };

    private static string LabelOf(MasterDataKind kind) => kind switch
    {
        MasterDataKind.Concern => "Concern",
        MasterDataKind.Designation => "Designation",
        _ => "Job Position"
    };

    private async Task<EditableRow> FindRowAsync(MasterDataKind kind, int id)
    {
        switch (kind)
        {
            case MasterDataKind.Concern:
            {
                var entity = await db.Concerns.FirstOrDefaultAsync(x => x.Id == id)
                    ?? throw new NotFoundException("Concern not found.");
                return new EditableRow(
                    entity.Id,
                    entity.Name,
                    entity.IsActive,
                    await UsageAsync(kind, id),
                    name => entity.Name = name,
                    on => entity.IsActive = on);
            }
            case MasterDataKind.Designation:
            {
                var entity = await db.Designations.FirstOrDefaultAsync(x => x.Id == id)
                    ?? throw new NotFoundException("Designation not found.");
                return new EditableRow(
                    entity.Id,
                    entity.Name,
                    entity.IsActive,
                    await UsageAsync(kind, id),
                    name => entity.Name = name,
                    on => entity.IsActive = on);
            }
            default:
            {
                var entity = await db.JobPositions.FirstOrDefaultAsync(x => x.Id == id)
                    ?? throw new NotFoundException("Job position not found.");
                return new EditableRow(
                    entity.Id,
                    entity.Title,
                    entity.IsActive,
                    await UsageAsync(kind, id),
                    name => entity.Title = name,
                    on => entity.IsActive = on);
            }
        }
    }

    private async Task<bool> NameTakenAsync(MasterDataKind kind, string name, int id) => kind switch
    {
        MasterDataKind.Concern => await db.Concerns.AnyAsync(x => x.Name == name && x.Id != id),
        MasterDataKind.Designation => await db.Designations.AnyAsync(x => x.Name == name && x.Id != id),
        _ => await db.JobPositions.AnyAsync(x => x.Title == name && x.Id != id)
    };

    private async Task<int> UsageAsync(MasterDataKind kind, int id) => kind switch
    {
        MasterDataKind.Concern =>
            await db.Candidates.CountAsync(c => c.ConcernId == id) +
            await db.InterviewEvaluations.CountAsync(e => e.ConcernId == id),
        MasterDataKind.Designation => await db.InterviewResults.CountAsync(r => r.DesignationFixedId == id),
        _ =>
            await db.Candidates.CountAsync(c => c.AppliedJobPositionId == id) +
            await db.InterviewEvaluations.CountAsync(e => e.AppliedJobPositionId == id)
    };

    private static string EntityOf(MasterDataKind kind) => kind switch
    {
        MasterDataKind.Concern => "Concern",
        MasterDataKind.Designation => "Designation",
        _ => "JobPosition"
    };

    private async Task AuditAsync(MasterDataKind kind, string verb, int id, string details)
    {
        await audit.WriteAsync(Me.UserId, Me.FullName, $"{LabelOf(kind)} {verb}", EntityOf(kind), id.ToString(), details);
    }

    private async Task<MasterDataManageView> BuildViewAsync()
    {
        var concerns = await db.Concerns
            .OrderBy(x => x.Name)
            .Select(x => new MasterDataItem(
                x.Id,
                x.Name,
                x.IsActive,
                db.Candidates.Count(c => c.ConcernId == x.Id) + db.InterviewEvaluations.Count(e => e.ConcernId == x.Id)))
            .ToListAsync();

        var designations = await db.Designations
            .OrderBy(x => x.Name)
            .Select(x => new MasterDataItem(
                x.Id,
                x.Name,
                x.IsActive,
                db.InterviewResults.Count(r => r.DesignationFixedId == x.Id)))
            .ToListAsync();

        var jobPositions = await db.JobPositions
            .OrderBy(x => x.Title)
            .Select(x => new MasterDataItem(
                x.Id,
                x.Title,
                x.IsActive,
                db.Candidates.Count(c => c.AppliedJobPositionId == x.Id) +
                db.InterviewEvaluations.Count(e => e.AppliedJobPositionId == x.Id)))
            .ToListAsync();

        return new MasterDataManageView(concerns, designations, jobPositions);
    }

    private static string RequireName(string value)
    {
        var name = (value ?? string.Empty).Trim();

        if (name.Length == 0)
        {
            throw new DomainException("A name is required.");
        }

        return name.Length <= 200 ? name : name[..200];
    }
}
