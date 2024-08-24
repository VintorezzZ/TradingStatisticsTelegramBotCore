using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using TradingStatisticsTelegramBotCore.BotStates;

namespace TradingStatisticsTelegramBotCore;

public enum EBotState
{
    None,
    DateChoosing,
    AdditionalInfoRequesting,
    StatisticsCollecting
}

public class BotProcessorOperation
{
    private readonly TelegramBotClient _bot;
    private readonly CancellationTokenSource _cts;
    private ClientDataStorage _clientDataStorage;
    
    private IBotState _currentState;
    private EBotState _currentStateType = EBotState.DateChoosing;
    private readonly EBotState _initialState = EBotState.DateChoosing;
    
    private Chat _currentChat;
    
    private readonly Dictionary<EBotState, EBotState> _botStatesChain = new()
    {
        { EBotState.DateChoosing, EBotState.AdditionalInfoRequesting },
        { EBotState.AdditionalInfoRequesting, EBotState.StatisticsCollecting },
        { EBotState.StatisticsCollecting, EBotState.None },
    };
    
    public BotProcessorOperation(CancellationTokenSource cts)
    {
        Logger.Log += SendLogMessageToClient;
        
        _cts = cts;
        _bot = new TelegramBotClient(Configuration.BOT_TOKEN, cancellationToken: _cts.Token);
    }

    private void SendLogMessageToClient(Logger.ELogType logType, string text)
    {
        _bot.SendTextMessageAsync(_currentChat, text);
    }

    public async Task Process()
    {
        var me = await _bot.GetMeAsync(cancellationToken: _cts.Token);
        
        _bot.OnError += OnError;
        _bot.OnMessage += OnMessage;
        _bot.OnUpdate += OnUpdate;

        Logger.Log(Logger.ELogType.Message, $"@{me.Username} is running... Press Enter to terminate");
        //Console.ReadKey();
        //await _cts.CancelAsync(); // stop the bot
    }
    
    private async Task OnError(Exception exception, HandleErrorSource source)
    {
        Console.WriteLine(exception); // just dump the exception to the console
    }

    private async Task OnMessage(Message msg, UpdateType type)
    {
        Logger.Log(Logger.ELogType.Message, $"Message received from {msg.From}: {msg.Text}");
        
        if (msg.Text?.ToLower() == "/start")
        {
            _currentChat = msg.Chat;
            
            await _bot.SendTextMessageAsync(msg.Chat, "Welcome! I am trading statistics bot.", cancellationToken: _cts.Token);

            _clientDataStorage = new ClientDataStorage();
            await SetState(_initialState, msg.Chat);
            
            return;
        }

        if (_currentState == null)
            return;
        
        if (await _currentState.Update(msg.Chat, msg.Text))
            await SetState(GetNextState(), msg.Chat);
    }
    
    private async Task OnUpdate(Update update)
    {
        if (_currentState == null)
            return;
        
        if (update is { CallbackQuery: { } query }) // non-null CallbackQuery
        {
            Logger.Log(Logger.ELogType.Message, $"Callback received from {query.From}: {query.Data}");
            
            await _bot.AnswerCallbackQueryAsync(query.Id, $"You picked {query.Data}", cancellationToken: _cts.Token);
            
            if (await _currentState.Update(query.Message!.Chat, query.Data))
                await SetState(GetNextState(), query.Message!.Chat);
        }
    }
    
    private EBotState GetNextState()
    {
        return _botStatesChain.GetValueOrDefault(_currentStateType, EBotState.None);
    }

    private async Task SetState(EBotState state, Chat chat)
    {
        if (_currentState != null)
            await _currentState.Exit(chat);

        switch (state)
        {
            case EBotState.DateChoosing:
                _currentState = new DateChoosingState(_bot, _clientDataStorage, _cts.Token);
                break;
            case EBotState.AdditionalInfoRequesting:
                _currentState = new AdditionalInfoRequestingState(_bot, _clientDataStorage, _cts.Token);
                break;
            case EBotState.StatisticsCollecting:
                _currentState = new StatisticsCollectingState(_bot, _clientDataStorage, _cts.Token);
                break;
            case EBotState.None:
                _currentState = null;
                break;
        }

        _currentStateType = state;
        
        Logger.Log(Logger.ELogType.Message, $"Entered state: {_currentStateType}");
        
        if (_currentState != null)
            await _currentState.Enter(chat);
    }
}