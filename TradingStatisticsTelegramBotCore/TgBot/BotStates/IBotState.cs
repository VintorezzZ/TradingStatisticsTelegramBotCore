using Telegram.Bot;
using Telegram.Bot.Types;

namespace TradingStatisticsTelegramBotCore.BotStates;

public interface IBotState
{
    Task Enter(Chat chat);
    Task<bool> Update(Chat chat, string? queryData);
    Task Exit(Chat chat);
}