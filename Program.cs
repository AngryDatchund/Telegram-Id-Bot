using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using File = System.IO.File;

public class Program
{
    private static TelegramBotClient client;
    private static string token;
    private static string tokenFilePath => AppDomain.CurrentDomain.BaseDirectory + "Token.txt";
    private static int lastUpdateId;
    private static DateTime nextNotify;
    private static int updatesRecordedCounter;
    
    public static void Main()
    {
        LoadToken();
        LoadBot();

        while (true)
        {
            try
            {
                CheckMessages();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            
            Thread.Sleep(1000);
        }
    }

    private static void LoadToken()
    {
        if (File.Exists(tokenFilePath) == true)
        {
            token = File.ReadAllText(tokenFilePath);
        }
        
        while (TokenIsValid() == false)
        {
            Write("Enter valid telegram bot token: ");
            var result = Console.ReadLine();
            token = result;
        }   

        File.WriteAllText(tokenFilePath, token);
    }

    private static void LoadBot()
    {
        client = new TelegramBotClient(token);
    }

    private static bool TokenIsValid()
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return false;
        }

        if (token.Contains(":") == false)
        {
            return false;
        }

        try
        {
            client = new TelegramBotClient(token);
            var result = client.GetMeAsync().Result;
            if (result.Id < 1)
            {
                return false;
            }
            
            Write($"Loaded bot '{result.FirstName}{result.LastName}' with id {result.Id}");
            
        }
        catch
        {
            Write("Bot token is invalid!");
            return false;
        }

        return true;
    }

    private static void CheckMessages()
    {
        var updates = client.GetUpdatesAsync(lastUpdateId).Result;
        updatesRecordedCounter += updates.Length;

        if (nextNotify < DateTime.Now || nextNotify == DateTime.MinValue)
        {
            Write($"Located {updatesRecordedCounter} updates for last 30 minutes, last update id {lastUpdateId}");
            nextNotify = DateTime.Now.AddMinutes(30);
            updatesRecordedCounter = 0;
        }

        foreach (var update in updates)
        {
            if (update.Message == null)
            {
                continue;
            }

            var message = update.Message;
            if (message == null)
            {
                continue;
            }

            CheckMessage(message);
        }

        if (updates.Length > 0)
        {
            lastUpdateId = updates.Max(x => x.Id) + 1;
        }
    }

    private static void CheckMessage(Message message)
    {
        var text = message.Text;
        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }
        
        var chatId = message.Chat.Id;
        var senderId = message.From?.Id ?? 0;

        if (text == "/start")
        {
            SendCommands(chatId);
            SendId(chatId, senderId);
            return;
        }

        if (text == "/id")
        {
            SendId(chatId, senderId);
            return;
        }
    }

    private static void SendCommands(long chatId)
    {
        client.SendTextMessageAsync(chatId, "Available commands:\n" +
                                            "/id - Get chat id and your id").Wait();
    }

    private static void SendId(long chatId, long senderId)
    {
        client.SendTextMessageAsync(chatId, $"Your id: `{senderId}`\n" +
                                            $"Chat id: `{chatId}`", ParseMode.MarkdownV2).Wait();
    }

    private static void Write(string text)
    {
        Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] {text}");
    }
}