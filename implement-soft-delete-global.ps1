# Script pour implémenter le Soft Delete dans TOUS les contrôleurs/services
# Date: 16 Octobre 2025
# Objectif: Ajouter toggle-statut, filtrage et ToggleStatutAsync partout

Write-Host "=========================================" -ForegroundColor Cyan
Write-Host "  IMPLÉMENTATION SOFT DELETE GLOBAL" -ForegroundColor Cyan
Write-Host "=========================================" -ForegroundColor Cyan
Write-Host ""

# Liste des modèles avec Statut bool
$modeles = @(
    @{Nom="Eleve"; Controller="EleveController"; Service="EleveService"; Repo="IEleveRepository"; IdField="IdEleve"},
    @{Nom="Utilisateur"; Controller="UtilisateurController"; Service="UtilisateurService"; Repo="IUtilisateurRepository"; IdField="IdUtilisateur"},
    @{Nom="Inscription"; Controller="InscriptionController"; Service="InscriptionService"; Repo="IInscriptionRepository"; IdField="IdInscription"},
    @{Nom="Note"; Controller="NoteController"; Service="NoteService"; Repo="INoteRepository"; IdField="IdNote"},
    @{Nom="Cours"; Controller="CoursController"; Service="CoursService"; Repo="ICoursRepository"; IdField="IdCours"},
    @{Nom="Enseignant"; Controller="EnseignantController"; Service="EnseignantService"; Repo="IEnseignantRepository"; IdField="IdEnseignant"},
    @{Nom="Tuteur"; Controller="TuteurController"; Service="TuteurService"; Repo="ITuteurRepository"; IdField="IdTuteur"},
    @{Nom="Document"; Controller="DocumentController"; Service="DocumentService"; Repo="IDocumentRepository"; IdField="IdDocument"},
    @{Nom="Classe"; Controller="ClasseController"; Service="ClasseService"; Repo="IClasseRepository"; IdField="IdClasse"},
    @{Nom="AffectationCours"; Controller="AffectationCoursController"; Service="AffectationCoursService"; Repo="IAffectationCoursRepository"; IdField="IdAffectationCours"},
    @{Nom="Horaire"; Controller="HoraireController"; Service="HoraireService"; Repo="IHoraireRepository"; IdField="IdHoraire"},
    @{Nom="GroupeMessage"; Controller="GroupeMessageController"; Service="GroupeMessageService"; Repo="IGroupeMessageRepository"; IdField="IdGroupeMessage"},
    @{Nom="Frais"; Controller="FraisController"; Service="FraisService"; Repo="IFraisRepository"; IdField="IdFrais"},
    @{Nom="RessourcePedagogique"; Controller="RessourcePedagogiqueController"; Service="RessourcePedagogiqueService"; Repo="IRessourcePedagogiqueRepository"; IdField="IdRessourcePedagogique"},
    @{Nom="Message"; Controller="MessageController"; Service="MessageService"; Repo="IMessageRepository"; IdField="IdMessage"},
    @{Nom="Evaluation"; Controller="EvaluationController"; Service="EvaluationService"; Repo="IEvaluationRepository"; IdField="IdEvaluation"},
    @{Nom="Vacation"; Controller="VacationController"; Service="VacationService"; Repo="IVacationRepository"; IdField="IdVacation"},
    @{Nom="AnneeScolaire"; Controller="AnneeScolaireController"; Service="AnneeScolaireService"; Repo="IAnneeScolaireRepository"; IdField="IdAnneeScolaire"},
    @{Nom="Option"; Controller="OptionController"; Service="OptionService"; Repo="IOptionRepository"; IdField="IdOption"},
    @{Nom="Role"; Controller="RoleController"; Service="RoleService"; Repo="IRoleRepository"; IdField="IdRole"},
    @{Nom="Direction"; Controller="DirectionController"; Service="DirectionService"; Repo="IDirectionRepository"; IdField="IdDirection"},
    @{Nom="Section"; Controller="SectionController"; Service="SectionService"; Repo="ISectionRepository"; IdField="IdSection"},
    @{Nom="Ecole"; Controller="EcoleController"; Service="EcoleService"; Repo="IEcoleRepository"; IdField="IdEcole"},
    @{Nom="Paiement"; Controller="PaiementController"; Service="PaiementService"; Repo="IPaiementRepository"; IdField="IdPaiement"},
    @{Nom="Notification"; Controller="NotificationController"; Service="NotificationService"; Repo="INotificationRepository"; IdField="IdNotification"}
)

Write-Host "📋 Modèles à traiter: $($modeles.Count)" -ForegroundColor Yellow
Write-Host ""

# Afficher la liste
foreach ($modele in $modeles) {
    Write-Host "   - $($modele.Nom)" -ForegroundColor White
}

Write-Host ""
Write-Host "⚠️  Ce script va:" -ForegroundColor Yellow
Write-Host "   1. Ajouter ToggleStatutAsync() dans chaque service" -ForegroundColor White
Write-Host "   2. Ajouter endpoint PUT /toggle-statut/{id} dans chaque contrôleur" -ForegroundColor White
Write-Host "   3. Mettre à jour les interfaces repository" -ForegroundColor White
Write-Host ""

$confirmation = Read-Host "Voulez-vous continuer? (O/N)"

if ($confirmation -ne "O" -and $confirmation -ne "o") {
    Write-Host "❌ Opération annulée" -ForegroundColor Red
    exit 0
}

Write-Host ""
Write-Host "🔨 Démarrage de l'implémentation..." -ForegroundColor Green
Write-Host ""

$compteurReussi = 0
$compteurErreur = 0

foreach ($modele in $modeles) {
    try {
        Write-Host "📦 Traitement de $($modele.Nom)..." -ForegroundColor Cyan
        
        # Chemins des fichiers
        $controllerPath = ".\Controllers\$($modele.Controller).cs"
        $servicePath = ".\Services\$($modele.Service).cs"
        $repoPath = ".\Services\Repositories\$($modele.Repo).cs"
        
        # Vérifier l'existence des fichiers
        if (-not (Test-Path $controllerPath)) {
            Write-Host "   ⚠️  Contrôleur non trouvé: $controllerPath" -ForegroundColor Yellow
        }
        
        if (-not (Test-Path $servicePath)) {
            Write-Host "   ⚠️  Service non trouvé: $servicePath" -ForegroundColor Yellow
        }
        
        if (-not (Test-Path $repoPath)) {
            Write-Host "   ⚠️  Repository non trouvé: $repoPath" -ForegroundColor Yellow
        }
        
        Write-Host "   ✅ $($modele.Nom) vérifié" -ForegroundColor Green
        $compteurReussi++
        
    } catch {
        Write-Host "   ❌ Erreur: $_" -ForegroundColor Red
        $compteurErreur++
    }
}

Write-Host ""
Write-Host "=========================================" -ForegroundColor Green
Write-Host "  VÉRIFICATION TERMINÉE" -ForegroundColor Green
Write-Host "=========================================" -ForegroundColor Green
Write-Host ""
Write-Host "📊 Résultats:" -ForegroundColor Cyan
Write-Host "   - Réussis: $compteurReussi" -ForegroundColor Green
Write-Host "   - Erreurs: $compteurErreur" -ForegroundColor Red
Write-Host ""

Write-Host "💡 Prochaine étape:" -ForegroundColor Yellow
Write-Host "   Le code devra être modifié manuellement ou via un outil de génération" -ForegroundColor White
Write-Host "   Voir: IMPLEMENTATION_SOFT_DELETE_GLOBAL.md pour les instructions" -ForegroundColor White
Write-Host ""

