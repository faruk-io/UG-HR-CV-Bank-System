using CvBank.Api.Auth;
using CvBank.Api.Models;
using CvBank.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace CvBank.Api.Controllers;

[ApiController]
[Route("api/evaluations")]
public class EvaluationsController(EvaluationService evaluations, IAuditWriter audit) : ControllerBase
{
    private const int MaxUploadBytes = 20 * 1024 * 1024;

    private SignedInUser Me => SignedInUser.From(User);

    [HttpGet]
    [Permission(Permissions.EvaluationView)]
    public async Task<ActionResult<PagedResult<EvaluationListItemDto>>> List([FromQuery] EvaluationQuery query)
    {
        return Ok(await evaluations.ListAsync(query));
    }

    [HttpGet("by-candidate/{candidateId:int}")]
    [Permission(Permissions.EvaluationView)]
    public async Task<ActionResult<List<CandidateEvaluationHistoryItemDto>>> HistoryForCandidate(int candidateId)
    {
        return Ok(await evaluations.HistoryAsync(candidateId));
    }

    [HttpGet("{id:int}")]
    [Permission(Permissions.EvaluationView)]
    public async Task<ActionResult<EvaluationDto>> Get(int id)
    {
        return Ok(await evaluations.GetAsync(id));
    }

    [HttpPost]
    [Permission(Permissions.EvaluationGenerate)]
    public async Task<ActionResult<EvaluationDto>> Create(EvaluationSaveRequest request)
    {
        var created = await evaluations.CreateAsync(request, Me);

        await audit.WriteAsync(Me.UserId, Me.FullName, "Evaluation Create", "InterviewEvaluation",
            created.Id.ToString(), $"{created.CandidateCode} | interview {created.InterviewNumber} on {created.InterviewDate:yyyy-MM-dd}");

        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }

    [HttpPut("{id:int}")]
    [Permission(Permissions.EvaluationGenerate)]
    public async Task<ActionResult<EvaluationDto>> Update(int id, EvaluationSaveRequest request)
    {
        var saved = await evaluations.UpdateAsync(id, request, Me, User.HasClaim(AuthClaims.Permission, Permissions.EvaluationManage));

        await audit.WriteAsync(Me.UserId, Me.FullName, "Evaluation Update", "InterviewEvaluation",
            saved.Id.ToString(), $"{saved.CandidateCode} | interview {saved.InterviewNumber} on {saved.InterviewDate:yyyy-MM-dd}");

        return Ok(saved);
    }

    [HttpDelete("{id:int}")]
    [Permission(Permissions.EvaluationManage)]
    public async Task<IActionResult> Delete(int id)
    {
        var existing = await evaluations.GetAsync(id);

        await evaluations.DeleteAsync(id);

        await audit.WriteAsync(Me.UserId, Me.FullName, "Evaluation Delete", "InterviewEvaluation",
            id.ToString(), $"{existing.CandidateCode} | interview {existing.InterviewNumber}");

        return NoContent();
    }

    [HttpPost("{id:int}/scan")]
    [Permission(Permissions.EvaluationUpload)]
    [RequestSizeLimit(MaxUploadBytes)]
    public async Task<ActionResult<CandidateDocumentDto>> UploadScan(int id, IFormFile file, [FromForm] string? remarks)
    {
        var document = await evaluations.AddScanAsync(id, file, remarks, Me);

        await audit.WriteAsync(Me.UserId, Me.FullName, "Evaluation Scan Upload", "CandidateDocument",
            document.Id.ToString(), $"evaluation {id} | {document.FileName}");

        return Ok(document);
    }

    [HttpGet("scans/{documentId:int}/download")]
    [Permission(Permissions.EvaluationView)]
    public async Task<IActionResult> DownloadScan(int documentId, [FromQuery] bool inline)
    {
        var (document, physicalPath) = await evaluations.GetScanAsync(documentId);

        await audit.WriteAsync(Me.UserId, Me.FullName, "Evaluation Scan Download", "CandidateDocument",
            document.Id.ToString(), document.FileName);

        return PhysicalFile(
            physicalPath,
            document.ContentType,
            inline ? null : document.FileName,
            enableRangeProcessing: true);
    }
}
