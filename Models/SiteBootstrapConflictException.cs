using System;

namespace CongoTravel.Models
{
    /// <summary>Raisons métier explicites pour un échec de création site + gérant (réponse HTTP 409).</summary>
    public enum SiteBootstrapConflictReason
    {
        SiteCodeAlreadyExists,
        GerantEmailAlreadyExists,
        GerantEmailSameAsSocieteContact,
        AgentGerantEmailAlreadyExists,
        AgentGerantMatriculeAlreadyExists
    }

    public class SiteBootstrapConflictException : Exception
    {
        public SiteBootstrapConflictReason Reason { get; }

        public SiteBootstrapConflictException(SiteBootstrapConflictReason reason, string message)
            : base(message)
        {
            Reason = reason;
        }
    }
}
