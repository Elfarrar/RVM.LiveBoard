namespace RVM.LiveBoard.API.Middleware;

public class CorrelationIdMiddleware(RequestDelegate next)
{
    private const string Header = "X-Correlation-ID";

    public async Task InvokeAsync(HttpContext context)
    {
        if (!context.Request.Headers.ContainsKey(Header))
            context.Request.Headers[Header] = Guid.NewGuid().ToString();

        context.Response.OnStarting(() =>
        {
            context.Response.Headers[Header] = context.Request.Headers[Header].ToString();
            return Task.CompletedTask;
        });

        await next(context);
    }
}
