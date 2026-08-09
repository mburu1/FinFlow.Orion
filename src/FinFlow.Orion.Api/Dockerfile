# Build from the repository root: docker build -f src/FinFlow.Orion.Api/Dockerfile -t finflow-orion-api .
FROM mcr.microsoft.com/dotnet/sdk:10.0-preview AS build
WORKDIR /src

# Copy csproj files first so `dotnet restore` is cached independently of source changes.
COPY FinFlow.Orion.slnx ./
COPY src/FinFlow.Orion.Api/FinFlow.Orion.Api.csproj src/FinFlow.Orion.Api/
COPY src/FinFlow.Orion.Application/FinFlow.Orion.Application.csproj src/FinFlow.Orion.Application/
COPY src/FinFlow.Orion.Contracts/FinFlow.Orion.Contracts.csproj src/FinFlow.Orion.Contracts/
COPY src/FinFlow.Orion.Domain/FinFlow.Orion.Domain.csproj src/FinFlow.Orion.Domain/
COPY src/FinFlow.Orion.Infrastructure/FinFlow.Orion.Infrastructure.csproj src/FinFlow.Orion.Infrastructure/
COPY src/FinFlow.Orion.Ledger/FinFlow.Orion.Ledger.csproj src/FinFlow.Orion.Ledger/
RUN dotnet restore src/FinFlow.Orion.Api/FinFlow.Orion.Api.csproj

COPY src/FinFlow.Orion.Api/ src/FinFlow.Orion.Api/
COPY src/FinFlow.Orion.Application/ src/FinFlow.Orion.Application/
COPY src/FinFlow.Orion.Contracts/ src/FinFlow.Orion.Contracts/
COPY src/FinFlow.Orion.Domain/ src/FinFlow.Orion.Domain/
COPY src/FinFlow.Orion.Infrastructure/ src/FinFlow.Orion.Infrastructure/
COPY src/FinFlow.Orion.Ledger/ src/FinFlow.Orion.Ledger/

RUN dotnet publish src/FinFlow.Orion.Api/FinFlow.Orion.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0-preview AS runtime
WORKDIR /app
COPY --from=build /app .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "FinFlow.Orion.Api.dll"]
