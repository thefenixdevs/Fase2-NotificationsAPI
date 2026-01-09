using NotificationsAPI.Domain.Entities;

namespace NotificationsAPI.Domain.Services;

public class NotificationDomainService : INotificationDomainService
{
    public Notification CreateWelcomeNotification(string email)
        => new("WELCOME", email);

    public Notification? CreatePurchaseNotification(string email, string paymentStatus)
    {
        if (paymentStatus != "Approved")
            return null;

        return new Notification("PURCHASE_CONFIRMED", email);
    }
}