# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Bento (便當訂餐系統) is a lunch ordering system built as an educational project demonstrating microservices patterns. The stack is .NET 8 Web API + React 18/TypeScript frontend + Blazor Server, backed by PostgreSQL, Redis, RabbitMQ, and MongoDB.

## Common Commands

### Backend (.NET 8)

```bash
# Build individual projects
dotnet build backend/Bento.Api/Bento.Api.csproj
dotnet build backend/Bento.Gateway/Bento.Gateway.csproj
dotnet build frontend/bento-blazor/bento-blazor.csproj

# Run locally (requires running infrastructure)
cd backend/Bento.Api && dotnet run --urls "http://0.0.0.0:5050"
cd backend/Bento.Gateway && dotnet run --urls "http://0.0.0.0:5000"

# EF Core migrations
dotnet ef migrations add <Name> --project backend/Bento.Api --output-dir Data/Migrations
dotnet ef database update --project backend/Bento.Api
```

### Frontend (React)

```bash
cd frontend/bento-client
npm ci
npm run dev       # dev server on :3000
npm run build     # production bundle via Vite
```

### Docker (Full Stack)

```bash
# Start everything
cp .env.example .env   # fill in passwords first
docker compose up -d --build

# Apply DB migrations after API container is running
docker exec -it bento-api dotnet ef database update
```

No test suite or linter is configured (educational scope).

## Architecture

### Service Topology

```
Browser → NGINX (:80) → React SPA (:3000) or Blazor (:3001)
                     → YARP Gateway (:5000/gateway) → Bento API (:5050/api)
                                                          ↓
                                              PostgreSQL · Redis · RabbitMQ · MongoDB
```

### Key Patterns

**Outbox Pattern** — the core reliability mechanism. When an order is created, `OrderService` writes the order and an `OutboxMessage` in a single PostgreSQL transaction. `OutboxDispatcherService` (a hosted background service) polls every 5 seconds, dispatches to RabbitMQ (`order-created` queue) and MongoDB, and retries with exponential backoff on failure. This guarantees at-least-once delivery even if RabbitMQ or MongoDB is temporarily unavailable.

**API Gateway (YARP)** — `Bento.Gateway` proxies all `/gateway/*` traffic to `http://bento-api:5050/api/*`, decoupling frontends from the API's direct address.

**Redis Cache** — menu data is cached after the first read. Cache is invalidated on POST/PUT/DELETE menu operations. Cache deletion requires the `X-Cache-Admin-Key` header (env var `CACHE_ADMIN_API_KEY`).

**Request/Response DTOs** — controllers never expose EF entities; they map to/from `*Request` and `*Response` models in `Models/Requests` and `Models/Responses`. FluentValidation rules in `Validators/` are auto-applied via middleware.

### Backend Project Layout

```
backend/Bento.Api/
  Controllers/          # UsersController, MenusController, OrdersController, CacheController
  Services/             # OrderService, OutboxDispatcherService, RabbitMqService, MongoService, RedisService
  Data/                 # BentoDbContext, Migrations/
  Models/               # Entities, Requests/, Responses/
  Validators/           # FluentValidation (OrderValidator)
  Constants/            # OrderStatuses, CacheKeys, OutboxTypes
  Program.cs            # DI registration & middleware pipeline
```

### Database Schema

```
users (1) → orders (*) → order_items (*) → menu_items (1)
outbox_messages               # event log, polled by OutboxDispatcherService
```

### Access Points (local docker-compose)

| Service | URL |
|---|---|
| React frontend | http://localhost:3000 |
| Blazor frontend | http://localhost:3001 |
| API (direct) | http://localhost:5050 |
| Swagger | http://localhost:5050/swagger |
| Gateway | http://localhost:5000 |
| RabbitMQ UI | http://localhost:15672 (guest/guest) |

## Environment Variables

Copy `.env.example` to `.env` before running docker-compose. Critical variables:

```
POSTGRES_PASSWORD, MONGO_INITDB_ROOT_PASSWORD, REDIS_PASSWORD
CACHE_ADMIN_API_KEY          # API-side key for cache deletion
VITE_BENTO_CACHE_ADMIN_KEY   # Must match CACHE_ADMIN_API_KEY (sent by React frontend)
VITE_BENTO_GATEWAY_BASE_URL  # e.g. http://localhost:5000/gateway
```

Generate a key: `openssl rand -base64 32`

## Kubernetes

Manifests are in `k8s/`. The namespace is `bento`. All app config lives in `k8s/configmap.yaml`. Note: the configmap contains plaintext credentials — use Secrets for any real deployment.
