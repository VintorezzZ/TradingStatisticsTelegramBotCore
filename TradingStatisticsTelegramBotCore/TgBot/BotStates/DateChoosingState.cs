using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace TradingStatisticsTelegramBotCore.BotStates;

public class DateChoosingState(TelegramBotClient bot, ClientDataStorage clientDataStorage, CancellationToken ctsToken) : IBotState
{
    private enum EDateChoosingType
    {
        CustomDate,
        Week,
        Month
    }

    private const string CUSTOM_DATE_TEXT = $"Enter date intervals in format 12.08.2024 - 18.08.2024 on each line";
    private const string WRONG_DATE_TEXT = $"Entered wrong date! Re-enter please.";
    
    private List<(DateTime, DateTime)> _dateIntervals = [];

    public async Task<bool> Enter(Chat chat)
    {
        Logger.Log((int)LogLevel.Debug, $"Enter {GetType().Name} state");

        await bot.SendTextMessageAsync(
            chat,
            "Choose time interval",
            replyMarkup: new InlineKeyboardMarkup().AddButtons(SetInteractionButtons()),
            cancellationToken: ctsToken);

        return false;
    }

    public async Task<bool> Update(Chat chat, string? queryData)
    {
        _dateIntervals.Clear();
        
        var data = queryData ?? string.Empty;

        if (data.IsNullOrEmpty())
        {
            await bot.SendTextMessageAsync(chat, WRONG_DATE_TEXT, cancellationToken: ctsToken);
            return false;
        }
        
        if (data.Equals(EDateChoosingType.CustomDate.ToString(), StringComparison.CurrentCultureIgnoreCase))
        {
            await bot.SendTextMessageAsync(chat, CUSTOM_DATE_TEXT, cancellationToken: ctsToken);
            return false;
        }

        if (data.Equals(EDateChoosingType.Week.ToString(), StringComparison.CurrentCultureIgnoreCase))
        {
            var startDate = DateTime.UtcNow.Date - TimeSpan.FromDays(7);
            var endDate = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, DateTime.UtcNow.Day, 23, 59, 59);
            _dateIntervals.Add((startDate, endDate));
            return true;
        }

        if (data.Equals(EDateChoosingType.Month.ToString(), StringComparison.CurrentCultureIgnoreCase))
        {
            var startDate = DateTime.UtcNow.Date - TimeSpan.FromDays(30);
            var endDate = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, DateTime.UtcNow.Day, 23, 59, 59);
            _dateIntervals.Add((startDate, endDate));
            return true;
        }

        if (TryParseCustomDate(data))
            return true;
        
        await bot.SendTextMessageAsync(chat, WRONG_DATE_TEXT, cancellationToken: ctsToken);
        
        return false;
    }

    public async Task Exit(Chat chat)
    {
        clientDataStorage.DateIntervals = _dateIntervals;

        var text = $"Entered date intervals:";

        foreach (var (startDate, endDate) in _dateIntervals)
            text += $"\n{startDate} - {endDate}";
        
        await bot.SendTextMessageAsync(chat, text, cancellationToken: ctsToken);
    }

    private bool TryParseCustomDate(string queryData)
    {
        using var stringReader = new StringReader(queryData);
        {
            while (true)
            {
                var textLine = stringReader.ReadLine();

                if (textLine != null)
                {
                    var dates = textLine.Replace(" ", "").Split("-");

                    if (dates.Length is < 2 or > 3)
                        return false;
        
                    dates[1] = dates[1].Insert(dates[1].Length, " 23:59:59");

                    if (DateTime.TryParse(dates[0], out var startDate) && DateTime.TryParse(dates[1], out var endDate))
                        _dateIntervals.Add((startDate, endDate));
                }
                else
                {
                    break;
                }
            }
        }
        
        return true;
    }
    
    private InlineKeyboardButton[] SetInteractionButtons()
    {
        var buttons = new List<InlineKeyboardButton>
        {
            EDateChoosingType.CustomDate.ToString(),
            EDateChoosingType.Week.ToString(),
            EDateChoosingType.Month.ToString()
        };

        return buttons.ToArray();
    }
}