using System.Diagnostics;
using Kdt.Consumer.Data;
using Kdt.Share.Entities;
using Kdt.Share.Messages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Kdt.Consumer.Handlers;

/// <summary>
/// 사용자 등록 요청을 처리하는 핸들러
/// </summary>
public class RegisterUserHandler
{
    private static readonly ActivitySource ActivitySource = new("Kdt.Consumer");
    private readonly AppDbContext _dbContext;
    private readonly ILogger<RegisterUserHandler> _logger;

    public RegisterUserHandler(AppDbContext dbContext, ILogger<RegisterUserHandler> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <summary>
    /// 사용자 등록 요청을 처리합니다.
    /// Wolverine이 자동으로 이 메서드를 검색하고 호출합니다.
    /// </summary>
    public async Task<RegisterUserResponse> Handle(RegisterUserRequest request)
    {
        using var activity = ActivitySource.StartActivity("RegisterUserRequest.Handle", ActivityKind.Consumer);

        activity?.SetTag("messaging.system", "rabbitmq");
        activity?.SetTag("messaging.destination", "register-user-requests");
        activity?.SetTag("request.id", request.RequestId.ToString());
        activity?.SetTag("user.id", request.UserId);

        _logger.LogInformation("Processing user registration request. RequestId: {RequestId}, UserId: {UserId}",
            request.RequestId, request.UserId);

        try
        {
            // 기존 사용자 확인
            var existingUser = await _dbContext.Users
                .FirstOrDefaultAsync(u => u.UserId == request.UserId);

            if (existingUser != null)
            {
                _logger.LogWarning("User already exists. UserId: {UserId}", request.UserId);
                activity?.SetTag("registration.result", "duplicate");

                return new RegisterUserResponse
                {
                    RequestId = request.RequestId,
                    Success = false,
                    Message = $"User '{request.UserId}' already exists",
                    ProcessedAt = DateTime.UtcNow
                };
            }

            // 비밀번호 해싱
            var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

            // 새 사용자 생성
            var user = new User
            {
                UserId = request.UserId,
                PasswordHash = passwordHash,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _dbContext.Users.Add(user);
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("User registered successfully. UserId: {UserId}, Id: {Id}",
                user.UserId, user.Id);

            activity?.SetTag("registration.result", "success");
            activity?.SetTag("user.db_id", user.Id);

            return new RegisterUserResponse
            {
                RequestId = request.RequestId,
                Success = true,
                Message = "User registered successfully",
                UserId = user.Id,
                ProcessedAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to register user. RequestId: {RequestId}, UserId: {UserId}",
                request.RequestId, request.UserId);

            activity?.SetTag("registration.result", "error");
            activity?.SetTag("error.message", ex.Message);

            return new RegisterUserResponse
            {
                RequestId = request.RequestId,
                Success = false,
                Message = $"Failed to register user: {ex.Message}",
                ProcessedAt = DateTime.UtcNow
            };
        }
    }
}
