# ShopApi

API REST e-commerce développée pendant mon stage chez Mobilis (encadrant : Ibrahim).

## Stack
- ASP.NET Core 8 (Minimal API)
- Entity Framework Core 8 — Code-First + Migrations
- ASP.NET Core Identity
- SQL Server 2022 (Docker)
- Swagger

## Lancer le projet

1. Démarrer SQL Server :
   docker start sqlserver

2. Configurer la chaîne de connexion (User Secrets) :
   dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost,1433;Database=ShopDb;User Id=sa;Password=***;TrustServerCertificate=True"

3. Appliquer les migrations :
   dotnet ef database update

4. F5 → Swagger s'ouvre.

## Roadmap
- [x] Entités + migrations (Category, Product)
- [x] Identity (ApplicationUser)
- [x] Endpoints CRUD de base
- [ ] Register / Login / Logout / Change password
- [ ] DTOs
- [ ] SSO avec OpenIddict
