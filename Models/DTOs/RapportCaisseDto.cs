namespace CongoTravel.Models.DTOs
{
    public class RapportCaisseDto
    {
        public int IdSociete { get; set; }
        public int? IdUtilisateur { get; set; }
        public string ModePeriode { get; set; } = "jour";
        public DateTime PeriodeDebut { get; set; }
        public DateTime PeriodeFin { get; set; }
        public string CodeDevisePrincipale { get; set; } = "CDF";
        public RapportCaisseSyntheseDto Synthese { get; set; } = new();
        public RapportCaisseBlocDevisePrincipaleDto Especes { get; set; } = new();
        public RapportCaisseElectroniqueDto Electronique { get; set; } = new();
        public List<RapportCaisseParDeviseItemDto> ParDevise { get; set; } = new();
    }

    public class RapportCaisseSyntheseDto
    {
        public decimal TotalEncaisse { get; set; }
        public int NombreTransactions { get; set; }
        public decimal PartEspecesPourcentage { get; set; }
        public decimal PartElectroniquePourcentage { get; set; }
    }

    public class RapportCaisseBlocDevisePrincipaleDto
    {
        public decimal MontantDevisePrincipale { get; set; }
        public int NombreTransactions { get; set; }
    }

    public class RapportCaisseElectroniqueDto : RapportCaisseBlocDevisePrincipaleDto
    {
        public RapportCaisseDetailElectroniqueDto Detail { get; set; } = new();
    }

    public class RapportCaisseDetailElectroniqueDto
    {
        public RapportCaisseBlocDevisePrincipaleDto MobileMoney { get; set; } = new();
        public RapportCaisseBlocDevisePrincipaleDto Carte { get; set; } = new();
        public RapportCaisseBlocDevisePrincipaleDto Virement { get; set; } = new();
        public RapportCaisseBlocDevisePrincipaleDto Autre { get; set; } = new();
    }

    public class RapportCaisseParDeviseItemDto
    {
        public string CodeDevisePaiement { get; set; } = "CDF";
        public RapportCaisseBlocDevisePaiementDto Especes { get; set; } = new();
        public RapportCaisseBlocDevisePaiementDto Electronique { get; set; } = new();
    }

    public class RapportCaisseBlocDevisePaiementDto
    {
        public decimal MontantPaye { get; set; }
        public int Count { get; set; }
    }
}
