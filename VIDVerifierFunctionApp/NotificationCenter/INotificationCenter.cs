namespace VIDVerifier.NotificationCenter;

public interface INotificationCenter
{
    public void SendPresentationPendingNotification(Guid requestId, string qrCode, string callerName);

    public void SendPresentationVerifiedNotification(Guid requestId, string callerName);

    public void SendCallbackCompletedNotification(Guid requestId, string callerName);

    public void SendNotification(INotification notification);
}
