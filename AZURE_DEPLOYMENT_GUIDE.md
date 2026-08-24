# Azure Deployment Guide - BibekSchool

## Prerequisites
- Azure Subscription
- Azure CLI installed (`az`)
- .NET 8 SDK
- Git

---

## Step 1: Create Azure Resources

### 1.1 Login to Azure
```bash
az login
az account set --subscription <YOUR_SUBSCRIPTION_ID>
```

### 1.2 Create Resource Group
```bash
az group create --name rg-bibekschool --location "East US"
```

### 1.3 Create Azure SQL Server & Database
```bash
# Create SQL Server (unique name required)
az sql server create \
  --name sql-bibekschool-<unique-suffix> \
  --resource-group rg-bibekschool \
  --location "East US" \
  --admin-user sqladmin \
  --admin-password "<STRONG_PASSWORD>"

# Create Database
az sql db create \
  --resource-group rg-bibekschool \
  --server sql-bibekschool-<unique-suffix> \
  --name BibekSchool \
  --service-objective S0 \
  --zone-redundant false

# Configure Firewall - Allow Azure Services
az sql server firewall-rule create \
  --resource-group rg-bibekschool \
  --server sql-bibekschool-<unique-suffix> \
  --name AllowAzureServices \
  --start-ip-address 0.0.0.0 \
  --end-ip-address 0.0.0.0

# Optional: Allow your IP for management
az sql server firewall-rule create \
  --resource-group rg-bibekschool \
  --server sql-bibekschool-<unique-suffix> \
  --name MyIP \
  --start-ip-address <YOUR_PUBLIC_IP> \
  --end-ip-address <YOUR_PUBLIC_IP>
```

### 1.4 Create App Service Plan & Web App
```bash
# Create App Service Plan (Linux, .NET 8)
az appservice plan create \
  --name asp-bibekschool \
  --resource-group rg-bibekschool \
  --location "East US" \
  --sku P1v3 \
  --is-linux

# Create Web App
az webapp create \
  --name app-bibekschool-<unique-suffix> \
  --resource-group rg-bibekschool \
  --plan asp-bibekschool \
  --runtime "DOTNET|8.0"
```

### 1.5 Configure Application Insights (Optional but Recommended)
```bash
az monitor app-insights component create \
  --app ai-bibekschool \
  --location "East US" \
  --resource-group rg-bibekschool \
  --application-type web
```

---

## Step 2: Configure Connection String & Secrets

### 2.1 Get Connection String
```bash
# Get the connection string (save for next step)
az sql db show-connection-string \
  --client dotnet \
  --name BibekSchool \
  --server sql-bibekschool-<unique-suffix> \
  --auth-type sql
```

Output format:
```
Server=tcp:sql-bibekschool-xxx.database.windows.net,1433;Initial Catalog=BibekSchool;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;
```

### 2.2 Set App Settings in Azure (NEVER in code)
```bash
# Connection String
az webapp config connection-string set \
  --resource-group rg-bibekschool \
  --name app-bibekschool-<unique-suffix> \
  --settings DefaultConnection="Server=tcp:sql-bibekschool-xxx.database.windows.net,1433;Initial Catalog=BibekSchool;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;Authentication=Active Directory Default;" \
  --connection-string-type SQLAzure

# Email Settings (use your actual SMTP credentials)
az webapp config appsettings set \
  --resource-group rg-bibekschool \
  --name app-bibekschool-<unique-suffix> \
  --settings \
    Email__SmtpServer="smtp.gmail.com" \
    Email__SmtpPort="587" \
    Email__SmtpUsername="your-email@gmail.com" \
    Email__SmtpPassword="your-app-password" \
    Email__FromEmail="your-email@gmail.com" \
    Email__FromName="Bibek School" \
    Email__EnableSsl="true" \
    Email__UseMock="false"

# Application Insights (if created)
az webapp config appsettings set \
  --resource-group rg-bibekschool \
  --name app-bibekschool-<unique-suffix> \
  --settings ApplicationInsights__ConnectionString="<from-step-1.5>"
```

---

## Step 3: Run Migrations Against Azure SQL

### Option A: From Local Machine (with Azure SQL Firewall Open)
```bash
# Set connection string locally
export ConnectionStrings__DefaultConnection="Server=tcp:sql-bibekschool-xxx.database.windows.net,1433;Initial Catalog=BibekSchool;User ID=sqladmin;Password=<STRONG_PASSWORD>;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"

# Run migrations
cd BibekSchool
dotnet ef database update --project BibekSchool.csproj
```

### Option B: Use Azure DevOps / GitHub Actions (Recommended for CI/CD)
See Step 5 for pipeline configuration.

---

## Step 4: Deploy Application

### 4.1 Build Release Package
```bash
cd BibekSchool
dotnet publish -c Release -o ./publish
```

### 4.2 Deploy via Zip Deploy (Azure CLI)
```bash
cd publish
zip -r ../deploy.zip .
cd ..

az webapp deployment source config-zip \
  --resource-group rg-bibekschool \
  --name app-bibekschool-<unique-suffix> \
  --src deploy.zip
```

### 4.3 Configure Startup Command (if needed)
```bash
az webapp config set \
  --resource-group rg-bibekschool \
  --name app-bibekschool-<unique-suffix> \
  --startup-file "dotnet BibekSchool.dll"
```

---

## Step 5: CI/CD Pipeline (GitHub Actions)

Create `.github/workflows/azure-deploy.yml`:

```yaml
name: Deploy to Azure

on:
  push:
    branches: [ main ]
  workflow_dispatch:

env:
  AZURE_RESOURCE_GROUP: rg-bibekschool
  AZURE_WEBAPP_NAME: app-bibekschool-<unique-suffix>
  DOTNET_VERSION: '8.0.x'

jobs:
  build-and-deploy:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      
      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: ${{ env.DOTNET_VERSION }}
      
      - name: Restore dependencies
        run: dotnet restore
      
      - name: Build
        run: dotnet build --configuration Release --no-restore
      
      - name: Run EF Migrations (via SQL Script)
        run: |
          dotnet tool install --global dotnet-ef
          dotnet ef migrations script --output migration.sql --project BibekSchool.csproj
          
      - name: Run Migration on Azure SQL
        uses: azure/sql-action@v2
        with:
          server-name: sql-bibekschool-<unique-suffix>
          connection-string: ${{ secrets.AZURE_SQL_CONNECTION_STRING }}
          sql-file: migration.sql
      
      - name: Publish
        run: dotnet publish -c Release -o ${{ env.DOTNET_ROOT }}/publish
      
      - name: Deploy to Azure Web App
        uses: azure/webapps-deploy@v2
        with:
          app-name: ${{ env.AZURE_WEBAPP_NAME }}
          publish-profile: ${{ secrets.AZURE_WEBAPP_PUBLISH_PROFILE }}
          package: ${{ env.DOTNET_ROOT }}/publish
```

### Required GitHub Secrets:
- `AZURE_SQL_CONNECTION_STRING` - Full Azure SQL connection string
- `AZURE_WEBAPP_PUBLISH_PROFILE` - Download from Azure Portal > Web App > Get Publish Profile

---

## Step 6: Verify Deployment

### 6.1 Check Application URL
```
https://app-bibekschool-<unique-suffix>.azurewebsites.net
```

### 6.2 Verify Key Flows
- [ ] Home page loads
- [ ] Login works (MainAdmin: bibekydv285@gmail.com)
- [ ] Student Registration works
- [ ] Password Reset (Email) works
- [ ] Student Dashboard loads data
- [ ] Teacher Dashboard loads data
- [ ] Admin Dashboard loads data
- [ ] CRUD operations (Create/Edit/Delete Students, Teachers, Classes)
- [ ] Marks entry and viewing
- [ ] Notifications work

### 6.3 Check Logs
```bash
# View logs
az webapp log tail --resource-group rg-bibekschool --name app-bibekschool-<unique-suffix>

# Or in Azure Portal: App Service > Log Stream
```

---

## Step 7: Production Hardening Checklist

### Security
- [ ] HTTPS Only: Enabled in Azure Portal (TLS 1.2 minimum)
- [ ] HSTS: Enabled via `app.UseHsts()` in production
- [ ] Security Headers: Configured in Program.cs
- [ ] SQL Firewall: Only Azure Services + required IPs
- [ ] Managed Identity: Consider for SQL auth (no passwords in connection string)
- [ ] Key Vault: Store secrets in Azure Key Vault, reference via `@Microsoft.KeyVault`

### Performance
- [ ] App Service Plan: P1v3 or higher for production
- [ ] SQL Database: S0 minimum, scale as needed
- [ ] Connection Pooling: Enabled by default in EF Core
- [ ] Caching: Consider Azure Redis Cache for sessions

### Monitoring
- [ ] Application Insights: Enabled
- [ ] Log Analytics: Connected
- [ ] Alerts: Configure for CPU, Memory, Errors, Slow Queries

---

## Step 8: Custom Domain & SSL (Optional)

```bash
# Add custom domain
az webapp config hostname add \
  --webapp-name app-bibekschool-<unique-suffix> \
  --resource-group rg-bibekschool \
  --hostname bibekschool.edu.np

# Bind SSL (managed certificate)
az webapp config ssl bind \
  --resource-group rg-bibekschool \
  --name app-bibekschool-<unique-suffix> \
  --certificate-thumbprint <THUMBPRINT> \
  --ssl-type SNI
```

---

## Connection String Formats

### SQL Authentication (Current)
```
Server=tcp:<server>.database.windows.net,1433;Initial Catalog=BibekSchool;User ID=<user>;Password=<password>;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;
```

### Azure AD Authentication (Recommended - No Passwords)
```
Server=tcp:<server>.database.windows.net,1433;Initial Catalog=BibekSchool;Authentication=Active Directory Default;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;
```
Requires: App Service Managed Identity + SQL Admin permissions.

---

## Troubleshooting

| Issue | Solution |
|-------|----------|
| "Login failed" | Check SQL firewall, credentials, connection string format |
| "Cannot open database" | Run migrations: `dotnet ef database update` |
| "Timeout" | Increase `CommandTimeout` in DbContext, check firewall |
| "Certificate validation" | `TrustServerCertificate=False` for production, `True` for dev |
| "Email not sending" | Verify SMTP credentials, check Azure outbound port 587 |
| "Static files 404" | Ensure `app.UseStaticFiles()` before `app.UseRouting()` |

---

## Cost Estimation (Monthly, East US)

| Resource | SKU | Est. Cost |
|----------|-----|-----------|
| App Service Plan | P1v3 | ~$110 |
| Azure SQL Database | S0 (10 DTU) | ~$15 |
| Application Insights | Free tier | ~$0-5 |
| **Total** | | **~$130-150/month** |

Scale down to B1/B2 for dev/test (~$15-30/month).

---

## Rollback Plan

```bash
# Quick rollback via deployment slots
az webapp deployment slot create --name app-bibekschool --resource-group rg-bibekschool --slot staging
# Deploy to staging, test, then swap
az webapp deployment slot swap --name app-bibekschool --resource-group rg-bibekschool --slot staging --target-slot production
```

---

## Files Modified for Azure

1. **Program.cs** - Azure SQL retry policy, production cookie config, security headers, forwarded headers
2. **appsettings.Production.json** - Production template (no secrets)
3. **appsettings.json** - Development only (no real credentials)
4. **DbSeeder.cs** - Safe seeding with error handling

---

## Next Steps

1. Run the Azure CLI commands above
2. Configure GitHub Actions secrets
3. Push to main branch to trigger deployment
4. Monitor Application Insights
5. Set up custom domain & SSL
6. Configure backup/retention for SQL Database