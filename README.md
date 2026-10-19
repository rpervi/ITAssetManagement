# IT Asset Management

Angular frontend and ASP.NET Core API for managing organizational IT assets. Asset categories, products, stock receipts, inventory, employees, and asset assignments are implemented; return processing, reports, and other unfinished workflows remain placeholders.

## Project structure

```text
backend/
  ITAssetManagement.sln
  ITAssetManagement.Api/             Controllers and API configuration
  ITAssetManagement.Application/     DTOs and service contracts/implementations
  ITAssetManagement.Domain/          Asset management entities and enums
  ITAssetManagement.Infrastructure/  EF Core SQL Server DbContext and configuration
  ITAssetManagement.Tests/           xUnit tests
frontend/                            Angular standalone application
tests/                               Reserved for additional test suites
```

The API references Application and Infrastructure. Application references Domain, and Infrastructure references Application and Domain. Repository contracts live in Application so business services can depend on abstractions without creating an Application-to-Infrastructure project cycle; EF Core implementations and Fluent API mappings are in Infrastructure. No sample data is included.

## Asset assignments

The assignment screen is available at `/assign-assets`. Authenticated Admin and ITStaff users can assign available individual assets or bulk stock; assignment history is readable by roles allowed to view inventory. Bulk assignments allocate FIFO receipt batches, and individual assignments update the asset-unit status. Requests support idempotency to avoid duplicate assignments after retries.

## Run the frontend

```powershell
Set-Location .\frontend
npm start
```

Open `http://localhost:4200`.

## Run the backend

`backend/ITAssetManagement.Api/appsettings.json` is configured to use the provided Windows-authenticated SQL Server instance, `INMUM-DEV-02`, and database `ITAssetManagementDB`. Do not add passwords to source control. For another local SQL Server using Windows authentication, use a connection string shaped like:

```powershell
$env:ConnectionStrings__DefaultConnection = "Server=YOUR_SQL_SERVER;Database=ITAssetManagementDB;Trusted_Connection=True;Encrypt=True;TrustServerCertificate=True"
dotnet run --project .\backend\ITAssetManagement.Api\ITAssetManagement.Api.csproj
```

For SQL authentication, store the connection string outside source control (for example, with .NET user secrets or `ConnectionStrings__DefaultConnection` environment configuration). The health endpoint is `GET http://localhost:5000/api/health` (the actual local port is printed at startup). Swagger UI is enabled in the Development environment at `/swagger`.

The migrations through `AddAssetAssignmentSupport` have been applied to the configured database and verified. For future model changes, migrations are managed from the backend directory, with the API as the startup project and Infrastructure as the migrations project:

```powershell
dotnet ef migrations add <MigrationName> --project .\ITAssetManagement.Infrastructure\ITAssetManagement.Infrastructure.csproj --startup-project .\ITAssetManagement.Api\ITAssetManagement.Api.csproj --output-dir Persistence\Migrations
dotnet ef database update --project .\ITAssetManagement.Infrastructure\ITAssetManagement.Infrastructure.csproj --startup-project .\ITAssetManagement.Api\ITAssetManagement.Api.csproj
```

Before applying future migrations, confirm the SQL Server endpoint and authentication and obtain the database owner's approval.

## Build and test

```powershell
dotnet restore .\backend\ITAssetManagement.sln
dotnet build .\backend\ITAssetManagement.sln
dotnet test .\backend\ITAssetManagement.sln
Set-Location .\frontend
npm install
npm run build
npm test
```
