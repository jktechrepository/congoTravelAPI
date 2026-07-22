-- Script pour exécuter la procédure stockée d'inscription
-- Exécutez ce script dans votre base de données SQL Server

-- 1. Créer la procédure stockée
-- Exécutez d'abord le contenu du fichier sp_CreateInscription.sql

-- 2. Exemples d'utilisation de la procédure stockée

-- Exemple 1: Nouveau élève avec nouveau tuteur
DECLARE @IdInscription INT, @IdEleve INT, @IdTuteur INT, @Message NVARCHAR(500), @Success BIT;

EXEC sp_CreateInscription
    @Type = 'Inscription',
    @IdEcole = 1,
    @IdClasse = 1,
    @IdAnneeScolaire = 1,
    @DateInscription = '2024-09-01',
    @StatutInscription = 'En attente',
    
    -- Données de l'élève
    @NomEleve = 'KABILA',
    @PostnomEleve = 'Joseph',
    @PrenomEleve = 'Kabila',
    @GenreEleve = 'M',
    @DateNaissanceEleve = '2010-05-15',
    @LieuNaissanceEleve = 'Kinshasa',
    @NationaliteEleve = 'Congolaise',
    @CommentaireEleve = 'Aucun commentaire',
    
    -- Données du tuteur
    @NomCompletTuteur = 'KABILA Laurent-Désiré',
    @GenreTuteur = 'M',
    @EmailTuteur = 'laurent.kabila@email.com',
    @TelephoneTuteur = '+243123456789',
    @NomCompletRepresentant = NULL,
    @TelephoneRepresentant = NULL,
    
    -- Paramètres de sortie
    @IdInscription = @IdInscription OUTPUT,
    @IdEleve = @IdEleve OUTPUT,
    @IdTuteur = @IdTuteur OUTPUT,
    @Message = @Message OUTPUT,
    @Success = @Success OUTPUT;

SELECT @Success as Success, @Message as Message, @IdInscription as IdInscription, @IdEleve as IdEleve, @IdTuteur as IdTuteur;

-- Exemple 2: Nouveau élève avec tuteur existant
DECLARE @IdInscription2 INT, @IdEleve2 INT, @IdTuteur2 INT, @Message2 NVARCHAR(500), @Success2 BIT;

EXEC sp_CreateInscription
    @Type = 'Inscription',
    @IdEcole = 1,
    @IdClasse = 2,
    @IdAnneeScolaire = 1,
    @DateInscription = '2024-09-01',
    @StatutInscription = 'En attente',
    
    -- Données de l'élève
    @NomEleve = 'KABILA',
    @PostnomEleve = 'Josephine',
    @PrenomEleve = 'Kabila',
    @GenreEleve = 'F',
    @DateNaissanceEleve = '2012-08-20',
    @LieuNaissanceEleve = 'Kinshasa',
    @NationaliteEleve = 'Congolaise',
    @CommentaireEleve = 'Sœur du premier élève',
    
    -- Données du tuteur (même tuteur que l'exemple précédent)
    @NomCompletTuteur = 'KABILA Laurent-Désiré',
    @GenreTuteur = 'M',
    @EmailTuteur = 'laurent.kabila@email.com',
    @TelephoneTuteur = '+243123456789',
    @NomCompletRepresentant = NULL,
    @TelephoneRepresentant = NULL,
    
    -- Paramètres de sortie
    @IdInscription = @IdInscription2 OUTPUT,
    @IdEleve = @IdEleve2 OUTPUT,
    @IdTuteur = @IdTuteur2 OUTPUT,
    @Message = @Message2 OUTPUT,
    @Success = @Success2 OUTPUT;

SELECT @Success2 as Success, @Message2 as Message, @IdInscription2 as IdInscription, @IdEleve2 as IdEleve, @IdTuteur2 as IdTuteur;

-- Exemple 3: Réinscription d'un élève existant
DECLARE @IdInscription3 INT, @IdEleve3 INT, @IdTuteur3 INT, @Message3 NVARCHAR(500), @Success3 BIT;

EXEC sp_CreateInscription
    @Type = 'Réinscription',
    @IdEcole = 1,
    @IdClasse = 3,
    @IdAnneeScolaire = 2,
    @DateInscription = '2024-09-01',
    @StatutInscription = 'En attente',
    
    -- Données de l'élève (seront ignorées car réinscription)
    @NomEleve = 'IGNORE',
    @PostnomEleve = 'IGNORE',
    @PrenomEleve = 'IGNORE',
    @GenreEleve = 'M',
    @DateNaissanceEleve = '2010-01-01',
    @LieuNaissanceEleve = 'IGNORE',
    @NationaliteEleve = 'IGNORE',
    @CommentaireEleve = NULL,
    
    -- Données du tuteur (seront ignorées car réinscription)
    @NomCompletTuteur = 'IGNORE',
    @GenreTuteur = 'M',
    @EmailTuteur = NULL,
    @TelephoneTuteur = NULL,
    @NomCompletRepresentant = NULL,
    @TelephoneRepresentant = NULL,
    
    -- Pour les cas de réinscription (utiliser les IDs de l'exemple 1)
    @IdEleveExistant = @IdEleve,  -- Utilise l'ID de l'élève créé dans l'exemple 1
    @IdTuteurExistant = @IdTuteur, -- Utilise l'ID du tuteur créé dans l'exemple 1
    
    -- Paramètres de sortie
    @IdInscription = @IdInscription3 OUTPUT,
    @IdEleve = @IdEleve3 OUTPUT,
    @IdTuteur = @IdTuteur3 OUTPUT,
    @Message = @Message3 OUTPUT,
    @Success = @Success3 OUTPUT;

SELECT @Success3 as Success, @Message3 as Message, @IdInscription3 as IdInscription, @IdEleve3 as IdEleve, @IdTuteur3 as IdTuteur;
