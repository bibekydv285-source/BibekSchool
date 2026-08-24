# Free Tier Azure Deployment - BibekSchool

## Free Azure Resources Available

| Resource | Free Tier Limits |
|----------|------------------|
| **App Service** | F1 Free: 1 GB RAM, 1 GB storage, 60 CPU min/day, custom domain not supported |
| **SQL Database** | ❌ No free tier (but 12 months free S0 = 250 GB, then ~$5/mo) |
| **PostgreSQL Flexible Server** | ✅ **Free**: 12 months, 32 GB storage, 1 vCore, 2 GB RAM |
| **MySQL Flexible Server** | ✅ **Free**: 12 months, 32 GB storage, 1 vCore, 2 GB RAM |
| **Container Apps** | ✅ Free: 180,000 vCPU-seconds, 360,000 GiB-seconds/month |
| **Static Web Apps** | ✅ Free: 100 GB bandwidth, custom domains, auth |
| **Application Insights** | ✅ Free: 5 GB/month ingestion |
| **Key Vault** | ✅ Free: 10,000 operations/month |

---

## Option 1: Azure Container Apps (Recommended for Free .NET 8)

### Why Container Apps?
- True free tier (not 12-month trial)
- Supports .NET 8 natively
- Custom domains + HTTPS free
- Auto-scales to zero (no cost when idle)
- Easy GitHub Actions deployment

### 1. Create Resources
```bash
az login
az group create --name rg-bibekschool-free --location "East US"

# Create Container Apps Environment (free)
az containerapp env create \
  --name cae-bibekschool \
  --resource-group rg-bibekschool-free \
  --location "East US"

# Create PostgreSQL Flexible Server (12 months free)
az postgres flexible-server create \
  --name pg-bibekschool-free \
  --resource-group rg-bibekschool-free \
  --location "East US" \
  --admin-user pgadmin \
  --admin-password "<STRONG_PASSWORD>" \
  --sku-name Standard_B1ms \
  --tier Burstable \
  --storage-size 32 \
  --version 15 \
  --public-access 0.0.0.0 \
  --yes

# Create database
az postgres flexible-server db create \
  --resource-group rg-bibekschool-free \
  --server-name pg-bibekschool-free \
  --database-name BibekSchool
```

### 2. Update Connection String for PostgreSQL
```json
// appsettings.Production.json
"ConnectionStrings": {
  "DefaultConnection": "Host=pg-bibekschool-free.postgres.database.azure.com;Database=BibekSchool;Username=pgadmin;Password={PASSWORD};SSL Mode=Require;Trust Server Certificate=true"
}
```

### 3. Modify Program.cs for PostgreSQL
```csharp
// Add NuGet: Npgsql.EntityFrameworkCore.PostgreSQL
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    var connStr = builder.Configuration.GetConnectionString("DefaultConnection");
    options.UseNpgsql(connStr, npgsqlOptions =>
    {
        npgsqlOptions.EnableRetryOnFailure(5, TimeSpan.FromSeconds(30), null);
        npgsqlOptions.CommandTimeout(60);
    });
});
```

### 4. Create Dockerfile
```dockerfile
# Dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["BibekSchool.csproj", "."]
RUN dotnet restore "BibekSchool.csproj"
COPY . .
RUN dotnet publish "BibekSchool.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "BibekSchool.dll"]
```

### 5. Deploy via GitHub Actions
```yaml
# .github/workflows/azure-container-apps.yml
name: Deploy to Azure Container Apps

on:
  push:
    branches: [main]

jobs:
  build-and-deploy:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      
      - name: Set up Docker Buildx
        uses: docker/setup-buildx-action@v3
      
      - name: Log in to Azure Container Registry
        uses: azure/docker-login@v1
        with:
          login-server: ${{ secrets.ACR_LOGIN_SERVER }}
          username: ${{ secrets.ACR_USERNAME }}
          password: ${{ secrets.ACR_PASSWORD }}
      
      - name: Build and push
        uses: docker/build-push-action@v5
        with:
          context: .
          push: true
          tags: ${{ secrets.ACR_LOGIN_SERVER }}/bibekschool:${{ github.sha }}
      
      - name: Deploy to Container Apps
        uses: azure/container-apps-deploy-action@v2
        with:
          resourceGroup: rg-bibekschool-free
          containerAppName: app-bibekschool
          imageToDeploy: ${{ secrets.ACR_LOGIN_SERVER }}/bibekschool:${{ github.sha }}
          envVars: |
            ASPNETCORE_ENVIRONMENT=Production
            ConnectionStrings__DefaultConnection=${{ secrets.POSTGRES_CONNECTION_STRING }}
            Email__SmtpServer=${{ secrets.EMAIL_SMTP_SERVER }}
            Email__SmtpPort=${{ secrets.EMAIL_SMTP_PORT }}
            Email__SmtpUsername=${{ secrets.EMAIL_SMTP_USERNAME }}
            Email__SmtpPassword=${{ secrets.EMAIL_SMTP_PASSWORD }}
            Email__FromEmail=${{ secrets.EMAIL_FROM_EMAIL }}
            Email__FromName=Bibek School
            Email__EnableSsl=true
            Email__UseMock=false
```

---

## Option 2: Azure Static Web Apps + Azure Functions (API)

### Architecture
- **Frontend**: Blazor WebAssembly (or keep Razor Pages with Static Web Apps)
- **Backend**: Azure Functions (.NET 8 Isolated)
- **Database**: PostgreSQL Flexible Server (free 12 months)

### Limitation
Static Web Apps doesn't support traditional Razor Pages with server-side rendering well. Better for Blazor WASM or React/Vue frontends.

---

## Option 3: Azure App Service F1 Free (Limited)

### Constraints
- ❌ No custom domain (only `*.azurewebsites.net`)
- ❌ No SSL on custom domain
- ❌ 60 CPU minutes/day (resets daily)
- ❌ Always On not supported (cold starts)
- ❌ No deployment slots
- ❌ 1 GB storage total

### If You Still Want F1:
```bash
# App Service Plan F1
az appservice plan create \
  --name asp-bibekschool-free \
  --resource-group rg-bibekschool-free \
  --location "East US" \
  --sku F1 \
  --is-linux

# Web App
az webapp create \
  --name app-bibekschool-free \
  --resource-group rg-bibekschool-free \
  --plan asp-bibekschool-free \
  --runtime "DOTNET|8.0"
```

### Connection String for F1 (use Azure SQL 12-month free)
```bash
# Get 12-month free Azure SQL (requires credit card for verification)
az sql server create \
  --name sql-bibekschool-free \
  --resource-group rg-bibekschool-free \
  --location "East US" \
  --admin-user sqladmin \
  --admin-password "<STRONG_PASSWORD>"

az sql db create \
  --resource-group rg-bibekschool-free \
  --server sql-bibekschool-free \
  --name BibekSchool \
  --service-objective Basic \
  --zone-redundant false
```

**Cost after 12 months**: ~$5/month for Basic tier.

---

## Option 4: Hybrid - Local DB + Free Hosting

### Use Free External PostgreSQL
| Provider | Free Tier |
|----------|-----------|
| **Supabase** | 500 MB PostgreSQL, free forever |
| **Neon** | 0.5 GB PostgreSQL, free forever |
| **ElephantSQL** | 20 MB (tiny) |
| **Railway** | $5 credit/month |

### Deploy App Only to Azure (Free Static Web Apps)
```bash
# Convert to Blazor WASM or use Static Web Apps for static content
# API runs elsewhere (Functions, Container Apps, or external)
```

---

## Recommended: Option 1 (Container Apps + PostgreSQL)

### Complete Free Setup Script
```bash
#!/bin/bash
# Run in Azure Cloud Shell or local CLI

RESOURCE_GROUP="rg-bibekschool-free"
LOCATION="East US"
ENV_NAME="cae-bibekschool"
APP_NAME="app-bibekschool"
PG_SERVER="pg-bibekschool-free"
PG_DB="BibekSchool"
PG_ADMIN="pgadmin"
PG_PASSWORD="<GENERATE_STRONG_PASSWORD>"

# 1. Resource Group
az group create -n $RESOURCE_GROUP -l "$LOCATION"

# 2. Container Apps Environment
az containerapp env create -n $ENV_NAME -g $RESOURCE_GROUP -l "$LOCATION"

# 3. PostgreSQL (12 months free)
az postgres flexible-server create \
  -n $PG_SERVER -g $RESOURCE_GROUP -l "$LOCATION" \
  -u $PG_ADMIN -p "$PG_PASSWORD" \
  --sku-name Standard_B1ms --tier Burstable \
  --storage-size 32 --version 15 \
  --public-access 0.0.0.0 --yes

az postgres flexible-server db create \
  -g $RESOURCE_GROUP -s $PG_SERVER -d $PG_DB

# 4. Get connection string
CONN_STR="Host=${PG_SERVER}.postgres.database.azure.com;Database=${PG_DB};Username=${PG_ADMIN};Password=${PG_PASSWORD};SSL Mode=Require;Trust Server Certificate=true"

# 5. Create Container App (initial deploy via GitHub Actions)
# First push triggers workflow
```

### GitHub Secrets Required
| Secret | Value |
|--------|-------|
| `AZURE_CREDENTIALS` | `az ad sp create-for-rbac --name "github-bibekschool" --role contributor --scopes /subscriptions/<sub>/resourceGroups/rg-bibekschool-free --sdk-auth` |
| `POSTGRES_CONNECTION_STRING` | From step 4 above |
| `ACR_LOGIN_SERVER` | `<acr-name>.azurecr.io` (create ACR: `az acr create -n <name> -g rg-bibekschool-free --sku Basic`) |
| `ACR_USERNAME` | ACR admin username |
| `ACR_PASSWORD` | ACR admin password |
| `EMAIL_*` | Your SMTP credentials |

---

## Cost Summary (Free Tier)

| Component | Cost |
|-----------|------|
| Container Apps | **Free** (180k vCPU-s/mo) |
| PostgreSQL Flexible Server | **Free 12 months** (then ~$12/mo) |
| Container Registry (Basic) | **~$5/mo** (or use GitHub Container Registry FREE) |
| Application Insights | **Free** (5 GB/mo) |
| **Total** | **$0 for 12 months**, then ~$17/mo |

### To Stay Free Forever:
- Use **GitHub Container Registry (GHCR)** instead of Azure ACR (free for public/private)
- After 12 months, migrate PostgreSQL to **Supabase/Neon free tier**

---

## Quick Test Before Full Deploy

```bash
# Test PostgreSQL connection locally
export ConnectionStrings__DefaultConnection="Host=pg-xxx.postgres.database.azure.com;Database=BibekSchool;Username=pgadmin;Password=xxx;SSL Mode=Require;Trust Server Certificate=true"

# Update Program.cs for Npgsql, then:
dotnet ef database update
dotnet run
```

---

## Files to Change for PostgreSQL

1. **BibekSchool.csproj** - Add package:
   ```xml
   <PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" Version="8.0.0" />
   ```

2. **Program.cs** - Change `UseSqlServer` to `UseNpgsql`

3. **Migrations** - Regenerate:
   ```bash
   dotnet ef migrations remove -f  # Remove SQL Server migrations
   dotnet ef migrations add InitialPostgres
   dotnet ef database update
   ```

---

## Verdict

| Approach | Free Forever? | Custom Domain? | Effort |
|----------|---------------|----------------|--------|
| Container Apps + PostgreSQL | 12 months free | ✅ Yes | Medium |
| App Service F1 + SQL | ❌ No (limited) | ❌ No | Low |
| Static Web Apps + Functions | ✅ Yes | ✅ Yes | High (rewrite) |
| External DB + Free Host | ✅ Yes | ✅ Yes | Medium |

**Best balance**: **Container Apps + PostgreSQL** with migration to Supabase/Neon after 12 months.