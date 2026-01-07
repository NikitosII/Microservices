FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["src/Microservices/Order.API/Order.API.csproj", "src/Microservices/Order.API/"]
COPY ["src/Common/EventBus/EventBus.csproj", "src/Common/EventBus/"]
RUN dotnet restore "src/Microservices/Order.API/Order.API.csproj"
COPY . .
WORKDIR "/src/src/Microservices/Order.API"
RUN dotnet build "Order.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Order.API.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Order.API.dll"]
