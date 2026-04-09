/*
 DB-First 反向工程範例（僅課程說明，不執行）

 1) 先安裝 EF CLI：
    dotnet tool install --global dotnet-ef

 2) 安裝對應 Provider：
    dotnet add package Npgsql.EntityFrameworkCore.PostgreSQL

 3) 使用 scaffold 反向產生模型與 DbContext：
    dotnet ef dbcontext scaffold \
      "Host=localhost;Port=5432;Database=bentodb;Username=bento;Password=your_password" \
      Npgsql.EntityFrameworkCore.PostgreSQL \
      --output-dir Models/DbFirst \
      --context-dir Data/DbFirst \
      --context BentoDbContextFromDb \
      --use-database-names \
      --data-annotations \
      --force

 4) 何時使用 DB-First：
    - 現有資料庫已上線且 schema 穩定
    - 團隊先由 DBA 管理結構，再由程式端引用

 5) 本專案主流程仍採 Code-First：
    - 以 Model + Migration 維護 schema
    - 便於課程示範一對多、多對多關聯與演進
*/
