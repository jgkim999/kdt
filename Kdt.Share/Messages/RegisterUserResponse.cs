using System;

namespace Kdt.Share.Messages
{
    /// <summary>
    /// 사용자 등록 응답 메시지
    /// </summary>
    public class RegisterUserResponse
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
        /// 생성된 사용자 ID
        /// </summary>
        public int? UserId { get; set; }

        /// <summary>
        /// Consumer에서 처리된 시간
        /// </summary>
        public DateTime ProcessedAt { get; set; }

        /// <summary>
        /// Consumer 서비스 이름
        /// </summary>
        public string ProcessedBy { get; set; } = "kdt-consumer";
    }
}
