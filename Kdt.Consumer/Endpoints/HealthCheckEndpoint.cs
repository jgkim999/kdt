using FastEndpoints;

namespace Kdt.Consumer.Endpoints;

public class HealthCheckResponse
{
    public string Status { get; set; } = "Healthy";
    public DateTime Timestamp { get; set; }
    public string Service { get; set; } = "kdt-consumer";
}

public class HealthCheckEndpointSummary : Summary<HealthCheckEndpoint>
{
    public HealthCheckEndpointSummary()
    {
        Summary = "Health Check";
        Description = "서비스 헬스 체크 엔드포인트입니다.";
        Response<HealthCheckResponse>(200, "서비스가 정상적으로 작동 중입니다.");
    }
}

public class HealthCheckEndpoint : EndpointWithoutRequest<HealthCheckResponse>
{
    private readonly ILogger<HealthCheckEndpoint> _logger;

    public HealthCheckEndpoint(ILogger<HealthCheckEndpoint> logger)
    {
        _logger = logger;
    }

    public override void Configure()
    {
        Get("/healthcheck");
        AllowAnonymous();
        Summary(new HealthCheckEndpointSummary());
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        _logger.LogDebug("HealthCheckEndpoint called from {Remote}", HttpContext.Connection.RemoteIpAddress);

        Response = new HealthCheckResponse
        {
            Status = "Healthy",
            Timestamp = DateTime.UtcNow,
            Service = "kdt-consumer"
        };

        await Task.CompletedTask;
    }
}
