# Azure Environment Variables Reference

## Required Application Settings (Set in Azure Portal > Configuration)

### Connection Strings
| Name | Value | Type |
|------|-------|------|
| DefaultConnection | `Server=tcp:<sql-server>.database.windows.net,1433;Initial Catalog=BibekSchool;Authentication=Active Directory Default;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;` | SQLAzure |

### Email Configuration
| Name | Example Value | Notes |
|------|---------------|-------|
| Email__SmtpServer | smtp.gmail.com | Your SMTP server |
| Email__SmtpPort | 587 | TLS port |
| Email__SmtpUsername | your-email@gmail.com | Full email |
| Email__SmtpPassword | xxxxxxxxxxxxxxxx | App Password (not account password) |
| Email__FromEmail | your-email@gmail.com | Sender email |
| Email__FromName | Bibek School | Display name |
| Email__EnableSsl | true | Required for Gmail |
| Email__UseMock | false | Must be false for production |

### Application Insights (Optional)
| Name | Value |
|------|-------|
| ApplicationInsights__ConnectionString | `InstrumentationKey=xxx;IngestionEndpoint=https://eastus-0.in.applicationinsights.azure.com/` |

### Application Settings
| Name | Value |
|------|-------|
| ASPNETCORE_ENVIRONMENT | Production |
| ASPNETCORE_HTTPS_PORT | 443 |
| WEBSITE_HTTPLOGGING_RETENTION_DAYS | 7 |

---

## GitHub Actions Secrets Required

| Secret Name | Value Source |
|-------------|--------------|
| AZURE_CREDENTIALS | `az ad sp create-for-rbac --name "github-actions-bibekschool" --role contributor --scopes /subscriptions/<sub-id>/resourceGroups/rg-bibekschool --sdk-auth` |
| AZURE_SQL_CONNECTION_STRING | Full connection string with SQL auth (for migration step) |
| AZURE_WEBAPP_PUBLISH_PROFILE | Download from Azure Portal > Web App > Get Publish Profile |

---

## Local Development Setup (DO NOT COMMIT)

Create `appsettings.Development.json` locally (gitignored):

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=BibekSchool;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"
  },
  "Email": {
    "SmtpServer": "smtp.gmail.com",
    "SmtpPort": 587,
    "SmtpUsername": "your-dev-email@gmail.com",
    "SmtpPassword": "your-dev-app-password",
    "FromEmail": "your-dev-email@gmail.com",
    "FromName": "Bibek School (Dev)",
    "EnableSsl": true,
    "UseMock": true
  }
}
```

---

## Azure CLI Quick Commands

```bash
# View all app settings
az webapp config appsettings list -g rg-bibekschool -n app-bibekschool-xxx

# View connection strings
az webapp config connection-string list -g rg-bibekschool -n app-bibekschool-xxx

# Set single setting
az webapp config appsettings set -g rg-bibekschool -n app-bibekschool-xxx --settings Key="Value"

# Restart app after config changes
az webapp restart -g rg-bibekschool -n app-bibekschool-xxx

# View logs
az webapp log tail -g rg-bibekschool -n app-bibekschool-xxx

# Enable diagnostic logging
az webapp log config -g rg-bibekschool -n app-bibekschool-xxx \
  --application-logging filesystem \
  --level information \
  --failed-request-logging filesystem \
  --detailed-error-logging filesystem \
  --web-server-logging filesystem
```

---

## SQL Database Management

```bash
# Run migration script manually
sqlcmd -S tcp:sql-bibekschool-xxx.database.windows.net,1433 \
  -d BibekSchool -U sqladmin -P "<password>" \
  -i migration.sql

# Check database size
az sql db show -g rg-bibekschool -s sql-bibekschool-xxx -n BibekSchool \
  --query "currentServiceObjectiveName,maxSizeBytes"

# Scale database
az sql db update -g rg-bibekschool -s sql-bibekschool-xxx -n BibekSchool \
  --service-objective S1
```

---

## Key Points

1. **NEVER** put real connection strings, passwords, or API keys in:
   - `appsettings.json`
   - `appsettings.Production.json`
   - Any committed file

2. **ALWAYS** use:
   - Azure App Service Configuration for production
   - `appsettings.Development.json` (gitignored) for local
   - GitHub/Azure DevOps Secrets for CI/CD

3. **Use Managed Identity** for SQL Authentication (no passwords):
   - Enable System-Assigned Identity on Web App
   - Grant `db_datareader`, `db_datawriter`, `db_ddladmin` to identity in SQL
   - Use `Authentication=Active Directory Default` in connection string