namespace TradingStatisticsTelegramBotCore;

public class ClientDataStorage
{
    public List<(DateTime, DateTime)> DateIntervals { get; set; }
    public int AllPredictions { get; set; }
    public int SuccessfulPredictions { get; set; }
    public double StartDeposit { get; set; }
    public string StatisticsText { get; set; }
}