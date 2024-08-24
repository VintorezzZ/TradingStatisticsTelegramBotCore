namespace TradingStatisticsTelegramBotCore;

public static class Logger
{
    public enum ELogType
    {
        Message,
        Warning,
        Error
    }
    
    public static Action<ELogType, string> Log { get; set; }
}