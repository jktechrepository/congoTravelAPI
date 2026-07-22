# Archive documentation Kenergie / ClientFacture

Ce dossier regroupe la documentation **héritée du projet Kenergie** (facturation énergie, ClientFacture, arriérés pré-existants) conservée à titre historique lors de la migration vers **CongoTravel** (transport bus).

## Contenu archivé

| Document source | Description |
|-----------------|-------------|
| [ANALYSE_EXPERT_SYSTEME_KENERGIE.md](../Themes/11_analyses_plans/ANALYSE_EXPERT_SYSTEME_KENERGIE.md) | Analyse expert du système Kenergie d'origine |
| Thèmes 06 / 08 (ClientFacture) | Voir `Documentation/Themes/06_facturation_paiement/` et `08_notifications_communication/` — sections marquées legacy |

## Documentation active CongoTravel

Utiliser en priorité :

- [INDEX_DOCUMENTATION_THEMATIQUE.md](../INDEX_DOCUMENTATION_THEMATIQUE.md)
- [05_transport_sync](../Themes/05_transport_sync/) — workflow réservation V2, billets, sync offline
- [06_facturation_paiement](../Themes/06_facturation_paiement/) — FlexPay, paiements voyage (pas ClientFacture)

## Alias sync legacy

Dans l'API sync, le champ `idClientFacture` des arriérés est un **alias** de `idPaiement` (API v2). Les nouveaux clients doivent utiliser `idPaiement`.
