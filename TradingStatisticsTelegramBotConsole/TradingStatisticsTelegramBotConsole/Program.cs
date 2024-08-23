using TradingStatisticsTelegramBotCore;

var cts = new CancellationTokenSource();
var botProcessor = new BotProcessor();
await botProcessor.Start(cts, GetVerificationCode);
Console.ReadKey();
cts.Cancel();

async Task<string> GetVerificationCode()
{
    Console.WriteLine("Enter verification code...");
    return Console.ReadLine();
}