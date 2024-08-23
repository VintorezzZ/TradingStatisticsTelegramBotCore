using TradingStatisticsTelegramBotCore;

var cts = new CancellationTokenSource();
var botProcessor = new BotProcessor();
await botProcessor.Start(cts);
Console.ReadKey();
cts.Cancel();