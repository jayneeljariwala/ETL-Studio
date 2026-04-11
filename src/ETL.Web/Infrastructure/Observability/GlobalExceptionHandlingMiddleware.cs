using Serilog;

namespace ETL.Web.Infrastructure.Observability;

public sealed class GlobalExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;

    public GlobalExceptionHandlingMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task Invoke(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            Log.Warning("Request was cancelled by the client. CorrelationId: {CorrelationId}", context.TraceIdentifier);
        }
        catch (Exception ex)
        {
            Log.Error(ex,
                "Unhandled exception. Method: {Method}, Path: {Path}, CorrelationId: {CorrelationId}",
                context.Request.Method,
                context.Request.Path,
                context.TraceIdentifier);

            if (context.Response.HasStarted)
            {
                throw;
            }

            context.Response.Clear();
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;

            var acceptsHtml = context.Request.Headers.Accept.Any(h =>
                h?.Contains("text/html", StringComparison.OrdinalIgnoreCase) == true);

            if (acceptsHtml)
            {
                context.Response.Redirect($"/Home/Error?requestId={Uri.EscapeDataString(context.TraceIdentifier)}");
                return;
            }

            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(new
            {
                error = "An unexpected error occurred.",
                correlationId = context.TraceIdentifier
            });
        }
    }
}
