using Entity;
using Entity.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using OpenAI.GPT3.Interfaces;
using OpenAI.GPT3;
using OpenAI.GPT3.Managers;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types;
using Telegram.Bot.Exceptions;
using Microsoft.VisualBasic;
using OpenAI.GPT3.ObjectModels;
using OpenAI.GPT3.ObjectModels.RequestModels;
using Message = Telegram.Bot.Types.Message;
using System.Threading;
using Entity.Repositories;

namespace Core;

public  class Worker
{
    private readonly TelegramBotClient _telegramBotClient;
    private readonly IConfiguration _config;
    private readonly IUnitOfWork _dbContext;
    private readonly Commander _commander;
    private readonly OpenAIService _openAiService;

    public Worker(IConfiguration config, IUnitOfWork dbContext, Commander commander)
    {
        _config = config;
        _dbContext = dbContext;
        _commander = commander;
        //var wqe = ;
        _telegramBotClient = new TelegramBotClient(config.GetValue<string>("TelegramApiKey"));
        _openAiService = new OpenAIService(new OpenAiOptions()
        {
            ApiKey = _config.GetValue<string>("OpenAiApiKey")!,
        });
    }

    public void Run()
    {

        CancellationTokenSource cts = new();

        ReceiverOptions receiverOptions = new()
        {
            AllowedUpdates = Array.Empty<UpdateType>() // receive all update types except ChatMember related updates
        };

        _telegramBotClient.StartReceiving(
            updateHandler: HandleUpdateAsync,
            pollingErrorHandler: HandlePollingErrorAsync,
            receiverOptions: receiverOptions,
            cancellationToken: cts.Token
        );
    }

    private async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update,
        CancellationToken cancellationToken)
    {
        switch (update.Message?.Type)
        { 
            case MessageType.Text:
            {
                if (update.Message.Text[0] == '/') 
                    await _commander.FigureOutAsync(update.Message.Text, update.Message.From);
                else
                    await HandleTextMessageAsync(botClient, update, cancellationToken);

            }break;
                
            case MessageType.Voice:
            {
                await GetTextMessageAsync(update);

            }break;

            default:
                break;
        }
    }

    private async Task HandleTextMessageAsync(ITelegramBotClient botClient, Update update,
        CancellationToken cancellationToken)
    {
        var chatId = update.Message.Chat.Id;
        var telegramUserId = update.Message.From.Id;
        var userFromDb = await _dbContext.Users.GetUserByTelegramId(telegramUserId);

        if (userFromDb is not null)
        {
            await _dbContext.Messages.AddMessageAsync(userFromDb.Id, chatId, update.Message.Text,
                ChatMessageRole.User);
            await _dbContext.CompleteAsync();

            var messages = _dbContext.Messages.GetLastMessages(userFromDb.Id, 10)
                .Select(m => new ChatMessage(ChatRoleHelper.GetRoleByEnum(m.ChatMessageRole), m.Text, null)).ToList();

            messages.Reverse();

            var completionResult = await _openAiService.ChatCompletion.CreateCompletion(new ChatCompletionCreateRequest
            {
                Messages = messages,
                Model = Models.ChatGpt3_5Turbo,
            });

            if (completionResult.Successful)
            {
                var answer = completionResult.Choices.First().Message.Content;
                await _dbContext.Messages.AddMessageAsync(userFromDb.Id, chatId, answer, ChatMessageRole.Assistant);
                await _dbContext.CompleteAsync();
                await botClient.SendTextMessageAsync(chatId, answer);
            }
            else
            {
                if (completionResult.Error == null)
                {
                    throw new Exception("Unknown Error");
                }

                if (completionResult.Error.Code == "context_length_exceeded")
                {

                }

                Console.WriteLine($"{completionResult.Error.Code}: {completionResult.Error.Message}");
            }
        }
        else
        {
            await SendHelpfulMessage(chatId);
        }
    }

    private async Task SendHelpfulMessage(long chatId)
    {
        Message answerMessage = await _telegramBotClient.SendTextMessageAsync(
            chatId: chatId,
            text: "Для начала общения используйте команду /start");
    }


    private async Task<string> GetTextMessageAsync(Update update)
    {
        string fileId = update.Message.Voice.FileId;


        return null;
        //throw new NotImplementedException();
    }

    static Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
    {
        var ErrorMessage = exception switch
        {
            ApiRequestException apiRequestException
                => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
            _ => exception.ToString()
        };

        Console.WriteLine(ErrorMessage);
        return Task.CompletedTask;
    }

}

