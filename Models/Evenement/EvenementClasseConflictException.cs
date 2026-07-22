namespace CongoTravel.Models.Evenement
{
    /// <summary>Conflit métier sur <c>EvenementClasse</c> (ex. code dupliqué par société).</summary>
    public class EvenementClasseConflictException : Exception
    {
        public EvenementClasseConflictException(string message) : base(message)
        {
        }
    }
}
