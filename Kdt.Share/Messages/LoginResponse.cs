using System;

namespace Kdt.Share.Messages
{
    /// <summary>
    /// 로그인 응답 메시지
    /// </summary>
    public class LoginResponse
    {
        /// <summary>
        /// 요청 ID (상관관계 추적용)
        /// </summary>
        public Guid RequestId { get; set; }

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
        /// 데이터베이스 사용자 ID (로그인 성공 시)
        /// </summary>
        public int? DbUserId { get; set; }

        /// <summary>
        /// Consumer에서 처리된 시간
        /// </summary>
        public DateTime ProcessedAt { get; set; }

        /// <summary>
        /// Consumer 서비스 이름
        /// </summary>
        public string ProcessedBy { get; set; } = "kdt-consumer";

        /// <summary>
        /// 캐시에서 조회되었는지 여부
        /// </summary>
        public bool FromCache { get; set; }
    }
}
