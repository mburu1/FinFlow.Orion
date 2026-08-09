# Build from the repository root: docker build -f src/FinFlow.Orion.Webhooks/Dockerfile -t finflow-orion-webhooks .
FROM mcr.microsoft.com/dotnet/sdk:10.0-preview AS build
WORKDIR /src

COPY FinFlow.Orion.slnx ./
COPY src/FinFlow.Orion.Webhooks/FinFlow.Orion.Webhooks.csproj src/FinFlow.Orion.Webhooks/
COPY src/FinFlow.Orion.Application/FinFlow.Orion.Application.csproj src/FinFlow.Orion.Application/
COPY src/FinFlow.Orion.Contracts/FinFlow.Orion.Contracts.csproj src/FinFlow.Orion.Contracts/
COPY src/FinFlow.Orion.Domain/FinFlow.Orion.Domain.csproj src/FinFlow.Orion.Domain/
COPY src/FinFlow.Orion.Infrastructure/FinFlow.Orion.Infrastructure.csproj src/FinFlow.Orion.Infrastructure/
COPY src/FinFlow.Orion.Ledger/FinFlow.Orion.Ledger.csproj src/FinFlow.Orion.Ledger/
RUN dotnet restore src/FinFlow.Orion.Webhooks/FinFlow.Orion.Webhooks.csproj

COPY src/FinFlow.Orion.Webhooks/ src/FinFlow.Orion.Webhooks/
COPY src/FinFlow.Orion.Application/ src/FinFlow.Orion.Application/
COPY src/FinFlow.Orion.Contracts/ src/FinFlow.Orion.Contracts/
COPY src/FinFlow.Orion.Domain/ src/FinFlow.Orion.Domain/
COPY src/FinFlow.Orion.Infrastructure/ src/FinFlow.Orion.Infrastructure/
COPY src/FinFlow.Orion.Ledger/ src/FinFlow.Orion.Ledger/

RUN dotnet publish src/FinFlow.Orion.Webhooks/FinFlow.Orion.Webhooks.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0-preview AS runtime
WORKDIR /app
COPY --from=build /app .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "FinFlow.Orion.Webhooks.dll"]
