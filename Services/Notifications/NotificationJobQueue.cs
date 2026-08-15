using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace CongoTravel.Services.Notifications
{
    public class NotificationJobQueue : INotificationJobQueue
    {
        private readonly Channel<NotificationDispatchResult> _channel;

        public NotificationJobQueue()
        {
            _channel = Channel.CreateUnbounded<NotificationDispatchResult>(new UnboundedChannelOptions
            {
                SingleReader = true,
                SingleWriter = false,
                AllowSynchronousContinuations = false
            });
        }

        public ValueTask EnqueueAsync(NotificationDispatchResult dispatchResult, CancellationToken cancellationToken = default)
        {
            if (dispatchResult == null)
            {
                throw new ArgumentNullException(nameof(dispatchResult));
            }

            return _channel.Writer.WriteAsync(dispatchResult, cancellationToken);
        }

        public async ValueTask<NotificationDispatchResult> DequeueAsync(CancellationToken cancellationToken)
        {
            var dispatchResult = await _channel.Reader.ReadAsync(cancellationToken);
            return dispatchResult;
        }
    }
}

