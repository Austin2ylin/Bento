# Bento 便當訂餐系統

以 .NET 8 Web API 為後端的便當訂餐示範專案，涵蓋 RESTful API、EF Core Code-First、DTO 分層、Redis 快取、RabbitMQ、MongoDB、Docker Compose、Kubernetes、React 與 Blazor。

## 功能與教材重點

- 使用者、菜單、訂單 API，採 RESTful plural routes。
- Controller 不直接回傳 EF Entity，透過 request/response models 控制 API contract。
- `OrderService` 承接訂單建立與狀態更新邏輯，避免 Controller 過胖。
- EF Core Code-First 建立 PostgreSQL 主資料表與 `outbox_messages`。
- 建立訂單時先寫 PostgreSQL 與 outbox，再由背景服務重試派送 RabbitMQ 與 MongoDB log。
- Redis 快取菜單清單，新增、更新、刪除菜單時會清除快取。
- 快取清除 API 受 `X-Cache-Admin-Key` 保護。
- React 18 + TypeScript 與 Blazor Server 皆透過 Gateway 呼叫 API。
- Docker Compose 與 Kubernetes 提供容器化部署範例。

## 技術堆疊

- .NET 8 Web API
- Entity Framework Core 8 + PostgreSQL
- FluentValidation
- Redis
- RabbitMQ
- MongoDB
- YARP Gateway
- React 18 + TypeScript + Vite + Axios
- Blazor Server
- Docker Compose / Kubernetes / NGINX / Jenkins

## 專案結構

```text
Bento/
├── docker-compose.yml
├── .env.example
├── backend/
│   ├── Bento.Api/
│   │   ├── Constants/
│   │   │   ├── CacheKeys.cs
│   │   │   ├── OrderStatuses.cs
│   │   │   └── OutboxMessageTypes.cs
│   │   ├── Controllers/
│   │   │   ├── CacheController.cs
│   │   │   ├── MenuController.cs
│   │   │   ├── OrderController.cs
│   │   │   └── UserController.cs
│   │   ├── Data/
│   │   │   ├── BentoDbContext.cs
│   │   │   ├── BentoDbContextFactory.cs
│   │   │   └── Migrations/
│   │   ├── Models/
│   │   │   ├── MenuItem.cs
│   │   │   ├── Order.cs
│   │   │   ├── OrderItem.cs
│   │   │   ├── OutboxMessage.cs
│   │   │   ├── Requests.cs
│   │   │   ├── Responses.cs
│   │   │   └── User.cs
│   │   ├── Services/
│   │   │   ├── MongoService.cs
│   │   │   ├── OrderService.cs
│   │   │   ├── OutboxDispatcherService.cs
│   │   │   ├── RabbitMqService.cs
│   │   │   └── RedisService.cs
│   │   ├── Validators/
│   │   │   └── OrderValidator.cs
│   │   └── Program.cs
│   ├── Bento.Gateway/
│   └── docker-compose.yml
├── frontend/
│   ├── bento-client/
│   │   ├── .npmrc.example
│   │   ├── src/
│   │   └── Dockerfile
│   ├── bento-blazor/
│   └── docker-compose.yml
├── infra/
└── k8s/
```

目前 Entity、Request、Response 都放在 `Models/`。這已經避免直接回傳 Entity，但如果教材要求更嚴格的分層，可以再拆成 `Models/Entities`、`Contracts/Requests`、`Contracts/Responses`。

## 資料模型與流程

主要關聯：

```text
User 1 --- * Order 1 --- * OrderItem * --- 1 MenuItem
```

建立訂單流程：

```text
POST /api/orders
  -> OrderService 驗證 user/menu
  -> 寫入 orders / order_items
  -> 同一個 DB transaction 寫入 outbox_messages
  -> OutboxDispatcherService 背景輪詢
  -> 成功派送 RabbitMQ order-created
  -> 成功寫入 MongoDB order log
```

這樣 RabbitMQ 或 MongoDB 暫時失敗時，事件不會直接遺失，而是保留在 `outbox_messages` 等待重試。

## API 端點

直接打 API：

```text
GET    /api/menus
GET    /api/menus/{id}
POST   /api/menus
PUT    /api/menus/{id}
DELETE /api/menus/{id}

GET    /api/orders
GET    /api/orders/{id}
POST   /api/orders
PATCH  /api/orders/{id}/status

GET    /api/users
GET    /api/users/{id}
POST   /api/users

GET    /api/cache/menu
DELETE /api/cache/menu
```

前端通常透過 Gateway 呼叫，所以實際 URL 會是：

```text
http://localhost:5000/gateway/api/menus
http://localhost:5000/gateway/api/orders
http://localhost:5000/gateway/api/users
```

`DELETE /api/cache/menu` 需要 header：

```text
X-Cache-Admin-Key: <你的 CACHE_ADMIN_API_KEY>
```

Development 環境若沒有設定 `Cache:AdminApiKey`，後端會允許清除快取；Production 或有設定 key 時必須帶正確 header。

## 必填設定

先複製環境檔：

```bash
cp .env.example .env
```

Windows PowerShell：

```powershell
Copy-Item .env.example .env
```

請至少修改 `.env` 裡這些值：

```env
POSTGRES_PASSWORD=your_postgres_password
MONGO_INITDB_ROOT_PASSWORD=your_mongo_password
REDIS_PASSWORD=your_redis_password
CACHE_ADMIN_API_KEY=replace_with_a_long_random_value
VITE_BENTO_CACHE_ADMIN_KEY=replace_with_the_same_value_as_CACHE_ADMIN_API_KEY
```

建議值：

- `POSTGRES_PASSWORD`：PostgreSQL 密碼，例如 `bento_postgres_12345`
- `MONGO_INITDB_ROOT_PASSWORD`：MongoDB root 密碼，例如 `bento_mongo_12345`
- `REDIS_PASSWORD`：Redis 密碼，例如 `bento_redis_12345`
- `CACHE_ADMIN_API_KEY`：快取管理 API key，請用長隨機字串
- `VITE_BENTO_CACHE_ADMIN_KEY`：React build-time 設定，填同一個快取管理 key

產生隨機 key：

```bash
openssl rand -base64 32
```

Windows PowerShell 可用：

```powershell
[Convert]::ToBase64String((1..32 | ForEach-Object { Get-Random -Maximum 256 }))
```

如果你的公司電腦有 proxy，需要自行建立本機 npm 設定：

```bash
cd frontend/bento-client
cp .npmrc.example .npmrc
```

Windows PowerShell：

```powershell
cd frontend/bento-client
Copy-Item .npmrc.example .npmrc
```

然後在 `.npmrc` 填公司 proxy。不要提交 `.npmrc`，因為裡面可能有帳密，`.gitignore` 已排除它。

## Docker 啟動

```bash
docker compose up -d --build
docker exec -it bento-api dotnet ef database update
```

常用網址：

| 服務 | 網址 |
| --- | --- |
| React 前端 | http://localhost:3000 |
| Blazor 前端 | http://localhost:3001 |
| Swagger UI | http://localhost:5050/swagger |
| API Gateway | http://localhost:5000 |
| RabbitMQ 管理介面 | http://localhost:15672 |
| Jenkins | http://localhost:8080 |

RabbitMQ 帳密取自 `.env`：

```env
RABBITMQ_DEFAULT_USER=guest
RABBITMQ_DEFAULT_PASS=guest
```

## 手動啟動

後端：

```bash
cd backend/Bento.Api
dotnet ef database update
dotnet run --urls "http://0.0.0.0:5050"
```

Gateway：

```bash
cd backend/Bento.Gateway
dotnet run --urls "http://0.0.0.0:5000"
```

React：

```bash
cd frontend/bento-client
npm ci
npm run build
npm run dev
```

Blazor：

```bash
cd frontend/bento-blazor
dotnet run --urls "http://0.0.0.0:3001"
```

## EF Core Migration

```bash
cd backend/Bento.Api
dotnet ef migrations add <MigrationName> --output-dir Data/Migrations
dotnet ef database update
```

Docker container 內套用：

```bash
docker exec -it bento-api dotnet ef database update
```

目前 migration 會建立：

- `users`
- `menu_items`
- `orders`
- `order_items`
- `outbox_messages`

## Kubernetes 注意事項

`k8s/configmap.yaml` 目前是課程示範用。正式環境不應把密碼與 API key 放 ConfigMap，應改用 Kubernetes Secret。

Production 必須設定 CORS：

```yaml
Cors__AllowedOrigins__0: "http://localhost:3000"
Cors__AllowedOrigins__1: "http://localhost:3001"
Cors__AllowedOrigins__2: "http://localhost"
```

React container 使用 `nginx-unprivileged`，container port 是 `8080`，Kubernetes service targetPort 也要是 `8080`。

## 公司 Windows 電腦執行

可以跑，建議使用：

- Windows 10/11
- Docker Desktop + WSL2 backend
- Git for Windows
- .NET 8 SDK
- Node.js 20 LTS
- Visual Studio Code

公司環境常見注意事項：

- Docker Desktop 要能拉 Docker Hub image。
- npm / NuGet 可能需要 proxy。
- Port 可能被公司軟體占用，尤其 `80`、`1433`、`3000`、`3001`、`5000`、`5050`、`5432`、`6379`、`8080`。
- 若 port 衝突，改 `.env` 的 `*_PORT` 值。
- `.npmrc` 只放本機，不要 commit。

Windows PowerShell 啟動：

```powershell
Copy-Item .env.example .env
docker compose up -d --build
docker exec -it bento-api dotnet ef database update
```

## 驗證指令

```bash
dotnet build backend/Bento.Api/Bento.Api.csproj --no-restore
dotnet build frontend/bento-blazor/bento-blazor.csproj --no-restore

cd frontend/bento-client
npm ci
npm run build
```

## 安全注意事項

- 不要提交 `.env`。
- 不要提交 `.npmrc`，尤其不能包含 proxy 帳密。
- `CACHE_ADMIN_API_KEY`、資料庫密碼、Redis 密碼、MongoDB 密碼應每個環境不同。
- Kubernetes 正式部署請改用 Secret，不要把密碼放 ConfigMap。

## 課程涵蓋主題

Docker 安裝與指令、SQL Server Container、Code-First 一對多與多對多關聯、EF Core Migration、PostgreSQL / MariaDB Container、MongoDB、Redis 快取、自訂驗證與路由限制、CORS 設定、Swagger UI、React 前端 Container、Blazor Server Container、RabbitMQ 非同步訊息、Outbox pattern、Jenkins CI/CD、NGINX Load Balancer、BFF API Gateway、Kubernetes 部署、Pod 水平擴展與容錯。

DB-First 與手機端（Xamarin / MAUI）不實作。DB-First 的 scaffold 指令與說明以註解方式保留在 `Examples/DatabaseFirstExample.cs`，供課程講解對比用途。

## 授權

MIT
