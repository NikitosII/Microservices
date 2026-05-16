FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["src/Microservices/Orchestrator.API/Orchestrator.API.csproj", "src/Microservices/Orchestrator.API/"]
COPY ["src/Common/EventBus/EventBus.csproj", "src/Common/EventBus/"]
RUN dotnet restore "src/Microservices/Orchestrator.API/Orchestrator.API.csproj"
COPY . .
WORKDIR "/src/src/Microservices/Orchestrator.API"
RUN dotnet build "Orchestrator.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Orchestrator.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Orchestrator.API.dll"]
