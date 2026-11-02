# GoodDeedBooks

GoodDeedBooks is an open-source business accounting application with a focus on nonprofit donation management. It builds on the AccountGo accounting codebase and combines core bookkeeping workflows with donor and donation tracking.

## What the application does

- Supports accounting workflows such as accounts receivable, accounts payable, inventory, and financial management.
- Manages donors and donation types, records donation transactions and payment details, and associates donations with general-ledger accounts.
- Supports Canadian tax-receipt workflows, including receipt generation and PDF output.

The application is evolving. Some parts of the repository retain the AccountGo name and older front-end implementations; features and setup instructions in this README describe the current GoodDeedBooks development setup.

## Application structure

- **`src/Api`** — ASP.NET Core API and data services.
- **`src/AccountGoWeb`** — the primary web application, built with ASP.NET Core and Razor components.
- **`src/Core`, `src/Services`, `src/Dto`, and `src/Infrastructure`** — domain models, business services, API contracts, and persistence support.
- **`src/MigrationService`** — applies database migrations during local Aspire orchestration.
- **`src/src.AppHost`** — .NET Aspire entry point that starts SQL Server, the API, the web application, and the migration service.
- **`src/BlazorGDB`** — an additional Blazor application project; it is not the web application launched by the current Aspire AppHost.

The main applications target .NET 10 and use Entity Framework Core with SQL Server. The repository also contains older or experimental front-end projects.

## Run locally

Prerequisites:

- .NET 10 SDK
- Docker Desktop or another Docker-compatible runtime, for the SQL Server container managed by Aspire

From the repository root, start the application:

```sh
dotnet run --project src/src.AppHost
```

Aspire starts the database, applies migrations, and launches the API and web application. Use the Aspire dashboard links in the console output to open the services and inspect their status and logs. The AppHost currently configures the API on port `8001` and the web application on port `8000`.

To build the solution:

```sh
dotnet build src/src.sln
```

## Donations and database changes

Donation records are associated with donors and donation types and include payment, amount, general-ledger, and tax-receipt information. When a migration affects an existing database, back it up before applying the migration. Some historical donation data cannot be automatically converted back to its former representation; use a database backup to roll back such changes.

Database migrations are run by the Aspire `MigrationService` during local startup. For deployment or other environments, configure the API and migration service with the appropriate SQL Server connection string and follow the deployment environment's migration process.

## Repository

Source code and issue tracking: [gooddeedbooks/gdb](https://github.com/gooddeedbooks/gdb).
