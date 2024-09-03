using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using WTelegram;

namespace TradingStatisticsTelegramBotCore.BotStates;

public class StatisticsCollectingState(TelegramBotClient bot, ClientDataStorage clientDataStorage, Client client, CancellationToken ctsToken) : IBotState
{
    private string _statisticsText;
    private DealsCollector _dealsCollector;
    
    public async Task<bool> Enter(Chat chat)
    {
        Logger.Log((int)LogLevel.Debug, $"Enter {GetType().Name} state");
        
        await bot.SendTextMessageAsync(
            chat,
            "Collecting statistics...",
            cancellationToken: ctsToken);
        
        try
        {
            _dealsCollector = new DealsCollector();
            
            await CreateStatistics(chat);
            await Exit(chat);
            await bot.SendTextMessageAsync(chat, "Enter /start to collect new data...", cancellationToken: ctsToken);
        }
        catch (Exception e)
        {
            Logger.Log((int)LogLevel.Error, e.Message);
        }
        
        return true;
    }

    public async Task<bool> Update(Chat chat, string queryData)
    {
        return true;
    }

    public async Task Exit(Chat chat)
    {
        
    }

    private async Task<bool> CreateStatistics(Chat chat)
    {
        var deals = await _dealsCollector.Collect(clientDataStorage.DateIntervals, Configuration.MessagesFromChatName, client);
        var statistics = TraderStatistics.Collect(deals, clientDataStorage.AllPredictions, clientDataStorage.SuccessfulPredictions, clientDataStorage.StartDeposit, clientDataStorage.DateIntervals);
        _statisticsText = TraderStatistics.GetText(statistics);

        clientDataStorage.StatisticsText = _statisticsText;

        Logger.Log((int)LogLevel.Debug, "Collecting statistics complete! Sending message...");

        await bot.SendTextMessageAsync(
            chat,
            "Collecting statistics complete! Sending message...",
            cancellationToken: ctsToken);
        
        await bot.SendTextMessageAsync(
            chat,
            clientDataStorage.StatisticsText,
            cancellationToken: ctsToken,
            parseMode: ParseMode.Html);
        
        return true;
    }
}