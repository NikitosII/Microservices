FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["src/Microservices/ShoppingCart.API/ShoppingCart.API.csproj", "src/Microservices/ShoppingCart.API/"]
COPY ["src/Common/EventBus/EventBus.csproj", "src/Common/EventBus/"]
RUN dotnet restore "src/Microservices/ShoppingCart.API/ShoppingCart.API.csproj"
COPY . .
WORKDIR "/src/src/Microservices/ShoppingCart.API"
RUN dotnet build "ShoppingCart.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ShoppingCart.API.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ShoppingCart.API.dll"]
