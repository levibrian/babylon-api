# Telegram Feature

## Product Overview

Integration with Telegram Bot API for portfolio notifications and alerts. This feature is currently **in progress** and not fully implemented.

## Planned Capabilities

- Send portfolio summary notifications to users via Telegram.
- Alert on significant portfolio events (large price movements, rebalancing triggers).
- Potentially receive commands via Telegram (check portfolio, trigger actions).

## Architecture

```
Features/Telegram/
├── Controllers/
│   └── TelegramController.cs             # Webhook endpoint for Telegram bot
└── Extensions/
    └── ServiceCollectionExtensions.cs     # DI registration (TelegramBotClient)
```

## Dependencies

- `Telegram.Bot` NuGet package (v22.6.2)
- Telegram Bot Token configured via appsettings

## Status

This feature is scaffolded but not feature-complete. The controller and DI registration exist but full business logic (notification service, message handlers) still needs implementation.
