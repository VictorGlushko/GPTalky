using CodeImageGenerator;
using Entity.Entities;
using Entity.Repositories;
using FFMpegCore;
using Microsoft.Extensions.Configuration;
using OpenAI.GPT3;
using OpenAI.GPT3.Managers;
using OpenAI.GPT3.ObjectModels;
using OpenAI.GPT3.ObjectModels.RequestModels;
using System.Text.RegularExpressions;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Message = Telegram.Bot.Types.Message;

namespace Core;

public class Worker
{
    private readonly TelegramBotClient _telegramBotClient;
    private readonly IConfiguration _config;
    private readonly IUnitOfWork _dbContext;
    private readonly Commander _commander;
    private readonly Generator _generator;
    private readonly OpenAIService _openAiService;

    public Worker(IConfiguration config, IUnitOfWork dbContext, Commander commander, Generator generator)
    {
        _config = config;
        _dbContext = dbContext;
        _commander = commander;
        _generator = generator;
        _telegramBotClient = new TelegramBotClient(_config.GetValue<string>("TelegramApiKey"));
        _openAiService = new OpenAIService(new OpenAiOptions()
        {
            ApiKey = _config.GetValue<string>("OpenAiApiKey")!,
        });


        if (!Directory.Exists(StaticLines.TmpFolder))
            Directory.CreateDirectory(StaticLines.TmpFolder);
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

    private async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        try
        {
            if (update.Message?.From is null) return;


            var chatId = update.Message.Chat.Id;
            var telegramUserId = update.Message.From.Id;

            await botClient.SendChatActionAsync(chatId, ChatAction.Typing);




            switch (update.Message?.Type)
            {
                case MessageType.Text:
                    {
                        var message = update.Message.Text;

                        if (message?.First() == StaticLines.CommandSymbol)
                        {
                            await _commander.FigureOutAsync(message, update.Message.From);
                        }
                        else
                        {
                            await HandleTextMessageAsync(telegramUserId, chatId, message);
                        }

                    }
                    break;

                case MessageType.Voice:
                    {
                        var saveResult = await GetTextMessageAsync(update.Message.Voice.FileId, chatId);

                        if (saveResult.IsSuccess)
                        {
                            await HandleTextMessageAsync(telegramUserId, chatId, saveResult.resultText);
                        }
                        else
                        {
                            Console.WriteLine(saveResult.resultText);
                        }
                    }
                    break;
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }

    private async Task HandleTextMessageAsync(long telegramUserId, long chatId, string message)
    {
        var userFromDb = await _dbContext.Users.GetUserByTelegramId(telegramUserId);
        if (userFromDb is not null)
        {
            await _dbContext.Messages.AddMessageAsync(userFromDb.Id, chatId, message,
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

                var ansyrtwer = completionResult.Choices;

                var answer = completionResult.Choices.First().Message.Content;

               // answer = System.IO.File.ReadAllText("w:\\VSProjects\\GPTalky\\GPTalky\\bin\\Debug\\net6.0\\win-x64\\tmp\\longMessage.txt");

          
                await _dbContext.Messages.AddMessageAsync(userFromDb.Id, chatId, answer, ChatMessageRole.Assistant);
                await _dbContext.CompleteAsync();

                var replacedAnswer = GPTalkyHelper.ReplaceSymbols(answer);

                //await System.IO.File.WriteAllTextAsync($".\\tmp\\lastMessage.txt", answer);

                try
                {
                    var result = await _telegramBotClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: replacedAnswer,
                        parseMode: ParseMode.MarkdownV2,
                        disableNotification: true);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    throw;
                }


              
                Regex reg = new Regex(@"```([^\n]+)\n(.+?)```", RegexOptions.Singleline);

                var matchesCollection = reg.Matches(answer);



                if (matchesCollection.Count > 0)
                {


                    
                    var qwe = matchesCollection.MaxBy(g =>g.Value.Length);
                    var code = qwe.Groups[2].Value;


                    var result = await _generator.GetCodeImageAsync(code);

                    if (result.IsSuccess)
                    {
                        await _telegramBotClient.SendPhotoAsync(chatId, InputFile.FromStream(result.Stream));

                    }
                    else
                    {
                        Console.WriteLine(result.Error);
                    }
    
                }
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


    public static byte[] ReadFully(Stream input)
    {
        byte[] buffer = new byte[16 * 1024];
        using (MemoryStream ms = new MemoryStream())
        {
            int read;
            while ((read = input.Read(buffer, 0, buffer.Length)) > 0)
            {
                ms.Write(buffer, 0, read);
            }
            return ms.ToArray();
        }
    }

    private async Task<(bool IsSuccess, string resultText)> GetTextMessageAsync(string fileId, long chatId)
    {
        CancellationToken ct = new CancellationToken();

        //string fileId = update.Message.Voice.FileId;

        string inputOggFile = Path.Combine(StaticLines.TmpFolder, $"{chatId}.ogg");
        string outputMp3File = Path.Combine(StaticLines.TmpFolder, $"{chatId}.mp3");

        await using (Stream fileStream = System.IO.File.Create(inputOggFile))
        {
            var file = await _telegramBotClient.GetInfoAndDownloadFileAsync(
                fileId: fileId,
                destination: fileStream,
                cancellationToken: ct);
        };

        await FFMpegArguments
                .FromFileInput(inputOggFile)
                .OutputToFile(outputMp3File)
                .ProcessAsynchronously();

        var sampleFile = await System.IO.File.ReadAllBytesAsync(outputMp3File);

        var audioResult = await _openAiService.Audio.CreateTranscription(new AudioCreateTranscriptionRequest
        {
            FileName = outputMp3File,
            File = sampleFile,
            Model = Models.WhisperV1,
            ResponseFormat = StaticValues.AudioStatics.ResponseFormat.VerboseJson
        });

        if (audioResult.Successful)
        {
            return (true, string.Join("\n", audioResult.Text));
        }
        else
        {
            if (audioResult.Error == null)
            {
                throw new Exception("Unknown Error");
            }

            var errorText = $"{audioResult.Error.Code}: {audioResult.Error.Message}";

            return (false, errorText);
        }
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

