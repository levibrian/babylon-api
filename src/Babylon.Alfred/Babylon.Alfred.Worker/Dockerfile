FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy csproj files (repo-root-relative!)
COPY src/Babylon.Alfred/Babylon.Alfred.Worker/Babylon.Alfred.Worker.csproj src/Babylon.Alfred/Babylon.Alfred.Worker/
COPY src/Babylon.Alfred/Babylon.Alfred.Api/Babylon.Alfred.Api.csproj src/Babylon.Alfred/Babylon.Alfred.Api/

# Restore
RUN dotnet restore src/Babylon.Alfred/Babylon.Alfred.Worker/Babylon.Alfred.Worker.csproj

# Copy sources
COPY src/Babylon.Alfred/Babylon.Alfred.Worker/ src/Babylon.Alfred/Babylon.Alfred.Worker/
COPY src/Babylon.Alfred/Babylon.Alfred.Api/ src/Babylon.Alfred/Babylon.Alfred.Api/

# Publish
RUN dotnet publish src/Babylon.Alfred/Babylon.Alfred.Worker/Babylon.Alfred.Worker.csproj \
    -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Babylon.Alfred.Worker.dll"]
