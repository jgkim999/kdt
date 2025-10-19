using FastEndpoints;

namespace Kdt.WebApi.Endpoints;

public class HelloWorldEndpoint : EndpointWithoutRequest
{
    private readonly ILogger<HelloWorldEndpoint> _logger;
    
    public HelloWorldEndpoint(ILogger<HelloWorldEndpoint> logger)
    {
        _logger = logger;
    }
    
    public override void Configure()
    {
        Get("/");
        AllowAnonymous();
    }
    
    public override async Task HandleAsync(CancellationToken ct)
    {
        _logger.LogDebug("HelloWorldEndpoint called. {Remote}", HttpContext.Connection.RemoteIpAddress);
        _logger.LogTrace("HelloWorldEndpoint called. {Remote}", HttpContext.Connection.RemoteIpAddress);
        _logger.LogInformation("HelloWorldEndpoint called. {Remote}", HttpContext.Connection.RemoteIpAddress);
        _logger.LogWarning("HelloWorldEndpoint called. {Remote}", HttpContext.Connection.RemoteIpAddress);
        _logger.LogError("HelloWorldEndpoint called. {Remote}", HttpContext.Connection.RemoteIpAddress);
        _logger.LogCritical("HelloWorldEndpoint called. {Remote}", HttpContext.Connection.RemoteIpAddress);
        
        Response = "Hello World!";
        await Task.CompletedTask;
    }
}
