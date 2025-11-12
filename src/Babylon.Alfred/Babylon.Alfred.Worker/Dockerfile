FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj files and restore dependencies
COPY ["Babylon.Alfred.Worker/Babylon.Alfred.Worker.csproj", "Babylon.Alfred.Worker/"]
COPY ["Babylon.Alfred.Api/Babylon.Alfred.Api.csproj", "Babylon.Alfred.Api/"]

RUN dotnet restore "Babylon.Alfred.Worker/Babylon.Alfred.Worker.csproj"

# Copy everything else and build
COPY . .
WORKDIR "/src/Babylon.Alfred.Worker"
RUN dotnet build "Babylon.Alfred.Worker.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Babylon.Alfred.Worker.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Babylon.Alfred.Worker.dll"]

