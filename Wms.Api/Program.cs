using Microsoft.EntityFrameworkCore;
using Wms.Api.Data;
using Wms.Api.Models;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("WmsDb")
    ?? throw new InvalidOperationException("No se encontró ConnectionStrings:WmsDb.");

builder.Services.AddDbContext<WmsDbContext>(options => options.UseNpgsql(connectionString));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapGet("/api/health", () => Results.Ok(new { status = "OK", application = "WMS Básico" }));

// Categorías
app.MapGet("/api/categories", async (WmsDbContext db) =>
    Results.Ok(await db.Categories.OrderBy(x => x.Name).ToListAsync()));

app.MapPost("/api/categories", async (Category input, WmsDbContext db) =>
{
    if (string.IsNullOrWhiteSpace(input.Name)) return Results.BadRequest("El nombre es obligatorio.");
    var item = new Category { Name = input.Name.Trim() };
    db.Categories.Add(item);
    await db.SaveChangesAsync();
    return Results.Created($"/api/categories/{item.Id}", item);
});

// Bodegas
app.MapGet("/api/warehouses", async (WmsDbContext db) =>
    Results.Ok(await db.Warehouses.OrderBy(x => x.Name).ToListAsync()));

app.MapPost("/api/warehouses", async (Warehouse input, WmsDbContext db) =>
{
    if (string.IsNullOrWhiteSpace(input.Name)) return Results.BadRequest("El nombre es obligatorio.");
    var item = new Warehouse { Name = input.Name.Trim(), Location = input.Location?.Trim() };
    db.Warehouses.Add(item);
    await db.SaveChangesAsync();
    return Results.Created($"/api/warehouses/{item.Id}", item);
});

// Productos
app.MapGet("/api/products", async (WmsDbContext db) =>
    Results.Ok(await db.Products.Include(x => x.Category).OrderBy(x => x.Name).ToListAsync()));

app.MapPost("/api/products", async (Product input, WmsDbContext db) =>
{
    if (string.IsNullOrWhiteSpace(input.Sku) || string.IsNullOrWhiteSpace(input.Name))
        return Results.BadRequest("SKU y nombre son obligatorios.");
    if (input.UnitCost < 0) return Results.BadRequest("El costo no puede ser negativo.");
    if (await db.Products.AnyAsync(x => x.Sku == input.Sku.Trim()))
        return Results.Conflict("Ya existe un producto con ese SKU.");
    if (input.CategoryId is not null && !await db.Categories.AnyAsync(x => x.Id == input.CategoryId))
        return Results.BadRequest("La categoría indicada no existe.");

    var item = new Product {
        Sku = input.Sku.Trim(), Name = input.Name.Trim(),
        Description = input.Description?.Trim(), UnitCost = input.UnitCost,
        CategoryId = input.CategoryId
    };
    db.Products.Add(item);
    await db.SaveChangesAsync();
    return Results.Created($"/api/products/{item.Id}", item);
});

app.MapPut("/api/products/{id:int}", async (int id, Product input, WmsDbContext db) =>
{
    var item = await db.Products.FindAsync(id);
    if (item is null) return Results.NotFound();
    if (string.IsNullOrWhiteSpace(input.Sku) || string.IsNullOrWhiteSpace(input.Name))
        return Results.BadRequest("SKU y nombre son obligatorios.");
    if (await db.Products.AnyAsync(x => x.Id != id && x.Sku == input.Sku.Trim()))
        return Results.Conflict("Ya existe otro producto con ese SKU.");
    item.Sku = input.Sku.Trim();
    item.Name = input.Name.Trim();
    item.Description = input.Description?.Trim();
    item.UnitCost = input.UnitCost;
    item.CategoryId = input.CategoryId;
    await db.SaveChangesAsync();
    return Results.Ok(item);
});

app.MapDelete("/api/products/{id:int}", async (int id, WmsDbContext db) =>
{
    var item = await db.Products.FindAsync(id);
    if (item is null) return Results.NotFound();
    if (await db.Inventory.AnyAsync(x => x.ProductId == id))
        return Results.Conflict("No se puede eliminar: el producto tiene registros de inventario.");
    db.Products.Remove(item);
    await db.SaveChangesAsync();
    return Results.NoContent();
});

// Inventario actual por producto y bodega
app.MapGet("/api/inventory", async (WmsDbContext db) =>
{
    var rows = await db.Inventory
        .Include(x => x.Product).Include(x => x.Warehouse)
        .OrderBy(x => x.Product!.Name).ThenBy(x => x.Warehouse!.Name)
        .Select(x => new {
            x.Id, ProductId = x.ProductId, Sku = x.Product!.Sku,
            Product = x.Product.Name, WarehouseId = x.WarehouseId,
            Warehouse = x.Warehouse!.Name, x.Quantity
        }).ToListAsync();
    return Results.Ok(rows);
});

// Registrar entrada o salida. El movimiento y el saldo se actualizan en una transacción.
app.MapPost("/api/movements", async (MovementRequest input, WmsDbContext db) =>
{
    if (input.ProductId <= 0 || input.WarehouseId <= 0 || input.Quantity <= 0)
        return Results.BadRequest("Producto, bodega y cantidad deben ser válidos.");
    if (input.Type is not ("IN" or "OUT"))
        return Results.BadRequest("Tipo debe ser IN (entrada) u OUT (salida).");
    if (!await db.Products.AnyAsync(x => x.Id == input.ProductId))
        return Results.BadRequest("El producto no existe.");
    if (!await db.Warehouses.AnyAsync(x => x.Id == input.WarehouseId))
        return Results.BadRequest("La bodega no existe.");

    await using var tx = await db.Database.BeginTransactionAsync();
    var stock = await db.Inventory.SingleOrDefaultAsync(x =>
        x.ProductId == input.ProductId && x.WarehouseId == input.WarehouseId);

    if (stock is null)
    {
        stock = new Inventory { ProductId = input.ProductId, WarehouseId = input.WarehouseId, Quantity = 0 };
        db.Inventory.Add(stock);
    }

    if (input.Type == "OUT" && stock.Quantity < input.Quantity)
        return Results.BadRequest("Existencia insuficiente para realizar la salida.");

    stock.Quantity += input.Type == "IN" ? input.Quantity : -input.Quantity;
    var movement = new InventoryMovement {
        ProductId = input.ProductId, WarehouseId = input.WarehouseId,
        Type = input.Type, Quantity = input.Quantity,
        Reference = input.Reference?.Trim(), CreatedAt = DateTime.UtcNow
    };
    db.Movements.Add(movement);
    await db.SaveChangesAsync();
    await tx.CommitAsync();
    return Results.Ok(new { message = "Movimiento registrado.", movement.Id, stock.Quantity });
});

app.MapGet("/api/movements", async (WmsDbContext db) =>
    Results.Ok(await db.Movements.Include(x => x.Product).Include(x => x.Warehouse)
        .OrderByDescending(x => x.CreatedAt).Take(200)
        .Select(x => new {
            x.Id, x.CreatedAt, x.Type, x.Quantity, x.Reference,
            Sku = x.Product!.Sku, Product = x.Product.Name, Warehouse = x.Warehouse!.Name
        }).ToListAsync()));

// Inicializa el esquema y datos de demostración para facilitar el primer arranque.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<WmsDbContext>();
    await db.Database.EnsureCreatedAsync();
    if (!await db.Categories.AnyAsync())
    {
        db.Categories.AddRange(new Category { Name = "General" }, new Category { Name = "Insumos" });
        await db.SaveChangesAsync();
    }
    if (!await db.Warehouses.AnyAsync())
    {
        db.Warehouses.Add(new Warehouse { Name = "Bodega principal", Location = "Principal" });
        await db.SaveChangesAsync();
    }
}

app.MapFallbackToFile("index.html");
app.Run();
