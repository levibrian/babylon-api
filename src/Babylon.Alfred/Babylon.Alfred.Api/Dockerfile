# ---------- BASE ----------
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080

# ---------- BUILD ----------
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy csproj (repo-root-relative!)
COPY src/Babylon.Alfred/Babylon.Alfred.Api/Babylon.Alfred.Api.csproj \
     src/Babylon.Alfred/Babylon.Alfred.Api/

RUN dotnet restore \
    src/Babylon.Alfred/Babylon.Alfred.Api/Babylon.Alfred.Api.csproj

# Copy all source code (repo root)
COPY . .

# Build
RUN dotnet build \
    src/Babylon.Alfred/Babylon.Alfred.Api/Babylon.Alfred.Api.csproj \
    -c Release -o /app/build

# ---------- PUBLISH ----------
FROM build AS publish
RUN dotnet publish \
    src/Babylon.Alfred/Babylon.Alfred.Api/Babylon.Alfred.Api.csproj \
    -c Release -o /app/publish /p:UseAppHost=false

# ---------- FINAL ----------
FROM base AS final
WORKDIR /app

# Install curl for health checks
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

COPY --from=publish /app/publish .

RUN adduser --disabled-password --gecos '' appuser && chown -R appuser /app
USER appuser

ENV ASPNETCORE_URLS=http://+:8080

HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
  CMD curl -f http://localhost/health || exit 1

ENTRYPOINT ["dotnet", "Babylon.Alfred.Api.dll"]
