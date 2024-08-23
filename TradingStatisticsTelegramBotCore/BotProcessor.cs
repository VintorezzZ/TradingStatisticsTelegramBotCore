namespace TradingStatisticsTelegramBotCore;

public class BotProcessor
{
    public async Task Start(CancellationTokenSource cts, Func<Task<string>> verificationCodeGetter)
    {
        Configuration.VerificationCodeGetter = verificationCodeGetter;
        
        var botProcessorOperation = new BotProcessorOperation(cts);
        await botProcessorOperation.Process();
    }
}