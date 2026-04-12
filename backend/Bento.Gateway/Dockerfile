FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["Bento.Gateway.csproj", "./"]
RUN dotnet restore "./Bento.Gateway.csproj"

COPY . .
RUN dotnet publish "./Bento.Gateway.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# 建立非 root 系統帳號，降低容器遭入侵後的影響範圍
RUN groupadd --system appgroup && useradd --system --gid appgroup --no-create-home appuser

ENV ASPNETCORE_URLS=http://+:5000
EXPOSE 5000

COPY --from=build /app/publish .
RUN chown -R appuser:appgroup /app

USER appuser

ENTRYPOINT ["dotnet", "Bento.Gateway.dll"]
