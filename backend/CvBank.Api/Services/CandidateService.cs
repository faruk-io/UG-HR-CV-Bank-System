using CvBank.Api.Auth;
using CvBank.Api.Data;
using CvBank.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace CvBank.Api.Services;

public class DomainException(string message) : Exception(message);

public sealed class NotFoundException(string message) : DomainException(message);

public sealed class DuplicateCandidateException(string message) : DomainException(message);

public class CandidateService(CvBankDbContext db, IFileStorage storage)
{
    public async Task<PagedResult<CandidateListItemDto>> ListAsync(CandidateQuery query)
    {
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 5, 100);
        var search = Trim(query.Search);

        var candidates = db.Candidates.AsNoTracking().AsQueryable();

        if (search is not null)
        {
            var like = search.ToLower();
            candidates = candidates.Where(c =>
                c.Name.ToLower().Contains(like) ||
                (c.CandidateCode != null && c.CandidateCode.ToLower().Contains(like)) ||
                (c.MobileNumber != null && c.MobileNumber.ToLower().Contains(like)) ||
                (c.Email != null && c.Email.ToLower().Contains(like)) ||
                (c.CurrentOrganization != null && c.CurrentOrganization.ToLower().Contains(like)) ||
                (c.CurrentDesignation != null && c.CurrentDesignation.ToLower().Contains(like)));
        }

        if (query.Status is { } status)
        {
            candidates = candidates.Where(c => c.Status == status);
        }

        if (query.JobPositionId is { } jobId)
        {
            candidates = candidates.Where(c => c.AppliedJobPositionId == jobId);
        }

        if (query.ConcernId is { } concernId)
        {
            candidates = candidates.Where(c => c.ConcernId == concernId);
        }

        if (query.MinExperience is { } minExperience)
        {
            candidates = candidates.Where(c => c.TotalYearsOfExperience >= minExperience);
        }

        if (query.MaxExperience is { } maxExperience)
        {
            candidates = candidates.Where(c => c.TotalYearsOfExperience <= maxExperience);
        }

        if (query.HasCv is { } hasCv)
        {
            candidates = candidates.Where(c => c.Documents.Any(d => d.Category == DocumentCategory.Cv && d.IsCurrent) == hasCv);
        }

        var total = await candidates.CountAsync();

        var items = await candidates
            .OrderByDescending(c => c.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new CandidateListItemDto(
                c.Id,
                c.CandidateCode,
                c.Name,
                c.MobileNumber,
                c.Email,
                c.CurrentOrganization,
                c.CurrentDesignation,
                c.AppliedJobPosition!.Title,
                c.Concern!.Name,
                c.TotalYearsOfExperience,
                c.Status,
                c.Documents.Any(d => d.Category == DocumentCategory.Cv && d.IsCurrent),
                c.CreatedAtUtc))
            .ToListAsync();

        return new PagedResult<CandidateListItemDto>(items, total, page, pageSize);
    }

    public async Task<CandidateDto> GetAsync(int id)
    {
        var candidate = await db.Candidates.AsNoTracking()
            .Include(c => c.CreatedBy)
            .Include(c => c.AppliedJobPosition)
            .Include(c => c.Concern)
            .Include(c => c.Evaluations)
            .Include(c => c.Documents.OrderByDescending(d => d.UploadedAtUtc))
            .ThenInclude(d => d.UploadedBy)
            .FirstOrDefaultAsync(c => c.Id == id);

        return candidate is null
            ? throw new NotFoundException($"Candidate {id} was not found.")
            : ToDto(candidate);
    }

    public async Task<IReadOnlyList<DuplicateCandidateDto>> FindDuplicatesAsync(string? mobileNumber, string? email, int? excludeId)
    {
        var mobile = NormalizeContact(mobileNumber);
        var mail = NormalizeContact(email);

        if (mobile is null && mail is null)
        {
            return [];
        }

        var rows = await db.Candidates.AsNoTracking()
            .Where(c => c.Id != excludeId)
            .Where(c =>
                (mobile != null && c.MobileNumber != null && c.MobileNumber.Replace(" ", "").Replace("-", "") == mobile) ||
                (mail != null && c.Email != null && c.Email.ToLower() == mail))
            .OrderBy(c => c.Name)
            .Select(c => new
            {
                c.Id,
                c.CandidateCode,
                c.Name,
                c.MobileNumber,
                c.Email,
                c.Status
            })
            .ToListAsync();

        return rows
            .Select(r => new DuplicateCandidateDto(
                r.Id,
                r.CandidateCode,
                r.Name,
                r.MobileNumber,
                r.Email,
                r.Status,
                string.Join(" + ", new[]
                {
                    mobile is not null && SameMobile(mobile, r.MobileNumber) ? "mobile" : null,
                    mail is not null && string.Equals(mail, r.Email?.Trim().ToLower(), StringComparison.Ordinal) ? "email" : null
                }.Where(x => x is not null))))
            .ToList();
    }

    public async Task<CandidateDto> CreateAsync(CandidateSaveRequest request, SignedInUser user)
    {
        var duplicates = await FindDuplicatesAsync(request.MobileNumber, request.Email, null);

        if (duplicates.Count > 0 && !request.AcceptPossibleDuplicate)
        {
            throw new DuplicateCandidateException(
                $"{duplicates.Count} existing candidate(s) match this mobile number or email. Open the existing record, or confirm to save anyway.");
        }

        ValidateDates(request);
        await EnsureMasterDataLinksAsync(request);

        var year = DateTime.UtcNow.Year;

        for (var attempt = 0; attempt < 5; attempt++)
        {
            var candidate = new Candidate
            {
                CandidateCode = await NextCandidateCodeAsync(year),
                CreatedAtUtc = DateTime.UtcNow,
                CreatedById = user.UserId
            };

            Apply(candidate, request, isNew: true);
            db.Candidates.Add(candidate);

            try
            {
                await db.SaveChangesAsync();
                return await GetAsync(candidate.Id);
            }
            catch (DbUpdateException ex) when (IsUniqueCodeViolation(ex))
            {
                db.Entry(candidate).State = EntityState.Detached;
            }
        }

        throw new DomainException("Could not generate a unique candidate code. Please try again.");
    }

    public async Task<CandidateDto> UpdateAsync(int id, CandidateSaveRequest request, SignedInUser user)
    {
        var candidate = await db.Candidates.FirstOrDefaultAsync(c => c.Id == id)
            ?? throw new NotFoundException($"Candidate {id} was not found.");

        var duplicates = await FindDuplicatesAsync(request.MobileNumber, request.Email, id);

        if (duplicates.Count > 0 && !request.AcceptPossibleDuplicate)
        {
            throw new DuplicateCandidateException(
                $"{duplicates.Count} other candidate(s) match this mobile number or email. Confirm to save anyway.");
        }

        ValidateDates(request);
        await EnsureMasterDataLinksAsync(request);

        Apply(candidate, request, isNew: false);
        candidate.UpdatedAtUtc = DateTime.UtcNow;

        await db.SaveChangesAsync();
        _ = user;

        return await GetAsync(candidate.Id);
    }

    public async Task<CandidateDto> SetStatusAsync(int id, CandidateStatus status, SignedInUser user)
    {
        var candidate = await db.Candidates.FirstOrDefaultAsync(c => c.Id == id)
            ?? throw new NotFoundException($"Candidate {id} was not found.");

        candidate.Status = status;
        candidate.UpdatedAtUtc = DateTime.UtcNow;

        await db.SaveChangesAsync();
        _ = user;

        return await GetAsync(id);
    }

    public async Task DeleteAsync(int id)
    {
        var candidate = await db.Candidates
            .Include(c => c.Documents)
            .Include(c => c.Evaluations)
            .FirstOrDefaultAsync(c => c.Id == id)
            ?? throw new NotFoundException($"Candidate {id} was not found.");

        if (candidate.Evaluations.Count > 0)
        {
            throw new DomainException(
                "This candidate has interview evaluation records. Delete the evaluations first, or keep the record in the archive.");
        }

        var paths = candidate.Documents.Select(d => d.StoredFileName).ToList();

        db.Candidates.Remove(candidate);
        await db.SaveChangesAsync();

        foreach (var path in paths)
        {
            storage.TryDeletePhysical(path);
        }
    }

    public async Task<CandidateDocumentDto> AddDocumentAsync(
        int candidateId,
        IFormFile file,
        DocumentCategory category,
        string? remarks,
        bool replaceCurrent,
        SignedInUser user)
    {
        var candidate = await db.Candidates.FirstOrDefaultAsync(c => c.Id == candidateId)
            ?? throw new NotFoundException($"Candidate {candidateId} was not found.");

        storage.Validate(file.FileName, file.ContentType, file.Length, category);

        if (category == DocumentCategory.Cv && !replaceCurrent)
        {
            var existingCurrent = await db.CandidateDocuments.AnyAsync(d =>
                d.CandidateId == candidateId && d.Category == DocumentCategory.Cv && d.IsCurrent);

            if (existingCurrent)
            {
                throw new DomainException("This candidate already has a current CV. Tick \"replace the current CV\" to upload a new version.");
            }
        }

        StoredUpload stored;
        await using (var stream = file.OpenReadStream())
        {
            stored = storage.Save(stream, file.FileName, file.ContentType, category);
        }

        if (replaceCurrent)
        {
            var previous = await db.CandidateDocuments.Where(d =>
                    d.CandidateId == candidateId && d.Category == category && d.IsCurrent)
                .ToListAsync();

            foreach (var superseded in previous)
            {
                superseded.IsCurrent = false;
            }
        }

        var document = new CandidateDocument
        {
            CandidateId = candidate.Id,
            Category = category,
            FileName = stored.FileName,
            StoredFileName = stored.RelativePath,
            ContentType = stored.ContentType,
            SizeBytes = stored.SizeBytes,
            IsCurrent = true,
            Remarks = Trim(remarks),
            UploadedAtUtc = DateTime.UtcNow,
            UploadedById = user.UserId
        };

        db.CandidateDocuments.Add(document);
        await db.SaveChangesAsync();

        return ToDocumentDto(document);
    }

    public async Task<CandidateDocument> GetDocumentAsync(int documentId)
    {
        return await db.CandidateDocuments.AsNoTracking()
                   .FirstOrDefaultAsync(d => d.Id == documentId)
               ?? throw new NotFoundException($"Document {documentId} was not found.");
    }

    public async Task DeleteDocumentAsync(int documentId, SignedInUser user)
    {
        var document = await db.CandidateDocuments.FirstOrDefaultAsync(d => d.Id == documentId)
            ?? throw new NotFoundException($"Document {documentId} was not found.");

        document.IsCurrent = false;
        document.Remarks = string.IsNullOrWhiteSpace(document.Remarks)
            ? $"Withdrawn by {user.UserName}"
            : $"{document.Remarks} | withdrawn by {user.UserName}";

        await db.SaveChangesAsync();
        _ = user;
    }

    private async Task<string> NextCandidateCodeAsync(int year)
    {
        var prefix = $"CVB-{year}-";

        var codes = await db.Candidates
            .Where(c => c.CandidateCode.StartsWith(prefix))
            .Select(c => c.CandidateCode)
            .ToListAsync();

        var highest = 0;
        foreach (var code in codes)
        {
            if (int.TryParse(code.AsSpan(prefix.Length), out var number) && number > highest)
            {
                highest = number;
            }
        }

        return prefix + (highest + 1).ToString("D4");
    }

    private async Task EnsureMasterDataLinksAsync(CandidateSaveRequest request)
    {
        if (request.AppliedJobPositionId is { } jobId &&
            !await db.JobPositions.AnyAsync(p => p.Id == jobId))
        {
            throw new DomainException("The selected applied position is no longer available.");
        }

        if (request.ConcernId is { } concernId &&
            !await db.Concerns.AnyAsync(c => c.Id == concernId))
        {
            throw new DomainException("The selected concern is no longer available.");
        }
    }

    private static void ValidateDates(CandidateSaveRequest request)
    {
        if (request.DateOfBirth is { } dob)
        {
            if (dob > DateTime.Today)
            {
                throw new DomainException("Date of birth cannot be in the future.");
            }

            if (dob.Year < 1940)
            {
                throw new DomainException("Please check the date of birth.");
            }
        }
    }

    private static void Apply(Candidate candidate, CandidateSaveRequest request, bool isNew)
    {
        candidate.Name = request.Name.Trim();
        candidate.MobileNumber = Trim(request.MobileNumber);
        candidate.Email = Trim(request.Email)?.ToLowerInvariant();
        candidate.DateOfBirth = request.DateOfBirth;
        candidate.PresentAddress = Trim(request.PresentAddress);
        candidate.CurrentOrganization = Trim(request.CurrentOrganization);
        candidate.CurrentDesignation = Trim(request.CurrentDesignation);
        candidate.DurationInCurrentOrganization = Trim(request.DurationInCurrentOrganization);
        candidate.TotalYearsOfExperience = request.TotalYearsOfExperience;
        candidate.AppliedJobPositionId = request.AppliedJobPositionId;
        candidate.ConcernId = request.ConcernId;
        candidate.PresentSalary = request.PresentSalary;
        candidate.ExpectedSalary = request.ExpectedSalary;
        candidate.NoticePeriod = Trim(request.NoticePeriod);
        candidate.Remarks = Trim(request.Remarks);

        if (request.Status is { } status)
        {
            candidate.Status = status;
        }
        else if (isNew)
        {
            candidate.Status = CandidateStatus.Shortlisted;
        }
    }

    private static CandidateDto ToDto(Candidate candidate) => new(
        candidate.Id,
        candidate.CandidateCode,
        candidate.Name,
        candidate.MobileNumber,
        candidate.Email,
        candidate.DateOfBirth,
        candidate.PresentAddress,
        candidate.CurrentOrganization,
        candidate.CurrentDesignation,
        candidate.DurationInCurrentOrganization,
        candidate.TotalYearsOfExperience,
        candidate.AppliedJobPositionId,
        candidate.AppliedJobPosition?.Title,
        candidate.ConcernId,
        candidate.Concern?.Name,
        candidate.PresentSalary,
        candidate.ExpectedSalary,
        candidate.NoticePeriod,
        candidate.Remarks,
        candidate.Status,
        candidate.CreatedAtUtc,
        candidate.CreatedBy?.FullName,
        candidate.UpdatedAtUtc,
        candidate.Evaluations.Count,
        candidate.Documents.Select(ToDocumentDto).ToList());

    private static CandidateDocumentDto ToDocumentDto(CandidateDocument document) => new(
        document.Id,
        document.Category,
        document.FileName,
        document.ContentType,
        document.SizeBytes,
        document.IsCurrent,
        document.UploadedAtUtc,
        document.UploadedBy?.FullName,
        document.Remarks);

    private static string? NormalizeContact(string? value)
    {
        var trimmed = Trim(value);
        return trimmed?.Replace(" ", "").Replace("-", "").Replace("+", "");
    }

    private static bool SameMobile(string normalized, string? stored) =>
        NormalizeContact(stored) == normalized;

    private static string? Trim(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static bool IsUniqueCodeViolation(DbUpdateException ex) =>
        ex.InnerException?.Message?.Contains("IX_Candidates_CandidateCode", StringComparison.Ordinal) == true;
}
