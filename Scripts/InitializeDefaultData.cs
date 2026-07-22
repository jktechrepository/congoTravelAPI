using CongoTravel.Data;
using CongoTravel.Models;
using Microsoft.EntityFrameworkCore;
using BCrypt.Net;

namespace CongoTravel.Scripts
{
    public class InitializeDefaultData
    {
        public static async Task RunAsync(string connectionString)
        {
            var optionsBuilder = new DbContextOptionsBuilder<CongoTravelDbContext>();
            optionsBuilder.UseMySql(
                connectionString,
                new MariaDbServerVersion(new Version(10, 11, 0))
            );

            using var context = new CongoTravelDbContext(optionsBuilder.Options);
            
            Console.WriteLine("🔧 Initialisation des données par défaut...\n");

            using (var transaction = await context.Database.BeginTransactionAsync())
            {
                try
                {
                    var currentDate = DateTime.Now;

                    // 1. Créer ou récupérer le rôle Super-Admin
                    var superAdminRole = await CreateOrGetSuperAdminRoleAsync(context, currentDate);

                    // 2. Créer ou récupérer la société par défaut
                    var defaultSociete = await CreateOrGetDefaultSocieteAsync(context, currentDate);

                    // 3. Créer l'Agent Manager Général + Utilisateur Super-Admin
                    var superAdminUser = await CreateOrGetSuperAdminWithAgentAsync(context, superAdminRole, defaultSociete, currentDate);

                    await transaction.CommitAsync();

                    Console.WriteLine("\n╔════════════════════════════════════════════════════════════╗");
                    Console.WriteLine("║     ✅ INITIALISATION DES DONNÉES PAR DÉFAUT TERMINÉE      ║");
                    Console.WriteLine("╚════════════════════════════════════════════════════════════╝");
                    Console.WriteLine($"📋 Rôle Super-Admin: ID {superAdminRole.IdRole}");
                    Console.WriteLine($"🏢 Société par défaut: ID {defaultSociete.IdSociete} - {defaultSociete.Nom}");
                    Console.WriteLine($"👤 Utilisateur Super-Admin: ID {superAdminUser.IdUtilisateur}");
                    Console.WriteLine($"   📧 Email: {superAdminUser.Email}");
                    Console.WriteLine($"   📱 Téléphone: {superAdminUser.Telephone}");
                    Console.WriteLine($"   🔑 Username: {superAdminUser.DefaultUsername}");
                    Console.WriteLine($"   ⚠️  Mot de passe par défaut: Super-Admin");
                    Console.WriteLine($"   🔒 Doit changer le mot de passe: {superAdminUser.DoitChangerMotDePasse}");
                    Console.WriteLine("╚════════════════════════════════════════════════════════════╝\n");
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    Console.WriteLine($"❌ Erreur lors de l'initialisation: {ex.Message}");
                    Console.WriteLine($"📚 Stack trace: {ex.StackTrace}");
                    throw;
                }
            }
        }

        private static async Task<Role> CreateOrGetSuperAdminRoleAsync(CongoTravelDbContext context, DateTime currentDate)
        {
            var existingRole = await context.Roles.FirstOrDefaultAsync(r => r.Nom == "Super-Admin");

            if (existingRole != null)
            {
                Console.WriteLine($"✅ Rôle Super-Admin existe déjà avec l'ID: {existingRole.IdRole}");
                return existingRole;
            }

            var newRole = new Role
            {
                Nom = "Super-Admin",
                DateCreation = currentDate
            };

            context.Roles.Add(newRole);
            await context.SaveChangesAsync();

            Console.WriteLine($"✅ Rôle Super-Admin créé avec l'ID: {newRole.IdRole}");
            return newRole;
        }

        private static async Task<Societe> CreateOrGetDefaultSocieteAsync(CongoTravelDbContext context, DateTime currentDate)
        {
            var existingSociete = await context.Societes.FirstOrDefaultAsync(e => e.Nom == "CongoTravel");

            if (existingSociete != null)
            {
                Console.WriteLine($"✅ Société par défaut existe déjà avec l'ID: {existingSociete.IdSociete}");
                return existingSociete;
            }

            var newSociete = new Societe
            {
                Nom = "CongoTravel",
                Devise = "Excellence et Innovation",
                Type = "Privée",
                Description = "Société d'excellence offrant des services de qualité énergétique",
                Telephone = "+243999999999",
                EmailContact = "contact@kenergie.cd",
                SiteWeb = "https://www.kenergie.cd",
                NomCompletResponsable = "Administrateur Super Admin",
                GenreResponsable = "Masculin",
                Statut = true,
                DateCreation = currentDate
            };

            context.Societes.Add(newSociete);
            await context.SaveChangesAsync();

            Console.WriteLine($"✅ Société par défaut créée avec l'ID: {newSociete.IdSociete}");
            Console.WriteLine($"   Nom: {newSociete.Nom}");
            Console.WriteLine($"   Email: {newSociete.EmailContact}");
            Console.WriteLine($"   Téléphone: {newSociete.Telephone}");
            return newSociete;
        }

        private static async Task<Utilisateur> CreateOrGetSuperAdminWithAgentAsync(
            CongoTravelDbContext context,
            Role superAdminRole,
            Societe defaultSociete,
            DateTime currentDate)
        {
            var existingUser = await context.Utilisateurs
                .FirstOrDefaultAsync(u => u.IdRole == superAdminRole.IdRole && u.IdSociete == defaultSociete.IdSociete);

            if (existingUser != null)
            {
                Console.WriteLine($"✅ Utilisateur Super-Admin existe déjà avec l'ID: {existingUser.IdUtilisateur}");
                return existingUser;
            }

            // 1. Créer l'Agent Manager Général
            var managerAgent = await CreateOrGetManagerGeneralAgentAsync(context, defaultSociete, currentDate);

            // 2. Générer le hash du mot de passe par défaut
            string motDePasseParDefaut = "Super-Admin";
            string motDePasseHash = BCrypt.Net.BCrypt.HashPassword(motDePasseParDefaut, BCrypt.Net.BCrypt.GenerateSalt(11));

            // 3. Créer l'Utilisateur Super-Admin lié à cet Agent
            var newUser = new Utilisateur
            {
                IdAgent = managerAgent.IdAgent,
                ReferenceUtilisateur = Guid.NewGuid(),
                NomComplet = managerAgent.NomComplet,
                Email = "superadmin@kenergie.cd",
                DefaultUsername = "SuperAdmin",
                Telephone = "+243999999999",
                MotDePasseHash = motDePasseHash,
                Genre = managerAgent.Genre,
                DateNaissance = managerAgent.DateNaissance,
                Statut = true,
                IdRole = superAdminRole.IdRole,
                IdSociete = defaultSociete.IdSociete,
                DateCreation = currentDate,
                IsConnecte = false,
                DoitChangerMotDePasse = true
            };

            context.Utilisateurs.Add(newUser);
            await context.SaveChangesAsync();

            Console.WriteLine($"✅ Utilisateur Super-Admin créé avec l'ID: {newUser.IdUtilisateur} (lié à l'Agent {managerAgent.IdAgent})");
            Console.WriteLine($"   Email: {newUser.Email}");
            Console.WriteLine($"   Username: {newUser.DefaultUsername}");
            Console.WriteLine($"   Téléphone: {newUser.Telephone}");
            Console.WriteLine($"   ⚠️  Mot de passe par défaut: {motDePasseParDefaut} (à changer à la première connexion)");
            return newUser;
        }

        private static async Task<Agent> CreateOrGetManagerGeneralAgentAsync(
            CongoTravelDbContext context,
            Societe defaultSociete,
            DateTime currentDate)
        {
            var existingManager = await context.Agents
                .FirstOrDefaultAsync(a => a.IdSociete == defaultSociete.IdSociete && a.Fonction == "Manager Général");

            if (existingManager != null)
            {
                Console.WriteLine($"✅ Agent Manager Général existe déjà avec l'ID: {existingManager.IdAgent}");
                return existingManager;
            }

            var managerAgent = new Agent
            {
                NomComplet = "Administrateur Super Admin",
                Genre = "Masculin",
                DateNaissance = DateTime.Now.AddYears(-40),
                TelephoneAgent = "+243999999999",
                EmailAgent = "superadmin@kenergie.cd",
                Statut = true,
                EtatCivil = "Marié",
                Fonction = "Manager Général",
                RoleAgent = "Super-Administrateur",
                Matricule = await GenerateUniqueMatriculeAgentAsync(context),
                IdSociete = defaultSociete.IdSociete,
                DateCreation = currentDate
            };

            context.Agents.Add(managerAgent);
            await context.SaveChangesAsync();

            Console.WriteLine($"✅ Agent Manager Général créé avec l'ID: {managerAgent.IdAgent} - Matricule: {managerAgent.Matricule}");
            return managerAgent;
        }

        private static async Task<string> GenerateUniqueMatriculeAgentAsync(CongoTravelDbContext context)
        {
            string matricule;

            do
            {
                string annee = DateTime.Now.Year.ToString().Substring(2);
                string guid = Guid.NewGuid().ToString("N").Substring(0, 6).ToUpper();
                matricule = $"NAT{annee}-{guid}";

            } while (await context.Agents.AnyAsync(a => a.Matricule == matricule));

            return matricule;
        }
    }
}

