using System;

namespace Kdt.Share.Response
{
    /// <summary>
    /// 현재 서버 시간을 반환하는 DTO
    /// </summary>
    public class ServerTimeResponse
    {
        /// <summary>
        /// 서버의 Local 시간
        /// </summary>
        public DateTime Local { get; set; }
        /// <summary>
        /// 서버의 Utc 시간
        /// </summary>
        public DateTime Utc { get; set; }
    }
}
