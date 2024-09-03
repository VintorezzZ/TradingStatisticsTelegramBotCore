using Microsoft.Extensions.Logging;

namespace TradingStatisticsTelegramBotCore;

public static class Logger
{
    /// <summary>
    /// First parameter - LogLevel enum by microsoft.
    /// Second parameter - text message.
    /// </summary>
    public static Action<int, string> Log { get; set; }
}