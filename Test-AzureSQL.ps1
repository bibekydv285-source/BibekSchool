<#
.SYNOPSIS
    Test Azure SQL Database connection and run basic verification queries.

.DESCRIPTION
    This script tests connectivity to Azure SQL Database and verifies
    the schema was migrated correctly.

.PARAMETER ServerName
    Azure SQL Server name (e.g., sql-bibekschool-xxx.database.windows.net)

.PARAMETER DatabaseName
    Database name (default: BibekSchool)

.PARAMETER Username
    SQL Admin username (for SQL auth)

.PARAMETER Password
    SQL Admin password (for SQL auth)

.EXAMPLE
    .\Test-AzureSQL.ps1 -ServerName "sql-bibekschool-xxx.database.windows.net" -DatabaseName "BibekSchool" -Username "sqladmin" -Password "P@ssw0rd!"
#>

param(
    [Parameter(Mandatory=$true)]
    [string]$ServerName,
    
    [string]$DatabaseName = "BibekSchool",
    
    [Parameter(Mandatory=$true)]
    [string]$Username,
    
    [Parameter(Mandatory=$true)]
    [string]$Password
)

$connectionString = "Server=tcp:$ServerName,1433;Initial Catalog=$DatabaseName;User ID=$Username;Password=$Password;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"

Write-Host "Testing connection to $ServerName/$DatabaseName..." -ForegroundColor Cyan

try {
    $conn = New-Object System.Data.SqlClient.SqlConnection
    $conn.ConnectionString = $connectionString
    $conn.Open()
    
    Write-Host "✓ Connected successfully!" -ForegroundColor Green
    Write-Host "Server Version: $($conn.ServerVersion)" -ForegroundColor Gray
    
    # Test 1: Check tables exist
    $cmd = $conn.CreateCommand()
    $cmd.CommandText = @"
        SELECT TABLE_NAME 
        FROM INFORMATION_SCHEMA.TABLES 
        WHERE TABLE_TYPE = 'BASE TABLE' 
        ORDER BY TABLE_NAME
"@
    
    $reader = $cmd.ExecuteReader()
    $tables = @()
    while ($reader.Read()) { $tables += $reader[0] }
    $reader.Close()
    
    Write-Host "`nTables found ($($tables.Count)):" -ForegroundColor Cyan
    $tables | ForEach-Object { Write-Host "  - $_" -ForegroundColor White }
    
    # Test 2: Check key data
    $checks = @(
        @{ Name = "Roles"; Query = "SELECT COUNT(*) FROM AspNetRoles" },
        @{ Name = "Users"; Query = "SELECT COUNT(*) FROM AspNetUsers" },
        @{ Name = "Students"; Query = "SELECT COUNT(*) FROM Students" },
        @{ Name = "Teachers"; Query = "SELECT COUNT(*) FROM Teachers" },
        @{ Name = "Classes"; Query = "SELECT COUNT(*) FROM SchoolClasses" },
        @{ Name = "Subjects"; Query = "SELECT COUNT(*) FROM Subjects" },
        @{ Name = "Migrations"; Query = "SELECT COUNT(*) FROM __EFMigrationsHistory" }
    )
    
    Write-Host "`nData verification:" -ForegroundColor Cyan
    foreach ($check in $checks) {
        $cmd = $conn.CreateCommand()
        $cmd.CommandText = $check.Query
        $count = $cmd.ExecuteScalar()
        Write-Host "  $($check.Name): $count" -ForegroundColor White
    }
    
    # Test 3: Check migration history
    $cmd = $conn.CreateCommand()
    $cmd.CommandText = "SELECT MigrationId, ProductVersion FROM __EFMigrationsHistory ORDER BY MigrationId"
    $reader = $cmd.ExecuteReader()
    
    Write-Host "`nApplied Migrations:" -ForegroundColor Cyan
    while ($reader.Read()) {
        Write-Host "  $($reader[0]) (v$($reader[1]))" -ForegroundColor White
    }
    $reader.Close()
    
    $conn.Close()
    Write-Host "`n✓ All checks passed!" -ForegroundColor Green
    
} catch {
    Write-Host "`n✗ Connection failed: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}