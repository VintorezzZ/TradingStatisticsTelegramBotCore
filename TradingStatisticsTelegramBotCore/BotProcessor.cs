namespace TradingStatisticsTelegramBotCore;

public class BotProcessor
{
    /// <summary>
    /// Starts the bot processing logic and statistics calculation.
    /// </summary>
    /// <param name="processThroughTelegramBot"> Enable logic processing only through tg bot.</param>
    /// <param name="cts"> Cancellation token source to stop the bot.</param>
    /// <param name="verificationCodeGetter"> Leave it as null so verification code will be processed via telegram bot. </param>
    public async Task Start(bool processThroughTelegramBot, CancellationTokenSource cts,  Func<Task<string>> verificationCodeGetter = null)
    {
        Configuration.IsProcessingThroughTelegramBot = processThroughTelegramBot;
        
        if (verificationCodeGetter != null)
            Configuration.VerificationCodeGetter = verificationCodeGetter;
        
        var botProcessorOperation = new BotProcessorOperation(cts);
        await botProcessorOperation.Process();
    }
}