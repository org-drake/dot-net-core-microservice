FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY dot-net-core-microservice.sln ./
COPY src/dot-net-core-microservice/dot-net-core-microservice.csproj src/dot-net-core-microservice/
RUN dotnet restore src/dot-net-core-microservice/

COPY . .
RUN dotnet publish src/dot-net-core-microservice/ -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

COPY --from=build /app/publish .

HEALTHCHECK --interval=30s --timeout=3s --start-period=10s --retries=3 \
    CMD curl -f http://localhost:8080/healthz || exit 1

USER $APP_UID
ENTRYPOINT ["dotnet", "dot-net-core-microservice.dll"]
