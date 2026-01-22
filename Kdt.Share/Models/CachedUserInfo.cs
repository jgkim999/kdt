using System;

namespace Kdt.Share.Models
{
    /// <summary>
    /// 캐시에 저장되는 사용자 정보
    /// </summary>
    public class CachedUserInfo
    {
        /// <summary>
        /// 데이터베이스 사용자 ID
        /// </summary>
        public int DbUserId { get; set; }

        /// <summary>
        /// 사용자 아이디
        /// </summary>
        public string UserId { get; set; } = string.Empty;

        /// <summary>
        /// 비밀번호 해시
        /// </summary>
        public string PasswordHash { get; set; } = string.Empty;

        /// <summary>
        /// 캐시에 저장된 시간
        /// </summary>
        public DateTime CachedAt { get; set; } = DateTime.UtcNow;
    }
}
