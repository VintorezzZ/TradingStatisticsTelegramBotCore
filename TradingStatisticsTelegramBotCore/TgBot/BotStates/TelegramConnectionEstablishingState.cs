using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types;
using TradingStatisticsTelegramBotCore.BotStates;

namespace TradingStatisticsTelegramBotCore;

public class TelegramConnectionEstablishingState(TelegramBotClient bot, TelegramConnectionService connectionService, CancellationToken ctsToken) : IBotState
{
    private Chat _chat;
    private string _loginInfoNeeded;
    
    public async Task<bool> Enter(Chat chat)
    {
        Logger.Log((int)LogLevel.Debug, $"Enter {GetType().Name} state");
        
        await bot.SendTextMessageAsync(
            chat,
            "Connecting to telegram...",
            cancellationToken: ctsToken);
        
        _chat = chat;
        
        WTelegram.Helpers.Log += TelegramLogHandler;
        
        try
        {
             _loginInfoNeeded = await connectionService.CreateConnection();
        }
        catch (Exception e)
        {
            Logger.Log((int)LogLevel.Error, e.Message);
            connectionService?.Dispose();
            throw;
        }

        return _loginInfoNeeded.IsNullOrEmpty();
    }

    public async Task<bool> Update(Chat chat, string queryData)
    {
        return await ContinueLogin(queryData);
    }

    private async Task<bool> ContinueLogin(string queryData)
    {
        string loginInfo;
        
        switch (_loginInfoNeeded)
        {
            case Configuration.VEFIFICATION_CODE_KEY_WORD:
            {
                var providedInfo = queryData ?? string.Empty;

                if (providedInfo.IsNullOrEmpty())
                    return false;

                var obfuscatedCode = providedInfo.Trim();

                loginInfo = obfuscatedCode;
                
                break;
            }
            default:
                return false;
                break;
        }
        
        _loginInfoNeeded = await connectionService.DoLogin(loginInfo);

        return _loginInfoNeeded.IsNullOrEmpty();
    }

    public async Task Exit(Chat chat)
    {
        
    }

    private void TelegramLogHandler(int errorType, string message)
    {
        var logLevel = (LogLevel)errorType;

        if (logLevel is LogLevel.Warning or LogLevel.Error or LogLevel.Information)
        {
            bot.SendTextMessageAsync(_chat, message);
            
            //if (message == "Wrong verification code!")
                //_verificationCode = null;
        }
    }
}