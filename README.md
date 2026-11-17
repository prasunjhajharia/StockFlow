# StockFlow

![CI](https://github.com/prasunjhajharia/StockFlow/actions/workflows/ci.yml/badge.svg)

An inventory and order management API. I built it to learn SQL properly, so most business rules live in PostgreSQL (functions, constraints, triggers) and the C# API mostly calls them. I used Dapper instead of EF Core on purpose, because I wanted to write the SQL myself and see exactly what runs.

![API docs](docs/scalar.png)

## What it does

- Products, warehouses, suppliers and customers
- Purchase orders (receiving adds stock) and sales orders (shipping removes stock)
- A stock ledger: every change is a row, and rows can never be edited or deleted
- Reports: low stock, stock value per warehouse, monthly sales
- JWT login with refresh tokens, and three roles (Admin, Manager, Staff)

## The parts I'm happiest with

**No overselling.** The stock function locks the row (`SELECT ... FOR UPDATE`), so if two people buy the last item at once, the second waits and then sees the real quantity. A test sends 25 buyers at 10 items: exactly 10 succeed and stock ends at 0.

**All or nothing orders.** Shipping an order with several lines ships everything or nothing. If one line is short, the whole thing rolls back.

**Refresh tokens.** Each token works once. If an old one is reused, I treat it as stolen and revoke all of that user's tokens. Only hashes are stored, never the tokens or passwords.

**Last admin protection.** The database refuses to remove or deactivate the last active Admin, even if the API has a bug.

## Tech

C# / .NET 10, ASP.NET Core Web API, PostgreSQL 18, Npgsql, Dapper, xUnit. CI runs on GitHub Actions.

## Folders

```
src/StockFlow.Api      controllers, auth, error handling
src/StockFlow.Core     models, interfaces, exceptions
src/StockFlow.Data     repositories (Dapper + SQL)
tests/StockFlow.Tests  SQL, concurrency and API tests
database/              numbered SQL files, run in order
```

## Running it

1. Install the .NET 10 SDK and PostgreSQL 18, then create two databases: `stockflow` and `stockflow_test`.
2. On `stockflow`, run `database/001_*.sql` to `009_*.sql` in order. Optionally run `seed_dev.sql` for demo data.
3. Create the first Admin with `database/create_admin.sql.example`. There is no public registration, so a new database starts with no users.
4. Set your secrets (they are not stored in the repo):

```
dotnet user-secrets set "ConnectionStrings:StockFlow" "Host=localhost;Port=5432;Database=stockflow;Username=postgres;Password=YOUR_PASSWORD" --project src/StockFlow.Api
dotnet user-secrets set "Jwt:Key" "A-RANDOM-STRING-OF-AT-LEAST-40-CHARACTERS" --project src/StockFlow.Api
dotnet user-secrets set "ConnectionStrings:StockFlowTest" "Host=localhost;Port=5432;Database=stockflow_test;Username=postgres;Password=YOUR_PASSWORD" --project tests/StockFlow.Tests
```

5. Start it with `dotnet run --project src/StockFlow.Api`. The API runs at http://localhost:5044, and the interactive docs are at http://localhost:5044/scalar/v1 (development only).

## Tests

`dotnet test` runs 13 tests against the `stockflow_test` database:

- **SQL and concurrency (6):** ledger rules, no overselling, all-or-nothing orders
- **API (7):** 401 without a token, 403 for the wrong role, 409 on duplicates, refresh token reuse, account lockout

The database is rebuilt from the SQL files on every run, and the tests refuse to touch any database whose name doesn't end in `_test`.

## Endpoints

| Area | Endpoints |
|---|---|
| Auth | `POST /api/auth/login`, `/refresh`, `/logout`, `GET /api/auth/me` |
| Users (Admin) | `GET/POST /api/users`, `PUT /api/users/{id}/role`, `/active` |
| Catalog | `/api/products`, `/api/warehouses`, `/api/suppliers`, `/api/customers` |
| Stock | `GET /api/stock`, `GET /api/stock/movements/{productId}`, `POST /api/stock/adjust` |
| Purchasing | `/api/purchase-orders` (create, `/order`, `/receive`, `/cancel`) |
| Sales | `/api/sales-orders` (create, `/confirm`, `/ship`, `/cancel`) |
| Reports | `/api/reports/low-stock`, `/stock-value`, `/sales-summary` |

More example requests are in the `.http` files in `src/StockFlow.Api`.

