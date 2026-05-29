# Modernization Plan: modernization-plan

**Project**: ZavaAlertService

---

## Technical Framework

- **Language**: C# (.NET Framework 4.8)
- **Framework**: ASP.NET/WCF service on .NET Framework v4.8
- **Build Tool**: MSBuild
- **Database**: SQL Server (via `System.Data.SqlClient`)
- **Key Dependencies**: WCF (`System.ServiceModel`), ASP.NET (`System.Web`), SQL Client (`System.Data`)

---

## Overview

> This migration modernizes ZavaAlertService for Azure deployment and cloud-ready operations. The application currently uses on-host connection configuration and direct service endpoint coupling. The new architecture will:
>
> - Move core runtime configuration toward Azure-native configuration and identity patterns
> - Modernize SQL connectivity for Azure-hosted database access
> - Deploy the service to Azure Container Apps with cloud-aligned operational posture
>
> The migration follows a phased approach: configuration/database modernization, security hardening, and deployment.

---

## Migration Impact Summary

| Application      | Original Service                | New Azure Service                    | Authentication   | Comments |
|------------------|---------------------------------|--------------------------------------|------------------|----------|
| ZavaAlertService | SQL Server via local config     | Azure SQL Database                   | Managed Identity | Modernize SQL connectivity and cloud auth |
| ZavaAlertService | Local app settings / hardcoded values | Azure App Configuration + Key Vault | Managed Identity | Externalize settings and remove hardcoded secrets |
| ZavaAlertService | Self-hosted runtime deployment  | Azure Container Apps                 | Managed Identity | Standard Azure deployment target |

---

## Open Questions & Questionnaire

- [x] Q: Should the plan include environment/infrastructure provisioning? → A: No; focus on code migration and deployment to existing/managed Azure resources.
- [x] Q: Should the plan include integration testing to verify migrated services? → A: No explicit request; integration test task not included.
- [x] Q: Should the plan include a security scan and CVE remediation task? → A: Yes (default).
- [x] Q: Which Azure deployment target should the plan use? → A: Azure Container Apps (default).
