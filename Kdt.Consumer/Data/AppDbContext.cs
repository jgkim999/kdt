using Kdt.Share.Entities;
using Microsoft.EntityFrameworkCore;

namespace Kdt.Consumer.Data;

/// <summary>
/// 애플리케이션 데이터베이스 컨텍스트
/// </summary>
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    /// <summary>
    /// 사용자 테이블
    /// </summary>
    public DbSet<User> Users { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User 엔티티 인덱스 설정
        modelBuilder.Entity<User>()
            .HasIndex(u => u.UserId)
            .IsUnique()
            .HasDatabaseName("IX_users_user_id");
    }
}
