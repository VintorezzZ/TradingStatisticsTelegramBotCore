using Microsoft.Extensions.Logging;
using TL;
using WTelegram;

namespace TradingStatisticsTelegramBotCore;

public class DealsCollector
{
    private const string DEAL_KEY_WORD = "#Deal";
    
    private Client _client;
    
    public async Task<List<DealData>> Collect(List<(DateTime, DateTime)> dateIntervals, string fromChatName, Client client)
    {
        _client = client;
        
        var messages = await GetMessages(dateIntervals, fromChatName);
        var deals = GetDeals(messages);
        
        return deals;
    }

    private static List<DealData> GetDeals(IEnumerable<MessageBase> messages)
    {
        Logger.Log((int) LogLevel.Debug, "Creating deals...");
        
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
                Logger.Log((int)LogLevel.Error, $"Error in message!\n{msg.message}\n");
                Logger.Log((int)LogLevel.Error, e.Message);
                throw;
            }
        }

        return deals;
    }

    private async Task<List<MessageBase>> GetMessages(List<(DateTime, DateTime)> dateIntervals, string fromChatName)
    {
        Logger.Log((int) LogLevel.Debug, "Getting messages...");
        
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
}