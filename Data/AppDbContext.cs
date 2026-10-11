using Microsoft.EntityFrameworkCore;
using Signal_CRUD_Demo_Backend.Models;

namespace Signal_CRUD_Demo_Backend.Data;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<PostEntity> Posts => Set<PostEntity>();
    public DbSet<UserEntity> Users => Set<UserEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PostEntity>(entity =>
        {
            entity.ToTable("Posts");
            entity.HasKey(post => post.Id);
            entity.Property(post => post.Title).IsRequired().HasMaxLength(200);
            entity.Property(post => post.Body).IsRequired().HasMaxLength(10000);
        });

        modelBuilder.Entity<UserEntity>(entity =>
        {
            entity.ToTable("Users");
            entity.HasKey(user => user.Id);
            entity.Property(user => user.Email).IsRequired().HasMaxLength(254);
            entity.HasIndex(user => user.Email).IsUnique();
            entity.Property(user => user.PasswordHash).IsRequired().HasMaxLength(512);
            entity.Property(user => user.DateOfBirth).IsRequired();
        });
    }
}
