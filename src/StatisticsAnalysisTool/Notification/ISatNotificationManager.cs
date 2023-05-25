using System.Threading.Tasks;

namespace StatisticsAnalysisTool.Notification;

public interface ISatNotificationManager
{
    Task ShowTrackingStatusAsync(string title, string message);

    Task ShowTradeAsync(Trade.Trade trade);
}