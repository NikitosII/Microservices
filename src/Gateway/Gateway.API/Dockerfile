FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["src/Gateway/Gateway.API/Gateway.API.csproj", "src/Gateway/Gateway.API/"]
RUN dotnet restore "src/Gateway/Gateway.API/Gateway.API.csproj"
COPY ["src/Gateway/Gateway.API/", "src/Gateway/Gateway.API/"]
WORKDIR "/src/src/Gateway/Gateway.API"
RUN dotnet build "Gateway.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Gateway.API.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Gateway.API.dll"]
