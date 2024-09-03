using Microsoft.Extensions.Logging;
using WTelegram;

namespace TradingStatisticsTelegramBotCore;

public class TelegramConnectionService : IDisposable
{
    private FileStream _clientSessionFileStream;
    
    public Client Client { get; private set; }
    
    public async Task<string> CreateConnection()
    {
        if (Client != null)
            return null;

        CreateClient();
        
        var infoNeeded = await DoLogin(Configuration.GetConfig(Configuration.PHONE_NUMBER_KEY_WORD)); // initial call with user's phone_number
        
        return infoNeeded;
    }
    
    private void CreateClient()
    {
        try
        {
            if (Configuration.IsAndroidPlatform)
            {
                Logger.Log((int)LogLevel.Debug, $"Opening or creating WTelegram.session...");
                _clientSessionFileStream = File.Open(Configuration.WTelegramSessionStorePath, FileMode.OpenOrCreate);
            }         

            Logger.Log((int)LogLevel.Debug, $"Trying to login...");

            var apiId = int.Parse(Configuration.GetConfig(Configuration.API_ID_KEY_WORD));
            var apiHash = Configuration.GetConfig(Configuration.API_HASH_KEY_WORD);
            Client = new Client(apiId, apiHash); // this constructor doesn't need a Config method
        }
        catch (Exception e)
        {
            Logger.Log((int)LogLevel.Error, "Failed to create WTelegram.session.");
            Logger.Log((int)LogLevel.Error, e.Message);
            
            Dispose();
            throw;
        }
    }
   
    public async Task<string> DoLogin(string loginInfo) // (add this method to your code)
    {
        var infoProvided = "";
        
        var infoNeeded = await Client.Login(loginInfo);
        
        if (!infoNeeded.IsNullOrEmpty())
        {
            infoProvided = infoNeeded switch
            {
                Configuration.PASSWORD_KEY_WORD => Configuration.GetConfig(Configuration.PASSWORD_KEY_WORD),
                _ => infoProvided
            };

            if (!infoProvided.IsNullOrEmpty())
            {
                await DoLogin(infoProvided);
            }
            else
            {
                Logger.Log((int)LogLevel.Warning, $"A {infoNeeded} is required...");
                return infoNeeded;
            }
        }
        
        Logger.Log((int)LogLevel.Information, $"We are logged-in as {Client.User} (id {Client.User.id})");
        
        return null;
    }
    
    public void Dispose()
    {
        Logger.Log((int)LogLevel.Debug, "Closing connection...");

        Client?.Reset();
        Client?.Dispose();
        Client = null;

        _clientSessionFileStream?.Dispose();
        _clientSessionFileStream?.Close();
        _clientSessionFileStream = null;
    }
}