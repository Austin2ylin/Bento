FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["bento-blazor.csproj", "./"]
RUN dotnet restore "./bento-blazor.csproj"

COPY . .
RUN dotnet publish "./bento-blazor.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# 建立非 root 系統帳號，降低容器遭入侵後的影響範圍
RUN groupadd --system appgroup && useradd --system --gid appgroup --no-create-home appuser

ENV ASPNETCORE_URLS=http://+:3001
EXPOSE 3001

COPY --from=build /app/publish .
RUN chown -R appuser:appgroup /app

USER appuser

ENTRYPOINT ["dotnet", "bento-blazor.dll"]
