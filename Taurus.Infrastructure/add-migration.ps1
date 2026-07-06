#!/usr/bin/env pwsh

param([String]$name)

if ($name -eq "")
{
    Write-Error "must specify migration name"
    exit
}

dotnet ef migrations add --context SqliteDbContext -o Persistence/Migrations/Sqlite $name
dotnet ef dbcontext optimize --context SqliteDbContext --output-dir Persistence/CompiledModels/Sqlite
dotnet ef migrations add --context PostgresDbContext -o Persistence/Migrations/Postgres $name
dotnet ef dbcontext optimize --context PostgresDbContext --output-dir Persistence/CompiledModels/Postgres

Write-Host "IMPORTANT! you must add new migration entries to the Taurus.Infrastructure/Persistence/PostgresDbContext and Taurus.Infrastructure/Persistence/SqliteDbContext" -ForegroundColor Cyan
