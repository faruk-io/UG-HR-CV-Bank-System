using CvBank.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace CvBank.Api.Infrastructure;

/// <summary>Turns the service layer's domain errors into short, user-readable messages.</summary>
public sealed class ApiExceptionFilter(ILogger<ApiExceptionFilter> logger) : IAsyncExceptionFilter
{
    public Task OnExceptionAsync(ExceptionContext context)
    {
        var status = context.Exception switch
        {
            DuplicateCandidateException => StatusCodes.Status409Conflict,
            NotFoundException => StatusCodes.Status404NotFound,
            InvalidUploadException or DomainException => StatusCodes.Status400BadRequest,
            _ => 0
        };

        if (status == 0)
        {
            return Task.CompletedTask;
        }

        logger.LogInformation("Rejected {Method} {Path} with {Status}: {Message}",
            context.HttpContext.Request.Method, context.HttpContext.Request.Path, status, context.Exception.Message);

        context.Result = new ObjectResult(new { message = context.Exception.Message }) { StatusCode = status };
        context.ExceptionHandled = true;

        return Task.CompletedTask;
    }
}
