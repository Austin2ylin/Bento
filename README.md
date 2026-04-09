# Bento 便當訂餐系統

以 .NET 8 Web API 為後端的示範專案，情境為簡易便當訂餐系統，涵蓋 Docker 容器化、Kubernetes 部署與微服務架構等現代後端開發實務。
前端使用 React 18，所有服務以 Docker Container 方式運行，不使用 IIS，不使用 .sln。

## 功能說明

- 瀏覽菜單、建立訂單，示範 EF Core Code-First 一對多與多對多資料關聯
- 菜單資料以 Redis 快取，可透過 API 手動清除
- 建立新訂單時透過 RabbitMQ 發送非同步通知
- 訂單 log 同步寫入 MongoDB，主資料庫使用 PostgreSQL
- 所有前端流量統一透過 BFF API Gateway（YARP）轉發
- NGINX 作為對外的 Load Balancer
- 附 Blazor Server 作為替代前端，課程對比示範用途
- Jenkins Pipeline 負責建置與推送 Docker Image
- 提供完整 Kubernetes 部署設定，含水平擴展示範

## 技術堆疊

**.NET 8** — Web API、Entity Framework Core 8（Code-First）、FluentValidation、YARP（Gateway）、Swagger UI
**React 18 + TypeScript** — Vite、Axios（統一打 Gateway）
**Blazor Server** — 替代前端示範，純 C# 撰寫，走同一個 Gateway
**資料庫** — PostgreSQL（主）、SQL Server、MariaDB、MongoDB、Redis
**基礎設施** — RabbitMQ、NGINX、Jenkins、Docker Compose、Kubernetes

## 專案結構

```
Bento/
├── docker-compose.yml
├── .env
├── .env.example
│
├── backend/
│   ├── Bento.Api/
│   │   ├── Entities/                        # EF Core Entity 類別（資料表對應）
│   │   │   ├── User.cs
│   │   │   ├── MenuItem.cs
│   │   │   ├── Order.cs
│   │   │   └── OrderItem.cs
│   │   │
│   │   ├── DTOs/                            # 前後端溝通用，Entity 不直接暴露給前端
│   │   │   ├── MenuItemDto.cs
│   │   │   ├── CreateMenuItemRequest.cs
│   │   │   ├── OrderDto.cs
│   │   │   ├── CreateOrderRequest.cs
│   │   │   └── UpdateOrderStatusRequest.cs
│   │   │
│   │   ├── Data/
│   │   │   ├── BentoDbContext.cs            # EF Core DbContext，設定所有關聯
│   │   │   └── Migrations/                  # dotnet ef migrations add 自動產生，勿手動修改
│   │   │       ├── 20240101000000_InitialCreate.cs
│   │   │       └── BentoDbContextModelSnapshot.cs
│   │   │
│   │   ├── Controllers/
│   │   │   ├── MenuController.cs
│   │   │   ├── OrderController.cs
│   │   │   ├── UserController.cs
│   │   │   └── CacheController.cs
│   │   │
│   │   ├── Services/
│   │   │   ├── RedisService.cs
│   │   │   ├── RabbitMqService.cs
│   │   │   └── MongoService.cs
│   │   │
│   │   ├── Validators/
│   │   │   └── CreateOrderRequestValidator.cs   # FluentValidation 自訂驗證規則
│   │   │
│   │   ├── Examples/
│   │   │   └── DatabaseFirstExample.cs      # DB-First scaffold 說明，全部為註解
│   │   │
│   │   ├── Program.cs
│   │   ├── appsettings.json
│   │   ├── appsettings.Development.json
│   │   ├── Bento.Api.csproj
│   │   └── Dockerfile
│   │
│   ├── Bento.Gateway/                       # BFF API Gateway（YARP）
│   │   ├── Program.cs
│   │   ├── appsettings.json                 # YARP 路由規則設定
│   │   ├── Bento.Gateway.csproj
│   │   └── Dockerfile
│   │
│   └── docker-compose.yml
│
├── frontend/
│   ├── bento-client/                        # React 18 + TypeScript（主力前端）
│   │   ├── src/
│   │   │   ├── pages/
│   │   │   │   ├── MenuPage.tsx
│   │   │   │   ├── OrderPage.tsx
│   │   │   │   └── DashboardPage.tsx
│   │   │   ├── components/
│   │   │   ├── api/
│   │   │   │   └── bentoApi.ts              # Axios 封裝，統一打 Gateway
│   │   │   ├── App.tsx
│   │   │   └── main.tsx
│   │   ├── vite.config.ts
│   │   ├── package.json
│   │   └── Dockerfile
│   │
│   ├── bento-blazor/                        # Blazor Server（課程示範）
│   │   ├── Pages/
│   │   │   ├── Menu.razor
│   │   │   └── Order.razor
│   │   ├── Services/
│   │   │   └── BentoApiService.cs
│   │   ├── Program.cs
│   │   ├── bento-blazor.csproj
│   │   └── Dockerfile
│   │
│   └── docker-compose.yml
│
├── infra/
│   ├── nginx/
│   │   ├── nginx.conf
│   │   └── Dockerfile
│   ├── jenkins/
│   │   ├── Dockerfile                       # Jenkins + Docker CLI
│   │   └── Jenkinsfile                      # Pipeline: build → test → docker push
│   └── docker-compose.yml
│
└── k8s/
    ├── namespace.yaml
    ├── configmap.yaml
    ├── api-deployment.yaml
    ├── gateway-deployment.yaml
    ├── frontend-deployment.yaml
    ├── blazor-deployment.yaml
    ├── nginx-deployment.yaml
    ├── postgres-statefulset.yaml
    ├── mongo-statefulset.yaml
    ├── redis-deployment.yaml
    └── rabbitmq-deployment.yaml
```

## Entity 資料模型（Code-First）

EF Core 依照這些 Entity 類別自動建立資料表，關聯設定在 `BentoDbContext.cs`。

```
User（使用者）                    MenuItem（便當品項）
┌──────────────┐                  ┌─────────────────┐
│ Id           │                  │ Id              │
│ Name         │                  │ Name            │  ← 如「排骨便當」
│ Email        │                  │ Price           │
└──────┬───────┘                  │ Category        │  ← 主餐 / 素食 / 套餐
       │ 1                        └────────┬────────┘
       │                                   │
       │ *                                 │ *（多對多）
┌──────┴───────────┐              ┌────────┴────────┐
│ Order（訂單）    │──── 1 : * ──▶│   OrderItem     │
│ Id               │              │ Id              │
│ OrderDate        │              │ OrderId         │
│ Status           │  ← 待確認   │ MenuItemId      │
│ UserId           │     備餐中   │ Quantity        │  ← 數量
└──────────────────┘     已完成  └─────────────────┘
```

## DTOs

Entity 不直接回傳給前端，透過 DTO 控制輸出欄位並與 Entity 解耦。

```
CreateOrderRequest   →  Controller 接收前端傳入
OrderDto             →  Controller 回傳給前端
CreateMenuItemRequest →  新增品項用
MenuItemDto          →  品項清單回傳用
```

## API 端點

```
GET    /api/menu
POST   /api/menu
PUT    /api/menu/{id}
DELETE /api/menu/{id}

GET    /api/order
POST   /api/order           寫入 PostgreSQL + 推 RabbitMQ 訊息
GET    /api/order/{id}
PATCH  /api/order/{id}/status

GET    /api/user
POST   /api/user

GET    /api/cache/menu      從 Redis 取快取，無則從 DB 撈並存入
DELETE /api/cache/menu      手動清除快取
```

Gateway 將上述端點掛在 `/gateway/*` 並轉發至 API。

## 快速開始

複製 `.env.example` 並填入設定值：

```bash
POSTGRES_USER=bento
POSTGRES_PASSWORD=...
POSTGRES_DB=bentodb
MSSQL_SA_PASSWORD=...
MONGO_INITDB_ROOT_USERNAME=bento
MONGO_INITDB_ROOT_PASSWORD=...
REDIS_PASSWORD=...
RABBITMQ_DEFAULT_USER=guest
RABBITMQ_DEFAULT_PASS=guest
```

**Docker（推薦）**

```bash
docker compose up -d
docker exec -it bento-api dotnet ef database update
```

**手動啟動**

```bash
cd backend/Bento.Api
dotnet ef database update
dotnet run --urls "http://0.0.0.0:5050"

cd frontend/bento-client
npm install && npm run dev
```

啟動後各服務網址：

| 服務              | 網址                                    |
| ----------------- | --------------------------------------- |
| React 前端        | http://localhost:3000                   |
| Blazor 前端       | http://localhost:3001                   |
| Swagger UI        | http://localhost:5050/swagger           |
| API Gateway       | http://localhost:5000                   |
| RabbitMQ 管理介面 | http://localhost:15672（guest / guest） |
| Jenkins           | http://localhost:8080                   |

## EF Core Migration 指令

```bash
# 進入 container
docker exec -it bento-api bash

# 首次建立 Migration（會在 Data/Migrations/ 產生檔案）
dotnet ef migrations add InitialCreate

# 套用至資料庫（建立資料表）
dotnet ef database update

# 之後每次修改 Entity 都要跑
dotnet ef migrations add <名稱>
dotnet ef database update

# 若要回滾上一個 Migration
dotnet ef migrations remove
```

## Docker 常用指令

```bash
docker compose up -d
docker compose down
docker compose down -v                        # 同時刪除 volume（資料會消失）
docker compose up -d --build bento-api        # 重新 build 特定服務
docker ps -a
docker logs bento-api -f
docker exec -it bento-api bash
docker volume ls
docker images
docker login                                  # 推送 image 前需先登入 Docker Hub
```

## Kubernetes 常用指令

```bash
kubectl apply -f k8s/
kubectl get pods -n bento
kubectl get svc -n bento
kubectl logs -n bento <pod-name>
kubectl exec -it -n bento <pod-name> -- bash
kubectl scale deployment bento-api --replicas=3 -n bento    # Pod 水平擴展示範
kubectl delete -f k8s/
```

## 課程涵蓋主題

Docker 安裝與指令、SQL Server Container、Code-First 一對多與多對多關聯、EF Core Migration、PostgreSQL / MariaDB Container、MongoDB、Redis 快取、自訂驗證與路由限制、CORS 設定、Swagger UI、React 前端 Container、Blazor Server Container、RabbitMQ 非同步訊息、Jenkins CI/CD、NGINX Load Balancer、BFF API Gateway、Kubernetes 部署、Pod 水平擴展與容錯。

DB-First 與手機端（Xamarin / MAUI）不實作。DB-First 的 scaffold 指令與說明以註解方式保留在 `Examples/DatabaseFirstExample.cs`，供課程講解對比用途。

## 授權

MIT
