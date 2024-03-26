using CqrsSqlServer.DataModel.Entities;
using Microsoft.EntityFrameworkCore;

namespace CqrsSqlServer.DataModel;

public class CqrsSqlServerContext : DbContext
{
    public DbSet<ProductListing> Products { get; set; }
    
    public const int ProductIdMaxLength = 128;
    public const int ProductNameMaxLength = 256;
    
    public CqrsSqlServerContext(DbContextOptions<CqrsSqlServerContext> options)
        : base(options)
    {
    }

    // The following configures EF to create a Sqlite database file in the
    // special "local" folder for your platform.
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.EnableDetailedErrors();
        base.OnConfiguring(optionsBuilder);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ProductListing>(entity =>
        {
            entity.Property(c => c.ProductId)
                .HasMaxLength(ProductIdMaxLength)
                .IsRequired();
            
            entity.Property(c => c.ProductName)
                .HasMaxLength(ProductNameMaxLength)
                .IsRequired();
            
            entity.HasKey(c => c.ProductId);
        });
    }
}