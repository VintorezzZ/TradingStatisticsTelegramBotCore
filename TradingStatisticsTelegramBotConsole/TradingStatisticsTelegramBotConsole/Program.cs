using TradingStatisticsTelegramBotCore;

var cts = new CancellationTokenSource();
var botProcessor = new BotProcessor();
Logger.Log += Log;
await botProcessor.Start(true, cts);
Console.ReadKey();
cts.Cancel();

void Log(int logLevel, string message)
{
    Console.WriteLine(message);
}