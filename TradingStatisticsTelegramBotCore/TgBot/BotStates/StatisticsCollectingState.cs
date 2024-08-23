using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace TradingStatisticsTelegramBotCore.BotStates;

public class StatisticsCollectingState(TelegramBotClient bot, ClientDataStorage clientDataStorage, CancellationToken ctsToken) : IBotState
{
    private string _statisticsText;
    private DealsCollector _dealsCollector;
    
    public async Task Enter(Chat chat)
    {
        Console.WriteLine("Collecting statistics...");
        
        await bot.SendTextMessageAsync(
            chat,
            "Collecting statistics...",
            cancellationToken: ctsToken);

        await CreateStatistics(chat);
        
        await Exit(chat);
        
        await bot.SendTextMessageAsync(chat, "Enter /start to collect new data...", cancellationToken: ctsToken);
    }

    public async Task<bool> Update(Chat chat, string? queryData)
    {
        return true;
    }

    public async Task Exit(Chat chat)
    {
        if (_dealsCollector != null)
        {
            _dealsCollector.Dispose();
            _dealsCollector = null;
        }
    }

    private async Task<bool> CreateStatistics(Chat chat)
    {
        if (_dealsCollector == null)
        {
            _dealsCollector = new DealsCollector();
            await _dealsCollector.Init();
        }
        
        var deals = await _dealsCollector.Collect(clientDataStorage.DateIntervals, Configuration.MessagesFromChatName);
        var statistics = TraderStatistics.Collect(deals, clientDataStorage.AllPredictions, clientDataStorage.SuccessfulPredictions, clientDataStorage.StartDeposit, clientDataStorage.DateIntervals);
        _statisticsText = TraderStatistics.GetText(statistics);

        clientDataStorage.StatisticsText = _statisticsText;
        
        Console.WriteLine("Collecting statistics complete! Sending message...");

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