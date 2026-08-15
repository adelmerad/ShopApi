# ShopApi

API REST e-commerce développée pendant mon stage chez Mobilis (encadrant : Ibrahim).

Depuis l'intégration du SSO, ShopApi est un **serveur de ressources** : il ne gère pas de login lui-même, il **valide les access tokens** émis par le serveur d'authentification (**ShopAuth**) et protège ses endpoints d'écriture.

## Stack

- ASP.NET Core 8 (Minimal API)
- OpenIddict 5 (Validation) — validation des tokens OAuth2 / OIDC
- Entity Framework Core 8 — Code-First + Migrations
- ASP.NET Core Identity
- SQL Server 2022 (Docker)
- Swagger

## Authentification (resource server)

- ShopApi ne connaît **aucun secret partagé** : il récupère la **clé publique** du serveur d'auth via son `jwks_uri`.
- Il n'accepte que les tokens dont l'`issuer` = `http://localhost:5124/` **et** l'`aud` contient `shop_api`.
- Endpoints de **lecture** (`GET`) publics ; endpoints d'**écriture** (`POST`) et `/api/me` protégés par `[Authorize]`.

> Prérequis : le serveur d'auth (**ShopAuth**) doit tourner sur `http://localhost:5124` (pour la découverte OIDC et les clés).

## Lancer le projet

1. Démarrer SQL Server :

```powershell
docker start sqlserver
```

2. Configurer la chaîne de connexion (User Secrets) :

```powershell
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost,1433;Database=ShopDb;User Id=sa;Password=***;TrustServerCertificate=True"
```

3. Appliquer les migrations :

```powershell
dotnet ef database update
```

4. Démarrer **ShopAuth** (port 5124), puis lancer ShopApi (F5) → Swagger s'ouvre (port 5050).

## Tester dans Swagger

1. **Authorize 🔒** → saisir le username/password d'un compte du serveur d'auth, cocher **`openid` + `shop_api`** (le `client_id` `postman` est pré-rempli, `client_secret` vide).
2. `GET /api/me` → **200** + les claims du token ; sans token → **401**.

## Endpoints

| Méthode | Route | Auth |
|---|---|---|
| GET | `/api/categories` | Public |
| POST | `/api/categories` | Bearer (`shop_api`) |
| GET | `/api/products` | Public |
| GET | `/api/products/{id}` | Public |
| POST | `/api/products` | Bearer (`shop_api`) |
| GET | `/api/me` | Bearer (`shop_api`) |

## Roadmap

- [x] Entités + migrations (Category, Product)
- [x] Identity (ApplicationUser)
- [x] Endpoints CRUD de base
- [x] SSO avec OpenIddict (resource server)
- [ ] DTOs
- [ ] Rôles / autorisations fines
