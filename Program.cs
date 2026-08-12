using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Validation.AspNetCore;
using ShopApi.Data;
using ShopApi.Entities.Idendity;
using ShopApi.Entities.Sales;


var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AplicationDBContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
    .AddEntityFrameworkStores<AplicationDBContext>()
    .AddDefaultTokenProviders();

// --- ShopApi = serveur de ressources : il VALIDE les tokens du serveur d'auth ---
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme;
});

builder.Services.AddOpenIddict()
    .AddValidation(options =>
    {
        // Le serveur d'auth qui a émis les tokens (doit matcher le claim "iss").
        options.SetIssuer("http://localhost:5124/");

        // On n'accepte QUE les tokens dont l'audience contient "shop_api".
        options.AddAudiences("shop_api");

        // Va chercher la config OIDC + les clés publiques (JWKS) via HTTP.
        options.UseSystemNetHttp();

        // Intègre la validation au pipeline ASP.NET Core.
        options.UseAspNetCore();
    });

builder.Services.AddAuthorization();


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
app.UseAuthorization();

// Endpoint de démo protégé : renvoie les claims du token présenté.
app.MapGet("/api/me", (ClaimsPrincipal user) =>
    Results.Ok(user.Claims.Select(c => new { c.Type, c.Value })))
   .RequireAuthorization();

app.MapGet("api/categories", async (AplicationDBContext db) =>
    await db.Categories.ToListAsync());
app.MapPost("/api/categories", async (Category category, AplicationDBContext db) =>
{
    db.Categories.Add(category);
    await db.SaveChangesAsync();
    return Results.Created($"/api/categories/{category.Id}", category);
}).RequireAuthorization();


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
}).RequireAuthorization();

app.Run();