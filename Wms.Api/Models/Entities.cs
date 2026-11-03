namespace Wms.Api.Models;

public sealed class Category
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
}

public sealed class Warehouse
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string? Location { get; set; }
}

public sealed class Product
{
    public int Id { get; set; }
    public string Sku { get; set; } = "";
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public decimal UnitCost { get; set; }
    public int? CategoryId { get; set; }
    public Category? Category { get; set; }
}

public sealed class Inventory
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public Product? Product { get; set; }
    public int WarehouseId { get; set; }
    public Warehouse? Warehouse { get; set; }
    public decimal Quantity { get; set; }
}

public sealed class InventoryMovement
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public Product? Product { get; set; }
    public int WarehouseId { get; set; }
    public Warehouse? Warehouse { get; set; }
    public string Type { get; set; } = "IN";
    public decimal Quantity { get; set; }
    public string? Reference { get; set; }
    public DateTime CreatedAt { get; set; }
}

public sealed class MovementRequest
{
    public int ProductId { get; set; }
    public int WarehouseId { get; set; }
    public string Type { get; set; } = "IN";
    public decimal Quantity { get; set; }
    public string? Reference { get; set; }
}
