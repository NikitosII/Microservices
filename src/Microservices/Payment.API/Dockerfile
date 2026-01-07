FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["src/Microservices/Payment.API/Payment.API.csproj", "src/Microservices/Payment.API/"]
COPY ["src/Common/EventBus/EventBus.csproj", "src/Common/EventBus/"]
RUN dotnet restore "src/Microservices/Payment.API/Payment.API.csproj"
COPY . .
WORKDIR "/src/src/Microservices/Payment.API"
RUN dotnet build "Payment.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Payment.API.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Payment.API.dll"]
