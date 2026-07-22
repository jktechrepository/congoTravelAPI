# =============================================
# Script de migration MySQL vers MariaDB 10
# KelasiNaBiso API - Migration automatisée
# =============================================

Write-Host "╔════════════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║  Migration MySQL → MariaDB 10 - KelasiNaBiso API             ║" -ForegroundColor Cyan
Write-Host "╚════════════════════════════════════════════════════════════════╝" -ForegroundColor Cyan
Write-Host ""

# Configuration
$MySQLHost = "localhost"
$MySQLPort = 3306
$MySQLUser = "kansa"
$MySQLPassword = "kansa2025"
$MySQLDatabase = "KelasiNaBisoDb"

$MariaDBHost = "localhost"
$MariaDBPort = 3306
$MariaDBUser = "kansa"
$MariaDBPassword = "kansa2025"
$MariaDBDatabase = "KelasiNaBisoDb"

$BackupFolder = ".\backup_mysql_to_mariadb"
$BackupFile = "$BackupFolder\kelasinabiso_backup_$(Get-Date -Format 'yyyyMMdd_HHmmss').sql"

# =============================================
# ÉTAPE 1: Vérifications préalables
# =============================================
Write-Host "📋 ÉTAPE 1: Vérifications préalables" -ForegroundColor Yellow
Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Yellow

# Créer le dossier de backup si nécessaire
if (-not (Test-Path $BackupFolder)) {
    New-Item -ItemType Directory -Path $BackupFolder | Out-Null
    Write-Host "✅ Dossier de backup créé: $BackupFolder" -ForegroundColor Green
} else {
    Write-Host "✅ Dossier de backup existe: $BackupFolder" -ForegroundColor Green
}

# Vérifier mysqldump
try {
    $mysqldumpVersion = & mysqldump --version 2>&1
    Write-Host "✅ mysqldump disponible: $mysqldumpVersion" -ForegroundColor Green
} catch {
    Write-Host "❌ ERREUR: mysqldump non trouvé. Installez MySQL Client Tools." -ForegroundColor Red
    Write-Host "   Téléchargement: https://dev.mysql.com/downloads/mysql/" -ForegroundColor Red
    exit 1
}

# Vérifier mysql client
try {
    $mysqlVersion = & mysql --version 2>&1
    Write-Host "✅ mysql client disponible: $mysqlVersion" -ForegroundColor Green
} catch {
    Write-Host "❌ ERREUR: mysql client non trouvé." -ForegroundColor Red
    exit 1
}

Write-Host ""

# =============================================
# ÉTAPE 2: Sauvegarde de la base MySQL
# =============================================
Write-Host "💾 ÉTAPE 2: Sauvegarde de la base MySQL" -ForegroundColor Yellow
Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Yellow

Write-Host "⏳ Export de la base '$MySQLDatabase' en cours..." -ForegroundColor Cyan

$mysqldumpArgs = @(
    "--host=$MySQLHost",
    "--port=$MySQLPort",
    "--user=$MySQLUser",
    "--password=$MySQLPassword",
    "--databases", $MySQLDatabase,
    "--routines",
    "--triggers",
    "--events",
    "--add-drop-database",
    "--result-file=$BackupFile"
)

try {
    & mysqldump $mysqldumpArgs 2>&1 | Out-Null
    
    if (Test-Path $BackupFile) {
        $fileSize = (Get-Item $BackupFile).Length / 1MB
        Write-Host "✅ Sauvegarde créée avec succès!" -ForegroundColor Green
        Write-Host "   📁 Fichier: $BackupFile" -ForegroundColor Green
        Write-Host "   📊 Taille: $([math]::Round($fileSize, 2)) MB" -ForegroundColor Green
    } else {
        throw "Le fichier de sauvegarde n'a pas été créé."
    }
} catch {
    Write-Host "❌ ERREUR lors de la sauvegarde: $_" -ForegroundColor Red
    exit 1
}

Write-Host ""

# =============================================
# ÉTAPE 3: Pause et confirmation
# =============================================
Write-Host "⚠️  POINT DE CONTRÔLE" -ForegroundColor Yellow
Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Yellow
Write-Host ""
Write-Host "Avant de continuer, assurez-vous que:" -ForegroundColor Cyan
Write-Host "  1. ✅ MariaDB 10.x est installé et en cours d'exécution" -ForegroundColor White
Write-Host "  2. ✅ Le service MySQL est arrêté (si MariaDB utilise le même port)" -ForegroundColor White
Write-Host "  3. ✅ Vous avez les droits d'administration sur MariaDB" -ForegroundColor White
Write-Host "  4. ✅ La base '$MariaDBDatabase' n'existe pas encore sur MariaDB" -ForegroundColor White
Write-Host ""
$confirmation = Read-Host "Voulez-vous continuer avec l'importation dans MariaDB? (O/N)"

if ($confirmation -ne "O" -and $confirmation -ne "o") {
    Write-Host "⏸️  Migration annulée par l'utilisateur." -ForegroundColor Yellow
    Write-Host "   La sauvegarde est disponible: $BackupFile" -ForegroundColor Yellow
    exit 0
}

Write-Host ""

# =============================================
# ÉTAPE 4: Importation dans MariaDB
# =============================================
Write-Host "📥 ÉTAPE 4: Importation dans MariaDB" -ForegroundColor Yellow
Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Yellow

Write-Host "⏳ Importation de la base '$MariaDBDatabase' en cours..." -ForegroundColor Cyan

$mysqlImportArgs = @(
    "--host=$MariaDBHost",
    "--port=$MariaDBPort",
    "--user=$MariaDBUser",
    "--password=$MariaDBPassword"
)

try {
    Get-Content $BackupFile | & mysql $mysqlImportArgs 2>&1 | Out-Null
    Write-Host "✅ Base de données importée avec succès dans MariaDB!" -ForegroundColor Green
} catch {
    Write-Host "❌ ERREUR lors de l'importation: $_" -ForegroundColor Red
    Write-Host "⚠️  La sauvegarde est disponible: $BackupFile" -ForegroundColor Yellow
    exit 1
}

Write-Host ""

# =============================================
# ÉTAPE 5: Vérification
# =============================================
Write-Host "🔍 ÉTAPE 5: Vérification de l'importation" -ForegroundColor Yellow
Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Yellow

$verifyQuery = "SELECT COUNT(*) as TableCount FROM information_schema.tables WHERE table_schema = '$MariaDBDatabase';"
$verifyArgs = @(
    "--host=$MariaDBHost",
    "--port=$MariaDBPort",
    "--user=$MariaDBUser",
    "--password=$MariaDBPassword",
    "--skip-column-names",
    "--batch",
    "-e", $verifyQuery
)

try {
    $tableCount = & mysql $verifyArgs 2>&1
    Write-Host "✅ Nombre de tables importées: $tableCount" -ForegroundColor Green
} catch {
    Write-Host "⚠️  Impossible de vérifier l'importation: $_" -ForegroundColor Yellow
}

Write-Host ""

# =============================================
# ÉTAPE 6: Instructions finales
# =============================================
Write-Host "🎉 MIGRATION TERMINÉE AVEC SUCCÈS!" -ForegroundColor Green
Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Green
Write-Host ""
Write-Host "📋 Prochaines étapes:" -ForegroundColor Cyan
Write-Host ""
Write-Host "  1. Restaurez les packages NuGet:" -ForegroundColor White
Write-Host "     dotnet restore" -ForegroundColor Yellow
Write-Host ""
Write-Host "  2. Testez la connexion à MariaDB:" -ForegroundColor White
Write-Host "     dotnet run" -ForegroundColor Yellow
Write-Host ""
Write-Host "  3. Vérifiez que l'API fonctionne correctement" -ForegroundColor White
Write-Host "     Ouvrez: http://localhost:5002/swagger" -ForegroundColor Yellow
Write-Host ""
Write-Host "  4. Si tout fonctionne, vous pouvez supprimer la base MySQL" -ForegroundColor White
Write-Host "     (Conservez le fichier de backup!)" -ForegroundColor Yellow
Write-Host ""
Write-Host "📁 Sauvegarde conservée: $BackupFile" -ForegroundColor Cyan
Write-Host ""
Write-Host "✨ Votre API KelasiNaBiso fonctionne maintenant avec MariaDB 10!" -ForegroundColor Green
Write-Host ""

