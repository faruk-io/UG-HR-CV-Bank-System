using CvBank.Api.Auth;
using CvBank.Api.Data;
using CvBank.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace CvBank.Api.Services;

public class EvaluationService(CvBankDbContext db, IFileStorage storage)
{
    public async Task<PagedResult<EvaluationListItemDto>> ListAsync(EvaluationQuery query)
    {
        var search = (query.Search ?? string.Empty).Trim().ToLower();
        var pageSize = Math.Clamp(query.PageSize, 5, 100);
        var page = Math.Max(1, query.Page);
        var dateFrom = query.DateFrom?.Date;
        var dateTo = query.DateTo?.Date.AddDays(1);

        var matched = db.InterviewEvaluations.AsNoTracking()
            .Where(e =>
                (search == "" ||
                 e.Candidate.Name.ToLower().Contains(search) ||
                 e.Candidate.CandidateCode.ToLower().Contains(search) ||
                 e.Candidate.CurrentOrganization!.ToLower().Contains(search) ||
                 e.Candidate.CurrentDesignation!.ToLower().Contains(search)) &&
                (query.CandidateId == null || e.CandidateId == query.CandidateId) &&
                (query.Status == null || e.Status == query.Status) &&
                (query.DateFrom == null || e.InterviewDate >= dateFrom) &&
                (query.DateTo == null || e.InterviewDate < dateTo) &&
                (query.JobPositionId == null ||
                 e.AppliedJobPositionId == query.JobPositionId ||
                 (e.AppliedJobPositionId == null && e.Candidate.AppliedJobPositionId == query.JobPositionId)) &&
                (query.Decision == null ||
                 (e.Result != null && e.Result.Decision == query.Decision) ||
                 (e.Result == null && InterviewDecision.NotTaken == query.Decision)) &&
                (query.HasScannedForm == null ||
                 query.HasScannedForm == db.CandidateDocuments.Any(d =>
                     d.InterviewEvaluationId == e.Id &&
                     d.Category == DocumentCategory.CompletedEvaluationForm &&
                     d.IsCurrent)))
            .OrderByDescending(e => e.InterviewDate)
            .ThenByDescending(e => e.Id);

        var total = await matched.CountAsync();

        var items = await matched
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(e => new EvaluationListItemDto(
                e.Id,
                e.CandidateId,
                e.Candidate.CandidateCode,
                e.Candidate.Name,
                e.AppliedJobPosition != null
                    ? e.AppliedJobPosition.Title
                    : e.Candidate.AppliedJobPosition!.Title,
                e.Concern != null ? e.Concern.Name : e.Candidate.Concern!.Name,
                e.InterviewNumber,
                e.InterviewDate,
                e.Status,
                e.Result != null ? e.Result.Decision : InterviewDecision.NotTaken,
                e.Ratings.Sum(r => r.Rating),
                EvaluationTemplate.MaxTotalRating,
                db.CandidateDocuments.Any(d =>
                    d.InterviewEvaluationId == e.Id &&
                    d.Category == DocumentCategory.CompletedEvaluationForm &&
                    d.IsCurrent),
                e.CreatedAtUtc,
                e.CreatedBy!.FullName))
            .ToListAsync();

        return new PagedResult<EvaluationListItemDto>(items, total, page, pageSize);
    }

    public async Task<List<CandidateEvaluationHistoryItemDto>> HistoryAsync(int candidateId)
    {
        var rows = await db.InterviewEvaluations.AsNoTracking()
            .Where(e => e.CandidateId == candidateId)
            .OrderByDescending(e => e.InterviewDate)
            .ThenByDescending(e => e.Id)
            .Select(e => new CandidateEvaluationHistoryItemDto(
                e.Id,
                e.InterviewNumber,
                e.InterviewDate,
                e.Status,
                e.AppliedJobPosition != null
                    ? e.AppliedJobPosition.Title
                    : e.Candidate.AppliedJobPosition!.Title,
                e.Concern != null ? e.Concern.Name : e.Candidate.Concern!.Name,
                e.Ratings.Sum(r => r.Rating),
                EvaluationTemplate.MaxTotalRating,
                e.Result != null ? e.Result.Decision : InterviewDecision.NotTaken,
                e.Result != null ? e.Result.Recommendation : null,
                null,
                db.CandidateDocuments.Any(d =>
                    d.InterviewEvaluationId == e.Id &&
                    d.Category == DocumentCategory.CompletedEvaluationForm &&
                    d.IsCurrent),
                e.Result != null ? e.Result.DesignationFixed!.Name : null,
                e.Result != null ? e.Result.SalaryFixed : null,
                e.Result != null ? e.Result.Grade : null,
                e.Result != null ? e.Result.DateOfJoining : null,
                e.CreatedBy!.FullName))
            .ToListAsync();

        if (rows.Count == 0)
        {
            return rows;
        }

        var names = await db.EvaluatorDetails.AsNoTracking()
            .Where(d => d.InterviewEvaluation.CandidateId == candidateId && d.EvaluatorName != null)
            .OrderBy(d => d.InterviewEvaluationId)
            .ThenBy(d => d.SectionNumber)
            .Select(d => new { d.InterviewEvaluationId, d.EvaluatorName })
            .ToListAsync();

        var byEvaluation = names
            .GroupBy(row => row.InterviewEvaluationId)
            .ToDictionary(group => group.Key, group => string.Join(", ", group.Select(row => row.EvaluatorName)));

        return rows
            .Select(row => row with { EvaluatorNames = byEvaluation.GetValueOrDefault(row.Id) })
            .ToList();
    }

    public async Task<EvaluationDto> GetAsync(int id)
    {
        var evaluation = await db.InterviewEvaluations.AsNoTracking()
            .Include(e => e.Candidate).ThenInclude(c => c.AppliedJobPosition)
            .Include(e => e.Candidate).ThenInclude(c => c.Concern)
            .Include(e => e.AppliedJobPosition)
            .Include(e => e.Concern)
            .Include(e => e.CreatedBy)
            .Include(e => e.Ratings)
            .Include(e => e.DocumentItems)
            .Include(e => e.References)
            .Include(e => e.Evaluators)
            .Include(e => e.Result).ThenInclude(r => r!.DesignationFixed)
            .Include(e => e.Approvals)
            .FirstOrDefaultAsync(e => e.Id == id);

        if (evaluation is null)
        {
            throw new NotFoundException($"Evaluation {id} was not found.");
        }

        var scans = await db.CandidateDocuments.AsNoTracking()
            .Include(d => d.UploadedBy)
            .Where(d => d.InterviewEvaluationId == id && d.Category == DocumentCategory.CompletedEvaluationForm)
            .OrderByDescending(d => d.UploadedAtUtc)
            .ToListAsync();

        return ToDto(evaluation, scans);
    }

    public async Task<InterviewEvaluation> GetTrackedAsync(int id)
    {
        return await db.InterviewEvaluations
            .Include(e => e.Candidate)
            .FirstOrDefaultAsync(e => e.Id == id)
            ?? throw new NotFoundException($"Evaluation {id} was not found.");
    }

    public async Task<EvaluationDto> CreateAsync(EvaluationSaveRequest request, SignedInUser user)
    {
        var candidate = await db.Candidates.Include(c => c.Evaluations).ThenInclude(e => e.Ratings)
            .FirstOrDefaultAsync(c => c.Id == request.CandidateId)
            ?? throw new NotFoundException($"Candidate {request.CandidateId} was not found.");

        await ValidateAsync(request);

        var interviewNumber = request.InterviewNumber ?? NextInterviewNumber(candidate);

        if (candidate.Evaluations.Any(e => e.InterviewNumber == interviewNumber))
        {
            throw new DomainException($"Interview number {interviewNumber} already exists for this candidate.");
        }

        var evaluation = new InterviewEvaluation
        {
            CandidateId = candidate.Id,
            InterviewNumber = interviewNumber,
            InterviewDate = request.InterviewDate,
            Status = request.Status,
            AppliedJobPositionId = request.AppliedJobPositionId ?? candidate.AppliedJobPositionId,
            ConcernId = request.ConcernId ?? candidate.ConcernId,
            PersonsInterviewTaken = request.PersonsInterviewTaken,
            Remarks = Trim(request.Remarks),
            CreatedAtUtc = DateTime.UtcNow,
            CreatedById = user.UserId
        };

        ApplySections(evaluation, request);
        db.InterviewEvaluations.Add(evaluation);

        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            db.ChangeTracker.Clear();
            throw new DomainException($"Interview number {interviewNumber} already exists for this candidate.");
        }

        return await GetAsync(evaluation.Id);
    }

    public async Task<EvaluationDto> UpdateAsync(int id, EvaluationSaveRequest request, SignedInUser user, bool mayManageOthers)
    {
        var evaluation = await db.InterviewEvaluations
            .Include(e => e.Ratings)
            .Include(e => e.DocumentItems)
            .Include(e => e.References)
            .Include(e => e.Evaluators)
            .Include(e => e.Approvals)
            .Include(e => e.Result)
            .FirstOrDefaultAsync(e => e.Id == id)
            ?? throw new NotFoundException($"Evaluation {id} was not found.");

        if (!mayManageOthers && evaluation.CreatedById != user.UserId)
        {
            throw new DomainException("This evaluation belongs to another user. Only a Manager or Super Admin may change it.");
        }

        await ValidateAsync(request);

        var interviewNumber = request.InterviewNumber ?? evaluation.InterviewNumber;

        var clash = await db.InterviewEvaluations.AnyAsync(e =>
            e.Id != id && e.CandidateId == evaluation.CandidateId && e.InterviewNumber == interviewNumber);

        if (clash)
        {
            throw new DomainException($"Interview number {interviewNumber} already exists for this candidate.");
        }

        evaluation.InterviewNumber = interviewNumber;
        evaluation.InterviewDate = request.InterviewDate;
        evaluation.Status = request.Status;
        evaluation.AppliedJobPositionId = request.AppliedJobPositionId;
        evaluation.ConcernId = request.ConcernId;
        evaluation.PersonsInterviewTaken = request.PersonsInterviewTaken;
        evaluation.Remarks = Trim(request.Remarks);

        db.EvaluationRatings.RemoveRange(evaluation.Ratings);
        db.EvaluationDocumentItems.RemoveRange(evaluation.DocumentItems);
        db.InterviewReferences.RemoveRange(evaluation.References);
        db.EvaluatorDetails.RemoveRange(evaluation.Evaluators);
        db.InterviewApprovals.RemoveRange(evaluation.Approvals);

        ApplySections(evaluation, request);

        await db.SaveChangesAsync();

        return await GetAsync(evaluation.Id);
    }

    public async Task DeleteAsync(int id)
    {
        var evaluation = await db.InterviewEvaluations
            .Include(e => e.Result)
            .FirstOrDefaultAsync(e => e.Id == id)
            ?? throw new NotFoundException($"Evaluation {id} was not found.");

        var scans = await db.CandidateDocuments
            .Where(d => d.InterviewEvaluationId == id)
            .ToListAsync();

        db.CandidateDocuments.RemoveRange(scans);
        db.InterviewEvaluations.Remove(evaluation);
        await db.SaveChangesAsync();

        foreach (var scan in scans)
        {
            storage.TryDeletePhysical(scan.StoredFileName);
        }
    }

    public async Task<CandidateDocumentDto> AddScanAsync(int evaluationId, IFormFile file, string? remarks, SignedInUser user)
    {
        var evaluation = await db.InterviewEvaluations.AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == evaluationId)
            ?? throw new NotFoundException($"Evaluation {evaluationId} was not found.");

        storage.Validate(file.FileName, file.ContentType, file.Length, DocumentCategory.CompletedEvaluationForm);

        StoredUpload stored;
        await using (var stream = file.OpenReadStream())
        {
            stored = storage.Save(stream, file.FileName, file.ContentType, DocumentCategory.CompletedEvaluationForm);
        }

        var replaced = await db.CandidateDocuments.Where(d =>
                d.InterviewEvaluationId == evaluationId &&
                d.Category == DocumentCategory.CompletedEvaluationForm &&
                d.IsCurrent)
            .ToListAsync();

        foreach (var old in replaced)
        {
            old.IsCurrent = false;
        }

        var document = new CandidateDocument
        {
            CandidateId = evaluation.CandidateId,
            InterviewEvaluationId = evaluation.Id,
            Category = DocumentCategory.CompletedEvaluationForm,
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

        return new CandidateDocumentDto(
            document.Id,
            document.Category,
            document.FileName,
            document.ContentType,
            document.SizeBytes,
            document.IsCurrent,
            document.UploadedAtUtc,
            user.FullName,
            document.Remarks);
    }

    public async Task<(CandidateDocumentDto Document, string PhysicalPath)> GetScanAsync(int documentId)
    {
        var document = await db.CandidateDocuments.AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == documentId && d.Category == DocumentCategory.CompletedEvaluationForm)
            ?? throw new NotFoundException($"The uploaded evaluation form was not found.");

        return (
            new CandidateDocumentDto(
                document.Id,
                document.Category,
                document.FileName,
                document.ContentType,
                document.SizeBytes,
                document.IsCurrent,
                document.UploadedAtUtc,
                null,
                document.Remarks),
            storage.GetPhysicalPath(document.StoredFileName));
    }

    private async Task ValidateAsync(EvaluationSaveRequest request)
    {
        if (!Enum.IsDefined(request.Status))
        {
            throw new DomainException("The interview status is not a recognised value.");
        }

        if (request.Result is not null && !Enum.IsDefined(request.Result.Decision))
        {
            throw new DomainException("The interview decision is not a recognised value.");
        }

        if (request.InterviewNumber is < 1 or > 20)
        {
            throw new DomainException("The interview number must be between 1 and 20.");
        }

        RequireUnique(request.Ratings.Select(r => r.DimensionCode), "dimension");
        RequireUnique(request.DocumentItems.Select(d => d.ItemCode), "document item");
        RequireUnique(request.References.Select(r => r.ReferenceNumber), "reference");
        RequireUnique(request.Evaluators.Select(e => e.SectionNumber), "evaluator section");
        RequireUnique(request.Approvals.Select(a => a.AuthorityNumber), "approval authority");

        if (request.Ratings.Sum(r => r.Rating) > EvaluationTemplate.MaxTotalRating)
        {
            throw new DomainException($"The total rating cannot exceed {EvaluationTemplate.MaxTotalRating}.");
        }

        if (request.Result?.DesignationFixedId is { } designationId &&
            !await db.Designations.AnyAsync(d => d.Id == designationId))
        {
            throw new DomainException("The fixed designation is not available. Select one from the list.");
        }
    }

    private static void RequireUnique(IEnumerable<int> codes, string label)
    {
        var list = codes.ToList();

        if (list.Count != list.Distinct().Count())
        {
            throw new DomainException($"Each {label} may only be listed once.");
        }
    }

    private static int NextInterviewNumber(Candidate candidate) =>
        candidate.Evaluations.Count == 0 ? 1 : candidate.Evaluations.Max(e => e.InterviewNumber) + 1;

    private void ApplySections(InterviewEvaluation evaluation, EvaluationSaveRequest request)
    {
        foreach (var input in request.Ratings.Where(r => r.Rating > 0))
        {
            var dimension = EvaluationTemplate.Dimensions.FirstOrDefault(d => d.Code == input.DimensionCode);

            if (dimension.Code == 0)
            {
                continue;
            }

            evaluation.Ratings.Add(new EvaluationRating
            {
                DimensionCode = dimension.Code,
                DimensionName = dimension.Name,
                Rating = Math.Clamp(input.Rating, 0, 5)
            });
        }

        foreach (var input in request.DocumentItems)
        {
            var item = EvaluationTemplate.DocumentChecklist.FirstOrDefault(d => d.Code == input.ItemCode);

            if (item.Code == 0)
            {
                continue;
            }

            evaluation.DocumentItems.Add(new EvaluationDocumentItem
            {
                ItemCode = item.Code,
                ItemName = item.Name,
                Details = Trim(input.Details),
                CopySubmitted = input.CopySubmitted,
                OriginalChecked = input.OriginalChecked
            });
        }

        foreach (var input in request.References)
        {
            if (input.ReferenceNumber is < 1 or > EvaluationTemplate.ReferenceSections)
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(input.ReferenceName) && string.IsNullOrWhiteSpace(input.Statement) &&
                string.IsNullOrWhiteSpace(input.Phone))
            {
                continue;
            }

            evaluation.References.Add(new InterviewReference
            {
                ReferenceNumber = input.ReferenceNumber,
                ReferenceName = Trim(input.ReferenceName),
                Designation = Trim(input.Designation),
                Relationship = Trim(input.Relationship),
                CompanyAndLocation = Trim(input.CompanyAndLocation),
                Phone = Trim(input.Phone),
                Statement = Trim(input.Statement)
            });
        }

        foreach (var input in request.Evaluators)
        {
            if (input.SectionNumber is < 1 or > EvaluationTemplate.EvaluatorSections)
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(input.Findings) && string.IsNullOrWhiteSpace(input.EvaluatorName))
            {
                continue;
            }

            evaluation.Evaluators.Add(new EvaluatorDetail
            {
                SectionNumber = input.SectionNumber,
                Findings = Trim(input.Findings),
                EvaluatorName = Trim(input.EvaluatorName),
                Designation = Trim(input.Designation),
                Department = Trim(input.Department),
                SignatureText = Trim(input.SignatureText)
            });
        }

        foreach (var input in request.Approvals)
        {
            if (input.AuthorityNumber < 1 || input.AuthorityNumber > EvaluationTemplate.ApprovalAuthorities.Length)
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(input.Decision) && string.IsNullOrWhiteSpace(input.SignatureText) &&
                input.ActedAtUtc is null)
            {
                continue;
            }

            evaluation.Approvals.Add(new InterviewApproval
            {
                AuthorityNumber = input.AuthorityNumber,
                AuthorityTitle = EvaluationTemplate.ApprovalAuthorities[input.AuthorityNumber - 1],
                Decision = Trim(input.Decision),
                SignatureText = Trim(input.SignatureText),
                ActedAtUtc = input.ActedAtUtc
            });
        }

        if (request.Result is { } result)
        {
            var entity = evaluation.Result ?? new InterviewResult();

            entity.Decision = result.Decision;
            entity.Recommendation = Trim(result.Recommendation);
            entity.ExpectedSalary = result.ExpectedSalary;
            entity.NoticePeriod = Trim(result.NoticePeriod);
            entity.DesignationFixedId = result.DesignationFixedId;
            entity.SalaryFixed = result.SalaryFixed;
            entity.Grade = Trim(result.Grade);
            entity.DateOfJoining = result.DateOfJoining;
            entity.Remarks = Trim(result.Remarks);
            entity.UpdatedAtUtc = DateTime.UtcNow;

            if (evaluation.Result is null)
            {
                evaluation.Result = entity;
            }
        }
    }

    private static EvaluationDto ToDto(InterviewEvaluation evaluation, IReadOnlyList<CandidateDocument> scans)
    {
        var ratings = EvaluationTemplate.Dimensions
            .Select(d =>
            {
                var saved = evaluation.Ratings.FirstOrDefault(r => r.DimensionCode == d.Code);

                return new EvaluationRatingLineDto(d.Code, d.Name, saved?.Rating ?? 0);
            })
            .ToList();

        var documentItems = EvaluationTemplate.DocumentChecklist
            .Select(d =>
            {
                var saved = evaluation.DocumentItems.FirstOrDefault(i => i.ItemCode == d.Code);

                return new DocumentItemLineDto(d.Code, d.Name, saved?.Details, saved?.CopySubmitted ?? false,
                    saved?.OriginalChecked ?? false);
            })
            .ToList();

        var references = Enumerable.Range(1, EvaluationTemplate.ReferenceSections)
            .Select(number =>
            {
                var saved = evaluation.References.FirstOrDefault(r => r.ReferenceNumber == number);

                return new ReferenceLineDto(
                    number,
                    saved?.ReferenceName,
                    saved?.Designation,
                    saved?.Relationship,
                    saved?.CompanyAndLocation,
                    saved?.Phone,
                    saved?.Statement);
            })
            .ToList();

        var evaluators = Enumerable.Range(1, EvaluationTemplate.EvaluatorSections)
            .Select(number =>
            {
                var saved = evaluation.Evaluators.FirstOrDefault(e => e.SectionNumber == number);

                return new EvaluatorLineDto(
                    number,
                    saved?.Findings,
                    saved?.EvaluatorName,
                    saved?.Designation,
                    saved?.Department,
                    saved?.SignatureText);
            })
            .ToList();

        var approvals = EvaluationTemplate.ApprovalAuthorities
            .Select((title, index) =>
            {
                var saved = evaluation.Approvals.FirstOrDefault(a => a.AuthorityNumber == index + 1);

                return new ApprovalLineDto(index + 1, title, saved?.Decision, saved?.SignatureText, saved?.ActedAtUtc);
            })
            .ToList();

        var candidate = evaluation.Candidate;

        return new EvaluationDto(
            evaluation.Id,
            candidate.Id,
            candidate.CandidateCode,
            evaluation.InterviewNumber,
            evaluation.InterviewDate,
            evaluation.Status,
            evaluation.AppliedJobPositionId ?? candidate.AppliedJobPositionId,
            evaluation.AppliedJobPosition?.Title ?? candidate.AppliedJobPosition?.Title,
            evaluation.ConcernId ?? candidate.ConcernId,
            evaluation.Concern?.Name ?? candidate.Concern?.Name,
            evaluation.PersonsInterviewTaken,
            evaluation.Remarks,
            evaluation.Ratings.Sum(r => r.Rating),
            EvaluationTemplate.MaxTotalRating,
            EvaluationTemplate.RatingScaleText,
            new EvaluationFormCandidateDto(
                candidate.Id,
                candidate.CandidateCode,
                candidate.Name,
                candidate.MobileNumber,
                candidate.Email,
                candidate.DateOfBirth,
                candidate.CurrentOrganization,
                candidate.CurrentDesignation,
                candidate.PresentSalary,
                candidate.DurationInCurrentOrganization,
                candidate.TotalYearsOfExperience,
                candidate.PresentAddress,
                candidate.ExpectedSalary,
                candidate.NoticePeriod),
            ratings,
            documentItems,
            references,
            evaluators,
            evaluation.Result is null
                ? null
                : new ResultLineDto(
                    evaluation.Result.Decision,
                    evaluation.Result.Recommendation,
                    evaluation.Result.ExpectedSalary,
                    evaluation.Result.NoticePeriod,
                    evaluation.Result.DesignationFixedId,
                    evaluation.Result.DesignationFixed?.Name,
                    evaluation.Result.SalaryFixed,
                    evaluation.Result.Grade,
                    evaluation.Result.DateOfJoining,
                    evaluation.Result.Remarks),
            approvals,
            scans.Select(s => new CandidateDocumentDto(
                s.Id,
                s.Category,
                s.FileName,
                s.ContentType,
                s.SizeBytes,
                s.IsCurrent,
                s.UploadedAtUtc,
                s.UploadedBy?.FullName,
                s.Remarks)).ToList(),
            evaluation.CreatedAtUtc,
            evaluation.CreatedById,
            evaluation.CreatedBy?.FullName);
    }

    private static string? Trim(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
