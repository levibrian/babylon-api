# ---------- BUILD ----------
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy ONLY the API csproj (repo-root-relative)
COPY src/Babylon.Alfred/Babylon.Alfred.Api/Babylon.Alfred.Api.csproj src/Babylon.Alfred/Babylon.Alfred.Api/

# Restore dependencies
RUN dotnet restore src/Babylon.Alfred/Babylon.Alfred.Api/Babylon.Alfred.Api.csproj

# Copy ONLY the API source
COPY src/Babylon.Alfred/Babylon.Alfred.Api/ src/Babylon.Alfred/Babylon.Alfred.Api/

# Publish
RUN dotnet publish src/Babylon.Alfred/Babylon.Alfred.Api/Babylon.Alfred.Api.csproj \
    -c Release \
    -o /app/publish \
    /p:UseAppHost=false

# ---------- RUNTIME ----------
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "Babylon.Alfred.Api.dll"]
