using TL;
using WTelegram;

namespace TradingStatisticsTelegramBotCore;

public class DealsCollector : IDisposable
{
    private const string DEAL_KEY_WORD = "#Deal";
    
    private Client _client;
    
    public async Task Init()
    {
        _client = await CreateTelegramConnection();
    }
    
    public async Task<List<DealData>> Collect(List<(DateTime, DateTime)> dateIntervals, string fromChatName)
    {
        var messages = await GetMessages(dateIntervals, fromChatName);
        var deals = GetDeals(messages);
        
        return deals;
    }

    private static List<DealData> GetDeals(IEnumerable<MessageBase> messages)
    {
        var deals = new List<DealData>();

        foreach (var msgBase in messages)
        {
            if (msgBase is not Message msg)
                continue;
            
            try
            {
                var dealData = DealData.Parse(msg);
                deals.Add(dealData);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error in message!\n {msg.message}\n");
                Console.WriteLine(e);
                throw;
            }
        }

        return deals;
    }

    private async Task<List<MessageBase>> GetMessages(List<(DateTime, DateTime)> dateIntervals, string fromChatName)
    {
        string inChatName = Configuration.ResultInChatName;
        long fromChatId = 0;
        long inChatId = 0;
        
        if (Configuration.ChatsIds.TryGetValue(fromChatName, out var fromId))
            fromChatId = fromId;
        
        if (Configuration.ChatsIds.TryGetValue(Configuration.ResultInChatName, out var inId))
            inChatId = inId;
        
        var chats = await _client.Messages_GetAllChats();

        foreach (var (id, chat) in chats.chats)
        {
            if (fromChatId != 0 && inChatId != 0)
                break;
                
            if (!chat.IsActive)
                continue;

            if (fromChatId == 0)
            {
                if (chat.MainUsername == fromChatName || chat.Title == fromChatName)
                {
                    fromChatId = id;
                    Configuration.ChatsIds.TryAdd(fromChatName, id);
                }
            }

            if (inChatId == 0)
            {
                if (chat.MainUsername == inChatName || chat.Title == inChatName)
                {
                    inChatId = id;
                    Configuration.ChatsIds.TryAdd(inChatName, id);
                }
            }
        }

        var messages = new List<MessageBase>();
        
        var peer = chats.chats[fromChatId]; // the chat (or User) we want

        foreach (var (startDate, endDate) in dateIntervals)
        {
            var foundMessages = await _client.Messages_Search(peer, DEAL_KEY_WORD, min_date: startDate, max_date: endDate);
            messages.AddRange(foundMessages.Messages);
        }
        
        return messages;
    }

    private async Task<Client> CreateTelegramConnection()
    {
        Client? client = null;
        
        try
        {
            client = new Client(Configuration.GetConfig);
            var myselfUser = await client.LoginUserIfNeeded();

            Console.WriteLine($"We are logged-in as {myselfUser} (id {myselfUser.id})");
            return client;
        }
        catch
        {
            client?.Dispose();
            throw;
        }
    }

    public void Dispose()
    {
        _client.Dispose();
        _client = null;
    }
}