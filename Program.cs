using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ShopApi.Data;
using ShopApi.Entities.Idendity;
using ShopApi.Entities.Sales;


var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AplicationDBContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
    .AddEntityFrameworkStores<AplicationDBContext>()
    .AddDefaultTokenProviders();


builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.ReferenceHandler =
        System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthentication();
//app.UseAuthorization();

app.MapGet("api/categories", async (AplicationDBContext db) =>
    await db.Categories.ToListAsync());
app.MapPost("/api/categories", async (Category category, AplicationDBContext db) =>
{
    db.Categories.Add(category);
    await db.SaveChangesAsync();
    return Results.Created($"/api/categories/{category.Id}", category);
});


app.MapGet("/api/products", async (AplicationDBContext db) =>
    await db.Products.Include(p => p.Category).ToListAsync());

app.MapGet("/api/products/{id}", async (int id, AplicationDBContext db) =>
{
    var product = await db.Products
    .Include(p => p.Category)
    .FirstOrDefaultAsync(p => p.Id == id);

    return product is not null ? Results.Ok(product) : Results.NotFound();
});

app.MapPost("/api/products", async (Product product, AplicationDBContext db) =>
{
    db.Products.Add(product);
    await db.SaveChangesAsync();
    return Results.Created($"/api/products/{product.Id}", product);
});

app.Run();