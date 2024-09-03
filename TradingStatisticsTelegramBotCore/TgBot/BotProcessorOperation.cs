using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using TradingStatisticsTelegramBotCore.BotStates;
using WTelegram;

namespace TradingStatisticsTelegramBotCore;

public enum EBotState
{
    None,
    DateChoosing,
    AdditionalInfoRequesting,
    TelegramConnectionEstablishing,
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
    
    private Chat _chat;
    private readonly TelegramConnectionService _connectionService;


    private readonly Dictionary<EBotState, EBotState> _botStatesChain = new()
    {
        { EBotState.DateChoosing, EBotState.AdditionalInfoRequesting },
        { EBotState.AdditionalInfoRequesting, EBotState.TelegramConnectionEstablishing },
        { EBotState.TelegramConnectionEstablishing, EBotState.StatisticsCollecting },
        { EBotState.StatisticsCollecting, EBotState.None },
    };

    public BotProcessorOperation(CancellationTokenSource cts)
    {
        Logger.Log += LogHandler;
        
        _cts = cts;
        _bot = new TelegramBotClient(Configuration.BOT_TOKEN, cancellationToken: _cts.Token);
        _connectionService = new TelegramConnectionService();
    }

    private void LogHandler(int level, string message)
    {
        if ((LogLevel)level == LogLevel.Debug && !Configuration.DebugEnabled)
            return;
        
        SendLogMessageToClient(message);
    }

    private void SendLogMessageToClient(string message)
    {
        _bot.SendTextMessageAsync(_chat, message);
    }

    public async Task Process()
    {
        var me = await _bot.GetMeAsync(cancellationToken: _cts.Token);

        _bot.OnError += (x, y) => Task.Run(() => OnError(x, y));
        _bot.OnMessage += (x, y) => Task.Run(() => OnMessage(x, y));
        _bot.OnUpdate += (x) => Task.Run(() => OnUpdate(x));

        Logger.Log((int)LogLevel.Information, $"@{me.Username} is running...");
        //await _cts.CancelAsync(); // stop the bot
    }
    
    private async Task OnError(Exception exception, HandleErrorSource source)
    {
        Logger.Log((int)LogLevel.Error, exception.Message);
    }

    private async Task OnMessage(Message msg, UpdateType type)
    {
        Logger.Log((int)LogLevel.Debug, $"Message received from {msg.From}: {msg.Text}");
        
        if (msg.Text?.ToLower() == "/start")
        {
            _chat = msg.Chat;
            
            await _bot.SendTextMessageAsync(msg.Chat, "Welcome! I am trading statistics bot.", cancellationToken: _cts.Token);

            _clientDataStorage = new ClientDataStorage();
            await SetState(_initialState, msg.Chat);
            
            return;
        }

        if (msg.Text?.ToLower() == "/debug_enable")
        {
            Configuration.DebugEnabled = true;
            await _bot.SendTextMessageAsync(msg.Chat, "Debug enabled.", cancellationToken: _cts.Token);
            return;
        }

        if (msg.Text?.ToLower() == "/debug_disable")
        {
            Configuration.DebugEnabled = false;
            await _bot.SendTextMessageAsync(msg.Chat, "Debug disabled.", cancellationToken: _cts.Token);
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
            Logger.Log((int)LogLevel.Debug, $"Callback received from {query.From}: {query.Data}");
            
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
            case EBotState.TelegramConnectionEstablishing:
                _currentState = new TelegramConnectionEstablishingState(_bot, _connectionService, _cts.Token);
                break;
            case EBotState.StatisticsCollecting:
                _currentState = new StatisticsCollectingState(_bot, _clientDataStorage, _connectionService.Client, _cts.Token);
                break;
            case EBotState.None:
                _currentState = null;
                break;
        }

        _currentStateType = state;
        
        if (_currentState != null)
        {
            var forceComplete = await _currentState.Enter(chat);

            if (forceComplete)
                await SetState(GetNextState(), chat);
        }
    }
}