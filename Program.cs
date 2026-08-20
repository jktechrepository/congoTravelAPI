using CongoTravel.Data;
using CongoTravel.Models;
using CongoTravel.Middleware;
using CongoTravel.Services;
using CongoTravel.Services.Repositories;
using CongoTravel.Services.Notifications;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using AutoMapper;
using Microsoft.Extensions.DependencyInjection;
using CongoTravel.Models.DTOs.Mapping;
using CongoTravel.Services.Repositories;
using CongoTravel.Services;
using Serilog;
using AspNetCoreRateLimit;
using System.Reflection;
using Amazon.S3;
using Amazon;
using CongoTravel.Helpers;
using CongoTravel.HealthChecks;
using CongoTravel.Configuration;
using CongoTravel.Extensions;
using CongoTravel.Models.DTOs.Client;

// ═══════════════════════════════════════════════════════════════════════════════════
// Assembly pour JWT (compatibilité entre JwtBearer 6.0.25 et JWT 8.3.1)
// ═══════════════════════════════════════════════════════════════════════════════════
AppDomain.CurrentDomain.AssemblyResolve += (sender, args) =>
{
    string assemblyName = new AssemblyName(args.Name).Name;
    if (assemblyName == "System.IdentityModel.Tokens.Jwt")
    {
        // Charger la version 8.3.1 au lieu de 6.10.0.0
        var assembly = Assembly.LoadFrom(
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "System.IdentityModel.Tokens.Jwt.dll")
        );
        return assembly;
    }
    return null;
};

// ═══════════════════════════════════════════════════════════════════════════════════
//  CONFIGURATION SERILOG (Étape 1 : Charger la configuration avant CreateBuilder)
// ═══════════════════════════════════════════════════════════════════════════════════

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

Log.Information(" Démarrage de CongoTravel...");

try
{
    var builder = WebApplication.CreateBuilder(args);
    
    // Désactiver la validation des scopes au démarrage pour éviter les erreurs StopTheHostException
    // Cette validation peut masquer l'exception réelle
    builder.Host.UseDefaultServiceProvider(options =>
    {
        options.ValidateScopes = false; // Désactiver la validation pour le démarrage
        options.ValidateOnBuild = false; // Désactiver la validation à la construction
    });

    //  Configurer Serilog à partir d'appsettings.json
    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .Enrich.WithMachineName()
        .Enrich.WithThreadId()
        .Enrich.WithEnvironmentName());

    Log.Information(" Serilog configuré avec succès");

// Configuration pour écouter sur toutes les interfaces réseau
// builder.WebHost.UseUrls("https://0.0.0.0:7110");

// Add services to the container.
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        // 🎯 ANTI-RÉFÉRENCE CIRCULAIRE: Ignorer les références circulaires
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
        // 🎯 ANTI-RÉFÉRENCE CIRCULAIRE: Écrire les enums comme strings
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
        // Uniformiser TimeSpan en JSON: "HH:mm:ss" (lecture + écriture)
        options.JsonSerializerOptions.Converters.Add(new TimeSpanHmsJsonConverter());
        options.JsonSerializerOptions.Converters.Add(new NullableTimeSpanHmsJsonConverter());
        // DateOnly : "yyyy-MM-dd" (+ ISO datetime en lecture) — évite 400 Swagger sur dateVisite / dateService
        options.JsonSerializerOptions.Converters.Add(new DateOnlyJsonConverter());
        options.JsonSerializerOptions.Converters.Add(new NullableDateOnlyJsonConverter());
        // TimeOnly : "HH:mm:ss" (+ "HH:mm" / ISO en lecture) — évite 400 Swagger sur startTime / endTime planifications
        options.JsonSerializerOptions.Converters.Add(new TimeOnlyJsonConverter());
        options.JsonSerializerOptions.Converters.Add(new NullableTimeOnlyJsonConverter());
    });

// ═══════════════════════════════════════════════════════════════════════════════════
//  PERFORMANCE OPTIMIZATIONS
// ═══════════════════════════════════════════════════════════════════════════════════

//  1. Response Compression (Gzip/Brotli) - Réduit la taille des réponses de 70-90%
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.Providers.Add<Microsoft.AspNetCore.ResponseCompression.BrotliCompressionProvider>();
    options.Providers.Add<Microsoft.AspNetCore.ResponseCompression.GzipCompressionProvider>();
});

builder.Services.Configure<Microsoft.AspNetCore.ResponseCompression.BrotliCompressionProviderOptions>(options =>
{
    options.Level = System.IO.Compression.CompressionLevel.Fastest;
});

builder.Services.Configure<Microsoft.AspNetCore.ResponseCompression.GzipCompressionProviderOptions>(options =>
{
    options.Level = System.IO.Compression.CompressionLevel.Fastest;
});

//  2. In-Memory Cache - Accélère les données statiques/semi-statiques
// Note : MemoryCache est configuré plus bas pour le Rate Limiting (sans SizeLimit)

Log.Information(" Performance optimizations configurées (Compression + Cache)");

// ═══════════════════════════════════════════════════════════════════════════════════
// RATE LIMITING - Protection contre abus et attaques brute-force
// ═══════════════════════════════════════════════════════════════════════════════════

// 1. Configuration du stockage en mémoire pour Rate Limiting
builder.Services.AddMemoryCache();
builder.Services.Configure<ClientRegistrationRateLimitOptions>(
    builder.Configuration.GetSection(ClientRegistrationRateLimitOptions.SectionName));

// 2. Configuration du Rate Limiting par IP
builder.Services.Configure<IpRateLimitOptions>(builder.Configuration.GetSection("IpRateLimiting"));

// 3. Configuration des politiques de Rate Limiting
builder.Services.Configure<IpRateLimitPolicies>(options =>
{
    options.IpRules = new List<IpRateLimitPolicy>();
});

// 4. Enregistrement des services requis
builder.Services.AddInMemoryRateLimiting();
builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();

Log.Information(" Rate Limiting configuré (AspNetCoreRateLimit)");

// Configuration JWT avec authentification Bearer
builder.Services.AddAuthentication("Bearer")
    .AddJwtBearer("Bearer", options =>
    {
        options.RequireHttpsMetadata = false; // Pour le développement
        options.SaveToken = true;
        options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(
                System.Text.Encoding.UTF8.GetBytes(builder.Configuration["Jwt:SecretKey"] ?? "CongoTravel-SecretKey-2025-V1-Ultra-Secure-Key-For-JWT-Token-Generation")
            ),
            ValidateIssuer = false, // Pas de validation d'issuer pour simplifier
            ValidateAudience = false, // Pas de validation d'audience pour simplifier
            ValidateLifetime = true, // Valider l'expiration du token
            ClockSkew = TimeSpan.Zero // Pas de tolérance sur l'expiration
        };

        // JWT via query string pour les connexions SignalR WebSocket
        options.Events = new Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                    context.Token = accessToken;
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "CongoTravelApi",
        Version = "v2",
        Description = "CongoTravel - API sécurisée avec JWT"
    });
    
    // ✅ Configuration pour éviter les conflits de schemaId
    // Utilise le nom complet du type (avec namespace) pour éviter les collisions
    c.CustomSchemaIds(type => type.FullName?.Replace("+", ".") ?? type.Name);
    
    // Configuration de l'authentification JWT dans Swagger
    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Entrez le token JWT (avec ou sans préfixe Bearer) : {votre_token}"
    });
    
    // Configuration alternative pour accepter les tokens sans "Bearer"
    c.AddSecurityDefinition("TokenOnly", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Entrez uniquement le token JWT (sans Bearer) : {votre_token}"
    });
    
    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });
});

builder.Services.AddDbContext<CongoTravelDbContext>(options =>
    options.UseMySql(
        builder.Configuration.GetConnectionString("CongoTravelConnection"),
        new MariaDbServerVersion(new Version(10, 11, 0)),
        mySqlOptions => mySqlOptions.EnableRetryOnFailure(
            maxRetryCount: 3,
            maxRetryDelay: TimeSpan.FromSeconds(5),
            errorNumbersToAdd: null)
        )
    .EnableSensitiveDataLogging(builder.Environment.IsDevelopment())
    .EnableDetailedErrors(builder.Environment.IsDevelopment()));

// Enregistrement du service JWT
builder.Services.AddScoped<ISimpleJwtService, SimpleJwtService>();
builder.Services.AddScoped<IRefreshTokenService, RefreshTokenService>(); //  REFRESH TOKEN : Service de gestion des refresh tokens
builder.Services.Configure<CongoTravel.Models.Options.GoogleAuthOptions>(
    builder.Configuration.GetSection(CongoTravel.Models.Options.GoogleAuthOptions.SectionName));
builder.Services.Configure<CongoTravel.Models.Options.AppleAuthOptions>(
    builder.Configuration.GetSection(CongoTravel.Models.Options.AppleAuthOptions.SectionName));
builder.Services.AddScoped<IGoogleTokenValidator, GoogleTokenValidator>();
builder.Services.AddScoped<IAppleTokenValidator, AppleTokenValidator>();
builder.Services.AddScoped<ExternalAuthAccountService>();
builder.Services.AddScoped<AuthentificationResponseBuilder>();
builder.Services.AddScoped<IGoogleAuthService, GoogleAuthService>();
builder.Services.AddScoped<IAppleAuthService, AppleAuthService>();

// AUDIT TRAIL: Service d'audit pour tracer toutes les modifications
builder.Services.AddScoped<IAuditService, AuditService>();

// CACHE SERVICE: Service de cache in-memory pour données statiques
builder.Services.AddScoped<ICacheService, CacheService>();

// Enregistrement des repositories
builder.Services.AddScoped<ISocieteRepository, SocieteService>();
builder.Services.AddScoped<IConfigSocieteRepository, ConfigSocieteService>();
builder.Services.AddScoped<ICategorieSiegeRepository, CategorieSiegeService>();
builder.Services.AddScoped<ISiteRepository, SiteService>();
builder.Services.AddScoped<IUtilisateurRepository, UtilisateurService>();
builder.Services.AddScoped<IAgentRepository, AgentService>();
builder.Services.AddScoped<IRoleRepository, RoleService>();
// Les fonctionnalités de facturation ne sont plus disponibles après la refactorisation
builder.Services.AddScoped<IClientRepository, ClientService>();
builder.Services.AddScoped<IDestinationRepository, DestinationService>();
builder.Services.AddScoped<ISiegeService, SiegeService>();
builder.Services.AddScoped<ISiegeDisponibiliteService, SiegeDisponibiliteService>();
builder.Services.AddScoped<IVoyageSeatAllocationService, VoyageSeatAllocationService>();
            builder.Services.AddScoped<IVehiculeRepository, VehiculeService>();
            builder.Services.AddScoped<IVehiculePhotoService, VehiculePhotoService>();
builder.Services.AddScoped<ITypeVehiculeRepository, TypeVehiculeService>();
builder.Services.AddScoped<IVoyageTarifService, VoyageTarifService>();
builder.Services.AddScoped<IBilletPricingEnrichmentService, BilletPricingEnrichmentService>();
builder.Services.AddScoped<IVoyageRepository, VoyageService>();
builder.Services.AddScoped<IPlanificationVoyageService, PlanificationVoyageService>();
builder.Services.AddScoped<IVoyageGenerationService, VoyageGenerationService>();
builder.Services.AddScoped<IVoyageReportService, VoyageReportService>();
builder.Services.AddScoped<IVoyageReportNotificationService, VoyageReportNotificationService>();
builder.Services.AddScoped<IFeuilleDeRouteService, FeuilleDeRouteService>();
builder.Services.AddScoped<IReservationRepository, ReservationService>();
builder.Services.AddScoped<IBilletRepository, BilletService>();
builder.Services.AddScoped<IBilletReportService, BilletReportService>();
builder.Services.AddScoped<IPaiementRepository, PaiementService>();

// Services pour le workflow paiement→billet automatique
builder.Services.AddScoped<IQrCodeService, QrCodeService>();
builder.Services.AddScoped<BilletEmissionService>();

// Configuration AutoMapper
builder.Services.AddAutoMapper(cfg => {
    cfg.AddProfile<VehiculeMappingProfile>();
    cfg.AddProfile<WorkflowReservationMappingProfile>();
});

            // Les fonctionnalités de facturation ne sont plus disponibles après la refactorisation
            builder.Services.AddScoped<INotificationPreferenceRepository, NotificationPreferenceService>();
            // Les fonctionnalités de facturation ne sont plus disponibles après la refactorisation
            builder.Services.AddScoped<DashboardService>(); // Service de statistiques du dashboard
            builder.Services.AddScoped<IStatistiquesService, StatistiquesService>();
            // Les fonctionnalités de facturation ne sont plus disponibles après la refactorisation
            // builder.Services.AddScoped<ExcelClientService>(); // Service d'import Excel pour les clients - À implémenter
            // Les fonctionnalités de facturation ne sont plus disponibles après la refactorisation
            builder.Services.AddScoped<ClientExportService>(); // Service d'export Excel pour les clients
            builder.Services.AddScoped<MetricsService>(); // Service de métriques système
// builder.Services.AddScoped<TypeDeCourantDataService>(); // Service d'initialisation des types de courant - À implémenter
builder.Services.AddScoped<ISmsNotificationService, TwilioSmsService>(); // Service SMS Twilio
// Services de communication
builder.Services.AddScoped<IClientFilterService, ClientFilterService>();
builder.Services.AddScoped<ICommunicationCampaignRepository, CommunicationCampaignService>();
builder.Services.AddScoped<ICommunicationDispatchService, CommunicationDispatchService>();
// Services de plaintes clients
builder.Services.AddScoped<IPlainteClientRepository, PlainteClientService>();
builder.Services.AddScoped<IPlainteClientNotificationService, PlainteClientNotificationService>();
// ✅ DEVOIRS À DOMICILE: Services pour la gestion des devoirs à domicile
// Configuration AWS S3
var awsAccessKeyId = builder.Configuration["AWS:S3:AccessKeyId"];
var awsSecretAccessKey = builder.Configuration["AWS:S3:SecretAccessKey"];
var awsRegion = builder.Configuration["AWS:S3:Region"] ?? "us-east-1";

if (!string.IsNullOrEmpty(awsAccessKeyId) && !string.IsNullOrEmpty(awsSecretAccessKey))
{
    // Configuration du client S3 avec credentials explicites
    var s3Config = new AmazonS3Config
    {
        RegionEndpoint = RegionEndpoint.GetBySystemName(awsRegion)
    };
    
    builder.Services.AddSingleton<IAmazonS3>(sp =>
    {
        return new AmazonS3Client(awsAccessKeyId, awsSecretAccessKey, s3Config);
    });
    
    // Utiliser le service S3
    builder.Services.AddScoped<IFileStorageService, S3FileStorageService>();
    Log.Information("✅ Stockage AWS S3 configuré et activé");
}
else
{
    // Fallback vers le stockage local si les credentials AWS ne sont pas configurés
    builder.Services.AddScoped<IFileStorageService, FileStorageService>();
    Log.Warning("⚠️  Credentials AWS S3 non configurés. Utilisation du stockage local.");
}

builder.Services.AddScoped<IAntivirusService, AntivirusService>();
// NOTIFICATIONS AVANCÉES
builder.Services.AddScoped<CongoTravel.Services.Repositories.INotificationService, CongoTravel.Services.NotificationService>();
builder.Services.AddScoped<CongoTravel.Services.Repositories.INotificationRepository, CongoTravel.Services.NotificationService>();
builder.Services.AddScoped<INotificationDispatcher, NotificationDispatcher>();
builder.Services.AddSingleton<INotificationJobQueue, NotificationJobQueue>();
builder.Services.AddScoped<IUserDeviceRepository, UserDeviceRepository>();

var firebaseCredentialsPath = builder.Configuration["Firebase:CredentialsPath"]
    ?? "Rusatravel-27-firebase-adminsdk-fbsvc-24045cb5ba.json";
var firebaseCredentialsFullPath = Path.Combine(Directory.GetCurrentDirectory(), firebaseCredentialsPath);

if (File.Exists(firebaseCredentialsFullPath))
{
    try
    {
        FirebaseNotificationService.InitializeFirebase(firebaseCredentialsFullPath);
        builder.Services.AddScoped<IFirebaseNotificationService, FirebaseNotificationService>();
        Log.Information("Firebase Admin SDK activé ({Path})", firebaseCredentialsFullPath);
    }
    catch (Exception ex)
    {
        builder.Services.AddScoped<IFirebaseNotificationService, NullFirebaseNotificationService>();
        Log.Warning(ex, "Échec init Firebase ({Path}) — push désactivé (NoOp).", firebaseCredentialsFullPath);
    }
}
else
{
    builder.Services.AddScoped<IFirebaseNotificationService, NullFirebaseNotificationService>();
    Log.Warning("Firebase credentials absents ({Path}) — push désactivé (NoOp).", firebaseCredentialsFullPath);
}

builder.Services.AddScoped<INotificationSender, NotificationSender>();
builder.Services.AddHostedService<NotificationJobWorker>();
builder.Services.AddEvenementTicketing();
builder.Services.AddSiteTouristiqueTicketing();
builder.Services.AddRestaurantReservations();
builder.Services.AddScoped<CongoTravel.Services.Repositories.IEmailService, CongoTravelAPI.Services.EmailService>();
builder.Services.AddScoped<IEmailVerificationService, EmailVerificationService>();

try
{
    Log.Information("📦 Enregistrement des services finaux...");
    // builder.Services.AddScoped<IEmailService, EmailService>(); // À implémenter
    builder.Services.AddScoped<ISignalRNotificationService, SignalRNotificationService>();
    builder.Services.AddScoped<IFlexPayRealtimeNotifier, FlexPayRealtimeNotifier>();
    // builder.Services.AddScoped<ISignalRStatistiquesService, SignalRStatistiquesService>(); // À implémenter
    builder.Services.AddScoped<IUsernameGeneratorService, UsernameGeneratorService>(); // Service de génération de noms d'utilisateur
    builder.Services.AddScoped<GerantDashboardService>(); // Service dashboard Gérant
    builder.Services.AddScoped<FinancierDashboardService>(); // Service dashboard Financier
    builder.Services.AddScoped<SuperAdminDashboardService>(); // Service dashboard Super-Admin transport
    builder.Services.AddScoped<CaissierDashboardService>(); // Service dashboard Caissier
    builder.Services.AddScoped<ClientDashboardService>(); // Service dashboard Client
            builder.Services.AddScoped<ReservationWithPaiementService>(); // Service de réservation avec paiement unifié
            builder.Services.AddScoped<ICashReservationWithPaiementService, CashReservationWithPaiementService>();
            builder.Services.AddScoped<IInfoPaiementResolutionService, InfoPaiementResolutionService>();
            builder.Services.AddScoped<IDeviseMontantConverter, DeviseMontantConverter>();
            builder.Services.AddScoped<IFlexPayReservationService, FlexPayReservationService>();
            builder.Services.AddScoped<IReservationWithPaiementReadService, ReservationWithPaiementReadService>();
            builder.Services.AddScoped<IFlexPayService, FlexPayService>();
            builder.Services.AddScoped<IFlexPayCallbackService, FlexPayCallbackService>();
            builder.Services.AddScoped<IFlexPayPayOutCallbackService, FlexPayPayOutCallbackService>();
            builder.Services.AddScoped<IReversementSiteService, ReversementSiteService>();
            builder.Services.AddScoped<IReversementAutomatiqueService, ReversementAutomatiqueService>();
            builder.Services.AddScoped<IReversementMontantResolver, PaiementElectroniqueReversementMontantResolver>();
            builder.Services.AddHttpClient("FlexPay");
            builder.Services.Configure<CongoTravel.Configuration.FlexPayOptions>(
                builder.Configuration.GetSection(CongoTravel.Configuration.FlexPayOptions.SectionName));

builder.Services.AddHealthChecks()
    .AddCheck<DatabaseHealthCheck>("database", tags: new[] { "ready" })
    .AddCheck<FlexPayConfigHealthCheck>("flexpay", tags: new[] { "ready" });

            // Services RBAC avec permissions
            builder.Services.AddHttpContextAccessor(); // Nécessaire pour ICurrentUserService
            builder.Services.AddScoped<IPermissionService, PermissionService>();
            builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
            Log.Information(" Services finaux enregistrés avec succès");
            Log.Information("✅ Services finaux enregistrés avec succès");

            // ═════════════════════════════════════════════════════════════
            // ✨ SYNCHRONISATION OFFLINE: Services de synchronisation
            // ═════════════════════════════════════════════════════════════════
            builder.Services.AddScoped<IWatermarkService, WatermarkService>(); // Watermark sécurisé
            builder.Services.AddScoped<ICursorService, CursorService>(); // Cursor pagination sécurisé
            builder.Services.AddScoped<ISyncService, SyncService>(); // Service principal de sync
            Log.Information("✅ Services de synchronisation enregistrés avec succès");
}
catch (Exception ex)
{
    Log.Fatal(ex, "❌ Erreur lors de l'enregistrement des services finaux");
    throw;
}

try
{
    Log.Information("📡 Configuration SignalR...");
    // SignalR: Ajouter SignalR avec configuration
    builder.Services.AddSignalR(options =>
    {
        options.EnableDetailedErrors = builder.Environment.IsDevelopment();
        options.KeepAliveInterval = TimeSpan.FromSeconds(15); // Ping toutes les 15 secondes
        options.ClientTimeoutInterval = TimeSpan.FromSeconds(30); // Timeout après 30 secondes
        options.HandshakeTimeout = TimeSpan.FromSeconds(15); // Timeout de handshake
    });
    Log.Information("✅ SignalR configuré avec succès");
}
catch (Exception ex)
{
    Log.Fatal(ex, "❌ Erreur lors de la configuration SignalR");
    throw;
}

// Configuration CORS améliorée
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend",
        policy =>
        {
            if (builder.Environment.IsDevelopment())
            {
                // En développement, permettre toutes les origines
                policy.SetIsOriginAllowed(origin => true)
                      .AllowAnyHeader()
                      .AllowAnyMethod()
                      .AllowCredentials();
            }
            else
            {
                // PRODUCTION : Configuration CORS complète et sécurisée
                var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>();
                
                if (allowedOrigins != null && allowedOrigins.Length > 0)
                {
                    policy.WithOrigins(allowedOrigins)
                          .WithHeaders(
                              "Content-Type",
                              "Authorization",
                              "Accept",
                              "Origin",
                              "X-Requested-With",
                              "X-Societe-Id",
                              "Cache-Control",  //  AJOUTÉ pour le web
                              "Pragma",         //  AJOUTÉ pour le web
                              "Expires"         //  AJOUTÉ pour le web
                          )
                          .WithMethods("GET", "POST", "PUT", "DELETE", "PATCH", "OPTIONS")
                          .AllowCredentials()
                          .SetPreflightMaxAge(TimeSpan.FromMinutes(10)); // Cache des réponses preflight
                }
                else
                {
                    // Fallback : Autoriser toutes les origines (moins sécurisé)
                    Log.Warning(" Aucune origine CORS configurée ! Utilisation du mode permissif.");
                    policy.SetIsOriginAllowed(origin => true)
                          .AllowAnyHeader()
                          .AllowAnyMethod()
                          .AllowCredentials();
                }
            }
        });
});

Log.Information("🔧 Construction de l'application...");
WebApplication app;
try
{
    // Tenter de construire l'application
    app = builder.Build();
    Log.Information("✅ Application construite avec succès");
}
catch (Exception buildEx)
{
    // Log détaillé de l'exception
    Log.Fatal(buildEx, "❌ ERREUR CRITIQUE lors de la construction de l'application");
    Log.Fatal("📚 Type d'exception: {ExceptionType}", buildEx.GetType().FullName);
    Log.Fatal("📚 Message: {Message}", buildEx.Message);
    Log.Fatal("📚 Stack trace: {StackTrace}", buildEx.StackTrace);
    
    // Extraire toutes les exceptions internes
    var innerEx = buildEx.InnerException;
    int depth = 0;
    while (innerEx != null && depth < 10)
    {
        Log.Fatal("📚 Exception interne #{Depth}: {Type} - {Message}", depth + 1, innerEx.GetType().FullName, innerEx.Message);
        Log.Fatal("📚 Stack trace interne #{Depth}: {StackTrace}", depth + 1, innerEx.StackTrace);
        innerEx = innerEx.InnerException;
        depth++;
    }
    
    // Si c'est une StopTheHostException, essayer d'extraire plus d'informations
    if (buildEx.GetType().Name.Contains("StopTheHostException"))
    {
        Log.Fatal("⚠️ StopTheHostException détectée - cette exception masque souvent l'exception réelle");
        
        // Essayer d'obtenir l'exception réelle via différentes méthodes
        Exception realException = null;
        
        // Méthode 1: Vérifier InnerException
        if (buildEx.InnerException != null)
        {
            realException = buildEx.InnerException;
            Log.Fatal("💡 Exception interne trouvée: {Type} - {Message}", realException.GetType().FullName, realException.Message);
        }
        
        // Méthode 2: Essayer d'accéder aux propriétés de l'exception
        try
        {
            var exceptionData = buildEx.Data;
            if (exceptionData != null && exceptionData.Count > 0)
            {
                Log.Fatal("📋 Données d'exception: {ExceptionData}", System.Text.Json.JsonSerializer.Serialize(exceptionData));
            }
        }
        catch (Exception dataEx)
        {
            Log.Fatal("❌ Erreur lors de la lecture des données d'exception: {Error}", dataEx.Message);
        }
        
        // Méthode 3: Utiliser la réflexion pour trouver des propriétés supplémentaires
        try
        {
            var properties = buildEx.GetType().GetProperties();
            foreach (var prop in properties)
            {
                if (prop.Name != "Message" && prop.Name != "StackTrace" && prop.Name != "InnerException")
                {
                    try
                    {
                        var value = prop.GetValue(buildEx);
                        if (value != null)
                        {
                            Log.Fatal("🔍 Propriété {PropertyName}: {PropertyValue}", prop.Name, value);
                        }
                    }
                    catch (Exception propEx)
                    {
                        Log.Fatal("❌ Erreur lecture propriété {PropertyName}: {Error}", prop.Name, propEx.Message);
                    }
                }
            }
        }
        catch (Exception reflEx)
        {
            Log.Fatal("❌ Erreur lors de la réflexion: {Error}", reflEx.Message);
        }
        
        // Si on a trouvé une vraie exception, la logger en détail
        if (realException != null)
        {
            Log.Fatal("🎯 EXCEPTION RÉELLE TROUVÉE:");
            Log.Fatal("📚 Type: {ExceptionType}", realException.GetType().FullName);
            Log.Fatal("📚 Message: {Message}", realException.Message);
            Log.Fatal("📚 Stack trace: {StackTrace}", realException.StackTrace);
            
            // Continuer à chercher les exceptions internes
            var innerDepth = 0;
            var currentInner = realException.InnerException;
            while (currentInner != null && innerDepth < 5)
            {
                Log.Fatal("📚 Exception interne #{Depth}: {Type} - {Message}", innerDepth + 1, currentInner.GetType().FullName, currentInner.Message);
                currentInner = currentInner.InnerException;
                innerDepth++;
            }
        }
        else
        {
            Log.Fatal("❌ Impossible d'extraire l'exception réelle de la StopTheHostException");
        }
    }
    
    throw;
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

//  ACTIVATION DE LA COMPRESSION (à mettre TRÈS TÔT dans le pipeline)
app.UseResponseCompression();
Log.Information(" Response Compression activée (Brotli/Gzip)");

//  ACTIVATION DU RATE LIMITING (AVANT l'authentification)
app.UseIpRateLimiting();
Log.Information(" Rate Limiting activé - Protection contre brute-force et abus");

// Swagger disponible dans tous les environnements
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "CongoTravel v1");
    c.RoutePrefix = "swagger";
    c.DocumentTitle = "CongoTravel - Documentation";
});

// Redirection HTTPS seulement en production
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseCors("AllowFrontend");

app.UseRouting();

// Activation de l'authentification et de l'autorisation JWT
// Ajout du middleware pour gérer automatiquement le préfixe "Bearer"
app.UseAutoBearer();

// Ajout du middleware pour tracker les métriques
app.UseMetricsTracking();

app.UseAuthentication(); // DOIT être avant UseAuthorization
app.UseAuthorization();

app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});
app.MapHealthChecks("/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = _ => false
});

app.MapControllers();

// Configuration des hubs SignalR
app.MapHub<CongoTravel.Hubs.NotificationHub>("/hubs/notifications");

// Apply migrations and initialize default data
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<CongoTravelDbContext>();
        var logger = services.GetRequiredService<ILogger<Program>>();
        
        // 1. Migrations automatiques en Development uniquement (Production : dotnet ef database update)
        if (app.Environment.IsDevelopment())
        {
            logger.LogInformation("Application des migrations EF (Development)...");
            await CongoTravel.Helpers.DevelopmentDatabaseMigrationHelper.MigrateSafelyAsync(context, logger);
        }

        // 2. Initialisation des données par défaut (pas de vues nécessaires pour les modèles conservés)

        // 3. Initialiser les données par défaut (Super-Admin, Ekelasi School, etc.)
        logger.LogInformation("Initialisation des données par défaut...");
        await context.InitializeDefaultDataAsync();
        logger.LogInformation("Initialisation des données par défaut terminée avec succès.");
        
        // 4.  NOUVEAU : Initialiser les permissions RBAC
        logger.LogInformation("Initialisation des permissions RBAC...");
        await PermissionSeeder.SeedPermissionsAsync(context);
        logger.LogInformation("Permissions RBAC initialisées avec succès.");
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Une erreur s'est produite lors de l'initialisation de la base de données.");
    }
}

Log.Information("✅ CongoTravelApi démarré et prêt à recevoir des requêtes");
Log.Information("📊 Environnement : {Environment}", app.Environment.EnvironmentName);
Log.Information("🔗 Swagger UI : https://localhost:7110/swagger");

// Initialiser les données de l'application
try
{
    var serviceProvider = app.Services;
    using var scope = serviceProvider.CreateScope();
    // var typeDeCourantDataService = scope.ServiceProvider.GetRequiredService<TypeDeCourantDataService>(); // À implémenter
    
    // await typeDeCourantDataService.InitializeDefaultTypesAsync(); // À implémenter
    // await typeDeCourantDataService.ValidateAndRepairDataAsync(); // À implémenter
    
    Log.Information("✅ Données de l'application initialisées avec succès");
}
catch (Exception ex)
{
    Log.Error(ex, "❌ Erreur lors de l'initialisation des données de l'application");
}

app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "❌ L'application s'est arrêtée de manière inattendue");
}
finally
{
    Log.Information("🛑 Arrêt de CongoTravelApi");
    Log.CloseAndFlush();
}

// Exposer Program pour les tests d'intégration
public partial class Program { }
