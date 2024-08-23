namespace TradingStatisticsTelegramBotCore;

public static class StringExtensions
{
    public static string FirstCharToUpper(this string input) =>
        input switch
        {
            null => throw new ArgumentNullException(nameof(input)),
            "" => throw new ArgumentException($"{nameof(input)} cannot be empty", nameof(input)),
            _ => string.Concat(input[0].ToString().ToUpper(), input.AsSpan(1))
        };

    public static string GetStringAfterCharWithoutSymbolsAtTheEnd(this string input, string separator,
        char[] chars) => input.Split(separator, 2)[1].TrimEnd(chars);
    
    public static bool IsNullOrEmpty(this string input) => input is null or ""; 
}