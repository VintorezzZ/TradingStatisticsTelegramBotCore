using Telegram.Bot;
using Telegram.Bot.Types;

namespace TradingStatisticsTelegramBotCore.BotStates;

public class AdditionalInfoRequestingState(TelegramBotClient bot, ClientDataStorage clientDataStorage, CancellationToken ctsToken) : IBotState
{
    private enum EDataRequestType
    {
        AllPredictions,
        SuccessfulPredictions,
        StartDeposit,
    }

    private int _allPredictions;
    private int _successfulPredictions;
    private double _startDeposit;

    private EDataRequestType _currentRequestType;
    
    public async Task Enter(Chat chat)
    {
        _currentRequestType = EDataRequestType.AllPredictions;
        
        await bot.SendTextMessageAsync(
            chat,
            "Enter all predictions count",
            cancellationToken: ctsToken);
    }

    public async Task<bool> Update(Chat chat, string? queryData)
    {
        var data = queryData ?? string.Empty;

        if (data.IsNullOrEmpty())
            return false;

        data = data.Replace(" ", "");

        switch (_currentRequestType)
        {
            case EDataRequestType.AllPredictions:
            {
                if (int.TryParse(data, out var value))
                {
                    _allPredictions = value;
                    _currentRequestType = EDataRequestType.SuccessfulPredictions;
                    
                    await bot.SendTextMessageAsync(
                        chat,
                        "Enter successful predictions count",
                        cancellationToken: ctsToken);
                }
                else
                {
                    await SendErrorMessage(chat);
                }
                
                break;
            }
            case EDataRequestType.SuccessfulPredictions:
            {
                if (int.TryParse(data, out var value))
                {
                    _successfulPredictions = value;
                    _currentRequestType = EDataRequestType.StartDeposit;
                    
                    await bot.SendTextMessageAsync(
                        chat,
                        "Enter start deposit",
                        cancellationToken: ctsToken);
                }
                else
                    await SendErrorMessage(chat);
                
                break;
            }
            case EDataRequestType.StartDeposit:
            {
                if (double.TryParse(data, out var value))
                {
                    _startDeposit = value;
                    return true;
                }

                await SendErrorMessage(chat);

                break;
            }
        }
        
        return false;
    }

    public async Task Exit(Chat chat)
    {
        clientDataStorage.AllPredictions = _allPredictions;
        clientDataStorage.SuccessfulPredictions = _successfulPredictions;
        clientDataStorage.StartDeposit = _startDeposit;
        
        Console.WriteLine($"Client has entered:\nAll predictions count: {_allPredictions}\nSuccessful predictions: {_successfulPredictions}\nStart deposit: {_startDeposit}");
    }

    private async Task SendErrorMessage(Chat chat)
    {
        await bot.SendTextMessageAsync(
            chat,
            "Invalid value! Re-enter please.",
            cancellationToken: ctsToken);
    }
}