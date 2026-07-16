using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Wargame.Application.Exceptions;

namespace Wargame.API.Middlewares;

/// <summary>
/// Intercepteur global des exceptions. Transforme les erreurs (Validation, Règles métier)
/// en réponses HTTP standardisées de type ProblemDetails (RFC 7807).
/// </summary>
public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        _logger.LogError(exception, "Une erreur s'est produite : {Message}", exception.Message);

        var problemDetails = new ProblemDetails
        {
            Instance = httpContext.Request.Path
        };

        if (exception is ValidationException validationException)
        {
            problemDetails.Title = "Erreur de validation";
            problemDetails.Status = StatusCodes.Status400BadRequest;
            problemDetails.Detail = "Une ou plusieurs erreurs de validation se sont produites.";
            problemDetails.Extensions["errors"] = validationException.Errors;
        }
        else if (exception is InvalidOperationException invalidOperationException)
        {
            problemDetails.Title = "Opération invalide (Règle métier)";
            problemDetails.Status = StatusCodes.Status400BadRequest;
            problemDetails.Detail = invalidOperationException.Message;
        }
        else
        {
            problemDetails.Title = "Erreur interne du serveur";
            problemDetails.Status = StatusCodes.Status500InternalServerError;
            problemDetails.Detail = "Une erreur inattendue s'est produite.";
        }

        httpContext.Response.StatusCode = problemDetails.Status.Value;
        httpContext.Response.ContentType = "application/problem+json";

        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

        return true; // Indique que l'exception a été gérée et ne doit pas se propager
    }
}
