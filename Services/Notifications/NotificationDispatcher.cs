using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CongoTravel.Data;
using CongoTravel.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CongoTravel.Services.Notifications
{
    public class NotificationDispatcher : INotificationDispatcher
    {
        private readonly CongoTravelDbContext _context;
        private readonly ILogger<NotificationDispatcher> _logger;

        public NotificationDispatcher(
            CongoTravelDbContext context,
            ILogger<NotificationDispatcher> logger)
        {
            _context = context;
            _logger = logger;
        }

        // Méthodes de préparation de notifications supprimées car les modèles associés ont été supprimés
    }
}

