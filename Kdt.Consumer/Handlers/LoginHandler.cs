using System.Diagnostics;
using System.Text.Json;
using Kdt.Consumer.Data;
using Kdt.Share.Messages;
using Kdt.Share.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace Kdt.Consumer.Handlers;

/// <summary>
/// 로그인 요청을 처리하는 핸들러
/// </summary>
public class LoginHandler
{
    private static readonly ActivitySource ActivitySource = new("Kdt.Consumer");
    private readonly AppDbContext _dbContext;
    private readonly ILogger<LoginHandler> _logger;
    private readonly IConnectionMultiplexer _redis;

    public LoginHandler(AppDbContext dbContext, ILogger<LoginHandler> logger, IConnectionMultiplexer redis)
    {
        _dbContext = dbContext;
        _logger = logger;
        _redis = redis;
    }

    /// <summary>
    /// 로그인 요청을 처리합니다.
    /// Wolverine이 자동으로 이 메서드를 검색하고 호출합니다.
    /// </summary>
    public async Task<LoginResponse> Handle(LoginRequest request)
    {
        using var activity = ActivitySource.StartActivity("LoginRequest.Handle", ActivityKind.Consumer);

        activity?.SetTag("messaging.system", "rabbitmq");
        activity?.SetTag("messaging.destination", "login-requests");
        activity?.SetTag("request.id", request.RequestId.ToString());
        activity?.SetTag("user.id", request.UserId);

        _logger.LogInformation("Processing login request. RequestId: {RequestId}, UserId: {UserId}",
            request.RequestId, request.UserId);

        try
        {
            // MySQL에서 사용자 조회
            var user = await _dbContext.Users
                .FirstOrDefaultAsync(u => u.UserId == request.UserId);

            if (user == null)
            {
                _logger.LogWarning("User not found. UserId: {UserId}", request.UserId);
                activity?.SetTag("login.result", "user_not_found");

                return new LoginResponse
                {
                    RequestId = request.RequestId,
                    Success = false,
                    Message = "Invalid credentials",
                    ProcessedAt = DateTime.UtcNow,
                    FromCache = false
                };
            }

            // 비밀번호 검증
            var isPasswordValid = BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash);

            if (!isPasswordValid)
            {
                _logger.LogWarning("Invalid password. UserId: {UserId}", request.UserId);
                activity?.SetTag("login.result", "invalid_password");

                return new LoginResponse
                {
                    RequestId = request.RequestId,
                    Success = false,
                    Message = "Invalid credentials",
                    ProcessedAt = DateTime.UtcNow,
                    FromCache = false
                };
            }

            _logger.LogInformation("Login successful from database. UserId: {UserId}, DbId: {DbId}",
                user.UserId, user.Id);

            // 로그인 성공 시 Valkey에 캐시 저장 (5분 TTL)
            await CacheUserInfoAsync(user);

            activity?.SetTag("login.result", "success");
            activity?.SetTag("login.source", "database");
            activity?.SetTag("user.db_id", user.Id);

            return new LoginResponse
            {
                RequestId = request.RequestId,
                Success = true,
                Message = "Login successful",
                UserId = user.UserId,
                DbUserId = user.Id,
                ProcessedAt = DateTime.UtcNow,
                FromCache = false
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process login. RequestId: {RequestId}, UserId: {UserId}",
                request.RequestId, request.UserId);

            activity?.SetTag("login.result", "error");
            activity?.SetTag("error.message", ex.Message);

            return new LoginResponse
            {
                RequestId = request.RequestId,
                Success = false,
                Message = $"Failed to process login: {ex.Message}",
                ProcessedAt = DateTime.UtcNow,
                FromCache = false
            };
        }
    }

    /// <summary>
    /// 사용자 정보를 Valkey에 캐시합니다 (5분 TTL)
    /// </summary>
    private async Task CacheUserInfoAsync(Kdt.Share.Entities.User user)
    {
        try
        {
            var db = _redis.GetDatabase();
            var cacheKey = $"user:login:{user.UserId}";

            var cachedUserInfo = new CachedUserInfo
            {
                DbUserId = user.Id,
                UserId = user.UserId,
                PasswordHash = user.PasswordHash,
                CachedAt = DateTime.UtcNow
            };

            var serialized = JsonSerializer.Serialize(cachedUserInfo);
            var expiry = TimeSpan.FromMinutes(5); // 5분 TTL

            // Redis SET 작업을 Activity로 추적
            using (var redisActivity = ActivitySource.StartActivity("Redis SET", ActivityKind.Client))
            {
                redisActivity?.SetTag("db.system", "redis");
                redisActivity?.SetTag("db.operation", "SET");
                redisActivity?.SetTag("db.redis.key", cacheKey);
                redisActivity?.SetTag("db.redis.ttl", expiry.TotalSeconds);

                await db.StringSetAsync(cacheKey, serialized, expiry);
            }

            _logger.LogInformation("User info cached in Valkey. UserId: {UserId}, Expiry: {Expiry}",
                user.UserId, expiry);
        }
        catch (Exception ex)
        {
            // 캐시 실패는 로그만 남기고 진행 (캐시는 선택사항)
            _logger.LogError(ex, "Failed to cache user info. UserId: {UserId}", user.UserId);
        }
    }
}
