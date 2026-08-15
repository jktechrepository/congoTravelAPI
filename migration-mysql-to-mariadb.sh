#!/bin/bash
# =============================================
# Script de migration MySQL vers MariaDB 10
# KelasiNaBiso API - Migration automatisée
# Pour Linux/macOS
# =============================================

# Couleurs pour le terminal
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
CYAN='\033[0;36m'
NC='\033[0m' # No Color

echo -e "${CYAN}╔════════════════════════════════════════════════════════════════╗${NC}"
echo -e "${CYAN}║  Migration MySQL → MariaDB 10 - KelasiNaBiso API             ║${NC}"
echo -e "${CYAN}╚════════════════════════════════════════════════════════════════╝${NC}"
echo ""

# Configuration
MYSQL_HOST="localhost"
MYSQL_PORT=3306
MYSQL_USER="kansa"
MYSQL_PASSWORD="kansa2025"
MYSQL_DATABASE="KelasiNaBisoDb"

MARIADB_HOST="localhost"
MARIADB_PORT=3306
MARIADB_USER="kansa"
MARIADB_PASSWORD="kansa2025"
MARIADB_DATABASE="KelasiNaBisoDb"

BACKUP_FOLDER="./backup_mysql_to_mariadb"
BACKUP_FILE="$BACKUP_FOLDER/kelasinabiso_backup_$(date +%Y%m%d_%H%M%S).sql"

# =============================================
# ÉTAPE 1: Vérifications préalables
# =============================================
echo -e "${YELLOW}📋 ÉTAPE 1: Vérifications préalables${NC}"
echo -e "${YELLOW}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"

# Créer le dossier de backup si nécessaire
if [ ! -d "$BACKUP_FOLDER" ]; then
    mkdir -p "$BACKUP_FOLDER"
    echo -e "${GREEN}✅ Dossier de backup créé: $BACKUP_FOLDER${NC}"
else
    echo -e "${GREEN}✅ Dossier de backup existe: $BACKUP_FOLDER${NC}"
fi

# Vérifier mysqldump
if ! command -v mysqldump &> /dev/null; then
    echo -e "${RED}❌ ERREUR: mysqldump non trouvé. Installez MySQL Client Tools.${NC}"
    exit 1
fi
echo -e "${GREEN}✅ mysqldump disponible: $(mysqldump --version)${NC}"

# Vérifier mysql client
if ! command -v mysql &> /dev/null; then
    echo -e "${RED}❌ ERREUR: mysql client non trouvé.${NC}"
    exit 1
fi
echo -e "${GREEN}✅ mysql client disponible: $(mysql --version)${NC}"

echo ""

# =============================================
# ÉTAPE 2: Sauvegarde de la base MySQL
# =============================================
echo -e "${YELLOW}💾 ÉTAPE 2: Sauvegarde de la base MySQL${NC}"
echo -e "${YELLOW}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"

echo -e "${CYAN}⏳ Export de la base '$MYSQL_DATABASE' en cours...${NC}"

if mysqldump --host="$MYSQL_HOST" \
             --port="$MYSQL_PORT" \
             --user="$MYSQL_USER" \
             --password="$MYSQL_PASSWORD" \
             --databases "$MYSQL_DATABASE" \
             --routines \
             --triggers \
             --events \
             --add-drop-database \
             --result-file="$BACKUP_FILE" 2>/dev/null; then
    
    if [ -f "$BACKUP_FILE" ]; then
        FILE_SIZE=$(du -h "$BACKUP_FILE" | cut -f1)
        echo -e "${GREEN}✅ Sauvegarde créée avec succès!${NC}"
        echo -e "${GREEN}   📁 Fichier: $BACKUP_FILE${NC}"
        echo -e "${GREEN}   📊 Taille: $FILE_SIZE${NC}"
    else
        echo -e "${RED}❌ ERREUR: Le fichier de sauvegarde n'a pas été créé.${NC}"
        exit 1
    fi
else
    echo -e "${RED}❌ ERREUR lors de la sauvegarde${NC}"
    exit 1
fi

echo ""

# =============================================
# ÉTAPE 3: Pause et confirmation
# =============================================
echo -e "${YELLOW}⚠️  POINT DE CONTRÔLE${NC}"
echo -e "${YELLOW}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
echo ""
echo -e "${CYAN}Avant de continuer, assurez-vous que:${NC}"
echo -e "  ${NC}1. ✅ MariaDB 10.x est installé et en cours d'exécution${NC}"
echo -e "  ${NC}2. ✅ Le service MySQL est arrêté (si MariaDB utilise le même port)${NC}"
echo -e "  ${NC}3. ✅ Vous avez les droits d'administration sur MariaDB${NC}"
echo -e "  ${NC}4. ✅ La base '$MARIADB_DATABASE' n'existe pas encore sur MariaDB${NC}"
echo ""
read -p "Voulez-vous continuer avec l'importation dans MariaDB? (O/N) " -n 1 -r
echo ""

if [[ ! $REPLY =~ ^[Oo]$ ]]; then
    echo -e "${YELLOW}⏸️  Migration annulée par l'utilisateur.${NC}"
    echo -e "${YELLOW}   La sauvegarde est disponible: $BACKUP_FILE${NC}"
    exit 0
fi

echo ""

# =============================================
# ÉTAPE 4: Importation dans MariaDB
# =============================================
echo -e "${YELLOW}📥 ÉTAPE 4: Importation dans MariaDB${NC}"
echo -e "${YELLOW}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"

echo -e "${CYAN}⏳ Importation de la base '$MARIADB_DATABASE' en cours...${NC}"

if mysql --host="$MARIADB_HOST" \
         --port="$MARIADB_PORT" \
         --user="$MARIADB_USER" \
         --password="$MARIADB_PASSWORD" \
         < "$BACKUP_FILE" 2>/dev/null; then
    echo -e "${GREEN}✅ Base de données importée avec succès dans MariaDB!${NC}"
else
    echo -e "${RED}❌ ERREUR lors de l'importation${NC}"
    echo -e "${YELLOW}⚠️  La sauvegarde est disponible: $BACKUP_FILE${NC}"
    exit 1
fi

echo ""

# =============================================
# ÉTAPE 5: Vérification
# =============================================
echo -e "${YELLOW}🔍 ÉTAPE 5: Vérification de l'importation${NC}"
echo -e "${YELLOW}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"

TABLE_COUNT=$(mysql --host="$MARIADB_HOST" \
                    --port="$MARIADB_PORT" \
                    --user="$MARIADB_USER" \
                    --password="$MARIADB_PASSWORD" \
                    --skip-column-names \
                    --batch \
                    -e "SELECT COUNT(*) FROM information_schema.tables WHERE table_schema = '$MARIADB_DATABASE';" 2>/dev/null)

if [ -n "$TABLE_COUNT" ]; then
    echo -e "${GREEN}✅ Nombre de tables importées: $TABLE_COUNT${NC}"
else
    echo -e "${YELLOW}⚠️  Impossible de vérifier l'importation${NC}"
fi

echo ""

# =============================================
# ÉTAPE 6: Instructions finales
# =============================================
echo -e "${GREEN}🎉 MIGRATION TERMINÉE AVEC SUCCÈS!${NC}"
echo -e "${GREEN}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
echo ""
echo -e "${CYAN}📋 Prochaines étapes:${NC}"
echo ""
echo -e "  ${NC}1. Restaurez les packages NuGet:${NC}"
echo -e "     ${YELLOW}dotnet restore${NC}"
echo ""
echo -e "  ${NC}2. Testez la connexion à MariaDB:${NC}"
echo -e "     ${YELLOW}dotnet run${NC}"
echo ""
echo -e "  ${NC}3. Vérifiez que l'API fonctionne correctement${NC}"
echo -e "     ${YELLOW}Ouvrez: http://localhost:5002/swagger${NC}"
echo ""
echo -e "  ${NC}4. Si tout fonctionne, vous pouvez supprimer la base MySQL${NC}"
echo -e "     ${YELLOW}(Conservez le fichier de backup!)${NC}"
echo ""
echo -e "${CYAN}📁 Sauvegarde conservée: $BACKUP_FILE${NC}"
echo ""
echo -e "${GREEN}✨ Votre API KelasiNaBiso fonctionne maintenant avec MariaDB 10!${NC}"
echo ""

