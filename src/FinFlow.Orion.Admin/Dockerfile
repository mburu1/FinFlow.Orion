# Build from the repository root: docker build -f src/FinFlow.Orion.Admin/Dockerfile -t finflow-orion-admin .
FROM mcr.microsoft.com/dotnet/sdk:10.0-preview AS build
WORKDIR /src

COPY FinFlow.Orion.slnx ./
COPY src/FinFlow.Orion.Admin/FinFlow.Orion.Admin.csproj src/FinFlow.Orion.Admin/
COPY src/FinFlow.Orion.Contracts/FinFlow.Orion.Contracts.csproj src/FinFlow.Orion.Contracts/
RUN dotnet restore src/FinFlow.Orion.Admin/FinFlow.Orion.Admin.csproj

COPY src/FinFlow.Orion.Admin/ src/FinFlow.Orion.Admin/
COPY src/FinFlow.Orion.Contracts/ src/FinFlow.Orion.Contracts/

RUN dotnet publish src/FinFlow.Orion.Admin/FinFlow.Orion.Admin.csproj -c Release -o /app --no-restore

# Blazor WebAssembly publishes to a static wwwroot — served by nginx, no .NET
# runtime needed in the final image.
FROM nginx:alpine AS runtime
COPY --from=build /app/wwwroot /usr/share/nginx/html
COPY src/FinFlow.Orion.Admin/nginx.conf /etc/nginx/conf.d/default.conf
COPY src/FinFlow.Orion.Admin/docker-entrypoint.sh /docker-entrypoint.sh
RUN chmod +x /docker-entrypoint.sh

EXPOSE 80
ENTRYPOINT ["/docker-entrypoint.sh"]
