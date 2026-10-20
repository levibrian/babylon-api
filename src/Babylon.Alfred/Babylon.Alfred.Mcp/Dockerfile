FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY src/Babylon.Alfred/Babylon.Alfred.Mcp/Babylon.Alfred.Mcp.csproj src/Babylon.Alfred/Babylon.Alfred.Mcp/
COPY src/Babylon.Alfred/Babylon.Alfred.Api/Babylon.Alfred.Api.csproj src/Babylon.Alfred/Babylon.Alfred.Api/

RUN dotnet restore src/Babylon.Alfred/Babylon.Alfred.Mcp/Babylon.Alfred.Mcp.csproj

COPY src/Babylon.Alfred/Babylon.Alfred.Mcp/ src/Babylon.Alfred/Babylon.Alfred.Mcp/
COPY src/Babylon.Alfred/Babylon.Alfred.Api/ src/Babylon.Alfred/Babylon.Alfred.Api/
COPY .ai/mcp-context.md .ai/mcp-context.md

RUN dotnet publish src/Babylon.Alfred/Babylon.Alfred.Mcp/Babylon.Alfred.Mcp.csproj \
    -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "Babylon.Alfred.Mcp.dll"]
