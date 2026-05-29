# Modernization Plan: Zava Alert Service Azure Modernization

**Project**: ZavaAlertService

---

## Technical Framework

- **Language**: C# on .NET Framework 4.8
- **Framework**: ASP.NET/WCF service style application
- **Build Tool**: MSBuild (`.csproj` with `packages.config`)
- **Database**: SQL Server connectivity via configuration
- **Key Dependencies**: RabbitMQ publisher integration, System.ServiceModel

---

## Overview

> This migration modernizes the Zava Alert Service and deploys it to Azure.
> The application currently runs on .NET Framework 4.8 with legacy service hosting
> and non-cloud-native integration patterns. The new architecture will:
>
> - Move the application to the latest .NET framework to improve supportability
> - Replace key integration points with Azure-native managed services
> - Deploy the service to Azure Container Apps for cloud-native operations
>
> The migration follows a phased approach: upgrade runtime first, then migrate
> service integrations, complete security remediation, and deploy to Azure.

---

## Migration Impact Summary

| Application | Original Service | New Azure Service | Authentication | Comments |
|-------------|------------------|-------------------|----------------|----------|
| ZavaAlertService | .NET Framework 4.8 runtime | .NET 10 runtime | N/A | Upgrade to latest framework |
| ZavaAlertService | RabbitMQ messaging | Azure Service Bus | Managed Identity | Cloud-native messaging |
| ZavaAlertService | App/web config values | Azure App Configuration | Managed Identity | Externalized configuration |
| ZavaAlertService | Current hosting model | Azure Container Apps | Managed Identity | Azure deployment target |

---

## Open Questions & Questionnaire

- [x] Q: Should the plan include environment/infrastructure provisioning? → A: No — use existing Azure environment (no new IaC task requested)
- [x] Q: Should integration testing be included? → A: No explicit request; excluded from this plan
- [x] Q: Should security/CVE remediation be included? → A: Yes — included by default
- [x] Q: Which Azure deployment target should be used? → A: Azure Container Apps (default)
