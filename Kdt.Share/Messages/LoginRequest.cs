using System;

namespace Kdt.Share.Messages
{
    /// <summary>
    /// 로그인 요청 메시지
    /// </summary>
    public class LoginRequest
    {
        /// <summary>
        /// 요청 ID (상관관계 추적용)
        /// </summary>
        public Guid RequestId { get; set; } = Guid.NewGuid();

        /// <summary>
        /// 사용자 아이디
        /// </summary>
        public string UserId { get; set; } = string.Empty;

        /// <summary>
        /// 비밀번호
        /// </summary>
        public string Password { get; set; } = string.Empty;

        /// <summary>
        /// 요청 시간
        /// </summary>
        public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
    }
}
