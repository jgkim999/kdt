using System;

namespace Kdt.Share.Messages
{
    /// <summary>
    /// 서버 시간 응답 메시지
    /// </summary>
    public class ServerTimeResponse
    {
        /// <summary>
        /// 요청 ID (상관관계 추적용)
        /// </summary>
        public Guid RequestId { get; set; }

        /// <summary>
        /// 로컬 시간
        /// </summary>
        public DateTime Local { get; set; }

        /// <summary>
        /// UTC 시간
        /// </summary>
        public DateTime Utc { get; set; }

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
