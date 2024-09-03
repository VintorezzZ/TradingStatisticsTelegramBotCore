using System.Text;
using Microsoft.Extensions.Logging;

namespace TradingStatisticsTelegramBotCore;

public class TraderStatistics
{
    private const double LOST_DEAL_RISK_VALUE = -0.2;
    private const double SUCCESS_DEAL_RISK_VALUE = 0.4;

    public class DealsAndPredictionsCommonStatistics
    {
        public int PredictionsOverallCount { get; set; }
        public int PredictionsSuccessfulCount { get; set; }
        public float PredictionsSuccessfulPercent { get; set; }
        public int DealsOverallCount { get; set; }
        public float DealsOverallRelativeOverallPredictionsPercent { get; set; }
        public int DealsProfitableCount { get; set; }
        public float DealsProfitableRelativeOverallDealsPercent { get; set; }
        public int DealsLosingCount { get; set; }
        public float DealsLosingRelativeOverallDealsPercent { get; set; }
    }
   
    public class ExtendedDealsAndPredictionsStatistics : DealsAndPredictionsCommonStatistics
    {
        public int DealsBreakevenCount { get; set; }
        public float DealsBreakevenRelativeOverallDealsPercent { get; set; }
        public double TakeProfitPerDealAverageValue { get; set; }
        public double StopLossPerDealAverageValue { get; set; }
        public double RisksEarnedTotalValue { get; set; }
        public double RisksLostTotalValue { get; set; }
        public double RisksProfitTotalValue { get; set; }
    }
    
    public class SubDealsAndPredictionsStatistics : DealsAndPredictionsCommonStatistics
    {
        public float PredictionsOverallRelativeAllPredictionsPercent { get; set; }
    }
    
    public class FinanceStatistics
    {
        public double DepositPrevious { get; set; }
        public double DepositFinal { get; set; }
        public double DepositDifferencePercent { get; set; }
        
        public double RiskMoneyPerDealAverageValue { get; set; }
        public double CommissionTotalValue { get; set; }
        public double MoneyLossTotalValue { get; set; }
        public double MoneyProfitWithoutCommissionTotalValue { get; set; }
        public double MoneyNetProfitTotalValue { get; set; }
    }
    
    public enum EStatisticsTimeInterval
    {
        Week,
        Month,
        Custom
    }
    
    public EStatisticsTimeInterval TimeInterval { get; private set; }
    public List<(DateTime, DateTime)> DateIntervals = new List<(DateTime, DateTime)>();
    
    public ExtendedDealsAndPredictionsStatistics DealsAndPredictions { get; private set; } = new();
    public FinanceStatistics Finance { get; private set; } = new();

    public SubDealsAndPredictionsStatistics ForexMarket { get; private set; } = new();
    public SubDealsAndPredictionsStatistics CryptoMarket { get; private set; } = new();
    public SubDealsAndPredictionsStatistics AmericaMarket { get; private set; } = new();
    public SubDealsAndPredictionsStatistics MoexMarket { get; private set; } = new();

    public SubDealsAndPredictionsStatistics BreakoutTradingStyle { get; private set; } = new();
    public SubDealsAndPredictionsStatistics FalseBreakoutTradingStyle { get; private set; } = new();
    public SubDealsAndPredictionsStatistics ReboundTradingStyle { get; private set; } = new();

    public static TraderStatistics Collect(List<DealData> deals, int predictionsOverallCount, int predictionsSuccessfulCount, double startDeposit, List<(DateTime, DateTime)> dateIntervals)
    {
        Logger.Log((int) LogLevel.Debug, "Collecting statistics...");
        
        EStatisticsTimeInterval timeIntervalType;
        
        if (dateIntervals.Count > 1)
        {
            timeIntervalType = EStatisticsTimeInterval.Custom;
        }
        else
        {
            var (startDate, endDate) = dateIntervals[0];
            var daysDiff = endDate.Day - startDate.Day;
        
            timeIntervalType = daysDiff switch
            {
                7 => EStatisticsTimeInterval.Week,
                30 => EStatisticsTimeInterval.Month,
                _ => EStatisticsTimeInterval.Custom
            };
        }
        
        var stats = new TraderStatistics
        {
            DateIntervals = dateIntervals,
            TimeInterval = timeIntervalType,
        };

        var realDealsCount = 0;
        var successfulDealsCount = 0;
        var lossDealsCount = 0;
        var breakevenDealsCount = 0;
        
        foreach (var deal in deals)
        {
            var isRealDeal = deal.ResultType is EResultType.Demo or EResultType.Real;
            
            if (isRealDeal)
            {
                realDealsCount++;

                switch (deal.RiskResult)
                {
                    case >= SUCCESS_DEAL_RISK_VALUE: successfulDealsCount++; break;
                    case < LOST_DEAL_RISK_VALUE: lossDealsCount++; break;
                    default: breakevenDealsCount++; break;
                }
                
                stats.Finance.MoneyNetProfitTotalValue += deal.FinancialResult;
                stats.Finance.CommissionTotalValue += deal.CommissionValue;
                stats.Finance.RiskMoneyPerDealAverageValue += deal.RiskMoneyValue;
            
                if (double.IsNegative(deal.FinancialResult))
                    stats.Finance.MoneyLossTotalValue += deal.FinancialResult;
                else
                    stats.Finance.MoneyLossTotalValue += deal.CommissionValue;

                if (deal.RiskResult < LOST_DEAL_RISK_VALUE)
                {
                    stats.DealsAndPredictions.RisksLostTotalValue += deal.RiskResult;
                    stats.DealsAndPredictions.StopLossPerDealAverageValue += deal.RiskResult;
                }
            
                if (deal.RiskResult >= SUCCESS_DEAL_RISK_VALUE)
                {
                    stats.DealsAndPredictions.RisksEarnedTotalValue += deal.RiskResult;
                    stats.DealsAndPredictions.TakeProfitPerDealAverageValue += deal.RiskResult;
                }
            }
            
            StartFillSubDealsAndPredictionsStatistics(GetMarketStat(deal.Market, stats), deal, isRealDeal);
            StartFillSubDealsAndPredictionsStatistics(GetTradingStyleStat(deal.Scenario, stats), deal, isRealDeal);
        }
        
        stats.Finance.RiskMoneyPerDealAverageValue = double.Round(stats.Finance.RiskMoneyPerDealAverageValue / realDealsCount, 2);
        stats.Finance.MoneyProfitWithoutCommissionTotalValue = double.Round(stats.Finance.MoneyNetProfitTotalValue + Math.Abs(stats.Finance.CommissionTotalValue), 1);
        stats.Finance.DepositPrevious = startDeposit;
        stats.Finance.DepositFinal = double.Round(startDeposit + stats.Finance.MoneyNetProfitTotalValue, 1);
        stats.Finance.DepositDifferencePercent = double.Round((stats.Finance.DepositFinal - startDeposit) / startDeposit * 100, 1);
        stats.Finance.CommissionTotalValue = double.Round(stats.Finance.CommissionTotalValue, 1);
        stats.Finance.MoneyNetProfitTotalValue = double.Round(stats.Finance.MoneyNetProfitTotalValue, 1);
        stats.DealsAndPredictions.PredictionsOverallCount = predictionsOverallCount;
        stats.DealsAndPredictions.PredictionsSuccessfulCount = predictionsSuccessfulCount;
        stats.DealsAndPredictions.PredictionsSuccessfulPercent = float.Round((float)predictionsSuccessfulCount / predictionsOverallCount * 100, 1);
        stats.DealsAndPredictions.DealsOverallCount = realDealsCount;
        stats.DealsAndPredictions.DealsOverallRelativeOverallPredictionsPercent = float.Round((float)realDealsCount / predictionsOverallCount * 100, 1);
        stats.DealsAndPredictions.DealsProfitableCount = successfulDealsCount;
        stats.DealsAndPredictions.DealsProfitableRelativeOverallDealsPercent = float.Round((float)successfulDealsCount / realDealsCount * 100, 1);
        stats.DealsAndPredictions.DealsLosingCount = lossDealsCount;
        stats.DealsAndPredictions.DealsLosingRelativeOverallDealsPercent = float.Round((float)lossDealsCount / realDealsCount * 100, 1);
        stats.DealsAndPredictions.DealsBreakevenCount = breakevenDealsCount;
        stats.DealsAndPredictions.DealsBreakevenRelativeOverallDealsPercent = float.Round((float)breakevenDealsCount / realDealsCount * 100, 1);
        stats.DealsAndPredictions.StopLossPerDealAverageValue = double.Round(stats.DealsAndPredictions.StopLossPerDealAverageValue / lossDealsCount, 2);
        stats.DealsAndPredictions.TakeProfitPerDealAverageValue = double.Round(stats.DealsAndPredictions.TakeProfitPerDealAverageValue / successfulDealsCount, 2);
        stats.DealsAndPredictions.RisksProfitTotalValue = stats.DealsAndPredictions.RisksEarnedTotalValue - Math.Abs(stats.DealsAndPredictions.RisksLostTotalValue);
        
        EndFillSubDealsAndPredictionsStatistics(GetMarketStat(EMarket.Forex, stats), stats);
        EndFillSubDealsAndPredictionsStatistics(GetMarketStat(EMarket.Crypto, stats), stats);
        EndFillSubDealsAndPredictionsStatistics(GetMarketStat(EMarket.America, stats), stats);
        EndFillSubDealsAndPredictionsStatistics(GetMarketStat(EMarket.Moex, stats), stats);
        
        EndFillSubDealsAndPredictionsStatistics(GetTradingStyleStat(EScenario.Breakout, stats), stats);
        EndFillSubDealsAndPredictionsStatistics(GetTradingStyleStat(EScenario.FalseBreakout, stats), stats);
        EndFillSubDealsAndPredictionsStatistics(GetTradingStyleStat(EScenario.Rebound, stats), stats);

        return stats;
    }

    private static void EndFillSubDealsAndPredictionsStatistics(SubDealsAndPredictionsStatistics statistics, TraderStatistics traderStatistics)
    {
        if (statistics.PredictionsOverallCount == 0)
            return;
        
        statistics.PredictionsOverallRelativeAllPredictionsPercent = float.Round((float)statistics.PredictionsOverallCount / traderStatistics.DealsAndPredictions.PredictionsOverallCount * 100, 1);
        statistics.PredictionsSuccessfulPercent = float.Round((float)statistics.PredictionsSuccessfulCount / statistics.PredictionsOverallCount * 100, 1);
        statistics.DealsOverallRelativeOverallPredictionsPercent = float.Round((float)statistics.DealsOverallCount / statistics.PredictionsOverallCount * 100, 1);

        if (statistics.DealsOverallCount == 0)
            return;
        
        statistics.DealsProfitableRelativeOverallDealsPercent = float.Round((float)statistics.DealsProfitableCount / statistics.DealsOverallCount * 100, 1);
        statistics.DealsLosingRelativeOverallDealsPercent = float.Round((float)statistics.DealsLosingCount / statistics.DealsOverallCount * 100, 1);
    }
    
    private static void StartFillSubDealsAndPredictionsStatistics(SubDealsAndPredictionsStatistics statistics, DealData deal, bool isRealDeal)
    {
        statistics.PredictionsOverallCount++;
                    
        if (deal.IsScenarioSuccessful)
            statistics.PredictionsSuccessfulCount++;

        if (isRealDeal)
        {
            statistics.DealsOverallCount++;

            switch (deal.RiskResult)
            {
                case >= SUCCESS_DEAL_RISK_VALUE: statistics.DealsProfitableCount++; break;
                case < LOST_DEAL_RISK_VALUE: statistics.DealsLosingCount++; break;
            }
        }
    }

    private static SubDealsAndPredictionsStatistics GetMarketStat(EMarket market, TraderStatistics stats)
    {
        var marketStat = market switch
        {
            EMarket.Forex => stats.ForexMarket,
            EMarket.Crypto => stats.CryptoMarket,
            EMarket.America => stats.AmericaMarket,
            EMarket.Moex => stats.MoexMarket,
            _ => throw new ArgumentOutOfRangeException()
        };
        return marketStat;
    }

    private static SubDealsAndPredictionsStatistics GetTradingStyleStat(EScenario scenario, TraderStatistics stats)
    {
        var scenarioStat = scenario switch
        {
            EScenario.Breakout => stats.BreakoutTradingStyle,
            EScenario.FalseBreakout => stats.FalseBreakoutTradingStyle,
            EScenario.Rebound => stats.ReboundTradingStyle,
            _ => throw new ArgumentOutOfRangeException()
        };
        return scenarioStat;
    }

    public static string GetText(TraderStatistics stats)
    {
        Logger.Log((int) LogLevel.Debug, "Creating statistics message...");
        
        var builder = new StringBuilder();
        
        string financeSign = stats.Finance.MoneyNetProfitTotalValue > 0 ? "+" : "-";
        string risksEarnedSign = stats.DealsAndPredictions.RisksEarnedTotalValue > 0 ? "+" : "-";
        string risksProfitSign = stats.DealsAndPredictions.RisksProfitTotalValue > 0 ? "+" : "-";
        string moneyWithoutCommissionSign = stats.Finance.MoneyProfitWithoutCommissionTotalValue > 0 ? "+" : "-";
        string moneyProfitSign = stats.Finance.MoneyNetProfitTotalValue > 0 ? "+" : "-";

        string intervalName = stats.TimeInterval switch
        {
            EStatisticsTimeInterval.Week => "недели",
            EStatisticsTimeInterval.Month => "месяца",
            _ => "за период"
        };

        string intervalsText = "";

        foreach (var (startDate, endDate) in stats.DateIntervals)
            intervalsText = $"\n{startDate:dd.MM.yyyy} - {endDate:dd.MM.yyyy}";
        
        builder.AppendLine($"Итог {intervalName} {intervalsText}");
        builder.AppendLine($"• Фин.рез: {financeSign}{stats.Finance.MoneyNetProfitTotalValue}$");
        builder.AppendLine($"• Депо: {stats.Finance.DepositPrevious}$ => {stats.Finance.DepositFinal}$ ({financeSign}{stats.Finance.DepositDifferencePercent}%)");
        builder.AppendLine($"");
        builder.AppendLine($"<b><u>Общая статистика по сделкам</u></b>");
        builder.AppendLine($"");
        builder.AppendLine($"• Всего прогнозов: {stats.DealsAndPredictions.PredictionsOverallCount}");
        builder.AppendLine($"• Успешные прогнозы: {stats.DealsAndPredictions.PredictionsSuccessfulCount}/{stats.DealsAndPredictions.PredictionsOverallCount} ({stats.DealsAndPredictions.PredictionsSuccessfulPercent}%)");
        builder.AppendLine($"");
        builder.AppendLine($"• Всего сделок: {stats.DealsAndPredictions.DealsOverallCount}/{stats.DealsAndPredictions.PredictionsOverallCount} ({stats.DealsAndPredictions.DealsOverallRelativeOverallPredictionsPercent}%)");
        builder.AppendLine($"• Прибыльные сделки: {stats.DealsAndPredictions.DealsProfitableCount}/{stats.DealsAndPredictions.DealsOverallCount} ({stats.DealsAndPredictions.DealsProfitableRelativeOverallDealsPercent}%)");
        builder.AppendLine($"• Убыточные сделки: {stats.DealsAndPredictions.DealsLosingCount}/{stats.DealsAndPredictions.DealsOverallCount} ({stats.DealsAndPredictions.DealsLosingRelativeOverallDealsPercent}%)");
        builder.AppendLine($"• Безубыточные сделки: {stats.DealsAndPredictions.DealsBreakevenCount}/{stats.DealsAndPredictions.DealsOverallCount} ({stats.DealsAndPredictions.DealsBreakevenRelativeOverallDealsPercent}%)");
        builder.AppendLine($"• Средний тейк на сделку: {stats.DealsAndPredictions.TakeProfitPerDealAverageValue}R");
        builder.AppendLine($"• Средний лосс на сделку: {stats.DealsAndPredictions.StopLossPerDealAverageValue}R");
        builder.AppendLine($"• Всего заработано рисков: {risksEarnedSign}{stats.DealsAndPredictions.RisksEarnedTotalValue}R");
        builder.AppendLine($"• Убыток в рисках: {stats.DealsAndPredictions.RisksLostTotalValue}R");
        builder.AppendLine($"• Прибыль в рисках: {risksProfitSign}{stats.DealsAndPredictions.RisksProfitTotalValue}R");
        builder.AppendLine($"");
        builder.AppendLine($"<b><u>Финансовый результат</u></b>");
        builder.AppendLine($"");
        builder.AppendLine($"• Риск на сделку (средний): {stats.Finance.RiskMoneyPerDealAverageValue}$");
        builder.AppendLine($"• Общая комиссия: {stats.Finance.CommissionTotalValue}$");
        builder.AppendLine($"• Общий убыток: {stats.Finance.MoneyLossTotalValue}$");
        builder.AppendLine($"• Прибыль (без комиссии) {moneyWithoutCommissionSign}{stats.Finance.MoneyProfitWithoutCommissionTotalValue}$");
        builder.AppendLine($"• Прибыль (чистая): {moneyProfitSign}{Math.Abs(stats.Finance.MoneyNetProfitTotalValue)}$");
        builder.AppendLine($"");
        builder.AppendLine($"• Депо: {stats.Finance.DepositPrevious} => {stats.Finance.DepositFinal} ({financeSign}{stats.Finance.DepositDifferencePercent}%)");
        builder.AppendLine($"");
        builder.AppendLine($"<b><u>Статистика по рынкам</u></b>");
        builder.AppendLine($"");
        CreateCommonStatisticsText(EMarket.Forex.ToString().ToUpper(), stats.ForexMarket, stats, builder);
        builder.AppendLine($"");
        CreateCommonStatisticsText(EMarket.Crypto.ToString().ToUpper(), stats.CryptoMarket, stats, builder);
        builder.AppendLine($"");
        CreateCommonStatisticsText(EMarket.America.ToString().ToUpper(), stats.AmericaMarket, stats, builder);
        builder.AppendLine($"");
        CreateCommonStatisticsText(EMarket.Moex.ToString().ToUpper(), stats.MoexMarket, stats, builder);
        builder.AppendLine($"");
        builder.AppendLine($"<b><u>Статистика по стилям торговли</u></b>");
        builder.AppendLine($"");
        CreateCommonStatisticsText(EScenario.Breakout.ToString(), stats.BreakoutTradingStyle, stats, builder);
        builder.AppendLine($"");
        CreateCommonStatisticsText(EScenario.FalseBreakout.ToString(), stats.FalseBreakoutTradingStyle, stats, builder);
        builder.AppendLine($"");
        CreateCommonStatisticsText(EScenario.Rebound.ToString(), stats.ReboundTradingStyle, stats, builder);
        builder.AppendLine($"");
        builder.AppendLine($"<b><u>Коммент</u></b>:");
        builder.AppendLine($"");
        builder.AppendLine($"<i>This message was generated by TradingStatisticsTelegramBot</i>");
        builder.AppendLine($"#{stats.TimeInterval.ToString().ToUpper()}");

        return builder.ToString();
    }

    private static void CreateCommonStatisticsText(string label, SubDealsAndPredictionsStatistics localStats, TraderStatistics stats, StringBuilder builder)
    {
        var allPredictions = stats.DealsAndPredictions.PredictionsOverallCount;
        
        builder.AppendLine($"<i><u>{label}</u></i>:");
        builder.AppendLine($"• Всего прогнозов: {localStats.PredictionsOverallCount}/{allPredictions} ({localStats.PredictionsOverallRelativeAllPredictionsPercent}%)");
        builder.AppendLine($"• Успешные прогнозы: {localStats.PredictionsSuccessfulCount}/{localStats.PredictionsOverallCount} ({localStats.PredictionsSuccessfulPercent}%)");
        builder.AppendLine($"• Всего сделок: {localStats.DealsOverallCount}/{localStats.PredictionsOverallCount} ({localStats.DealsOverallRelativeOverallPredictionsPercent}%)");
        builder.AppendLine($"• Успешные сделки: {localStats.DealsProfitableCount}/{localStats.DealsOverallCount} ({localStats.DealsProfitableRelativeOverallDealsPercent}%)");
        builder.AppendLine($"• Убыточные сделки: {localStats.DealsLosingCount}/{localStats.DealsOverallCount} ({localStats.DealsLosingRelativeOverallDealsPercent}%)");
    }
}