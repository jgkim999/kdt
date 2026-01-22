using System.Diagnostics;
using System.Text.Json;
using FastEndpoints;
using Kdt.Share.Messages;
using Kdt.Share.Models;
using StackExchange.Redis;
using Wolverine;

namespace Kdt.WebApi.Endpoints;

/// <summary>
/// 로그인 요청 DTO
/// </summary>
public class LoginRequestDto
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
/// 로그인 응답 DTO
/// </summary>
public class LoginResponseDto
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
    /// 사용자 ID (로그인 성공 시)
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// 캐시에서 조회되었는지 여부
    /// </summary>
    public bool FromCache { get; set; }
}

public class LoginEndpointSummary : Summary<LoginEndpoint>
{
    public LoginEndpointSummary()
    {
        Summary = "사용자 로그인 (Valkey Cache + RabbitMQ)";
        Description = "Valkey 캐시를 먼저 확인하고, 없으면 RabbitMQ를 통해 Consumer에서 MySQL을 조회합니다.";
        Response<LoginResponseDto>(200, "로그인 성공");
        Response<LoginResponseDto>(401, "로그인 실패 (잘못된 자격 증명)");
        Response<LoginResponseDto>(400, "잘못된 요청");
        Response<ProblemDetails>(500, "서버 오류");
    }
}

public class LoginEndpoint : Endpoint<LoginRequestDto, LoginResponseDto>
{
    private static readonly ActivitySource ActivitySource = new("Kdt.WebApi");
    private readonly ILogger<LoginEndpoint> _logger;
    private readonly IMessageBus _messageBus;
    private readonly IConnectionMultiplexer _redis;

    public LoginEndpoint(ILogger<LoginEndpoint> logger, IMessageBus messageBus, IConnectionMultiplexer redis)
    {
        _logger = logger;
        _messageBus = messageBus;
        _redis = redis;
    }

    public override void Configure()
    {
        Post("/auth/login");
        AllowAnonymous();
        Summary(new LoginEndpointSummary());
    }

    public override async Task HandleAsync(LoginRequestDto req, CancellationToken ct)
    {
        using var activity = ActivitySource.StartActivity("LoginEndpoint.HandleAsync", ActivityKind.Producer);

        _logger.LogInformation("LoginEndpoint called. UserId: {UserId}, Remote: {Remote}",
            req.UserId, HttpContext.Connection.RemoteIpAddress);

        // 입력 검증
        if (string.IsNullOrWhiteSpace(req.UserId) || string.IsNullOrWhiteSpace(req.Password))
        {
            _logger.LogWarning("Invalid login request. UserId or Password is empty");

            HttpContext.Response.StatusCode = 400;
            Response = new LoginResponseDto
            {
                Success = false,
                Message = "UserId and Password are required"
            };
            return;
        }

        activity?.SetTag("user.id", req.UserId);

        try
        {
            var db = _redis.GetDatabase();
            var cacheKey = $"user:login:{req.UserId}";

            // 1. Valkey 캐시에서 먼저 확인
            RedisValue cachedData;
            using (var redisActivity = ActivitySource.StartActivity("Redis GET", ActivityKind.Client))
            {
                redisActivity?.SetTag("db.system", "redis");
                redisActivity?.SetTag("db.operation", "GET");
                redisActivity?.SetTag("db.redis.key", cacheKey);

                cachedData = await db.StringGetAsync(cacheKey);

                redisActivity?.SetTag("db.redis.hit", !cachedData.IsNullOrEmpty);
            }

            if (!cachedData.IsNullOrEmpty)
            {
                _logger.LogInformation("User found in cache. UserId: {UserId}", req.UserId);
                activity?.SetTag("cache.hit", true);

                var cachedUserInfo = JsonSerializer.Deserialize<CachedUserInfo>(cachedData.ToString());

                if (cachedUserInfo != null)
                {
                    // 비밀번호 확인
                    var isPasswordValid = BCrypt.Net.BCrypt.Verify(req.Password, cachedUserInfo.PasswordHash);

                    if (isPasswordValid)
                    {
                        _logger.LogInformation("Login successful from cache. UserId: {UserId}", req.UserId);
                        activity?.SetTag("login.result", "success");
                        activity?.SetTag("login.source", "cache");

                        HttpContext.Response.StatusCode = 200;
                        Response = new LoginResponseDto
                        {
                            Success = true,
                            Message = "Login successful (from cache)",
                            UserId = req.UserId,
                            FromCache = true
                        };
                        return;
                    }
                    else
                    {
                        _logger.LogWarning("Invalid password from cache. UserId: {UserId}", req.UserId);
                        activity?.SetTag("login.result", "invalid_password");

                        HttpContext.Response.StatusCode = 401;
                        Response = new LoginResponseDto
                        {
                            Success = false,
                            Message = "Invalid credentials",
                            FromCache = true
                        };
                        return;
                    }
                }
            }

            _logger.LogInformation("User not found in cache, querying via RabbitMQ. UserId: {UserId}", req.UserId);
            activity?.SetTag("cache.hit", false);

            // 2. 캐시에 없으면 RabbitMQ를 통해 Consumer에 요청
            var request = new LoginRequest
            {
                RequestId = Guid.NewGuid(),
                UserId = req.UserId,
                Password = req.Password,
                RequestedAt = DateTime.UtcNow
            };

            activity?.SetTag("messaging.system", "rabbitmq");
            activity?.SetTag("messaging.destination", "login-requests");
            activity?.SetTag("messaging.operation", "send");
            activity?.SetTag("request.id", request.RequestId.ToString());

            _logger.LogInformation("Sending LoginRequest to RabbitMQ. RequestId: {RequestId}, UserId: {UserId}",
                request.RequestId, request.UserId);

            // Consumer로부터 응답을 받기 위해 InvokeAsync 사용 (Request-Reply 패턴)
            var messageResponse = await _messageBus.InvokeAsync<LoginResponse>(request, ct);

            // 응답 수신 정보 추가
            activity?.SetTag("response.received", true);
            activity?.SetTag("response.id", messageResponse.RequestId.ToString());
            activity?.SetTag("response.success", messageResponse.Success);
            activity?.SetTag("response.processed_by", messageResponse.ProcessedBy);
            activity?.SetTag("response.from_cache", messageResponse.FromCache);

            _logger.LogInformation("Received LoginResponse from Consumer. RequestId: {RequestId}, Success: {Success}, FromCache: {FromCache}",
                messageResponse.RequestId, messageResponse.Success, messageResponse.FromCache);

            // 응답 DTO로 변환하여 반환
            HttpContext.Response.StatusCode = messageResponse.Success ? 200 : 401;
            Response = new LoginResponseDto
            {
                Success = messageResponse.Success,
                Message = messageResponse.Message,
                UserId = messageResponse.UserId,
                FromCache = messageResponse.FromCache
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process login. UserId: {UserId}", req.UserId);

            activity?.SetTag("error", true);
            activity?.SetTag("error.message", ex.Message);

            HttpContext.Response.StatusCode = 500;
            Response = new LoginResponseDto
            {
                Success = false,
                Message = $"Failed to process login: {ex.Message}"
            };
        }
    }
}
