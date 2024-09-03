using Microsoft.Extensions.Logging;

namespace TradingStatisticsTelegramBotCore;

public static class TaskExtensions
{
    private static readonly Action<Task> _handleFinishedTask = HandleFinishedTask;
        
    public static async Task WaitUntil(Func<bool> predicate)
    {
        while (!predicate())
            await Task.Yield();
    }
        
    public static void HandleExceptions(this Task task)
    {
        task.ContinueWith(_handleFinishedTask);
    }
        
    private static void HandleFinishedTask(Task task)
    {
        if (task.IsFaulted)
            Logger.Log((int)LogLevel.Error, task.Exception.Message);
    }
}