# Configuration & Externalized Settings Inventory

The project uses XML configuration files and container environment variables as primary configuration sources, with minimal profile differentiation and no external secret manager integration.

## Configuration Sources

| Source | Type | Path/Location | Notes |
|---|---|---|---|
| Web.config | XML app config | `Web.config` | WCF service endpoints and SQL connection string |
| App.config | XML app settings | `App.config` | RabbitMQ host, port, credentials, queue name |
| docker-compose.yml | Container runtime config | `docker-compose.yml` | Service ports, environment variables, startup dependencies |
| packages.config | Dependency config | `packages.config` | NuGet package declaration for CodeDom provider |

## Build Profiles

| Profile | Activation | Purpose | Key Dependencies/Plugins |
|---|---|---|---|
| Debug | `Configuration=Debug` | Local development build symbols and diagnostics | .NET Framework build targets |
| Release | `Configuration=Release` | Optimized build output | .NET Framework build targets |

## Runtime Profiles

| Profile | Activation Method | Config Files | Key Overrides |
|---|---|---|---|
| Default | WCF host startup | `Web.config`, `App.config` | Service endpoint, SQL connection, RabbitMQ settings |
| Docker compose runtime | `docker compose up` | `docker-compose.yml` + mounted app configs | DB and MQ host/port and credentials via env vars |

## Properties Inventory

| Property Key | Default | Profiles | Source |
|---|---|---|---|
| `connectionStrings:DefaultConnection` | `Server=db;Database=ZavaBank;User Id=sa;******;` | Default, Docker | Web.config |
| `RabbitMqHost` | `rabbitmq` | Default | App.config |
| `RabbitMqPort` | `5672` | Default | App.config |
| `RabbitMqUser` | `guest` | Default | App.config |
| `RabbitMqPassword` | `[MASKED]` | Default | App.config |
| `AlertQueueName` | `alerts.triggered` | Default | App.config |
| `DB_HOST` | `db` | Docker | docker-compose env |
| `DB_PORT` | `1433` | Docker | docker-compose env |
| `DB_NAME` | `ZavaBank` | Docker | docker-compose env |
| `DB_USER` | `sa` | Docker | docker-compose env |
| `DB_PASSWORD` | `[MASKED]` | Docker | docker-compose env |
| `RABBITMQ_HOST` | `rabbitmq` | Docker | docker-compose env |
| `RABBITMQ_PORT` | `5672` | Docker | docker-compose env |
| `RABBITMQ_USER` | `guest` | Docker | docker-compose env |
| `RABBITMQ_PASSWORD` | `[MASKED]` | Docker | docker-compose env |
| `ALERT_QUEUE` | `alerts.triggered` | Docker | docker-compose env |

## Startup Parameters & Resource Requirements

| Service | JVM/Runtime Options | Memory | Instance Count |
|---|---|---|---|
| zava-alert-service | Mono xsp4 startup via container entrypoint | Not specified | 1 |
| db | SQL Server container defaults | Not specified | 1 |
| rabbitmq | RabbitMQ container defaults | Not specified | 1 |

## Startup Dependency Chain

1. `db` and `rabbitmq` start first.
2. `zava-alert-service` waits on compose `depends_on` start conditions.
3. Service readiness is exposed by `/Health.ashx`.

## Secrets & Sensitive Configuration

| Secret Reference | Type | Storage (masked) |
|---|---|---|
| `DefaultConnection` password | Database credential | Web.config (`[MASKED]`) |
| `RabbitMqPassword` | MQ credential | App.config (`[MASKED]`) |
| `DB_PASSWORD` | Database credential | docker-compose env (`[MASKED]`) |
| `RABBITMQ_PASSWORD` | MQ credential | docker-compose env (`[MASKED]`) |

### Secrets Provisioning Workflow

Secrets are currently embedded in local configuration and compose environment settings. Deployment initializes containers with these values directly; no managed identity, vault-backed retrieval, or central secret rotation workflow was detected.

## Feature Flags

| Flag Name | Default | Controlled By |
|---|---|---|
| None detected | N/A | N/A |

## Framework & Runtime Versions

| Component | Version | Source |
|---|---|---|
| .NET Framework target | 4.8 | `ZavaAlertService.csproj` |
| ASP.NET/WCF hosting | .NET Framework 4.8 stack | `Web.config`, service contract files |
| RabbitMQ image | 3-management | `docker-compose.yml` |
| SQL Server image | 2019-latest | `docker-compose.yml` |
| Mono base image | 6.12 | `Dockerfile` |
| Dotnet SDK in assessment environment | 8.0.413 | `dotnet --version` output |
