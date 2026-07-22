using CongoTravel.Models;
using CongoTravel.Models.DTOs;

namespace CongoTravel.Helpers
{
    public static class RapportCaisseMetricsHelper
    {
        public static (DateTime FromUtc, DateTime ToUtc, string ModePeriode, bool IsValid, string? ErrorMessage) ResolvePeriode(
            DateTime? datePrecise,
            DateTime? dateDebut,
            DateTime? dateFin)
        {
            if (dateDebut.HasValue ^ dateFin.HasValue)
            {
                return (default, default, string.Empty, false, "Les paramètres dateDebut et dateFin doivent être fournis ensemble.");
            }

            if (dateDebut.HasValue && dateFin.HasValue)
            {
                var from = dateDebut.Value.Date;
                var to = dateFin.Value.Date.AddDays(1).AddTicks(-1);
                if (from > to)
                {
                    return (default, default, string.Empty, false, "dateDebut doit être inférieure ou égale à dateFin.");
                }

                return (from, to, "intervalle", true, null);
            }

            var date = (datePrecise ?? DateTime.UtcNow).Date;
            return (date, date.AddDays(1).AddTicks(-1), "jour", true, null);
        }

        public static RapportCaisseDto BuildRapportCaisse(
            IReadOnlyList<Paiement> paiements,
            int idSociete,
            int? idUtilisateur,
            DateTime periodeDebutUtc,
            DateTime periodeFinUtc,
            string modePeriode,
            string codeDevisePrincipale)
        {
            decimal ResolveMontant(Paiement p) => CaissierTransportMetricsHelper.ResolveMontantPaye(p);

            decimal SumBucket(MethodePaiementHelper.RecetteBucket bucket) =>
                paiements
                    .Where(p => MethodePaiementHelper.GetRecetteBucket(p.MethodePaiement) == bucket)
                    .Sum(ResolveMontant);

            int CountBucket(MethodePaiementHelper.RecetteBucket bucket) =>
                paiements.Count(p => MethodePaiementHelper.GetRecetteBucket(p.MethodePaiement) == bucket);

            var especeMontant = SumBucket(MethodePaiementHelper.RecetteBucket.Espece);
            var especeCount = CountBucket(MethodePaiementHelper.RecetteBucket.Espece);

            var mobileMoneyMontant = SumBucket(MethodePaiementHelper.RecetteBucket.MobileMoney);
            var mobileMoneyCount = CountBucket(MethodePaiementHelper.RecetteBucket.MobileMoney);

            var carteMontant = SumBucket(MethodePaiementHelper.RecetteBucket.Carte);
            var carteCount = CountBucket(MethodePaiementHelper.RecetteBucket.Carte);

            var virementMontant = SumBucket(MethodePaiementHelper.RecetteBucket.Virement);
            var virementCount = CountBucket(MethodePaiementHelper.RecetteBucket.Virement);

            var autreMontant = SumBucket(MethodePaiementHelper.RecetteBucket.Autre);
            var autreCount = CountBucket(MethodePaiementHelper.RecetteBucket.Autre);

            var electroniqueMontant = mobileMoneyMontant + carteMontant + virementMontant + autreMontant;
            var electroniqueCount = mobileMoneyCount + carteCount + virementCount + autreCount;

            var total = especeMontant + electroniqueMontant;
            var totalTransactions = paiements.Count;

            var parDevise = paiements
                .GroupBy(p => string.IsNullOrWhiteSpace(p.CodeDevisePaiement) ? codeDevisePrincipale : p.CodeDevisePaiement)
                .Select(g =>
                {
                    decimal EspecesMontant() => g
                        .Where(x => MethodePaiementHelper.GetRecetteBucket(x.MethodePaiement) == MethodePaiementHelper.RecetteBucket.Espece)
                        .Sum(x => x.MontantPaye ?? 0m);

                    int EspecesCount() => g.Count(x =>
                        MethodePaiementHelper.GetRecetteBucket(x.MethodePaiement) == MethodePaiementHelper.RecetteBucket.Espece);

                    decimal ElectroniqueMontant() => g
                        .Where(x => MethodePaiementHelper.GetRecetteBucket(x.MethodePaiement) != MethodePaiementHelper.RecetteBucket.Espece)
                        .Sum(x => x.MontantPaye ?? 0m);

                    int ElectroniqueCount() => g.Count(x =>
                        MethodePaiementHelper.GetRecetteBucket(x.MethodePaiement) != MethodePaiementHelper.RecetteBucket.Espece);

                    return new RapportCaisseParDeviseItemDto
                    {
                        CodeDevisePaiement = g.Key,
                        Especes = new RapportCaisseBlocDevisePaiementDto
                        {
                            MontantPaye = EspecesMontant(),
                            Count = EspecesCount()
                        },
                        Electronique = new RapportCaisseBlocDevisePaiementDto
                        {
                            MontantPaye = ElectroniqueMontant(),
                            Count = ElectroniqueCount()
                        }
                    };
                })
                .OrderBy(x => x.CodeDevisePaiement)
                .ToList();

            return new RapportCaisseDto
            {
                IdSociete = idSociete,
                IdUtilisateur = idUtilisateur,
                ModePeriode = modePeriode,
                PeriodeDebut = periodeDebutUtc,
                PeriodeFin = periodeFinUtc,
                CodeDevisePrincipale = codeDevisePrincipale,
                Synthese = new RapportCaisseSyntheseDto
                {
                    TotalEncaisse = total,
                    NombreTransactions = totalTransactions,
                    PartEspecesPourcentage = total > 0m ? Math.Round(especeMontant / total * 100m, 2) : 0m,
                    PartElectroniquePourcentage = total > 0m ? Math.Round(electroniqueMontant / total * 100m, 2) : 0m
                },
                Especes = new RapportCaisseBlocDevisePrincipaleDto
                {
                    MontantDevisePrincipale = especeMontant,
                    NombreTransactions = especeCount
                },
                Electronique = new RapportCaisseElectroniqueDto
                {
                    MontantDevisePrincipale = electroniqueMontant,
                    NombreTransactions = electroniqueCount,
                    Detail = new RapportCaisseDetailElectroniqueDto
                    {
                        MobileMoney = new RapportCaisseBlocDevisePrincipaleDto
                        {
                            MontantDevisePrincipale = mobileMoneyMontant,
                            NombreTransactions = mobileMoneyCount
                        },
                        Carte = new RapportCaisseBlocDevisePrincipaleDto
                        {
                            MontantDevisePrincipale = carteMontant,
                            NombreTransactions = carteCount
                        },
                        Virement = new RapportCaisseBlocDevisePrincipaleDto
                        {
                            MontantDevisePrincipale = virementMontant,
                            NombreTransactions = virementCount
                        },
                        Autre = new RapportCaisseBlocDevisePrincipaleDto
                        {
                            MontantDevisePrincipale = autreMontant,
                            NombreTransactions = autreCount
                        }
                    }
                },
                ParDevise = parDevise
            };
        }
    }
}
