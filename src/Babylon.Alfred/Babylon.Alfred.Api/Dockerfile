FROM mcr.microsoft.com/dotnet/aspnet:8.0 as base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["Babylon.Alfred.Api/Babylon.Alfred.Api.csproj", "Babylon.Alfred.Api/"]
COPY ["Babylon.Alfred.Api.Tests/Babylon.Alfred.Api.Tests.csproj", "Babylon.Alfred.Api.Tests/"]

RUN dotnet restore "Babylon.Alfred.Api/Babylon.Alfred.Api.csproj"

COPY . .
WORKDIR "/src/Babylon.Alfred.Api/"
RUN dotnet build "Babylon.Alfred.Api.csproj" -c Release -o /app/build

WORKDIR "/src/Babylon.Alfred.Api.Tests/"
RUN dotnet test "Babylon.Alfred.Api.Tests.csproj"

WORKDIR "/src/Babylon.Alfred.Api/"
FROM build as publish
RUN dotnet publish "Babylon.Alfred.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base as final
WORKDIR /app
COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "Babylon.Alfred.Api.dll"]
