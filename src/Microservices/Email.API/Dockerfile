FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["src/Microservices/Email.API/Email.API.csproj", "src/Microservices/Email.API/"]
COPY ["src/Common/EventBus/EventBus.csproj", "src/Common/EventBus/"]
RUN dotnet restore "src/Microservices/Email.API/Email.API.csproj"
COPY ["src/Microservices/Email.API/", "src/Microservices/Email.API/"]
COPY ["src/Common/EventBus/", "src/Common/EventBus/"]
WORKDIR "/src/src/Microservices/Email.API"
RUN dotnet build "Email.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Email.API.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Email.API.dll"]
