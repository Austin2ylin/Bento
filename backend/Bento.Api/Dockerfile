FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["Bento.Api.csproj", "./"]
RUN dotnet restore "./Bento.Api.csproj"

COPY . .
RUN dotnet publish "./Bento.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:5050
EXPOSE 5050

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Bento.Api.dll"]
