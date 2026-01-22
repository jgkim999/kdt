using System.Diagnostics;
using FastEndpoints;
using Kdt.Share.Messages;
using Wolverine;

namespace Kdt.WebApi.Endpoints;

/// <summary>
/// 사용자 등록 요청 DTO
/// </summary>
public class RegisterUserRequestDto
{
    /// <summary>
    /// 사용자 아이디
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// 비밀번호
    /// </summary>
    public string Password { get; set; } = string.Empty;
}

/// <summary>
/// 사용자 등록 응답 DTO
/// </summary>
public class RegisterUserResponseDto
{
    /// <summary>
    /// 성공 여부
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// 메시지
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// 생성된 사용자 ID
    /// </summary>
    public int? UserId { get; set; }
}

public class RegisterUserEndpointSummary : Summary<RegisterUserEndpoint>
{
    public RegisterUserEndpointSummary()
    {
        Summary = "사용자 등록 (via RabbitMQ)";
        Description = "RabbitMQ를 통해 Consumer에서 사용자를 등록합니다.";
        Response<RegisterUserResponseDto>(200, "사용자 등록 성공");
        Response<RegisterUserResponseDto>(400, "사용자 등록 실패 (중복 등)");
        Response<ProblemDetails>(500, "서버 오류");
    }
}

public class RegisterUserEndpoint : Endpoint<RegisterUserRequestDto, RegisterUserResponseDto>
{
    private static readonly ActivitySource ActivitySource = new("Kdt.WebApi");
    private readonly ILogger<RegisterUserEndpoint> _logger;
    private readonly IMessageBus _messageBus;

    public RegisterUserEndpoint(ILogger<RegisterUserEndpoint> logger, IMessageBus messageBus)
    {
        _logger = logger;
        _messageBus = messageBus;
    }

    public override void Configure()
    {
        Post("/users/register");
        AllowAnonymous();
        Summary(new RegisterUserEndpointSummary());
    }

    public override async Task HandleAsync(RegisterUserRequestDto req, CancellationToken ct)
    {
        using var activity = ActivitySource.StartActivity("RegisterUserEndpoint.HandleAsync", ActivityKind.Producer);

        _logger.LogInformation("RegisterUserEndpoint called. UserId: {UserId}, Remote: {Remote}",
            req.UserId, HttpContext.Connection.RemoteIpAddress);

        // 입력 검증
        if (string.IsNullOrWhiteSpace(req.UserId) || string.IsNullOrWhiteSpace(req.Password))
        {
            _logger.LogWarning("Invalid registration request. UserId or Password is empty");

            HttpContext.Response.StatusCode = 400;
            Response = new RegisterUserResponseDto
            {
                Success = false,
                Message = "UserId and Password are required"
            };
            return;
        }

        // RegisterUserRequest 메시지를 RabbitMQ로 발행하고 응답 대기
        var request = new RegisterUserRequest
        {
            RequestId = Guid.NewGuid(),
            UserId = req.UserId,
            Password = req.Password,
            RequestedAt = DateTime.UtcNow
        };

        // 추적 정보 추가
        activity?.SetTag("messaging.system", "rabbitmq");
        activity?.SetTag("messaging.destination", "register-user-requests");
        activity?.SetTag("messaging.operation", "send");
        activity?.SetTag("request.id", request.RequestId.ToString());
        activity?.SetTag("user.id", request.UserId);

        _logger.LogInformation("Sending RegisterUserRequest to RabbitMQ. RequestId: {RequestId}, UserId: {UserId}",
            request.RequestId, request.UserId);

        try
        {
            // Consumer로부터 응답을 받기 위해 InvokeAsync 사용 (Request-Reply 패턴)
            var messageResponse = await _messageBus.InvokeAsync<RegisterUserResponse>(request, ct);

            // 응답 수신 정보 추가
            activity?.SetTag("response.received", true);
            activity?.SetTag("response.id", messageResponse.RequestId.ToString());
            activity?.SetTag("response.success", messageResponse.Success);
            activity?.SetTag("response.processed_by", messageResponse.ProcessedBy);

            _logger.LogInformation("Received RegisterUserResponse from Consumer. RequestId: {RequestId}, Success: {Success}",
                messageResponse.RequestId, messageResponse.Success);

            // 응답 DTO로 변환하여 반환
            HttpContext.Response.StatusCode = messageResponse.Success ? 200 : 400;
            Response = new RegisterUserResponseDto
            {
                Success = messageResponse.Success,
                Message = messageResponse.Message,
                UserId = messageResponse.UserId
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process user registration. RequestId: {RequestId}", request.RequestId);

            activity?.SetTag("error", true);
            activity?.SetTag("error.message", ex.Message);

            HttpContext.Response.StatusCode = 500;
            Response = new RegisterUserResponseDto
            {
                Success = false,
                Message = $"Failed to process registration: {ex.Message}"
            };
        }
    }
}
