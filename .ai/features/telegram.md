# Telegram Feature

## Status: SCAFFOLDED — NOT FEATURE COMPLETE

**Do not implement any business logic without a separate spec approved first.**

---

## What IS Wired

- `TelegramController.cs` — Webhook endpoint stub
- `TelegramBotClient` registered in DI
- Dependency: `Telegram.Bot` v22.6.2

---

## What Is NOT Wired

- Notification service (portfolio alerts, price movements)
- Message handlers (inbound commands)
- User-to-chat ID mapping
- Delivery confirmation

---

## Spec Required Before Any Implementation

Before touching this feature, define:

- [ ] Notification triggers (what events send messages?)
- [ ] Command surface (what can users do via Telegram?)
- [ ] User enrollment flow (how do users link their Telegram account?)
- [ ] Error handling (rate limits, bot blocking)
- [ ] Privacy/security model (who can message the bot?)
