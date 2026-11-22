using CvBank.Api.Auth;
using CvBank.Api.Models;
using CvBank.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace CvBank.Api.Controllers;

[ApiController]
[Route("api/candidates")]
public class CandidatesController(
    CandidateService candidates,
    DashboardService dashboard,
    IFileStorage storage,
    IAuditWriter audit) : ControllerBase
{
    private SignedInUser Me => SignedInUser.From(User);

    [HttpGet]
    [Permission(Permissions.CandidateView)]
    public async Task<ActionResult<PagedResult<CandidateListItemDto>>> List([FromQuery] CandidateQuery query)
    {
        return Ok(await candidates.ListAsync(query));
    }

    [HttpGet("{id:int}/summary")]
    [Permission(Permissions.CandidateView)]
    public async Task<ActionResult<CandidateSummaryDto>> Summary(int id)
    {
        return Ok(await dashboard.CandidateSummaryAsync(id));
    }

    [HttpGet("duplicates")]
    [Permission(Permissions.CandidateView)]
    public async Task<ActionResult<IReadOnlyList<DuplicateCandidateDto>>> Duplicates(
        [FromQuery] string? mobileNumber,
        [FromQuery] string? email,
        [FromQuery] int? excludeId)
    {
        return Ok(await candidates.FindDuplicatesAsync(mobileNumber, email, excludeId));
    }

    [HttpGet("{id:int}")]
    [Permission(Permissions.CandidateView)]
    public async Task<ActionResult<CandidateDto>> Get(int id)
    {
        return Ok(await candidates.GetAsync(id));
    }

    [HttpPost]
    [Permission(Permissions.CandidateCreate)]
    public async Task<ActionResult<CandidateDto>> Create(CandidateSaveRequest request)
    {
        var created = await candidates.CreateAsync(request, Me);

        await audit.WriteAsync(Me.UserId, Me.FullName, "Candidate Create", "Candidate",
            created.Id.ToString(), $"{created.CandidateCode} | {created.Name}");

        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }

    [HttpPut("{id:int}")]
    [Permission(Permissions.CandidateUpdate)]
    public async Task<ActionResult<CandidateDto>> Update(int id, CandidateSaveRequest request)
    {
        var before = await candidates.GetAsync(id);
        var updated = await candidates.UpdateAsync(id, request, Me);

        await audit.WriteAsync(Me.UserId, Me.FullName, "Candidate Update", "Candidate",
            updated.Id.ToString(), $"{updated.CandidateCode} | {updated.Name} (was {before.Name})");

        return Ok(updated);
    }

    [HttpPut("{id:int}/status")]
    [Permission(Permissions.CandidateUpdate)]
    public async Task<ActionResult<CandidateDto>> SetStatus(int id, CandidateStatusRequest request)
    {
        var updated = await candidates.SetStatusAsync(id, request.Status, Me);

        await audit.WriteAsync(Me.UserId, Me.FullName, "Candidate Status", "Candidate",
            updated.Id.ToString(), $"{updated.CandidateCode} | {request.Status}");

        return Ok(updated);
    }

    [HttpDelete("{id:int}")]
    [Permission(Permissions.CandidateDelete)]
    public async Task<IActionResult> Delete(int id)
    {
        var existing = await candidates.GetAsync(id);
        await candidates.DeleteAsync(id);

        await audit.WriteAsync(Me.UserId, Me.FullName, "Candidate Delete", "Candidate",
            id.ToString(), $"{existing.CandidateCode} | {existing.Name}");

        return NoContent();
    }

    [HttpPost("{id:int}/documents")]
    [Permission(Permissions.CvUpload)]
    [RequestFormLimits(MultipartBodyLengthLimit = 20 * 1024 * 1024)]
    [RequestSizeLimit(20 * 1024 * 1024)]
    public async Task<ActionResult<CandidateDocumentDto>> Upload(
        int id,
        IFormFile file,
        [FromForm] DocumentCategory category,
        [FromForm] string? remarks,
        [FromForm] bool replaceCurrent)
    {
        if (file is null || file.Length == 0)
        {
            throw new DomainException("Choose a file first.");
        }

        var candidate = await candidates.GetAsync(id);

        var document = await candidates.AddDocumentAsync(id, file, category, remarks, replaceCurrent, Me);

        var action = category == DocumentCategory.Cv ? "CV Upload" : "Document Upload";
        await audit.WriteAsync(Me.UserId, Me.FullName, action, "CandidateDocument",
            document.Id.ToString(), $"{candidate.CandidateCode} | {document.FileName} | {document.SizeBytes} bytes");

        return CreatedAtAction(nameof(Download), new { documentId = document.Id }, document);
    }

    [HttpGet("documents/{documentId:int}/download")]
    [Permission(Permissions.CvDownload)]
    public async Task<IActionResult> Download(int documentId, [FromQuery] bool inline)
    {
        var document = await candidates.GetDocumentAsync(documentId);
        var path = storage.GetPhysicalPath(document.StoredFileName);

        if (!System.IO.File.Exists(path))
        {
            throw new NotFoundException("The stored file is missing on the server. Ask the administrator to check the document folder.");
        }

        await audit.WriteAsync(Me.UserId, Me.FullName,
            document.Category == DocumentCategory.Cv ? "CV Download" : "Document Download",
            "CandidateDocument", documentId.ToString(), document.FileName);

        return PhysicalFile(path, document.ContentType, inline ? null : document.FileName, enableRangeProcessing: true);
    }

    [HttpDelete("documents/{documentId:int}")]
    [Permission(Permissions.DocumentManage)]
    public async Task<IActionResult> DeleteDocument(int documentId)
    {
        var document = await candidates.GetDocumentAsync(documentId);
        await candidates.DeleteDocumentAsync(documentId, Me);

        await audit.WriteAsync(Me.UserId, Me.FullName, "Document Withdraw", "CandidateDocument",
            documentId.ToString(), document.FileName);

        return NoContent();
    }
}
