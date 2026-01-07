FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["src/Microservices/Coupon.API/Coupon.API.csproj", "src/Microservices/Coupon.API/"]
RUN dotnet restore "src/Microservices/Coupon.API/Coupon.API.csproj"
COPY . .
WORKDIR "/src/src/Microservices/Coupon.API"
RUN dotnet build "Coupon.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Coupon.API.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Coupon.API.dll"]
