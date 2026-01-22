using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Kdt.Share.Entities
{
    /// <summary>
    /// 사용자 엔티티
    /// </summary>
    [Table("users")]
    public class User
    {
        /// <summary>
        /// 사용자 고유 ID
        /// </summary>
        [Key]
        [Column("id")]
        public int Id { get; set; }

        /// <summary>
        /// 사용자 아이디
        /// </summary>
        [Required]
        [MaxLength(50)]
        [Column("user_id")]
        public string UserId { get; set; } = string.Empty;

        /// <summary>
        /// 비밀번호 해시
        /// </summary>
        [Required]
        [MaxLength(256)]
        [Column("password_hash")]
        public string PasswordHash { get; set; } = string.Empty;

        /// <summary>
        /// 생성 시간
        /// </summary>
        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// 수정 시간
        /// </summary>
        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
