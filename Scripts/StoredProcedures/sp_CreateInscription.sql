-- =============================================
-- Procédure stockée pour créer une inscription
-- Gère les 3 cas : Nouveau élève + Nouveau tuteur, Nouveau élève + Ancien tuteur, Ancien élève
-- =============================================
CREATE OR ALTER PROCEDURE [dbo].[sp_CreateInscription]
    @Type NVARCHAR(50),                    -- 'Inscription' ou 'Réinscription'
    @IdEcole INT,
    @IdClasse INT,
    @IdAnneeScolaire INT,
    @DateInscription DATETIME2,
    @StatutInscription NVARCHAR(20) = 'En attente',
    
    -- Données de l'élève
    @NomEleve NVARCHAR(100),
    @PostnomEleve NVARCHAR(100),
    @PrenomEleve NVARCHAR(100),
    @GenreEleve NVARCHAR(10),
    @DateNaissanceEleve DATE,
    @LieuNaissanceEleve NVARCHAR(100),
    @NationaliteEleve NVARCHAR(50),
    @CommentaireEleve NVARCHAR(MAX) = NULL,
    
    -- Données du tuteur
    @NomCompletTuteur NVARCHAR(150),
    @GenreTuteur NVARCHAR(10),
    @EmailTuteur NVARCHAR(100) = NULL,
    @TelephoneTuteur NVARCHAR(20) = NULL,
    @NomCompletRepresentant NVARCHAR(150) = NULL,
    @TelephoneRepresentant NVARCHAR(20) = NULL,
    
    -- Pour les cas de réinscription
    @IdEleveExistant INT = NULL,
    @IdTuteurExistant INT = NULL,
    
    -- Paramètres de sortie
    @IdInscription INT OUTPUT,
    @IdEleve INT OUTPUT,
    @IdTuteur INT OUTPUT,
    @Message NVARCHAR(500) OUTPUT,
    @Success BIT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    
    BEGIN TRY
        BEGIN TRANSACTION;
        
        DECLARE @NewIdTuteur INT = NULL;
        DECLARE @NewIdEleve INT = NULL;
        DECLARE @NewIdInscription INT = NULL;
        DECLARE @TuteurExists BIT = 0;
        DECLARE @EleveExists BIT = 0;
        
        -- Vérifier si c'est une réinscription avec un élève existant
        IF @IdEleveExistant IS NOT NULL
        BEGIN
            SET @EleveExists = 1;
            SET @NewIdEleve = @IdEleveExistant;
            
            -- Mettre à jour le statut de l'élève
            UPDATE Eleves 
            SET Statut = 'True' 
            WHERE IdEleve = @IdEleveExistant;
            
            -- Mettre à jour le statut du tuteur associé
            UPDATE Tuteurs 
            SET Statut = 'True' 
            WHERE IdTuteur = (SELECT IdTuteur FROM Eleves WHERE IdEleve = @IdEleveExistant);
            
            SET @Message = 'Réinscription effectuée avec succès pour un élève existant.';
        END
        ELSE
        BEGIN
            -- Cas d'un nouvel élève
            -- Vérifier si le tuteur existe déjà
            IF @IdTuteurExistant IS NOT NULL
            BEGIN
                SET @TuteurExists = 1;
                SET @NewIdTuteur = @IdTuteurExistant;
                
                -- Mettre à jour le statut du tuteur existant
                UPDATE Tuteurs 
                SET Statut = 'True' 
                WHERE IdTuteur = @IdTuteurExistant;
                
                SET @Message = 'Inscription effectuée avec succès. Tuteur existant réactivé.';
            END
            ELSE
            BEGIN
                -- Vérifier si un tuteur avec les mêmes coordonnées existe déjà
                SELECT @NewIdTuteur = IdTuteur, @TuteurExists = 1
                FROM Tuteurs 
                WHERE NomComplet = @NomCompletTuteur 
                  AND Telephone = @TelephoneTuteur 
                  AND IdEcole = @IdEcole;
                
                IF @TuteurExists = 1
                BEGIN
                    -- Mettre à jour le statut du tuteur existant
                    UPDATE Tuteurs 
                    SET Statut = 'True' 
                    WHERE IdTuteur = @NewIdTuteur;
                    
                    SET @Message = 'Inscription effectuée avec succès. Tuteur existant trouvé et réactivé.';
                END
                ELSE
                BEGIN
                    -- Créer un nouveau tuteur
                    INSERT INTO Tuteurs (
                        NomComplet, Genre, Email, Telephone, 
                        NomCompletRepresentant, TelephoneRepresentant, 
                        IdEcole, Statut, DateCreation
                    )
                    VALUES (
                        @NomCompletTuteur, @GenreTuteur, @EmailTuteur, @TelephoneTuteur,
                        @NomCompletRepresentant, @TelephoneRepresentant,
                        @IdEcole, 'True', GETDATE()
                    );
                    
                    SET @NewIdTuteur = SCOPE_IDENTITY();
                    SET @Message = 'Inscription effectuée avec succès. Nouveau tuteur créé.';
                END
            END
            
            -- Créer le nouvel élève
            INSERT INTO Eleves (
                ReferenceEleve, Nom, Postnom, Prenom, NomComplet,
                Genre, DateNaissance, LieuNaissance, Nationalité,
                Commentaire, IdClasse, IdTuteur, Statut, DateCreation,
                -- Champs d'adresse hérités
                Province, Ville, Commune, Quartier, Avenue, Numero
            )
            VALUES (
                NEWID(), @NomEleve, @PostnomEleve, @PrenomEleve,
                @NomEleve + ' ' + @PostnomEleve + ' ' + @PrenomEleve,
                @GenreEleve, @DateNaissanceEleve, @LieuNaissanceEleve, @NationaliteEleve,
                @CommentaireEleve, @IdClasse, @NewIdTuteur, 'True', GETDATE(),
                -- Valeurs par défaut pour l'adresse (à adapter selon vos besoins)
                'Kinshasa', 'Kinshasa', 'Commune', 'Quartier', 'Avenue', 'Numero'
            );
            
            SET @NewIdEleve = SCOPE_IDENTITY();
        END
        
        -- Créer l'inscription
        INSERT INTO Inscriptions (
            Type, IdEleve, IdEcole, IdClasse, IdAnneeScolaire,
            DateInscription, StatutInscription, DateCreation
        )
        VALUES (
            @Type, @NewIdEleve, @IdEcole, @IdClasse, @IdAnneeScolaire,
            @DateInscription, @StatutInscription, GETDATE()
        );
        
        SET @NewIdInscription = SCOPE_IDENTITY();
        
        -- Assigner les valeurs de sortie
        SET @IdInscription = @NewIdInscription;
        SET @IdEleve = @NewIdEleve;
        SET @IdTuteur = @NewIdTuteur;
        SET @Success = 1;
        
        COMMIT TRANSACTION;
        
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;
            
        SET @Success = 0;
        SET @Message = 'Erreur lors de l''inscription : ' + ERROR_MESSAGE();
        SET @IdInscription = NULL;
        SET @IdEleve = NULL;
        SET @IdTuteur = NULL;
        
        -- Log de l'erreur (optionnel)
        INSERT INTO ErrorLog (ErrorMessage, ErrorDate, ProcedureName)
        VALUES (ERROR_MESSAGE(), GETDATE(), 'sp_CreateInscription');
    END CATCH
END
