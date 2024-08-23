namespace TradingStatisticsTelegramBotCore;

public class Configuration
{
    public const int API_ID = 27589744;
    public const string API_HASH = "d691292c90cbcde49eecc5b3b5feb323";
    public const string BOT_TOKEN = "7309601398:AAHJ7rdMNN07-C8PuoD0KQFksNj1YUl-hk8";
    public const string PHONE_NUMBER = "+79218852563";
    public static Func<Task<string>> VerificationCodeGetter;
    
    public static string MessagesFromChatName = "Mike Sergeev | Trading | Chat";
    public static string ResultInChatName = "vintorez_trading";
    public static long ResultInChatId = 0; //2209913204
    public static long MessagesFromChatId = 0; //1172794060

    public static Dictionary<string, long> ChatsIds = new Dictionary<string, long>();
    
    public static string GetConfig(string what)
    {
        switch (what)
        {
            case "api_id": return API_ID.ToString();
            case "api_hash": return API_HASH;
            case "phone_number": return PHONE_NUMBER;
            case "verification_code": return VerificationCodeGetter().GetAwaiter().GetResult();
            case "first_name": return "Mikhail";      // if sign-up is required
            case "last_name": return "Sergeev";        // if sign-up is required
            case "password": return "Telegram228337!";     // if user has enabled 2FA
            default: return null;                  // let WTelegramClient decide the default config
        }
    }
}