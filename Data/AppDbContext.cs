using Microsoft.EntityFrameworkCore;
using Signal_CRUD_Demo_Backend.Models;

namespace Signal_CRUD_Demo_Backend.Data;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<PostEntity> Posts => Set<PostEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PostEntity>(entity =>
        {
            entity.ToTable("Posts");
            entity.HasKey(post => post.Id);
            entity.Property(post => post.Title).IsRequired().HasMaxLength(200);
            entity.Property(post => post.Body).IsRequired().HasMaxLength(10000);
        });
    }
}
