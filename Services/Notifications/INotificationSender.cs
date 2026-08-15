using System.Threading;
using System.Threading.Tasks;

namespace CongoTravel.Services.Notifications
{
    public interface INotificationSender
    {
        Task SendAsync(NotificationDispatchResult dispatchResult, CancellationToken cancellationToken = default);
    }
}

