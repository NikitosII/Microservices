FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["src/Microservices/Product.API/Product.API.csproj", "src/Microservices/Product.API/"]
COPY ["src/Common/EventBus/EventBus.csproj", "src/Common/EventBus/"]
RUN dotnet restore "src/Microservices/Product.API/Product.API.csproj"
COPY ["src/Microservices/Product.API/", "src/Microservices/Product.API/"]
COPY ["src/Common/EventBus/", "src/Common/EventBus/"]
WORKDIR "/src/src/Microservices/Product.API"
RUN dotnet build "Product.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Product.API.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Product.API.dll"]
