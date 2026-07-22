using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using CongoTravel.Configuration;
using CongoTravel.Data;
using CongoTravel.Helpers;
using CongoTravel.Models;
using CongoTravel.Models.DTOs.FlexPay;
using CongoTravel.Models.DTOs.ReversementSite;
using CongoTravel.Models.Enums;

namespace CongoTravel.Services
{
    public class FlexPayPayOutCallbackService : IFlexPayPayOutCallbackService
    {
        private readonly CongoTravelDbContext _context;
        private readonly ILogger<FlexPayPayOutCallbackService> _logger;

        public FlexPayPayOutCallbackService(
            CongoTravelDbContext context,
            ILogger<FlexPayPayOutCallbackService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<FlexPayPayOutCallbackProcessResultDto> ProcessCallbackAsync(
            FlexPayCallbackDto callback,
            string? payloadComplet,
            string? headers,
            string? ipSource,
            CancellationToken cancellationToken = default)
        {
            var audit = new CallbackFlexPay
            {
                OrderNumber = callback.OrderNumber,
                Code = callback.Code,
                Reference = callback.Reference,
                ProviderReference = callback.ProviderReference,
                Amount = callback.Amount,
                AmountCustomer = callback.AmountCustomer,
                Phone = callback.Phone,
                Currency = callback.Currency,
                Channel = callback.Channel,
                CreatedAt = callback.CreatedAt,
                PayloadComplet = payloadComplet,
                Headers = headers,
                IpSource = ipSource
            };

            try
            {
                if (string.IsNullOrWhiteSpace(callback.OrderNumber) && string.IsNullOrWhiteSpace(callback.Reference))
                {
                    audit.MessageErreur = "Callback PayOut sans orderNumber ni reference.";
                    _context.CallbacksFlexPay.Add(audit);
                    await _context.SaveChangesAsync(cancellationToken);
                    return new FlexPayPayOutCallbackProcessResultDto
                    {
                        Success = false,
                        Message = audit.MessageErreur
                    };
                }

                var reversement = await FindReversementAsync(callback, cancellationToken);
                if (reversement == null)
                {
                    audit.MessageErreur = "Reversement site introuvable pour ce callback PayOut.";
                    _context.CallbacksFlexPay.Add(audit);
                    await _context.SaveChangesAsync(cancellationToken);
                    return new FlexPayPayOutCallbackProcessResultDto
                    {
                        Success = false,
                        Message = audit.MessageErreur
                    };
                }

                if (reversement.Statut is StatutReversementSite.Succes or StatutReversementSite.Echec or StatutReversementSite.Annule)
                {
                    audit.TraiteAvecSucces = true;
                    audit.DetailsTraitement = "Déjà finalisé (idempotence).";
                    _context.CallbacksFlexPay.Add(audit);
                    await _context.SaveChangesAsync(cancellationToken);
                    return new FlexPayPayOutCallbackProcessResultDto
                    {
                        Success = true,
                        AlreadyProcessed = true,
                        Message = audit.DetailsTraitement,
                        IdReversementSite = reversement.IdReversementSite,
                        Statut = reversement.Statut
                    };
                }

                ApplyCallbackToReversement(reversement, callback);
                reversement.DateCallback = DateTime.UtcNow;

                audit.TraiteAvecSucces = reversement.Statut == StatutReversementSite.Succes;
                audit.DetailsTraitement = reversement.MessageFlexPay;
                _context.CallbacksFlexPay.Add(audit);
                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation(
                    "Callback PayOut traité — reversement {IdReversementSite}, statut {Statut}, orderNumber {OrderNumber}",
                    reversement.IdReversementSite, reversement.Statut, reversement.OrderNumber);

                return new FlexPayPayOutCallbackProcessResultDto
                {
                    Success = reversement.Statut == StatutReversementSite.Succes,
                    Message = reversement.MessageFlexPay ?? "Callback PayOut traité.",
                    IdReversementSite = reversement.IdReversementSite,
                    Statut = reversement.Statut
                };
            }
            catch (Exception ex)
            {
                audit.MessageErreur = ex.Message;
                _context.CallbacksFlexPay.Add(audit);
                await _context.SaveChangesAsync(cancellationToken);
                _logger.LogError(ex, "Erreur traitement callback PayOut {OrderNumber}", callback.OrderNumber);
                throw;
            }
        }

        internal static void ApplyCallbackToReversement(ReversementSite reversement, FlexPayCallbackDto callback)
        {
            if (!string.IsNullOrWhiteSpace(callback.OrderNumber))
                reversement.OrderNumber = callback.OrderNumber.Trim();
            if (!string.IsNullOrWhiteSpace(callback.ProviderReference))
                reversement.ProviderReference = callback.ProviderReference.Trim();
            if (!string.IsNullOrWhiteSpace(callback.Channel))
                reversement.Channel = callback.Channel.Trim();

            reversement.CodeFlexPay = callback.Code;
            reversement.Statut = string.Equals(callback.Code?.Trim(), "0", StringComparison.Ordinal)
                ? StatutReversementSite.Succes
                : StatutReversementSite.Echec;
            reversement.MessageFlexPay = reversement.Statut == StatutReversementSite.Succes
                ? "Reversement confirmé par FlexPay."
                : $"Reversement refusé par FlexPay (code {callback.Code}).";
        }

        private async Task<ReversementSite?> FindReversementAsync(
            FlexPayCallbackDto callback,
            CancellationToken cancellationToken)
        {
            if (!string.IsNullOrWhiteSpace(callback.OrderNumber))
            {
                var byOrder = await _context.ReversementsSite
                    .FirstOrDefaultAsync(r => r.OrderNumber == callback.OrderNumber, cancellationToken);
                if (byOrder != null)
                    return byOrder;
            }

            if (!string.IsNullOrWhiteSpace(callback.Reference))
            {
                return await _context.ReversementsSite
                    .FirstOrDefaultAsync(r => r.Reference == callback.Reference, cancellationToken);
            }

            return null;
        }
    }
}
