# Build from the repository root: docker build -f src/FinFlow.Orion.Workers/Dockerfile -t finflow-orion-workers .
FROM mcr.microsoft.com/dotnet/sdk:10.0-preview AS build
WORKDIR /src

COPY FinFlow.Orion.slnx ./
COPY src/FinFlow.Orion.Workers/FinFlow.Orion.Workers.csproj src/FinFlow.Orion.Workers/
COPY src/FinFlow.Orion.Application/FinFlow.Orion.Application.csproj src/FinFlow.Orion.Application/
COPY src/FinFlow.Orion.Contracts/FinFlow.Orion.Contracts.csproj src/FinFlow.Orion.Contracts/
COPY src/FinFlow.Orion.Domain/FinFlow.Orion.Domain.csproj src/FinFlow.Orion.Domain/
COPY src/FinFlow.Orion.Infrastructure/FinFlow.Orion.Infrastructure.csproj src/FinFlow.Orion.Infrastructure/
COPY src/FinFlow.Orion.Ledger/FinFlow.Orion.Ledger.csproj src/FinFlow.Orion.Ledger/
RUN dotnet restore src/FinFlow.Orion.Workers/FinFlow.Orion.Workers.csproj

COPY src/FinFlow.Orion.Workers/ src/FinFlow.Orion.Workers/
COPY src/FinFlow.Orion.Application/ src/FinFlow.Orion.Application/
COPY src/FinFlow.Orion.Contracts/ src/FinFlow.Orion.Contracts/
COPY src/FinFlow.Orion.Domain/ src/FinFlow.Orion.Domain/
COPY src/FinFlow.Orion.Infrastructure/ src/FinFlow.Orion.Infrastructure/
COPY src/FinFlow.Orion.Ledger/ src/FinFlow.Orion.Ledger/

RUN dotnet publish src/FinFlow.Orion.Workers/FinFlow.Orion.Workers.csproj -c Release -o /app --no-restore

# Worker service — no Kestrel/HTTP listener needed, so the smaller runtime image
# (not aspnet) is sufficient.
FROM mcr.microsoft.com/dotnet/runtime:10.0-preview AS runtime
WORKDIR /app
COPY --from=build /app .

ENTRYPOINT ["dotnet", "FinFlow.Orion.Workers.dll"]
