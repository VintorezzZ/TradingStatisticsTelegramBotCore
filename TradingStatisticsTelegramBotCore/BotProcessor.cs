namespace TradingStatisticsTelegramBotCore;

public class BotProcessor
{
    public async Task Start(CancellationTokenSource cts)
    {
        var botProcessorOperation = new BotProcessorOperation(cts);
        await botProcessorOperation.Process();
    }
}