# Installation Simplifiée des Triggers de Synchronisation

Write-Host "`n==================================================================" -ForegroundColor Cyan
Write-Host "INSTALLATION DES TRIGGERS DE SYNCHRONISATION" -ForegroundColor Green
Write-Host "==================================================================`n" -ForegroundColor Cyan

Write-Host "IMPORTANT : Ce script va installer 4 triggers SQL automatiques`n" -ForegroundColor Yellow

# Instructions manuelles
Write-Host "INSTRUCTIONS :`n" -ForegroundColor Cyan

Write-Host "1. Ouvre MySQL Workbench ou phpMyAdmin" -ForegroundColor White
Write-Host "2. Connecte-toi à la base de données : KnbV2_db" -ForegroundColor White
Write-Host "3. Fais un BACKUP avant :" -ForegroundColor White
Write-Host "   Server → Data Export → Export to Self-Contained File`n" -ForegroundColor Gray

Write-Host "4. Execute le fichier SQL :" -ForegroundColor White
Write-Host "   File → Run SQL Script → Migrations\AddSyncTriggers.sql`n" -ForegroundColor Gray

Write-Host "5. Vérifie la création des triggers :" -ForegroundColor White
Write-Host "   SHOW TRIGGERS WHERE ``Trigger`` LIKE 'sync_%';`n" -ForegroundColor Gray

Write-Host "RÉSULTAT ATTENDU : 4 triggers créés`n" -ForegroundColor Green
Write-Host "  - sync_agent_to_utilisateur_update" -ForegroundColor White
Write-Host "  - sync_tuteur_to_utilisateur_update" -ForegroundColor White
Write-Host "  - sync_utilisateur_to_agent_update" -ForegroundColor White
Write-Host "  - sync_utilisateur_to_tuteur_update`n" -ForegroundColor White

Write-Host "==================================================================`n" -ForegroundColor Cyan

# Ouvrir le fichier SQL directement
$sqlFile = "Migrations\AddSyncTriggers.sql"
if (Test-Path $sqlFile) {
    Write-Host "Ouvrir le fichier SQL maintenant ? (O/N) " -ForegroundColor Yellow -NoNewline
    $response = Read-Host
    
    if ($response -eq "O" -or $response -eq "o") {
        Start-Process notepad.exe $sqlFile
        Write-Host "`nFichier ouvert dans Notepad" -ForegroundColor Green
        Write-Host "Copie le contenu et execute-le dans MySQL Workbench`n" -ForegroundColor Cyan
    }
}

Write-Host "Documentation complète : GUIDE_SYNCHRONISATION_UTILISATEUR_AGENT_TUTEUR.md`n" -ForegroundColor Cyan

