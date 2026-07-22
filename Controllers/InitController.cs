using CongoTravel.Data;
using CongoTravel.Models;
using CongoTravel.Services.Repositories;
using CongoTravel.Services.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using BCrypt.Net;
using CongoTravel.Attributes;

namespace CongoTravel.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class InitController : ControllerBase
    {
        private const string DefaultInitializedSiteCode = "DEFAUT";

        private readonly CongoTravelDbContext _context;

        public InitController(CongoTravelDbContext context)
        {
            _context = context;
        }

        [HttpPost("initialize")]
        [Permission("Permission.Assign")]
        public async Task<IActionResult> InitializeDefaultData()
        {
            try
            {
                var currentDate = DateTime.Now;

                // 1. Créer ou récupérer les rôles
                var superAdminRole = await CreateOrGetSuperAdminRoleAsync(currentDate);
                var adminRole = await CreateOrGetAdminRoleAsync(currentDate);
                var gerantRole = await CreateOrGetGerantRoleAsync(currentDate);

                // 2. Créer ou récupérer la société par défaut
                var defaultSociete = await CreateOrGetDefaultSocieteAsync(currentDate);

                // 2b. Site par défaut (idempotent, même logique que CongoTravelDbContext.InitializeDefaultDataAsync)
                var defaultSite = await CreateOrGetDefaultSiteAsync(defaultSociete, currentDate);

                // 3. Créer l'Agent Manager Général + Utilisateur Super-Admin
                var superAdminUser = await CreateOrGetSuperAdminWithAgentAsync(superAdminRole, defaultSociete, currentDate);

                // 4. Créer l'Agent Admin + Utilisateur Admin
                var adminUser = await CreateOrGetAdminWithAgentAsync(adminRole, defaultSociete, currentDate);

                // 5. Créer l'Agent Gerant + Utilisateur Gerant
                var gerantUser = await CreateOrGetGerantWithAgentAsync(gerantRole, defaultSociete, defaultSite, currentDate);

                // 4. Initialiser les permissions (si elles n'existent pas encore)
                var permissionsCount = await _context.Permissions.CountAsync();
                var rolePermissionsCount = await _context.RolePermissions.CountAsync();
                
                // Vérifier si les permissions sont assignées au Super-Admin
                var superAdminRolePermissionsCount = await _context.RolePermissions
                    .Where(rp => rp.IdRole == superAdminRole.IdRole)
                    .CountAsync();
                
                if (permissionsCount == 0 || rolePermissionsCount == 0 || superAdminRolePermissionsCount == 0)
                {
                    // Les permissions n'existent pas OU ne sont pas assignées aux rôles
                    // SeedPermissionsAsync vérifie et assigne automatiquement
                    await PermissionSeeder.SeedPermissionsAsync(_context);
                }

                return Ok(new
                {
                    success = true,
                    message = "Données initialisées avec succès",
                    data = new
                    {
                        roles = new[]
                        {
                            new { id = superAdminRole.IdRole, nom = superAdminRole.Nom },
                            new { id = adminRole.IdRole, nom = adminRole.Nom }
                        },
                        societe = new { id = defaultSociete.IdSociete, nom = defaultSociete.Nom },
                        siteParDefaut = new
                        {
                            id = defaultSite.IdSite,
                            code = defaultSite.CodeSite,
                            nom = defaultSite.NomSite,
                            idSociete = defaultSite.IdSociete
                        },
                        utilisateurs = new[]
                        {
                            new
                            {
                                id = superAdminUser.IdUtilisateur,
                                email = superAdminUser.Email,
                                username = superAdminUser.DefaultUsername,
                                telephone = superAdminUser.Telephone,
                                idSite = superAdminUser.IdSite,
                                role = "Super-Admin",
                                motDePasseParDefaut = "Super-Admin"
                            },
                            new
                            {
                                id = adminUser.IdUtilisateur,
                                email = adminUser.Email,
                                username = adminUser.DefaultUsername,
                                telephone = adminUser.Telephone,
                                idSite = adminUser.IdSite,
                                role = "Admin",
                                motDePasseParDefaut = "Admin"
                            },
                            new
                            {
                                id = gerantUser.IdUtilisateur,
                                email = gerantUser.Email,
                                username = gerantUser.DefaultUsername,
                                telephone = gerantUser.Telephone,
                                idSite = gerantUser.IdSite,
                                role = "Gerant",
                                motDePasseParDefaut = "Gerant"
                            }
                        },
                        permissions = new
                        {
                            initialisees = permissionsCount == 0,
                            nombre = await _context.Permissions.CountAsync()
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message, stackTrace = ex.StackTrace });
            }
        }

        [HttpPost("fix-permissions")]
        [Permission("Permission.Assign")]
        public async Task<IActionResult> FixPermissions()
        {
            try
            {
                var superAdminRole = await _context.Roles.FirstOrDefaultAsync(r => r.Nom == "Super-Admin");
                if (superAdminRole == null)
                {
                    return BadRequest(new { success = false, message = "Rôle Super-Admin non trouvé" });
                }

                // Vérifier l'état actuel
                var permissionsCount = await _context.Permissions.CountAsync();
                var superAdminPermissionsCount = await _context.RolePermissions
                    .Where(rp => rp.IdRole == superAdminRole.IdRole)
                    .CountAsync();

                // Forcer l'assignation des permissions
                await PermissionSeeder.SeedPermissionsAsync(_context);

                // Vérifier l'état après correction
                var superAdminPermissionsCountAfter = await _context.RolePermissions
                    .Where(rp => rp.IdRole == superAdminRole.IdRole)
                    .CountAsync();

                return Ok(new
                {
                    success = true,
                    message = "Permissions corrigées avec succès",
                    data = new
                    {
                        avant = new
                        {
                            permissions_total = permissionsCount,
                            super_admin_permissions = superAdminPermissionsCount
                        },
                        apres = new
                        {
                            permissions_total = await _context.Permissions.CountAsync(),
                            super_admin_permissions = superAdminPermissionsCountAfter,
                            permissions_ajoutees = superAdminPermissionsCountAfter - superAdminPermissionsCount
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message, stackTrace = ex.StackTrace });
            }
        }

        private async Task<Role> CreateOrGetSuperAdminRoleAsync(DateTime currentDate)
        {
            var existingRole = await _context.Roles.FirstOrDefaultAsync(r => r.Nom == "Super-Admin");

            if (existingRole != null)
            {
                return existingRole;
            }

            var newRole = new Role
            {
                Nom = "Super-Admin",
                DateCreation = currentDate
            };

            _context.Roles.Add(newRole);
            await _context.SaveChangesAsync();

            return newRole;
        }

        private async Task<Role> CreateOrGetAdminRoleAsync(DateTime currentDate)
        {
            var existingRole = await _context.Roles.FirstOrDefaultAsync(r => r.Nom == "Admin");

            if (existingRole != null)
            {
                return existingRole;
            }

            var newRole = new Role
            {
                Nom = "Admin",
                Niveau = 2,
                DateCreation = currentDate
            };

            _context.Roles.Add(newRole);
            await _context.SaveChangesAsync();

            return newRole;
        }

        private async Task<Role> CreateOrGetGerantRoleAsync(DateTime currentDate)
        {
            var existingRole = await _context.Roles.FirstOrDefaultAsync(r => r.Nom == "Gerant");

            if (existingRole != null)
            {
                return existingRole;
            }

            var newRole = new Role
            {
                Nom = "Gerant",
                Niveau = 3,
                DateCreation = currentDate
            };

            _context.Roles.Add(newRole);
            await _context.SaveChangesAsync();

            return newRole;
        }

        private async Task<Societe> CreateOrGetDefaultSocieteAsync(DateTime currentDate)
        {
            var existingSociete = await _context.Societes.FirstOrDefaultAsync(e => e.Nom == "CongoTravel");

            if (existingSociete != null)
            {
                return existingSociete;
            }

            var newSociete = new Societe
            {
                Nom = "CongoTravel",
                Devise = "Excellence et Innovation",
                Type = "Privée",
                Description = "Société d'excellence offrant des services de qualité énergétique",
                Telephone = "+243999999999",
                EmailContact = "contact@congotravel.cd",
                SiteWeb = "https://www.congotravel.cd",
                NomCompletResponsable = "Administrateur Super Admin",
                GenreResponsable = "Masculin",
                Statut = true,
                DateCreation = currentDate
            };

            _context.Societes.Add(newSociete);
            await _context.SaveChangesAsync();

            return newSociete;
        }

        private async Task<Site> CreateOrGetDefaultSiteAsync(Societe defaultSociete, DateTime currentDate)
        {
            var existingSite = await _context.Sites.FirstOrDefaultAsync(s =>
                s.IdSociete == defaultSociete.IdSociete &&
                s.CodeSite == DefaultInitializedSiteCode);

            if (existingSite != null)
            {
                return existingSite;
            }

            var newSite = new Site
            {
                IdSociete = defaultSociete.IdSociete,
                CodeSite = DefaultInitializedSiteCode,
                NomSite = "Site principal CongoTravel",
                Ville = null,
                Adresse = null,
                Telephone = defaultSociete.Telephone,
                NomResponsableSite = string.IsNullOrWhiteSpace(defaultSociete.NomCompletResponsable)
                    ? "Responsable Site"
                    : defaultSociete.NomCompletResponsable.Trim(),
                Email = string.IsNullOrWhiteSpace(defaultSociete.EmailContact) ? null : defaultSociete.EmailContact.Trim(),
                Genre = string.IsNullOrWhiteSpace(defaultSociete.GenreResponsable) ? "Masculin" : defaultSociete.GenreResponsable.Trim(),
                Statut = true,
                IsSitePrincipal = true,
                DateCreation = currentDate
            };

            _context.Sites.Add(newSite);
            await _context.SaveChangesAsync();

            return newSite;
        }

        private async Task<Utilisateur> CreateOrGetSuperAdminWithAgentAsync(Role superAdminRole, Societe defaultSociete, DateTime currentDate)
        {
            var existingUser = await _context.Utilisateurs
                .FirstOrDefaultAsync(u => u.IdRole == superAdminRole.IdRole && u.IdSociete == defaultSociete.IdSociete);

            if (existingUser != null)
            {
                // Vérifier si l'association UserRole existe, sinon la créer
                var existingUserRole = await _context.UserRoles
                    .FirstOrDefaultAsync(ur => ur.IdUtilisateur == existingUser.IdUtilisateur && ur.IdRole == superAdminRole.IdRole);
                
                if (existingUserRole == null)
                {
                    var userRole = new UserRole
                    {
                        IdUtilisateur = existingUser.IdUtilisateur,
                        IdRole = superAdminRole.IdRole,
                        IsPrimary = true, // Rôle principal
                        DateAttribution = currentDate,
                        Statut = true // Statut actif
                    };
                    
                    _context.UserRoles.Add(userRole);
                    await _context.SaveChangesAsync();
                }
                
                return existingUser;
            }

            // 1. Créer l'Agent Manager Général
            var managerAgent = await CreateOrGetManagerGeneralAgentAsync(defaultSociete, currentDate);

            // 2. Générer le hash du mot de passe par défaut
            string motDePasseParDefaut = "Super-Admin";
            string motDePasseHash = BCrypt.Net.BCrypt.HashPassword(motDePasseParDefaut, BCrypt.Net.BCrypt.GenerateSalt(11));

            // 3. Créer l'Utilisateur Super-Admin lié à cet Agent
            var newUser = new Utilisateur
            {
                IdAgent = managerAgent.IdAgent,
                ReferenceUtilisateur = Guid.NewGuid(),
                NomComplet = managerAgent.NomComplet,
                Email = "superadmin@congotravel.cd",
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

            _context.Utilisateurs.Add(newUser);
            await _context.SaveChangesAsync();

            // 4. Créer l'association UserRole (Multi-rôles)
            var newUserRoleCheck = await _context.UserRoles
                .FirstOrDefaultAsync(ur => ur.IdUtilisateur == newUser.IdUtilisateur && ur.IdRole == superAdminRole.IdRole);
            
            if (newUserRoleCheck == null)
            {
                var userRole = new UserRole
                {
                    IdUtilisateur = newUser.IdUtilisateur,
                    IdRole = superAdminRole.IdRole,
                    IsPrimary = true, // Rôle principal
                    DateAttribution = currentDate,
                    Statut = true // Statut actif
                };
                
                _context.UserRoles.Add(userRole);
                await _context.SaveChangesAsync();
            }

            return newUser;
        }

        private async Task<Utilisateur> CreateOrGetAdminWithAgentAsync(Role adminRole, Societe defaultSociete, DateTime currentDate)
        {
            var existingUser = await _context.Utilisateurs
                .FirstOrDefaultAsync(u => u.IdRole == adminRole.IdRole && u.IdSociete == defaultSociete.IdSociete && u.Email == "admin@congotravel.cd");

            if (existingUser != null)
            {
                // Vérifier si l'association UserRole existe, sinon la créer
                var existingUserRole = await _context.UserRoles
                    .FirstOrDefaultAsync(ur => ur.IdUtilisateur == existingUser.IdUtilisateur && ur.IdRole == adminRole.IdRole);
                
                if (existingUserRole == null)
                {
                    var userRole = new UserRole
                    {
                        IdUtilisateur = existingUser.IdUtilisateur,
                        IdRole = adminRole.IdRole,
                        IsPrimary = true, // Rôle principal
                        DateAttribution = currentDate,
                        Statut = true // Statut actif
                    };
                    
                    _context.UserRoles.Add(userRole);
                    await _context.SaveChangesAsync();
                }
                
                return existingUser;
            }

            // 1. Créer l'Agent Admin
            var adminAgent = await CreateOrGetAdminAgentAsync(defaultSociete, currentDate);

            // 2. Générer le hash du mot de passe par défaut
            string motDePasseParDefaut = "Admin";
            string motDePasseHash = BCrypt.Net.BCrypt.HashPassword(motDePasseParDefaut, BCrypt.Net.BCrypt.GenerateSalt(11));

            // 3. Créer l'Utilisateur Admin lié à cet Agent
            var newUser = new Utilisateur
            {
                IdAgent = adminAgent.IdAgent,
                ReferenceUtilisateur = Guid.NewGuid(),
                NomComplet = adminAgent.NomComplet,
                Email = "admin@congotravel.cd",
                DefaultUsername = "Admin",
                Telephone = "+243888888888",
                MotDePasseHash = motDePasseHash,
                Genre = adminAgent.Genre,
                DateNaissance = adminAgent.DateNaissance,
                Statut = true,
                IdRole = adminRole.IdRole,
                IdSociete = defaultSociete.IdSociete,
                DateCreation = currentDate,
                IsConnecte = false,
                DoitChangerMotDePasse = true
            };

            _context.Utilisateurs.Add(newUser);
            await _context.SaveChangesAsync();

            // 4. Créer l'association UserRole (Multi-rôles)
            var newUserRoleCheck = await _context.UserRoles
                .FirstOrDefaultAsync(ur => ur.IdUtilisateur == newUser.IdUtilisateur && ur.IdRole == adminRole.IdRole);
            
            if (newUserRoleCheck == null)
            {
                var userRole = new UserRole
                {
                    IdUtilisateur = newUser.IdUtilisateur,
                    IdRole = adminRole.IdRole,
                    IsPrimary = true, // Rôle principal
                    DateAttribution = currentDate,
                    Statut = true // Statut actif
                };
                
                _context.UserRoles.Add(userRole);
                await _context.SaveChangesAsync();
            }

            return newUser;
        }

        private async Task<Agent> CreateOrGetManagerGeneralAgentAsync(Societe defaultSociete, DateTime currentDate)
        {
            var existingManager = await _context.Agents
                .FirstOrDefaultAsync(a => a.IdSociete == defaultSociete.IdSociete && a.Fonction == "Manager Général");

            if (existingManager != null)
            {
                return existingManager;
            }

            var managerAgent = new Agent
            {
                NomComplet = "Administrateur Super Admin",
                Genre = "Masculin",
                DateNaissance = DateTime.Now.AddYears(-40),
                TelephoneAgent = "+243999999999",
                EmailAgent = "superadmin@congotravel.cd",
                Statut = true,
                EtatCivil = "Marié",
                Fonction = "Manager Général",
                RoleAgent = "Super-Administrateur",
                Matricule = await GenerateUniqueMatriculeAgentAsync(),
                IdSociete = defaultSociete.IdSociete,
                DateCreation = currentDate
            };

            _context.Agents.Add(managerAgent);
            await _context.SaveChangesAsync();

            return managerAgent;
        }

        private async Task<string> GenerateUniqueMatriculeAgentAsync()
        {
            string matricule;

            do
            {
                string annee = DateTime.Now.Year.ToString().Substring(2);
                string guid = Guid.NewGuid().ToString("N").Substring(0, 6).ToUpper();
                matricule = $"NAT{annee}-{guid}";

            } while (await _context.Agents.AnyAsync(a => a.Matricule == matricule));

            return matricule;
        }

        private async Task<Agent> CreateOrGetAdminAgentAsync(Societe defaultSociete, DateTime currentDate)
        {
            var existingAdmin = await _context.Agents
                .FirstOrDefaultAsync(a => a.IdSociete == defaultSociete.IdSociete && a.Fonction == "Administrateur");

            if (existingAdmin != null)
            {
                return existingAdmin;
            }

            var adminAgent = new Agent
            {
                NomComplet = "Administrateur CongoTravel",
                Genre = "Masculin",
                DateNaissance = DateTime.Now.AddYears(-35),
                TelephoneAgent = "+243888888888",
                EmailAgent = "admin@congotravel.cd",
                Statut = true,
                EtatCivil = "Marié",
                Fonction = "Administrateur",
                RoleAgent = "Admin",
                Matricule = await GenerateUniqueMatriculeAgentAsync(),
                IdSociete = defaultSociete.IdSociete,
                DateCreation = currentDate
            };

            _context.Agents.Add(adminAgent);
            await _context.SaveChangesAsync();

            return adminAgent;
        }

        // POST: api/Init/test-email
        /// <summary>
        /// Endpoint de test pour vérifier l'envoi d'email
        /// </summary>
        [HttpPost("test-email")]
        [Permission("Permission.Assign")]
        public async Task<ActionResult<object>> TestEmail([FromBody] TestEmailRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Email))
                {
                    return BadRequest(new { success = false, message = "L'email est requis" });
                }

                var emailService = HttpContext.RequestServices.GetRequiredService<IEmailService>();
                
                bool success = await emailService.SendWelcomeEmailAsync(
                    request.Email,
                    request.NomComplet ?? "Test User",
                    request.Username ?? "TestUser",
                    request.Telephone ?? "+243000000000",
                    request.MotDePasse ?? "123456",
                    request.Role ?? "Test",
                    request.NomSociete ?? "CongoTravel",
                    request.Genre ?? "Masculin",
                    request.Fonction ?? "Test",
                    request.Matricule
                );

                if (success)
                {
                    return Ok(new
                    {
                        success = true,
                        message = "Email de test envoyé avec succès",
                        email = request.Email
                    });
                }
                else
                {
                    return StatusCode(500, new
                    {
                        success = false,
                        message = "Échec de l'envoi de l'email. Vérifiez les logs pour plus de détails.",
                        email = request.Email
                    });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Erreur lors du test d'envoi d'email",
                    error = ex.Message
                });
            }
        }

        public class TestEmailRequest
        {
            public string Email { get; set; }
            public string NomComplet { get; set; }
            public string Username { get; set; }
            public string Telephone { get; set; }
            public string MotDePasse { get; set; }
            public string Role { get; set; }
            public string NomSociete { get; set; }
            public string Genre { get; set; }
            public string Fonction { get; set; }
            public string Matricule { get; set; }
        }

        [HttpGet("diagnostic-permissions/{userId}")]
        public async Task<IActionResult> DiagnosticPermissions(int userId)
        {
            try
            {
                var user = await _context.Utilisateurs
                    .Include(u => u.UserRoles)
                        .ThenInclude(ur => ur.Role)
                            .ThenInclude(r => r.RolePermissions)
                                .ThenInclude(rp => rp.Permission)
                    .FirstOrDefaultAsync(u => u.IdUtilisateur == userId);

                if (user == null)
                {
                    return NotFound(new { success = false, message = "Utilisateur non trouvé" });
                }

                var userRoles = user.UserRoles.Where(ur => ur.Statut == true).ToList();
                var allPermissions = new List<object>();

                foreach (var userRole in userRoles)
                {
                    var rolePermissions = userRole.Role?.RolePermissions?.ToList() ?? new List<RolePermission>();
                    foreach (var rp in rolePermissions)
                    {
                        if (rp.Permission?.Statut == true)
                        {
                            allPermissions.Add(new
                            {
                                permissionId = rp.Permission.IdPermission,
                                permissionNom = rp.Permission.Nom,
                                roleId = userRole.Role?.IdRole,
                                roleNom = userRole.Role?.Nom
                            });
                        }
                    }
                }

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        utilisateur = new
                        {
                            id = user.IdUtilisateur,
                            email = user.Email,
                            nomComplet = user.NomComplet
                        },
                        userRoles = userRoles.Select(ur => new
                        {
                            idUserRole = ur.IdUserRole,
                            idRole = ur.Role?.IdRole,
                            roleNom = ur.Role?.Nom,
                            isPrimary = ur.IsPrimary,
                            statut = ur.Statut,
                            rolePermissionsCount = ur.Role?.RolePermissions?.Count ?? 0
                        }),
                        permissions = allPermissions,
                        permissionsCount = allPermissions.Count
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message, stackTrace = ex.StackTrace });
            }
        }

        private async Task<Utilisateur> CreateOrGetGerantWithAgentAsync(Role gerantRole, Societe defaultSociete, Site defaultSite, DateTime currentDate)
        {
            var existingUser = await _context.Utilisateurs
                .FirstOrDefaultAsync(u => u.IdRole == gerantRole.IdRole && u.IdSociete == defaultSociete.IdSociete && u.Email == "gerant@congotravel.cd");

            if (existingUser != null)
            {
                // Vérifier si l'association UserRole existe, sinon la créer
                var existingUserRole = await _context.UserRoles
                    .FirstOrDefaultAsync(ur => ur.IdUtilisateur == existingUser.IdUtilisateur && ur.IdRole == gerantRole.IdRole);
                
                if (existingUserRole == null)
                {
                    var userRole = new UserRole
                    {
                        IdUtilisateur = existingUser.IdUtilisateur,
                        IdRole = gerantRole.IdRole,
                        IsPrimary = true, // Rôle principal
                        DateAttribution = currentDate,
                        Statut = true // Statut actif
                    };
                    
                    _context.UserRoles.Add(userRole);
                    await _context.SaveChangesAsync();
                }

                var patchNeeded = false;
                if (existingUser.IdSite == null && defaultSite.IdSociete == defaultSociete.IdSociete)
                {
                    existingUser.IdSite = defaultSite.IdSite;
                    patchNeeded = true;
                }

                if (existingUser.IdAgent.HasValue)
                {
                    var linkedAgent = await _context.Agents.FirstOrDefaultAsync(a => a.IdAgent == existingUser.IdAgent.Value);
                    if (linkedAgent != null &&
                        linkedAgent.IdSite == null &&
                        linkedAgent.IdSociete == defaultSociete.IdSociete &&
                        defaultSite.IdSociete == defaultSociete.IdSociete)
                    {
                        linkedAgent.IdSite = defaultSite.IdSite;
                        patchNeeded = true;
                    }
                }

                if (patchNeeded)
                {
                    await _context.SaveChangesAsync();
                }
                
                return existingUser;
            }

            // 1. Créer l'Agent Gerant
            var gerantAgent = await CreateOrGetGerantAgentAsync(defaultSociete, defaultSite, currentDate);

            // 2. Générer le hash du mot de passe par défaut
            string motDePasseParDefaut = "Gerant";
            string motDePasseHash = BCrypt.Net.BCrypt.HashPassword(motDePasseParDefaut, BCrypt.Net.BCrypt.GenerateSalt(11));

            // 3. Créer l'Utilisateur Gerant lié à cet Agent
            var newUser = new Utilisateur
            {
                IdAgent = gerantAgent.IdAgent,
                ReferenceUtilisateur = Guid.NewGuid(),
                NomComplet = gerantAgent.NomComplet,
                Email = "gerant@congotravel.cd",
                DefaultUsername = "Gerant",
                Telephone = "+243777777777",
                MotDePasseHash = motDePasseHash,
                Genre = gerantAgent.Genre,
                DateNaissance = gerantAgent.DateNaissance,
                Statut = true,
                IdRole = gerantRole.IdRole,
                IdSociete = defaultSociete.IdSociete,
                IdSite = defaultSite.IdSite,
                DateCreation = currentDate,
                IsConnecte = false,
                DoitChangerMotDePasse = true
            };

            _context.Utilisateurs.Add(newUser);
            await _context.SaveChangesAsync();

            // 4. Créer l'association UserRole (Multi-rôles)
            var newUserRoleCheck = await _context.UserRoles
                .FirstOrDefaultAsync(ur => ur.IdUtilisateur == newUser.IdUtilisateur && ur.IdRole == gerantRole.IdRole);
            
            if (newUserRoleCheck == null)
            {
                var userRole = new UserRole
                {
                    IdUtilisateur = newUser.IdUtilisateur,
                    IdRole = gerantRole.IdRole,
                    IsPrimary = true, // Rôle principal
                    DateAttribution = currentDate,
                    Statut = true // Statut actif
                };
                
                _context.UserRoles.Add(userRole);
                await _context.SaveChangesAsync();
            }

            return newUser;
        }

        private async Task<Agent> CreateOrGetGerantAgentAsync(Societe defaultSociete, Site defaultSite, DateTime currentDate)
        {
            var existingGerant = await _context.Agents
                .FirstOrDefaultAsync(a => a.IdSociete == defaultSociete.IdSociete && a.Fonction == "Gérant");

            if (existingGerant != null)
            {
                if (existingGerant.IdSite == null && defaultSite.IdSociete == defaultSociete.IdSociete)
                {
                    existingGerant.IdSite = defaultSite.IdSite;
                    await _context.SaveChangesAsync();
                }
                return existingGerant;
            }

            var gerantAgent = new Agent
            {
                NomComplet = "Gérant CongoTravel",
                Genre = "Masculin",
                DateNaissance = DateTime.Now.AddYears(-40),
                TelephoneAgent = "+243777777777",
                EmailAgent = "gerant@congotravel.cd",
                Statut = true,
                EtatCivil = "Marié",
                Fonction = "Gérant",
                RoleAgent = "Gerant",
                Matricule = await GenerateUniqueMatriculeAgentAsync(),
                IdSociete = defaultSociete.IdSociete,
                IdSite = defaultSite.IdSite,
                DateCreation = currentDate
            };

            _context.Agents.Add(gerantAgent);
            await _context.SaveChangesAsync();

            return gerantAgent;
        }
    }
}

