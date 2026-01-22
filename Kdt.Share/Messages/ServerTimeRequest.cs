using System;

namespace Kdt.Share.Messages
{
    /// <summary>
    /// 서버 시간 요청 메시지
    /// </summary>
    public class ServerTimeRequest
    {
        /// <summary>
        /// 요청 ID (상관관계 추적용)
        /// </summary>
        public Guid RequestId { get; set; } = Guid.NewGuid();

        /// <summary>
        /// 요청 시간
        /// </summary>
        public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
    }
}
