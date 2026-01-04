# dot-net-core-microservice

Basic .NET 8 Web API with controller and service layers plus Swagger/OpenAPI.

## Project Structure
```
dot-net-core-microservice/
├── src/
│   └── dot-net-core-microservice/
│       ├── Controllers/           # API controllers
│       ├── Services/             # Business logic services
│       ├── Models/               # Data models
│       ├── Program.cs            # Application entry point
│       └── *.csproj              # Project file
└── tests/
    └── dot-net-core-microservice.Tests/
        ├── Controllers/          # Controller unit tests
        ├── Services/             # Service unit tests
        └── *.csproj              # Test project file
```

## Prerequisites
- .NET 8 SDK installed (`dotnet --version` to verify)

## Restore & run
```bash
# From the root directory
dotnet restore src/dot-net-core-microservice/
dotnet run --project src/dot-net-core-microservice/
```
The app launches with Swagger UI at:
- http://localhost:5191/swagger
- https://localhost:7291/swagger

## Run tests
```bash
# From the root directory
dotnet test tests/dot-net-core-microservice.Tests/
```

## Sample request
```bash
curl http://localhost:5191/api/weatherforecast
```

## Notes
- HTTPS profile uses the local development certificate that ships with the .NET SDK; trust it if your browser warns.
- Swagger is enabled for the Development environment by default.
- Unit tests cover both service and controller layers using xUnit and Moq.

