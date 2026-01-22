using System.Diagnostics;
using FastEndpoints;
using Kdt.Share.Messages;
using Wolverine;
using ApiResponse = Kdt.Share.Response;

namespace Kdt.WebApi.Endpoints;

public class ServerTimeEndpointSummary : Summary<ServerTimeEndpoint>
{
    public ServerTimeEndpointSummary()
    {
        Summary = "서버 시간 (via RabbitMQ)";
        Description = "RabbitMQ를 통해 Consumer에서 처리된 서버 시간을 반환합니다.";
        Response<ApiResponse.ServerTimeResponse>(200, "성공적으로 서버 시간을 반환했습니다.");
        Response<ProblemDetails>(400, "문제 발생시");
    }
}

public class ServerTimeEndpoint : EndpointWithoutRequest<ApiResponse.ServerTimeResponse>
{
    private static readonly ActivitySource ActivitySource = new("Kdt.WebApi");
    private readonly ILogger<ServerTimeEndpoint> _logger;
    private readonly IMessageBus _messageBus;

    public ServerTimeEndpoint(ILogger<ServerTimeEndpoint> logger, IMessageBus messageBus)
    {
        _logger = logger;
        _messageBus = messageBus;
    }

    public override void Configure()
    {
        Get("/serverTime");
        AllowAnonymous();
        Summary(new ServerTimeEndpointSummary());
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        using var activity = ActivitySource.StartActivity("ServerTimeEndpoint.HandleAsync", ActivityKind.Producer);

        _logger.LogInformation("ServerTimeEndpoint called via RabbitMQ. {Remote}", HttpContext.Connection.RemoteIpAddress);

        // ServerTimeRequest 메시지를 RabbitMQ로 발행하고 응답 대기
        var request = new ServerTimeRequest
        {
            RequestId = Guid.NewGuid(),
            RequestedAt = DateTime.UtcNow
        };

        // 추적 정보 추가
        activity?.SetTag("messaging.system", "rabbitmq");
        activity?.SetTag("messaging.destination", "servertime-requests");
        activity?.SetTag("messaging.operation", "send");
        activity?.SetTag("request.id", request.RequestId.ToString());

        _logger.LogInformation("Sending ServerTimeRequest to RabbitMQ. RequestId: {RequestId}", request.RequestId.ToString());

        // Consumer로부터 응답을 받기 위해 InvokeAsync 사용 (Request-Reply 패턴)
        var messageResponse = await _messageBus.InvokeAsync<ServerTimeResponse>(request, ct);

        // 응답 수신 정보 추가
        activity?.SetTag("response.received", true);
        activity?.SetTag("response.id", messageResponse.RequestId.ToString());
        activity?.SetTag("response.processed_by", messageResponse.ProcessedBy);

        _logger.LogInformation("Received ServerTimeResponse from Consumer. RequestId: {RequestId}", messageResponse.RequestId.ToString());

        // 기존 Response 모델로 변환하여 반환
        Response = new ApiResponse.ServerTimeResponse()
        {
            Local = messageResponse.Local,
            Utc = messageResponse.Utc
        };
    }
}
