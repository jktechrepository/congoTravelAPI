using System;

namespace CongoTravel.Models
{
    /// <summary>Raisons métier explicites pour un échec de création société + site + gérant (réponse HTTP 409).</summary>
    public enum SocieteBootstrapConflictReason
    {
        SiteCodeAlreadyExists,
        GerantEmailAlreadyExists,
        GerantEmailSameAsSocieteContact,
        AgentGerantEmailAlreadyExists,
        AgentGerantMatriculeAlreadyExists,
        SocieteContactEmailAlreadyUsed
    }

    public class SocieteBootstrapConflictException : Exception
    {
        public SocieteBootstrapConflictReason Reason { get; }

        public SocieteBootstrapConflictException(SocieteBootstrapConflictReason reason, string message)
            : base(message)
        {
            Reason = reason;
        }
    }
}
