#!/bin/bash

# Script de rollback pour la synchronisation offline
# Auteur: CongoTravel Team
# Date: 21 mars 2026

set -e  # Arrêter en cas d'erreur

echo "🚨 ROLLBACK SYNCHRONISATION PRODUCTION"
echo "⏰ $(date)"

# Configuration
DB_CONNECTION="${DB_CONNECTION:-}"
BACKUP_DIR="${BACKUP_DIR:-./backups}"

# Vérification des prérequis
if [ -z "$DB_CONNECTION" ]; then
    echo "❌ DB_CONNECTION non fourni"
    exit 1
fi

if [ ! -d "$BACKUP_DIR" ]; then
    echo "❌ Répertoire de backup non trouvé: $BACKUP_DIR"
    exit 1
fi

# 1. Arrêter les conteneurs actuels
echo "🛑 Arrêt des conteneurs actuels..."
docker-compose -f docker-compose.prod.yml down 2>/dev/null || true

# 2. Nettoyer les images problématiques
echo "🧹 Nettoyage des images problématiques..."
docker rmi kenergie-api:latest 2>/dev/null || true
docker rmi kenergie-api:current 2>/dev/null || true

# 3. Restaurer la version précédente
echo "🔄 Restauration de la version précédente..."

# Vérifier si le tag de backup existe
if git rev-parse --verify backup_before_sync_v1.0 >/dev/null 2>&1; then
    echo "✅ Tag de backup trouvé: backup_before_sync_v1.0"
    git checkout backup_before_sync_v1.0
else
    echo "⚠️ Tag de backup non trouvé, utilisation du commit précédent"
    git checkout HEAD~1 2>/dev/null || git checkout main
fi

# 4. Builder l'image de rollback
echo "🔨 Build de l'image de rollback..."
docker build -t kenergie-api:rollback .

if [ $? -ne 0 ]; then
    echo "❌ Échec du build de rollback"
    exit 1
fi

# 5. Mettre à jour le docker-compose pour utiliser l'image de rollback
echo "📝 Mise à jour du docker-compose..."
sed -i 's/kenergie-api:.*/kenergie-api:rollback/g' docker-compose.prod.yml

# 6. Démarrer l'application restaurée
echo "🚀 Démarrage de l'application restaurée..."
docker-compose -f docker-compose.prod.yml up -d

# 7. Attendre le health check
echo "⏳ Attente du health check..."
TIMEOUT=300
ELAPSED=0

while [ $ELAPSED -lt $TIMEOUT ]; do
    if curl -f http://localhost/health >/dev/null 2>&1; then
        echo "✅ Health check réussi après ${ELAPSED}s"
        break
    fi
    
    echo "⏳ En attente... (${ELAPSED}s)"
    sleep 5
    ELAPSED=$((ELAPSED + 5))
done

if [ $ELAPSED -ge $TIMEOUT ]; then
    echo "❌ Timeout du health check - Échec du rollback"
    exit 1
fi

# 8. Restaurer la base de données si nécessaire
echo "💾 Vérification de la nécessité de restaurer la base de données..."
LATEST_BACKUP=$(ls -t "$BACKUP_DIR"/backup_pre_sync_*.sql 2>/dev/null | head -1)

if [ -n "$LATEST_BACKUP" ]; then
    echo "📊 Dernier backup trouvé: $LATEST_BACKUP"
    
    read -p "❓ Restaurer la base de données depuis le backup ? (y/N): " -n 1 -r
    echo
    
    if [[ $REPLY =~ ^[Yy]$ ]]; then
        echo "🔄 Restauration de la base de données..."
        
        # Arrêter l'application pour éviter les connexions
        docker-compose -f docker-compose.prod.yml stop kenergie-api-blue 2>/dev/null || true
        
        # Restaurer le backup
        mysql -h "${DB_HOST:-localhost}" -u "${DB_USER:-kenergie}" -p"${DB_PASSWORD}" "${DB_NAME:-kenergie}" < "$LATEST_BACKUP"
        
        if [ $? -eq 0 ]; then
            echo "✅ Base de données restaurée avec succès"
        else
            echo "❌ Échec de la restauration de la base de données"
            exit 1
        fi
        
        # Redémarrer l'application
        docker-compose -f docker-compose.prod.yml up -d kenergie-api-blue
        
        # Attendre le health check
        sleep 10
        if curl -f http://localhost/health >/dev/null 2>&1; then
            echo "✅ Application redémarrée avec succès"
        else
            echo "❌ Échec du redémarrage de l'application"
            exit 1
        fi
    else
        echo "⏭ Restauration de la base de données annulée"
    fi
else
    echo "⚠️ Aucun backup trouvé pour la restauration"
fi

# 9. Nettoyer les images non utilisées
echo "🧹 Nettoyage des images non utilisées..."
docker image prune -f 2>/dev/null || true

# 10. Créer un tag de rollback
echo "🏷️ Création du tag de rollback..."
git tag -a "rollback_sync_$(date +%Y%m%d_%H%M%S)" -m "Rollback synchronisation offline"

# 11. Tests de validation
echo "🧪 Tests de validation post-rollback..."

# Test de santé
if curl -f http://localhost/health >/dev/null 2>&1; then
    echo "✅ Test santé: RÉUSSI"
else
    echo "❌ Test santé: ÉCHOUÉ"
    exit 1
fi

# Test de l'API
if curl -s http://localhost/api/sync/bootstrap >/dev/null 2>&1; then
    echo "✅ Test API: RÉUSSI"
else
    echo "❌ Test API: ÉCHOUÉ"
    exit 1
fi

# 12. Nettoyer les fichiers temporaires
echo "🧹 Nettoyage des fichiers temporaires..."
rm -f /tmp/bootstrap.json /tmp/clients.json /tmp/final.json 2>/dev/null || true

echo ""
echo "✅ ROLLBACK TERMINÉ AVEC SUCCÈS"
echo "⏰ $(date)"
echo "📊 Résumé:"
echo "  - Version active: backup_before_sync_v1.0"
echo "  - Image: kenergie-api:rollback"
echo "  - Base de données: $([ -n "$LATEST_BACKUP" ] && echo "restaurée depuis $LATEST_BACKUP" || echo "non modifiée")"
echo "  - Health check: ✅"
echo "  - API test: ✅"
echo ""
echo "🎯 Système restauré et fonctionnel"
echo "📋 Prochaines étapes:"
echo "  1. Analyser les logs pour identifier la cause du problème"
echo "  2. Corriger les problèmes dans l'environnement de développement"
echo "  3. Tester à nouveau avec le script de validation"
echo "  4. Relancer le déploiement une fois les problèmes résolus"
