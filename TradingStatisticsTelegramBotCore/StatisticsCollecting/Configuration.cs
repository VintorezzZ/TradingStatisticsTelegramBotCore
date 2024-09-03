namespace TradingStatisticsTelegramBotCore;

public class Configuration
{
    private const int API_ID = 27589744;
    private const string API_HASH = "d691292c90cbcde49eecc5b3b5feb323";
    private const string PHONE_NUMBER = "+79082268063"; //"+79218852563";
    private const string PASSWORD = "0708"; //"Telegram228337!";
    public const string BOT_TOKEN = "7309601398:AAHJ7rdMNN07-C8PuoD0KQFksNj1YUl-hk8";

    public const string VEFIFICATION_CODE_KEY_WORD = "verification_code";
    public const string PASSWORD_KEY_WORD = "password";
    public const string API_ID_KEY_WORD = "api_id";
    public const string API_HASH_KEY_WORD = "api_hash";
    public const string PHONE_NUMBER_KEY_WORD = "phone_number";
    
    public static Func<Task<string>> VerificationCodeGetter;
    public static bool IsAndroidPlatform;
    public static string WTelegramSessionStorePath = "/storage/emulated/0/Download/WTelegram.session";
    public static bool IsProcessingThroughTelegramBot;
    public static bool DebugEnabled;

    public static string MessagesFromChatName = "Mike Sergeev | Trading | Chat";
    public static string ResultInChatName = "vintorez_trading";
    public static long ResultInChatId = 0; //2209913204
    public static long MessagesFromChatId = 0; //1172794060

    public static Dictionary<string, long> ChatsIds = new Dictionary<string, long>();
    
    public static string GetConfig(string what)
    {
        switch (what)
        {
            case API_ID_KEY_WORD: return API_ID.ToString();
            case API_HASH_KEY_WORD: return API_HASH;
            case PHONE_NUMBER_KEY_WORD: return PHONE_NUMBER;
            case VEFIFICATION_CODE_KEY_WORD: return VerificationCodeGetter().GetAwaiter().GetResult();
            case "first_name": return "Mikhail";      // if sign-up is required
            case "last_name": return "Sergeev";        // if sign-up is required
            case PASSWORD_KEY_WORD: return PASSWORD;     // if user has enabled 2FA
            default: return null;                  // let WTelegramClient decide the default config
        }
    }
}