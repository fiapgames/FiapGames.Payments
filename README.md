# FiapGames.Payments

ASP.NET Core Web API responsável pelo processamento de pagamentos do FiapGames. Consome eventos `OrderPlacedEvent` via RabbitMQ (MassTransit), simula a aprovação/rejeição do pagamento, persiste o resultado no PostgreSQL e publica `PaymentProcessedEvent` para os demais serviços (Catalog, Notifications).

## Arquitetura

```
Configuration/   Opções de configuração (RabbitMqOptions)
Consumers/       Consumers MassTransit (OrderPlacedConsumer)
Controllers/     Endpoints HTTP (HealthController)
Data/            DbContext (PaymentsDbContext)
Models/          Entidades persistidas (Payment)
Services/        Regras de negócio (IPaymentService / PaymentService)
Migrations/      Migrations do EF Core
```

Fluxo:

```
RabbitMQ (order-placed) -> OrderPlacedConsumer -> PaymentService -> PostgreSQL
                                                 -> Publish(PaymentProcessedEvent) -> RabbitMQ
```

A API expõe apenas um endpoint HTTP de health check (`GET /health`); todo o processamento de pagamentos ocorre de forma assíncrona via eventos.

## Stack

- .NET 10 / ASP.NET Core Web API
- MassTransit + RabbitMQ
- Entity Framework Core + PostgreSQL
- Swagger (Swashbuckle)

## Executando localmente

```bash
docker compose up -d --build
```

Isso inicia RabbitMQ, PostgreSQL e a API (porta `8080`). As migrations do EF Core são aplicadas automaticamente na inicialização.

- Health check: `http://localhost:8080/health`
- Swagger: `http://localhost:8080/swagger`
- RabbitMQ management: `http://localhost:15672`

## Configuração

| Seção | Variável | Descrição |
|---|---|---|
| `ConnectionStrings` | `Postgres` | Connection string do PostgreSQL |
| `RabbitMq` | `Host`, `VirtualHost`, `UserName`, `Password` | Conexão com o RabbitMQ |

## Deploy (Kubernetes)

Manifests em `k8s/`:

- `namespace.yaml`
- `configmap.yaml` / `secret.yaml` — configuração do `payments-api`
- `deployment.yaml` — deployment da imagem `lucasceifador/fiapgames-payments-api:latest`

## Build e push da imagem Docker

```bash
docker build -t lucasceifador/fiapgames-payments-api:latest .
docker push lucasceifador/fiapgames-payments-api:latest
```
