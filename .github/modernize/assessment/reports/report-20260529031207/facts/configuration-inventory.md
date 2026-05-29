# Configuration & Externalized Settings Inventory

Configuration is file-first with selective environment-variable overrides for data and messaging integrations, and there is no dedicated external configuration service in this repository.

## Configuration Sources

| Source | Type | Path/Location | Notes |
|---|---|---|---|
| ASP.NET config | XML | `web.config` | WCF service model, connection string, health handler, debug settings |
| Project build config | MSBuild XML | `ZavaAlertService.csproj` | Framework target and Debug/Release build configurations |
| Container build config | Dockerfile | `Dockerfile` | Mono runtime image and compile behavior |
| Environment variables | Process env | `DB_*`, `DATABASE_*`, `RABBITMQ_*` | Used for DB and RabbitMQ settings in runtime code |

## Build Profiles

| Profile | Activation | Purpose | Key Dependencies/Plugins |
|---|---|---|---|
| Debug | Default when unspecified | Local/dev compilation with debug symbols | .NET Framework 4.8 references |
| Release | Explicit `Configuration=Release` | Production-oriented build output path | .NET Framework 4.8 references |

## Runtime Profiles

| Profile | Activation Method | Config Files | Key Overrides |
|---|---|---|---|
| Default | Host defaults | `web.config` | WCF endpoint and default SQL connection string |
| Env override mode | Process environment | `DbConfig.cs`, `RabbitMqPublisher.cs` | DB and RabbitMQ host/credential overrides |

## Properties Inventory

| Property Key | Default | Profiles | Source |
|---|---|---|---|
| `connectionStrings/ZavaBankDb` | SQL Server connection string in `web.config` | Default | `web.config` |
| `system.web/compilation@debug` | `true` | Default | `web.config` |
| `DB_HOST` / `DATABASE_HOST` | none | Env override mode | Environment variable |
| `DB_PORT` / `DATABASE_PORT` | none | Env override mode | Environment variable |
| `DB_NAME` / `DATABASE_NAME` | none | Env override mode | Environment variable |
| `DB_USER` / `DATABASE_USER` | none | Env override mode | Environment variable |
| `DB_PASSWORD` / `DATABASE_PASSWORD` | none | Env override mode | Environment variable |
| `RABBITMQ_HOST` | `rabbitmq` | Env override mode | Environment variable |
| `RABBITMQ_MANAGEMENT_PORT` | `15672` | Env override mode | Environment variable |
| `RABBITMQ_USER` | `zava_app` | Env override mode | Environment variable |
| `RABBITMQ_PASSWORD` | `zava_pass` | Env override mode | Environment variable |
| `RABBITMQ_VHOST` | `/zavabank` | Env override mode | Environment variable |

## Startup Parameters & Resource Requirements

| Service | JVM/Runtime Options | Memory | Instance Count |
|---|---|---|---|
| ZavaAlertService (IIS/WCF) | None explicitly declared in repo | Not specified | Not specified |
| Mono container build path | Dockerfile compiles with `mcs` | Not specified | Not specified |

## Startup Dependency Chain

1. Application host starts and loads `web.config`.
2. Service operations depend on SQL Server connectivity before request handling can complete.
3. Alert publishing depends on RabbitMQ Management API availability when alerts trigger.

## Secrets & Sensitive Configuration

| Secret Reference | Type | Storage (masked) |
|---|---|---|
| `connectionStrings/ZavaBankDb` password segment | DB credential | `web.config` (masked in process handling) |
| `DB_PASSWORD` / `DATABASE_PASSWORD` | DB credential | Environment variable |
| `RABBITMQ_PASSWORD` | Messaging credential | Environment variable |

### Secrets Provisioning Workflow

Secrets are expected from host-level configuration: either embedded connection string in `web.config` or runtime environment variables for DB and RabbitMQ credentials. The code reads these values at request time and binds them directly to SQL and RabbitMQ client requests; no managed identity, secret vault, or rotation workflow is defined in-repo.

## Feature Flags

| Flag Name | Default | Controlled By |
|---|---|---|
| None detected | N/A | N/A |

## Framework & Runtime Versions

| Component | Version | Source |
|---|---|---|
| .NET Framework target | 4.8 | `ZavaAlertService.csproj` |
| WCF service stack | Framework inbox (4.8) | `IAlertService.cs`, `web.config` |
| ASP.NET runtime target | 4.8 | `web.config` |
| Docker base image | `mono:6.12` | `Dockerfile` |
| MSBuild ToolsVersion | 4.0 project format | `ZavaAlertService.csproj` |
