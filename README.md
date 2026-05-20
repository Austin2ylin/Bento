# Bento 便當訂餐系統

以 .NET 8 Web API 為核心的便當訂餐示範專案，涵蓋微服務架構、EF Core Code-First、Outbox Pattern、Redis Cache-Aside、RabbitMQ、MongoDB、YARP Gateway、React 18、Blazor Server、Docker Compose、Kubernetes 與 Jenkins CI/CD。

---

## 架構總覽

```
瀏覽器
  │
  ├── :3000  React SPA (bento-client, nginx-unprivileged :8080)
  ├── :3001  Blazor Server (bento-blazor)
  │
  :80  NGINX (反向代理 + WebSocket 升級，輪流分發給兩個前端)
  │
  :5000  YARP Gateway (bento-gateway)  ← /gateway/* → /api/*
  │
  :5050  Bento API (bento-api)
     │
     ├── PostgreSQL :5432   主資料庫 (users / menu_items / orders / order_items / outbox_messages)
     ├── Redis      :6379   菜單快取 (Cache-Aside, TTL 10 分鐘)
     ├── RabbitMQ   :5672   非同步訂單事件佇列 (order-created)
     └── MongoDB    :27017  訂單事件 Log (schema-less BsonDocument)
```

**Outbox Pattern 流程：**

```
POST /api/orders
  → OrderService 驗證使用者與菜單
  → 同一個 PostgreSQL transaction：
      寫入 orders / order_items
      寫入 outbox_messages (x2：rabbitmq + mongo-log)
  → OutboxDispatcherService 每 5 秒輪詢
      → 推送 RabbitMQ  (order-created:rabbitmq)
      → 寫入 MongoDB   (order-created:mongo-log)
      → 失敗時指數退避重試，最長間隔 300 秒
```

---

## 技術堆疊

| 層級 | 技術 |
|------|------|
| API | .NET 8 Web API、EF Core 8、FluentValidation、YARP |
| 資料庫 | PostgreSQL 16、Redis 7、RabbitMQ 3、MongoDB 7 |
| 前端 | React 18 + TypeScript + Vite + Axios |
| 前端 (管理) | Blazor Server (.NET 8) |
| 容器 | Docker Compose (modular `include:`)、NGINX |
| CI/CD | Jenkins (Jenkinsfile)、Docker Hub |
| 部署 | Kubernetes (minikube / 任意叢集)、Render |

---

## 專案結構

```
Bento/
├── docker-compose.yml                  # 主入口，include 三個子檔
├── docker-compose.alternatives.yml     # MSSQL / MariaDB 替代方案
├── render.yaml                         # Render 雲端部署設定
├── .env.example                        # 環境變數範本（複製為 .env）
│
├── backend/
│   ├── docker-compose.yml              # bento-api、bento-gateway、postgres、mongo、redis、rabbitmq
│   ├── Bento.Api/
│   │   ├── Controllers/                # UsersController、MenusController、OrdersController、CacheController
│   │   ├── Services/                   # OrderService、OutboxDispatcherService、RedisService、RabbitMqService、MongoService
│   │   ├── Data/                       # BentoDbContext、BentoDbContextFactory、Migrations/
│   │   ├── Models/                     # Entities、Requests.cs、Responses.cs
│   │   ├── Validators/                 # OrderValidator (FluentValidation)
│   │   ├── Constants/                  # OrderStatuses、CacheKeys、OutboxMessageTypes
│   │   ├── Examples/                   # DatabaseFirstExample.cs（DB-First 參考）
│   │   └── Program.cs
│   └── Bento.Gateway/
│       ├── appsettings.json            # YARP 路由：/gateway/{**catch-all} → bento-api:5050
│       └── Program.cs
│
├── frontend/
│   ├── docker-compose.yml              # bento-client、bento-blazor
│   ├── bento-client/                   # React 18 + TypeScript + Vite
│   │   ├── src/
│   │   │   ├── api/bentoApi.ts         # Axios 封裝，呼叫 Gateway
│   │   │   ├── pages/                  # DashboardPage、MenuPage、OrderPage
│   │   │   └── types.ts
│   │   └── nginx.conf                  # SPA fallback，監聽 8080（nginx-unprivileged）
│   └── bento-blazor/                   # Blazor Server
│       ├── Pages/                      # Index.razor、Menu.razor、Order.razor
│       └── Services/BentoApiService.cs # HttpClient 封裝，呼叫 Gateway
│
├── infra/
│   ├── docker-compose.yml              # nginx、jenkins
│   ├── nginx/nginx.conf                # upstream bento_frontends（client:8080 + blazor:3001）
│   └── jenkins/
│       ├── Dockerfile
│       └── Jenkinsfile                 # build → test → docker build → docker push
│
└── k8s/
    ├── namespace.yaml
    ├── configmap.yaml                  # bento-app-config、bento-nginx-config
    ├── secret.yaml.example             # 複製為 secret.yaml，填入 base64 值
    ├── api-deployment.yaml             # replicas: 2、readinessProbe、livenessProbe
    ├── gateway-deployment.yaml
    ├── frontend-deployment.yaml        # React (nginx-unprivileged, port 8080)
    ├── blazor-deployment.yaml
    ├── nginx-deployment.yaml           # type: LoadBalancer
    ├── postgres-statefulset.yaml       # Headless Service + PVC 5Gi
    ├── mongo-statefulset.yaml          # Headless Service + PVC 5Gi
    ├── redis-deployment.yaml
    ├── rabbitmq-deployment.yaml
    └── hpa.yaml                        # bento-api：min 2、max 5、CPU 70%
```

---

## 快速啟動（Docker Compose）

### 1. 建立環境設定

```bash
cp .env.example .env
```

Windows PowerShell：

```powershell
Copy-Item .env.example .env
```

### 2. 填入必填值

開啟 `.env`，至少修改以下五個變數：

```env
POSTGRES_PASSWORD=your_postgres_password
MONGO_INITDB_ROOT_PASSWORD=your_mongo_password
REDIS_PASSWORD=your_redis_password
CACHE_ADMIN_API_KEY=replace_with_a_long_random_value
VITE_BENTO_CACHE_ADMIN_KEY=replace_with_the_same_value_as_CACHE_ADMIN_API_KEY
```

產生隨機 key：

```bash
openssl rand -base64 32
```

Windows PowerShell：

```powershell
[Convert]::ToBase64String((1..32 | ForEach-Object { Get-Random -Maximum 256 }))
```

> `CACHE_ADMIN_API_KEY` 與 `VITE_BENTO_CACHE_ADMIN_KEY` 必須填入**相同的值**。前者是 API 端驗證用，後者是 React build-time 注入，呼叫 `DELETE /api/cache/menu` 時附在 `X-Cache-Admin-Key` Header。

### 3. 啟動所有服務

```bash
docker compose up -d --build
```

Migration 已在 API 啟動時自動執行（`Program.cs` 呼叫 `db.Database.Migrate()`），不需要額外手動執行。

若需要手動套用（例如 container 重啟後）：

```bash
docker exec -it bento-api dotnet ef database update
```

### 服務位址

| 服務 | 網址 | 說明 |
|------|------|------|
| React 前端 | http://localhost:3000 | Vite 打包後由 nginx-unprivileged 服務 |
| Blazor 前端 | http://localhost:3001 | Blazor Server，含 SignalR |
| NGINX | http://localhost:80 | 反向代理，輪流分發兩個前端 |
| Swagger UI | http://localhost:5050/swagger | API 文件與測試介面 |
| Bento API | http://localhost:5050 | 直接打 API |
| API Gateway | http://localhost:5000 | 通常透過 `/gateway` 前綴使用 |
| RabbitMQ 管理 | http://localhost:15672 | 帳密取自 `.env` 的 `RABBITMQ_DEFAULT_USER/PASS` |
| Jenkins | http://localhost:8080 | CI/CD 管理介面 |

> 初次啟動 Jenkins 需要從容器 log 取得初始密碼：`docker logs jenkins`

---

## 本機開發（不用 Docker）

執行前需有本機或外部的 PostgreSQL、Redis、RabbitMQ、MongoDB 服務，並在 `backend/Bento.Api/appsettings.Development.json` 填入對應的連線字串。

**API：**

```bash
cd backend/Bento.Api
dotnet ef database update
dotnet run --urls "http://0.0.0.0:5050"
```

**Gateway：**

```bash
cd backend/Bento.Gateway
dotnet run --urls "http://0.0.0.0:5000"
```

**React：**

```bash
cd frontend/bento-client
cp .npmrc.example .npmrc   # 企業網路 proxy 才需要
npm ci
npm run dev
```

**Blazor：**

```bash
cd frontend/bento-blazor
dotnet run --urls "http://0.0.0.0:3001"
```

---

## API 端點

前端通常透過 Gateway 呼叫（`:5000/gateway/api/...`），也可以直接打 API（`:5050/api/...`）。

### 使用者

| 方法 | 路徑 | 說明 |
|------|------|------|
| `GET` | `/api/users` | 取得全部使用者 |
| `GET` | `/api/users/{id}` | 取得單一使用者 |
| `POST` | `/api/users` | 建立使用者（Email 唯一） |

**POST /api/users 請求範例：**

```json
{
  "name": "王小明",
  "email": "ming@example.com"
}
```

### 菜單

| 方法 | 路徑 | 說明 |
|------|------|------|
| `GET` | `/api/menus` | 取得全部菜單（Redis 快取 10 分鐘） |
| `GET` | `/api/menus/{id}` | 取得單一菜單項目 |
| `POST` | `/api/menus` | 新增菜單（同時清除 Redis 快取） |
| `PUT` | `/api/menus/{id}` | 更新菜單（同時清除 Redis 快取） |
| `DELETE` | `/api/menus/{id}` | 刪除菜單（同時清除 Redis 快取） |

Migration 預設種入 4 筆菜單資料：排骨便當 $110、雞腿便當 $120、鯖魚便當 $130、素食便當 $100。

### 訂單

| 方法 | 路徑 | 說明 |
|------|------|------|
| `GET` | `/api/orders` | 取得全部訂單（含使用者、品項） |
| `GET` | `/api/orders/{id}` | 取得單一訂單 |
| `POST` | `/api/orders` | 建立訂單（觸發 Outbox） |
| `PATCH` | `/api/orders/{id}/status` | 更新訂單狀態 |

**POST /api/orders 請求範例：**

```json
{
  "userId": 1,
  "items": [
    { "menuItemId": 1, "quantity": 2 },
    { "menuItemId": 3, "quantity": 1 }
  ]
}
```

**訂單狀態（PATCH）允許值：**`待確認`、`製作中`、`已完成`、`已取消`

**驗證規則（FluentValidation）：**

- `userId` > 0
- `items` 不可為空
- 同一個 `menuItemId` 不可重複
- 單一品項數量：1 ～ 20
- 全部品項總數量 ≤ 50

### 快取管理

| 方法 | 路徑 | 說明 |
|------|------|------|
| `GET` | `/api/cache/menu` | 查看快取狀態（回傳 `source: redis\|database`） |
| `DELETE` | `/api/cache/menu` | 強制清除菜單快取（需要 Header） |

`DELETE /api/cache/menu` 需要攜帶：

```
X-Cache-Admin-Key: <你的 CACHE_ADMIN_API_KEY>
```

> Development 環境若未設定 `Cache:AdminApiKey`，允許無 Header 清除。Production 環境若未設定則拒絕所有請求。

---

## 資料模型

```
users
  id, name, email(唯一), created_at

menu_items
  id, name, price(18,2), is_available, updated_at

orders
  id, user_id → users.id (CASCADE), status(20), total_amount(18,2), ordered_at

order_items
  PK(order_id, menu_item_id)
  order_id  → orders.id    (CASCADE)
  menu_item_id → menu_items.id (RESTRICT)
  quantity, unit_price(18,2)

outbox_messages
  id(GUID), type(120), aggregate_id(order.id)
  created_at, processed_at(null=未處理)
  attempt_count, next_attempt_at, last_error(1000)
  Index: (processed_at, next_attempt_at)、aggregate_id
```

---

## EF Core Migration

新增 migration：

```bash
cd backend/Bento.Api
dotnet ef migrations add <MigrationName> --output-dir Data/Migrations
dotnet ef database update
```

Container 內套用：

```bash
docker exec -it bento-api dotnet ef database update
```

---

## 替代資料庫

`docker-compose.alternatives.yml` 提供 MSSQL 與 MariaDB，可替換主 PostgreSQL：

```bash
# 啟動 MSSQL
docker compose -f docker-compose.alternatives.yml up mssql -d

# 啟動 MariaDB
docker compose -f docker-compose.alternatives.yml up mariadb -d
```

MSSQL 最低記憶體需求 2 GB，預設為 Developer 版。替換後需修改 `.env` 的連線字串，並更換 EF Core Provider（`Microsoft.EntityFrameworkCore.SqlServer` 或 `Pomelo.EntityFrameworkCore.MySql`）。

---

## Kubernetes 部署

Manifests 位於 `k8s/`，Namespace 為 `bento`。

### 1. 建立 Namespace

```bash
kubectl apply -f k8s/namespace.yaml
```

### 2. 建立 Secret

```bash
cp k8s/secret.yaml.example k8s/secret.yaml
# 編輯 secret.yaml，將所有 <base64_encoded> 換成實際的 base64 值
# 產生方式：echo -n "your_value" | base64
kubectl apply -f k8s/secret.yaml
```

`secret.yaml` 已加入 `.gitignore`，不會被 commit。需填入的欄位：

| Key | 說明 |
|-----|------|
| `ConnectionStrings__PostgreSql` | 完整 Postgres 連線字串 |
| `POSTGRES_PASSWORD` | Postgres 密碼 |
| `Redis__ConnectionString` | Redis 連線字串（含密碼） |
| `REDIS_PASSWORD` | Redis 密碼 |
| `Mongo__ConnectionString` | MongoDB 連線字串（含密碼） |
| `MONGO_INITDB_ROOT_PASSWORD` | MongoDB root 密碼 |
| `Cache__AdminApiKey` | 快取管理 API 金鑰 |

### 3. 建立 ConfigMap

```bash
kubectl apply -f k8s/configmap.yaml
```

### 4. 修改 Image 名稱

四個 Deployment 的 `image` 欄位預設為 `your-dockerhub-name/...`，需替換為你的 Docker Hub 帳號：

```bash
sed -i 's/your-dockerhub-name/<your-username>/g' \
  k8s/api-deployment.yaml \
  k8s/blazor-deployment.yaml \
  k8s/frontend-deployment.yaml \
  k8s/gateway-deployment.yaml
```

### 5. 套用所有 Manifests

```bash
kubectl apply -f k8s/postgres-statefulset.yaml
kubectl apply -f k8s/mongo-statefulset.yaml
kubectl apply -f k8s/redis-deployment.yaml
kubectl apply -f k8s/rabbitmq-deployment.yaml
kubectl apply -f k8s/api-deployment.yaml
kubectl apply -f k8s/gateway-deployment.yaml
kubectl apply -f k8s/frontend-deployment.yaml
kubectl apply -f k8s/blazor-deployment.yaml
kubectl apply -f k8s/nginx-deployment.yaml
kubectl apply -f k8s/hpa.yaml
```

### HPA

`hpa.yaml` 設定 `bento-api`：最少 2 個 Pod、最多 5 個、CPU 使用率超過 70% 自動擴展。

啟用前須安裝 metrics-server：

```bash
# minikube
minikube addons enable metrics-server
```
---

## CI/CD（Jenkins）

`infra/jenkins/Jenkinsfile` 定義四個 Stage：

| Stage | 說明 |
|-------|------|
| `build` | `dotnet build` 三個 .NET 專案（API、Gateway、Blazor） |
| `dotnet test` | 自動掃描 `*Tests.csproj`，找不到則跳過 |
| `docker build` | 建置四個 Image（api、gateway、client、blazor），Tag 為 `BUILD_NUMBER` |
| `docker push` | 推送到 Docker Hub（需要 `dockerhub` Credential） |

Image 命名規則：`<DOCKERHUB_NAMESPACE>/<service>:<BUILD_NUMBER>`

Jenkins 掛載了宿主機的 `/var/run/docker.sock`，可直接執行 `docker` 命令。

---

## Render 部署

`render.yaml` 定義了 Render 上的完整部署設定：

| 服務 | 類型 | 說明 |
|------|------|------|
| `bento-api` | Web Service | 從 Dockerfile 部署，PostgreSQL 連線字串由 Render 自動注入 |
| `bento-gateway` | Web Service | YARP 設定透過環境變數覆寫 API 位址 |
| `bento-blazor` | Web Service | Gateway URL 指向 Render 上的 bento-gateway |
| `bento-client` | Web Service | Build args 注入 `VITE_BENTO_GATEWAY_BASE_URL` |
| `bento-postgres` | PostgreSQL | Render 托管資料庫（free plan，256 MB） |

Redis、MongoDB、RabbitMQ 不包含在 `render.yaml` 中，需另外使用外部免費服務：
- Redis：Upstash（10k commands/day）
- MongoDB：MongoDB Atlas M0（512 MB）
- RabbitMQ：CloudAMQP Little Lemur（1M messages/month）

部署後需在 Render Dashboard 手動設定 `Redis__ConnectionString`、`Mongo__ConnectionString`、`RabbitMq__*` 等環境變數（`render.yaml` 中標記為 `sync: false`）。

---

## 設定檔與環境變數

### 運作原理

.NET 的設定系統有優先順序，**後者覆蓋前者**：

```
appsettings.json          ← 最低優先，放本機 Docker 預設值（可 commit）
appsettings.{Env}.json    ← 環境特定覆蓋（Development.json 可 commit，含佔位值）
環境變數                   ← 最高優先，正式環境在此填真實密碼（不 commit）
```

環境變數命名規則：將 JSON 路徑的 `:` 換成 `__`。

| JSON 路徑 | 對應環境變數名稱 |
|-----------|----------------|
| `ConnectionStrings:PostgreSql` | `ConnectionStrings__PostgreSql` |
| `Redis:ConnectionString` | `Redis__ConnectionString` |
| `RabbitMq:Host` | `RabbitMq__Host` |
| `RabbitMq:Password` | `RabbitMq__Password` |
| `Mongo:ConnectionString` | `Mongo__ConnectionString` |
| `Cache:AdminApiKey` | `Cache__AdminApiKey` |


---

### 各環境設定方式

#### Docker Compose（本機）

真實密碼填在 `.env`（已加入 `.gitignore`，不會 commit）：

```bash
cp .env.example .env
# 編輯 .env，填入各服務密碼
```

`backend/docker-compose.yml` 的 `environment:` 區塊會把 `.env` 的值組合成完整連線字串，自動覆蓋 `appsettings.json`：

```yaml
environment:
  ConnectionStrings__PostgreSql: Host=postgres;...;Password=${POSTGRES_PASSWORD}
  Redis__ConnectionString: redis:6379,password=${REDIS_PASSWORD}
  Mongo__ConnectionString: mongodb://${MONGO_INITDB_ROOT_USERNAME}:${MONGO_INITDB_ROOT_PASSWORD}@mongo:27017
  Cache__AdminApiKey: ${CACHE_ADMIN_API_KEY}
```

#### Render（雲端）

在 Render Dashboard → 各服務 → **Environment** 頁籤逐一填入，或透過 `render.yaml` 的 `envVars` 設定（`sync: false` 的欄位需要手動在 Dashboard 填）：

| 環境變數 | 說明 |
|----------|------|
| `ConnectionStrings__PostgreSql` | Render 可設定為自動從 Database 注入 |
| `Redis__ConnectionString` | Upstash endpoint + token |
| `RabbitMq__Host` | CloudAMQP hostname |
| `RabbitMq__UserName` | CloudAMQP username |
| `RabbitMq__Password` | CloudAMQP password |
| `RabbitMq__VirtualHost` | CloudAMQP vhost |
| `Mongo__ConnectionString` | MongoDB Atlas 連線字串 |
| `Cache__AdminApiKey` | 快取管理金鑰（Render 可 auto-generate） |
| `Cors__AllowedOrigins__0` | React 前端的 Render URL |
| `Cors__AllowedOrigins__1` | Blazor 前端的 Render URL |

#### Kubernetes

填在 `k8s/secret.yaml`（已加入 `.gitignore`，不會 commit）。詳見 [Kubernetes 部署](#kubernetes-部署)一節。

---

### .env 必填變數

| 變數 | 說明 |
|------|------|
| `POSTGRES_PASSWORD` | PostgreSQL 密碼 |
| `MONGO_INITDB_ROOT_PASSWORD` | MongoDB root 密碼 |
| `REDIS_PASSWORD` | Redis 認證密碼 |
| `CACHE_ADMIN_API_KEY` | 快取清除 API 金鑰（`openssl rand -base64 32`） |
| `VITE_BENTO_CACHE_ADMIN_KEY` | 與 `CACHE_ADMIN_API_KEY` 相同值，注入 React build |

其他變數皆有預設值，視需求修改：

| 變數 | 預設值 | 說明 |
|------|--------|------|
| `POSTGRES_USER` | `bento` | PostgreSQL 帳號 |
| `POSTGRES_DB` | `bentodb` | PostgreSQL 資料庫名稱 |
| `RABBITMQ_DEFAULT_USER` | `guest` | RabbitMQ 帳號（正式環境應改） |
| `RABBITMQ_DEFAULT_PASS` | `guest` | RabbitMQ 密碼（正式環境應改） |
| `VITE_BENTO_GATEWAY_BASE_URL` | `http://localhost:5000/gateway` | React 呼叫 Gateway 的 URL |
| `BLAZOR_GATEWAY_BASE_URL` | `http://bento-gateway:5000/gateway` | Blazor 呼叫 Gateway 的 URL |
| `DOCKERHUB_USERNAME` | — | Docker Hub 帳號（Jenkins CI/CD push 用） |
| `DOCKERHUB_TOKEN` | — | Docker Hub Access Token |

企業網路 proxy（通常不需要）：`PROXY_USER`、`PROXY_PASS`、`HTTP_PROXY`、`HTTPS_PROXY`
