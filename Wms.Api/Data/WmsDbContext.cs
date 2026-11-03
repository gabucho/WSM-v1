using Microsoft.EntityFrameworkCore;
using Wms.Api.Models;

namespace Wms.Api.Data;

public sealed class WmsDbContext(DbContextOptions<WmsDbContext> options) : DbContext(options)
{
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Warehouse> Warehouses => Set<Warehouse>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Inventory> Inventory => Set<Inventory>();
    public DbSet<InventoryMovement> Movements => Set<InventoryMovement>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Category>().HasIndex(x => x.Name).IsUnique();
        modelBuilder.Entity<Product>().HasIndex(x => x.Sku).IsUnique();
        modelBuilder.Entity<Inventory>().HasIndex(x => new { x.ProductId, x.WarehouseId }).IsUnique();
        modelBuilder.Entity<Product>().Property(x => x.UnitCost).HasPrecision(18, 2);
        modelBuilder.Entity<Inventory>().Property(x => x.Quantity).HasPrecision(18, 3);
        modelBuilder.Entity<InventoryMovement>().Property(x => x.Quantity).HasPrecision(18, 3);
        modelBuilder.Entity<Product>().HasOne(x => x.Category).WithMany().HasForeignKey(x => x.CategoryId);
        modelBuilder.Entity<Inventory>().HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId);
        modelBuilder.Entity<Inventory>().HasOne(x => x.Warehouse).WithMany().HasForeignKey(x => x.WarehouseId);
        modelBuilder.Entity<InventoryMovement>().HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId);
        modelBuilder.Entity<InventoryMovement>().HasOne(x => x.Warehouse).WithMany().HasForeignKey(x => x.WarehouseId);
    }
}
