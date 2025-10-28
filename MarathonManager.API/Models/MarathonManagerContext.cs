using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Identity; // Thêm
using Microsoft.AspNetCore.Identity.EntityFrameworkCore; // Thêm
using Microsoft.EntityFrameworkCore;

namespace MarathonManager.API.Models;

// 1. Kế thừa từ IdentityDbContext
public partial class MarathonManagerContext : IdentityDbContext<User, IdentityRole<int>, int>
{
    public MarathonManagerContext()
    {
    }

    // Constructor này là đúng, giữ nguyên
    public MarathonManagerContext(DbContextOptions<MarathonManagerContext> options)
        : base(options)
    {
    }

    // Giữ nguyên các DbSet cho các bảng tùy chỉnh
    public virtual DbSet<BlogPost> BlogPosts { get; set; }
    public virtual DbSet<Comment> Comments { get; set; }
    public virtual DbSet<Like> Likes { get; set; }
    public virtual DbSet<Race> Races { get; set; }
    public virtual DbSet<RaceDistance> RaceDistances { get; set; }
    public virtual DbSet<Registration> Registrations { get; set; }
    public virtual DbSet<Result> Results { get; set; }

    // 2. Xóa DbSet<User> vì IdentityDbContext đã có sẵn
    // public virtual DbSet<User> Users { get; set; }

    // 3. Xóa phương thức OnConfiguring
    // (Vì bạn đã cấu hình chuỗi kết nối trong Program.cs)
    /*
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        => optionsBuilder.UseSqlServer("Name=ConnectionStrings:MyCnn");
    */

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // 4. BẮT BUỘC: Gọi base.OnModelCreating(modelBuilder) ĐẦU TIÊN
        //    Điều này sẽ tự động cấu hình các bảng Identity (AspNetUsers, AspNetRoles...)
        base.OnModelCreating(modelBuilder);

        // Giữ nguyên tất cả cấu hình cho các bảng tùy chỉnh của bạn

        modelBuilder.Entity<BlogPost>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__BlogPost__3214EC07B6AB6514");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.FeaturedImageUrl).HasMaxLength(500);
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValue("Draft");
            entity.Property(e => e.Title).HasMaxLength(200);

            entity.HasOne(d => d.Author).WithMany(p => p.BlogPosts)
                .HasForeignKey(d => d.AuthorId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__BlogPosts__Autho__571DF1D5");

            entity.HasOne(d => d.Race).WithMany(p => p.BlogPosts)
                .HasForeignKey(d => d.RaceId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK__BlogPosts__RaceI__5812160E");
        });

        modelBuilder.Entity<Comment>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Comments__3214EC070E5AC4C3");

            entity.Property(e => e.Content).HasMaxLength(1000);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");

            entity.HasOne(d => d.BlogPost).WithMany(p => p.Comments)
                .HasForeignKey(d => d.BlogPostId)
                .HasConstraintName("FK__Comments__BlogPo__5BE2A6F2");

            entity.HasOne(d => d.User).WithMany(p => p.Comments)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Comments__UserId__5CD6CB2B");
        });

        modelBuilder.Entity<Like>(entity =>
        {
            entity.HasKey(e => new { e.UserId, e.BlogPostId }).HasName("PK__Likes__94A9B85A8D4C34AF");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");

            entity.HasOne(d => d.BlogPost).WithMany(p => p.Likes)
                .HasForeignKey(d => d.BlogPostId)
                .HasConstraintName("FK__Likes__BlogPostI__619B8048");

            entity.HasOne(d => d.User).WithMany(p => p.Likes)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Likes__UserId__60A75C0F");
        });

        modelBuilder.Entity<Race>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Races__3214EC077BC504AA");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.ImageUrl).HasMaxLength(500);
            entity.Property(e => e.Location).HasMaxLength(300);
            entity.Property(e => e.Name).HasMaxLength(200);
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .HasDefaultValue("Pending");

            entity.HasOne(d => d.Organizer).WithMany(p => p.Races)
                .HasForeignKey(d => d.OrganizerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Races__Organizer__403A8C7D");
        });

        modelBuilder.Entity<RaceDistance>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__RaceDist__3214EC07666D34AC");

            entity.Property(e => e.DistanceInKm).HasColumnType("decimal(5, 2)");
            entity.Property(e => e.Name).HasMaxLength(100);
            entity.Property(e => e.RegistrationFee).HasColumnType("decimal(18, 2)");

            entity.HasOne(d => d.Race).WithMany(p => p.RaceDistances)
                .HasForeignKey(d => d.RaceId)
                .HasConstraintName("FK__RaceDista__RaceI__440B1D61");
        });

        modelBuilder.Entity<Registration>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Registra__3214EC07FDB65A78");

            entity.HasIndex(e => new { e.RunnerId, e.RaceDistanceId }, "UQ_Runner_RaceDistance").IsUnique();

            entity.Property(e => e.BibNumber).HasMaxLength(10);
            entity.Property(e => e.PaymentStatus)
                .HasMaxLength(50)
                .HasDefaultValue("Pending");
            entity.Property(e => e.RegistrationDate).HasDefaultValueSql("(getdate())");

            entity.HasOne(d => d.RaceDistance).WithMany(p => p.Registrations)
                .HasForeignKey(d => d.RaceDistanceId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Registrat__RaceD__4BAC3F29");

            entity.HasOne(d => d.Runner).WithMany(p => p.Registrations)
                .HasForeignKey(d => d.RunnerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Registrat__Runne__4AB81AF0");
        });

        modelBuilder.Entity<Result>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Results__3214EC07FB2FC1FD");

            entity.HasIndex(e => e.RegistrationId, "UQ__Results__6EF58811614E7D53").IsUnique();

            entity.Property(e => e.CompletionTime).HasPrecision(3);
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .HasDefaultValue("DidNotStart");

            entity.HasOne(d => d.Registration).WithOne(p => p.Result)
                .HasForeignKey<Result>(d => d.RegistrationId)
                .HasConstraintName("FK__Results__Registr__5165187F");
        });

        // 5. Xóa toàn bộ khối modelBuilder.Entity<User>
        /*
        modelBuilder.Entity<User>(entity =>
        {
            // ... (Tất cả cấu hình này giờ do Identity quản lý)
        });
        */

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}