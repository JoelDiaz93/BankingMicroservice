using System.Diagnostics;
using Accounts.Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Accounts.Api.Middleware;

public sealed class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error procesando {Method} {Path}", context.Request.Method, context.Request.Path);
            await WriteProblemAsync(context, ex);
        }
    }

    private static async Task WriteProblemAsync(HttpContext context, Exception exception)
    {
        var (status, title, detail) = exception switch
        {
            InsufficientBalanceException => (StatusCodes.Status409Conflict, "Saldo insuficiente", "Saldo no disponible"),
            KeyNotFoundException => (StatusCodes.Status404NotFound, "Recurso no encontrado", exception.Message),
            ArgumentException => (StatusCodes.Status400BadRequest, "Solicitud inválida", exception.Message),
            DomainException => (StatusCodes.Status422UnprocessableEntity, "Regla de negocio inválida", exception.Message),
            DbUpdateException => (StatusCodes.Status409Conflict, "Conflicto de persistencia", "La operación viola una restricción de datos."),
            _ => (StatusCodes.Status500InternalServerError, "Error interno", "Ocurrió un error inesperado.")
        };

        var problem = new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = detail,
            Instance = context.Request.Path
        };
        problem.Extensions["traceId"] = Activity.Current?.Id ?? context.TraceIdentifier;

        context.Response.StatusCode = status;
        context.Response.ContentType = "application/problem+json";
        await context.Response.WriteAsJsonAsync(problem);
    }
}
