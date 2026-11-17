# StockFlow
![CI](https://github.com/prasunjhajharia/StockFlow/actions/workflows/ci.yml/badge.svg)

![API docs](docs/scalar.png)

An inventory and order management API. I built it to learn SQL properly, so most of the business rules live in PostgreSQL (functions, constraints, triggers) and the C# API mostly calls them.

I used Dapper instead of EF Core on purpose. I wanted to write the SQL myself and see exactly what runs.

## What it does

- Products, warehouses, suppliers and customers
- Purchase orders (receiving one adds stock) and sales orders (shipping one removes stock)
- A stock ledger: every change to stock is saved as a row, and rows can never be edited or deleted
- Reports: low stock, stock value per warehouse, monthly sales
- Login with JWT, refresh tokens, and three roles (Admin, Manager, Staff)

## The parts I'm happiest with

**Overselling.** Two people can try to buy the last item at the same moment. The stock function locks the row (`SELECT ... FOR UPDATE`), so the second buyer waits and then sees the real quantity. There is a test that sends 25 buyers at 10 items. Exactly 10 succeed and stock ends at 0.

**All or nothing orders.** Shipping an order with several lines either ships everything or nothing. If one line has too little stock, the whole thing rolls back.

**Refresh tokens.** Each one can be used once. If an old one is used again, I treat it as stolen and revoke all of that user's tokens. Only hashes are stored, never the tokens or passwords themselves.

**Last admin protection.** The database refuses to remove or deactivate the last active Admin, even if the API has a bug.

## Tech

C# / .NET 10, ASP.NET Core Web API, PostgreSQL 18, Npgsql, Dapper, xUnit.

## Folders

```
src/StockFlow.Api      controllers, auth, error handling
src/StockFlow.Core     models, interfaces, exceptions
src/StockFlow.Data     repositories (Dapper + SQL)
tests/StockFlow.Tests  tests that run against a real test database
database/              numbered SQL files, run in order
```

## Running it

1. Install the .NET 10 SDK and PostgreSQL 18.
2. Create two databases: `stockflow` and `stockflow_test`.
3. Run `database/001_*.sql` to `009_*.sql` in order on `stockflow`.
4. Optional: run `database/seed_dev.sql` for demo data.
5. Create the first Admin using `database/create_admin.sql.example`. There is no public registration, so a new database starts with no users.
6. Set your secrets (these are not stored in the repo):

```
dotnet user-secrets set "ConnectionStrings:StockFlow" "Host=localhost;Port=5432;Database=stockflow;Username=postgres;Password=YOUR_PASSWORD" --project src/StockFlow.Api
dotnet user-secrets set "Jwt:Key" "A-RANDOM-STRING-OF-AT-LEAST-40-CHARACTERS" --project src/StockFlow.Api
dotnet user-secrets set "ConnectionStrings:StockFlowTest" "Host=localhost;Port=5432;Database=stockflow_test;Username=postgres;Password=YOUR_PASSWORD" --project tests/StockFlow.Tests
```

7. Start it: `dotnet run --project src/StockFlow.Api` (http://localhost:5044)
8. Run the tests: `dotnet test`. This wipes and rebuilds `stockflow_test` every time, and refuses to run on any database whose name doesn't end in `_test`.

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

Example requests are in the `.http` files in `src/StockFlow.Api`.


