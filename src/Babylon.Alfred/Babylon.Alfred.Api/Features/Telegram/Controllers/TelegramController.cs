using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace Babylon.Alfred.Api.Features.Telegram.Controllers;

[ApiController]
[Route("/api/v1/telegram")]
public class TelegramController(
    ITelegramBotClient botClient,
    ILogger<TelegramController> logger) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Post([FromBody] Update update)
    {
        try
        {
            if (update == null)
                return BadRequest("Could not parse update");

            if (update.Type is not UpdateType.Message)
                return BadRequest("Received request was not a message");
            
            if (update.Type is not UpdateType.Message)
            {
                return BadRequest("Received request was not a message");
            }

            var message = update.Message!;
            var chatId = message.Chat.Id;
            var text = message.Text;
            
            switch (text)
            {
                case "/start":
                    await botClient.SendMessage(chatId, "Welcome to Babylon Tracker! Use /add to add an expense.");
                    break;
                case "/add":
                    await botClient.SendMessage(chatId,
                        "Please enter your expense in the format: 'Amount Category Date' (e.g., '300 Insurance 2024-04-01').");
                    break;
                case "/view":
                    await botClient.SendMessage(chatId, "Fetching your recent expenses...");
                    // Call Babylon API to fetch expenses
                    break;
                default:
                    await botClient.SendMessage(chatId, $"You said: {text}");
                    break;
            }

            return Ok();
        }
        catch(Exception e)
        {
            return Problem();
        }
    }
}