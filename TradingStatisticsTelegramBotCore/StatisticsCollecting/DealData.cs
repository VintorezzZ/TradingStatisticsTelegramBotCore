using TL;

namespace TradingStatisticsTelegramBotCore;

public enum EMarket
{
    Forex,
    Crypto,
    America,
    Moex
}

public enum EScenario
{
    Breakout,
    FalseBreakout,
    Rebound
}

public enum EDirection
{
    Long,
    Short,
    Flat
}

public enum EDealType
{
    OneDayRegular,
    InPlay,
    Investment,
}

public enum EResultType
{
    Real,
    Demo,
    Idea
}

public class DealData
{
    public string Asset;
    public EMarket Market;
    public DateTime Datetime;
    public EScenario Scenario;
    public string ScenarioAdditionalInfo;
    public EDirection Direction;
    public EDealType DealType;
    public string InfoSource;
    public string PriceLevel;
    public string MoveEnergy;
    public string DayTradingVolume;
    public EDirection GlobalTrendDirection;
    public EDirection LocalTrendDirection;
    public string EntryPoint;
    public string StopLoss;
    public string[] Preconditions;
    public bool IsScenarioSuccessful;
    public bool IsScenarioSystemic;
    public TimeSpan Duration;
    public EResultType ResultType;
    public double RiskResult;
    public double RiskPotential;
    public int ReEntriesCount;
    public double RiskMoneyValue;
    public double AssetVolume;
    public double AssetMoneyVolume;
    public double CommissionValue;
    public double FinancialResult;
    public string MistakesText;
    public string CommentText;

    public static DealData Parse(Message message)
    {
        var dealData = new DealData();
        var msgText = message.message;
        
        //var market = Regex.Match(msgText, @"^([\w\-]+)").Value;
        
        using var reader = new StringReader(msgText);
        
        var textLine = reader.ReadLine(); 
        
        ParseFirstLine(textLine, dealData);

        while(true)
        {
            textLine = reader.ReadLine();
            
            if (textLine != null)
            {
                if (textLine.Contains("Дата:"))
                {
                    ParseDatetime(textLine, dealData);
                }
                else if (textLine.Contains("Сценарий:"))
                {
                    dealData.ScenarioAdditionalInfo = textLine.GetStringAfterCharWithoutSymbolsAtTheEnd(":", ['.']);
                }
                else if (textLine.Contains("Источник:"))
                {
                    dealData.InfoSource = textLine.GetStringAfterCharWithoutSymbolsAtTheEnd(":", ['.']);
                }
                else if (textLine.Contains("Уровень:"))
                {
                    dealData.PriceLevel = textLine.GetStringAfterCharWithoutSymbolsAtTheEnd(":", ['.']);
                }
                else if (textLine.Contains("Энергия:"))
                {
                    dealData.MoveEnergy = textLine.GetStringAfterCharWithoutSymbolsAtTheEnd(":", ['.']);
                }
                else if (textLine.Contains("Объем торгов:"))
                {
                    dealData.DayTradingVolume = textLine.GetStringAfterCharWithoutSymbolsAtTheEnd(":", ['.']);
                }
                else if (textLine.Contains("Глобал. тренд:"))
                {
                    dealData.GlobalTrendDirection = ParseMoveDirection(textLine);;
                }
                else if (textLine.Contains("Локал. тренд:"))
                {
                    dealData.LocalTrendDirection = ParseMoveDirection(textLine);
                }
                else if (textLine.Contains("ТВХ:"))
                {
                    dealData.EntryPoint = textLine.GetStringAfterCharWithoutSymbolsAtTheEnd(":", ['.']);
                }
                else if (textLine.Contains("СЛ:"))
                {
                    dealData.StopLoss = textLine.GetStringAfterCharWithoutSymbolsAtTheEnd(":", ['.']);
                }
                else if (textLine.Contains("Предпосылки"))
                {
                    var preconditions = new List<string>();
                    textLine = reader.ReadLine();
                    
                    while (textLine.Length > 0)
                    {
                        preconditions.Add(textLine.TrimEnd(';', '.'));
                        textLine = reader.ReadLine();
                    }
                    
                    dealData.Preconditions = preconditions.ToArray();
                }
                else if (textLine.Contains("Отработка сценария:"))
                {
                    var result = textLine.GetStringAfterCharWithoutSymbolsAtTheEnd(":", ['.']).ToLower();
                    dealData.IsScenarioSuccessful = result.Contains("да");
                }
                else if (textLine.Contains("Системная сделка:"))
                {
                    var result = textLine.GetStringAfterCharWithoutSymbolsAtTheEnd(":", ['.']).ToLower();
                    dealData.IsScenarioSystemic = result.Contains("да");
                }
                else if (textLine.Contains("Продолжительность:"))
                {
                    var timeText = textLine.GetStringAfterCharWithoutSymbolsAtTheEnd(":", ['.']);
                    var words = timeText.Split(' ').ToList();
                    words.RemoveAll(s => s == "");
                    var hours = 0;
                    var minutes = 0;
                    
                    foreach (var word in words)
                    {
                        if (word.Contains('ч'))
                            hours = Int32.Parse(word.Replace("ч", ""));
                        else if (word.Contains('м'))
                            minutes = Int32.Parse(word.Replace("м", ""));
                    }
                    
                    var duration = new TimeSpan(hours, minutes, 0);
                    dealData.Duration = duration;
                }
                else if (textLine.Contains("Торговый результат:"))
                {
                    var dealResultText = textLine.GetStringAfterCharWithoutSymbolsAtTheEnd(":", ['.']);
                    var words = dealResultText.Split(' ').ToList();
                    words.RemoveAll(s => s == "");
                
                    dealData.ResultType = Enum.Parse<EResultType>(words[0].TrimEnd('.').Replace("#", ""));
                    dealData.RiskResult = double.Parse(words[1].TrimEnd('.').Replace("R", "").Replace(".", ","));
                }
                else if (textLine.Contains("Потенциал:"))
                {
                    var potentialText = textLine.GetStringAfterCharWithoutSymbolsAtTheEnd(":", ['.']).Split("R")[0].Replace(" ", "");
                    
                    if (potentialText.Contains('-'))
                    {
                        var words = potentialText.Split('-');
                        potentialText = words[0];
                    }
                    
                    var d = double.Parse(potentialText.Replace(".", ","));
                    dealData.RiskPotential = d;
                }
                else if (textLine.Contains("Перезаходы:"))
                {
                    if (dealData.ResultType == EResultType.Idea)
                        continue;
                    
                    var reEntriesText = textLine.GetStringAfterCharWithoutSymbolsAtTheEnd(":", ['.']).Replace(" ", "");
                    var reEntriesCount = 0;
                    
                    if (!reEntriesText.IsNullOrEmpty())
                        reEntriesCount = int.Parse(reEntriesText);
                    
                    dealData.ReEntriesCount = reEntriesCount;
                }
                else if (textLine.Contains("Риск:"))
                {
                    if (dealData.ResultType == EResultType.Idea)
                        continue;
                    
                    var text = textLine.GetStringAfterCharWithoutSymbolsAtTheEnd(":", ['.']).Replace("$", "").Replace(" ", "");
                    
                    if (!text.IsNullOrEmpty())
                        dealData.RiskMoneyValue = double.Parse(text);
                }
                else if (textLine.Contains("Объем:"))
                {
                    if (dealData.ResultType == EResultType.Idea)
                        continue;
                    
                    var assetVolumeText = textLine.GetStringAfterCharWithoutSymbolsAtTheEnd(":", ['.']);
                    var words = assetVolumeText.Split(' ').ToList();
                    words.RemoveAll(s => s is " " or "");

                    if (words.Count > 0 && double.TryParse(words[0], out var assetVolume))
                        dealData.AssetVolume = assetVolume;
                }
                else if (textLine.Contains("Комиссия:"))
                {
                    if (dealData.ResultType == EResultType.Idea)
                        continue;
                    
                    var text = textLine.GetStringAfterCharWithoutSymbolsAtTheEnd(":", ['.']).Replace("$", "").Replace(".", ",");
                    var words = text.Split(' ').ToList();
                    words.RemoveAll(s => s is " " or "");

                    if (words.Count > 0 && double.TryParse(words[0], out var commisionValue))
                        dealData.CommissionValue = commisionValue;
                }
                else if (textLine.Contains("Фин.рез:"))
                {
                    if (dealData.ResultType == EResultType.Idea)
                        continue;
                    
                    var text = textLine.GetStringAfterCharWithoutSymbolsAtTheEnd(":", ['.']).Replace("$", "").Replace(".", ",");
                    var words = text.Split(' ').ToList();
                    words.RemoveAll(s => s is " " or "");

                    if (words.Count > 0 && double.TryParse(words[0], out var financialResult))
                        dealData.FinancialResult = financialResult;
                }
                else if (textLine.Contains("Ошибки:"))
                {
                    dealData.MistakesText = textLine.GetStringAfterCharWithoutSymbolsAtTheEnd(":", ['.']);
                }
                else if (textLine.Contains("Коммент:"))
                {
                    var comment = textLine.Split(":")[1];
                    textLine = reader.ReadToEnd();
                    comment += textLine;
                    dealData.CommentText = comment;
                }
            }
            else
            {
                break;
            }
        }
        
        return dealData;
    }

    private static EDirection ParseMoveDirection(string textLine)
    {
        EDirection direction;
                    
        var text = textLine.GetStringAfterCharWithoutSymbolsAtTheEnd(":", ['.']).Replace(" ", "");

        if (text.Contains("лонг", StringComparison.CurrentCultureIgnoreCase))
            direction = EDirection.Long;
        else if (text.Contains("шорт", StringComparison.CurrentCultureIgnoreCase))
            direction = EDirection.Short;
        else
            direction = EDirection.Flat;
        return direction;
    }

    private static void ParseDatetime(string textLine, DealData dealData)
    {
        var dateString = textLine.GetStringAfterCharWithoutSymbolsAtTheEnd(":", ['.']);
        var datetime = DateTime.Parse(dateString);
        dealData.Datetime = datetime;
    }

    private static void ParseFirstLine(string? textLine, DealData dealData)
    {
        var words = textLine.Split(' ');
        
        var market = Enum.Parse<EMarket>(words[0].Replace("#", "").ToLower().FirstCharToUpper());
        var asset = words[1];
        var scenario = Enum.Parse<EScenario>(words[2].Replace("#", ""));
        var moveDirection = Enum.Parse<EDirection>(words[3].Replace("#", ""));
        var dealType = EDealType.OneDayRegular;
        
        if (words.Length >= 5 && !words[4].IsNullOrEmpty())
            dealType = Enum.Parse<EDealType>(words[4].Replace("#", ""));
        
        dealData.Asset = asset;
        dealData.Market = market;
        dealData.Scenario = scenario;
        dealData.Direction = moveDirection;
        dealData.DealType = dealType;
    }
}