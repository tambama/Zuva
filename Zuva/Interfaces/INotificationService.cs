using System;
using Zuva.Models;

namespace Zuva.Interfaces
{
    public interface INotificationService
    {
        void LogEvent(string message);
        void SendTelegramNotification(string message);
        void NotifyMacroTimeEntered(DateTime time);
        void NotifyGauntletDetected(Direction direction);
        void NotifyCisdConfirmation(Direction direction);
        void NotifyLiquiditySwept(SwingPoint sweptPoint, LiquidityType liquidityType);
    }
}