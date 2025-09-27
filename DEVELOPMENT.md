# 🚀 Development Setup Guide

## Prerequisites
- .NET 8.0 SDK
- Cursor IDE (or VS Code)

## Quick Start

### 1. Install Extensions
Cursor will prompt you to install recommended extensions, or install them manually:
- C# Dev Kit
- C# 
- .NET Install Tool
- REST Client
- Thunder Client
- GitLens

### 2. Run the API
```bash
# Option 1: Using Cursor tasks
Ctrl+Shift+P → "Tasks: Run Task" → "run-api"

# Option 2: Using terminal
dotnet run --project src/Babylon.Alfred/Babylon.Alfred.Api/Babylon.Alfred.Api.csproj

# Option 3: Debug mode
F5 (uses launch.json configuration)
```

### 3. Test the API
- Open `test-api.http` file
- Click "Send Request" above any HTTP request
- Or use Thunder Client extension

## Available Tasks

| Task | Command | Description |
|------|---------|-------------|
| `build` | `dotnet build` | Build the solution |
| `run-api` | `dotnet run` | Run the API in development mode |
| `watch` | `dotnet watch run` | Run with hot reload |
| `publish` | `dotnet publish` | Publish for production |

## API Endpoints

### Investments
- `GET /api/v1/investments` - Investment summary
- `GET /api/v1/investments/holdings` - Current holdings
- `GET /api/v1/investments/assets` - All assets

### Transactions
- `GET /api/v1/investments/transactions` - All transactions
- `POST /api/v1/investments/transactions` - Create transaction
- `GET /api/v1/investments/transactions/{id}` - Get specific transaction
- `PUT /api/v1/investments/transactions/{id}` - Update transaction
- `DELETE /api/v1/investments/transactions/{id}` - Delete transaction

## Development Features

### Code Quality
- **EditorConfig** - Consistent code formatting
- **IntelliSense** - Enhanced C# IntelliSense
- **Error Lens** - Inline error highlighting
- **Code Spell Checker** - Catch typos

### API Testing
- **REST Client** - Test APIs with `.http` files
- **Thunder Client** - GUI-based API testing
- **Swagger** - Auto-generated API documentation at `/swagger`

### Git Integration
- **GitLens** - Enhanced Git capabilities
- **Auto-rename tags** - Rename paired XML/HTML tags

## Project Structure

```
src/Babylon.Alfred/
├── Babylon.Alfred.Api/           # Main API project
│   ├── Features/                 # Feature-based architecture
│   │   ├── Investments/          # Investment tracking feature
│   │   ├── Telegram/             # Telegram bot feature
│   │   └── Startup/              # Application startup
│   ├── Shared/                   # Shared components
│   └── Program.cs               # Application entry point
└── Babylon.Alfred.sln           # Solution file
```

## Debugging

1. Set breakpoints in your code
2. Press `F5` or use "Run and Debug" panel
3. The API will start in debug mode
4. Use the browser or REST Client to test endpoints

## Hot Reload

Use the `watch` task for development with hot reload:
```bash
Ctrl+Shift+P → "Tasks: Run Task" → "watch"
```

This will automatically restart the API when you make changes to the code.

## Troubleshooting

### Common Issues
1. **Port already in use**: Change the port in `launchSettings.json`
2. **Build errors**: Check that all NuGet packages are restored
3. **IntelliSense not working**: Restart Cursor and ensure C# Dev Kit is installed

### Useful Commands
```bash
# Restore packages
dotnet restore

# Clean and rebuild
dotnet clean && dotnet build

# Check for updates
dotnet list package --outdated
```


