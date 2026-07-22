namespace CongoTravel.Services
{
    public interface IDeviseMontantConverter
    {
        Task<(decimal MontantCible, decimal Taux)> ConvertAsync(
            int idSociete,
            decimal montant,
            string codeSource,
            string codeCible,
            DateTime dateRef,
            CancellationToken cancellationToken = default);
    }
}
