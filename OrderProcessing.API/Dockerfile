FROM mcr.microsoft.com/dotnet/aspnet:8.0.23 AS base
RUN apt-get update && apt-get install -y curl
USER $APP_UID
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0.417 AS build
WORKDIR /src
COPY ["OrderProcessing.API/OrderProcessing.API.csproj", "OrderProcessing.API/"]
COPY ["Entravel.ServiceDefaults/Entravel.ServiceDefaults.csproj", "Entravel.ServiceDefaults/"]
RUN dotnet restore "OrderProcessing.API/OrderProcessing.API.csproj"
COPY . .
WORKDIR "/src/OrderProcessing.API"

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./OrderProcessing.API.csproj" --no-restore -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
HEALTHCHECK --interval=30s --timeout=5s --start-period=5s --retries=3 CMD curl --fail http://localhost:8080/health || exit 1
ENTRYPOINT ["dotnet", "OrderProcessing.API.dll"]
