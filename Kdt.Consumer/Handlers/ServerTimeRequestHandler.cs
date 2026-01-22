using Kdt.Share.Messages;
using Wolverine;

namespace Kdt.Consumer.Handlers;

/// <summary>
/// ServerTimeRequest 메시지를 처리하는 핸들러
/// </summary>
public class ServerTimeRequestHandler
{
    private readonly ILogger<ServerTimeRequestHandler> _logger;

    public ServerTimeRequestHandler(ILogger<ServerTimeRequestHandler> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// ServerTimeRequest를 처리하고 ServerTimeResponse를 반환
    /// Wolverine은 메서드 이름이 Handle이고 메시지 타입을 파라미터로 받으면 자동으로 핸들러로 인식
    /// </summary>
    public ServerTimeResponse Handle(ServerTimeRequest request)
    {
        _logger.LogInformation("Processing ServerTimeRequest. RequestId: {RequestId}, RequestedAt: {RequestedAt}",
            request.RequestId, request.RequestedAt);

        // 서버 시간 생성
        var response = new ServerTimeResponse
        {
            RequestId = request.RequestId,
            Local = DateTime.Now,
            Utc = DateTime.UtcNow,
            ProcessedAt = DateTime.UtcNow,
            ProcessedBy = "kdt-consumer"
        };

        _logger.LogInformation("ServerTimeRequest processed. RequestId: {RequestId}", request.RequestId);

        // Wolverine이 자동으로 응답을 WebApi로 전송
        return response;
    }
}
