# Telegram Feature

**Status**: Scaffolded, NOT feature-complete. Do not implement business logic without separate spec.

## What's Wired
- `TelegramController.cs` - Webhook endpoint (stub)
- `TelegramBotClient` registered in DI
- Dependency: `Telegram.Bot` v22.6.2

## What's NOT Wired
- Notification service (portfolio alerts, price movements)
- Message handlers (inbound commands)
- User-to-chat ID mapping
- Delivery confirmation

## Spec Required Before Implementation
- [ ] Define notification triggers (what events send messages?)
- [ ] Define command surface (what can users do via Telegram?)
- [ ] Define user enrollment flow (how do users link Telegram account?)
- [ ] Define error handling (rate limits, bot blocking)
- [ ] Define privacy/security model (who can message the bot?)
