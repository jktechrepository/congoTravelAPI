using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using CongoTravel.Models;
using CongoTravel.Models.DTOs;
using CongoTravel.Services.Repositories;
using FastReport;
using FastReport.Export.Html;
using FastReport.Export.PdfSimple;
using FastReport.Table;

namespace CongoTravel.Services
{
    public class BilletReportService : IBilletReportService
    {
        private const string ReportRelativePath = "Reports/Billet_A4.frx";
        private const string DataSourceName = "Billet";

        private readonly IBilletRepository _billetRepository;
        private readonly IConfigSocieteRepository _configSocieteRepository;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<BilletReportService> _logger;

        public BilletReportService(
            IBilletRepository billetRepository,
            IConfigSocieteRepository configSocieteRepository,
            IWebHostEnvironment environment,
            ILogger<BilletReportService> logger)
        {
            _billetRepository = billetRepository;
            _configSocieteRepository = configSocieteRepository;
            _environment = environment;
            _logger = logger;
        }

        public Task<BilletPdfGenerationOutcome> GeneratePdfAsync(int idBillet, CancellationToken cancellationToken = default)
            => GenerateExportAsync(
                idBillet,
                RenderPdf,
                $"billet_d_avion_a4-{idBillet}.pdf",
                "application/pdf",
                cancellationToken);

        public Task<BilletPdfGenerationOutcome> GenerateHtmlPreviewAsync(int idBillet, CancellationToken cancellationToken = default)
            => GenerateExportAsync(
                idBillet,
                RenderHtml,
                $"billet_d_avion_a4-{idBillet}.html",
                "text/html; charset=utf-8",
                cancellationToken);

        private async Task<BilletPdfGenerationOutcome> GenerateExportAsync(
            int idBillet,
            Func<BilletReportModel, byte[]> render,
            string fileName,
            string contentType,
            CancellationToken cancellationToken)
        {
            var billet = await _billetRepository.GetByIdAsync(idBillet);
            if (billet == null)
                return BilletPdfGenerationOutcome.NotFound(idBillet);

            if (!IsAerialVehicle(billet))
            {
                return BilletPdfGenerationOutcome.NotAerial(
                    "Le billet A4 est réservé aux billets de type véhicule aérien.");
            }

            var config = await _configSocieteRepository.GetBySocieteAsync(billet.IdSociete, cancellationToken);
            var model = MapToReportModel(billet, config);
            var bytes = await Task.Run(() => render(model), cancellationToken);

            return BilletPdfGenerationOutcome.Success(new BilletPdfResult
            {
                Content = bytes,
                FileName = fileName,
                ContentType = contentType
            });
        }

        public static bool IsAerialVehicle(Billet billet)
        {
            var libelle = billet.Reservation?.Voyage?.Vehicule?.TypeVehicule?.Libelle;
            if (string.IsNullOrWhiteSpace(libelle))
                return false;

            var normalized = RemoveDiacritics(libelle).ToLowerInvariant();
            return normalized.Contains("aerien");
        }

        private static string RemoveDiacritics(string text)
        {
            var normalized = text.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder(normalized.Length);
            foreach (var c in normalized)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                    sb.Append(c);
            }
            return sb.ToString().Normalize(NormalizationForm.FormC);
        }

        private static BilletReportModel MapToReportModel(Billet billet, ConfigSociete? config)
        {
            var reservation = billet.Reservation;
            var voyage = reservation?.Voyage;
            var client = reservation?.Client;
            var passenger = billet.ReservationPassenger;
            var societe = billet.Societe?.Nom ?? "CongoTravel";

            var nomClient = client?.NomClient?.Trim() ?? string.Empty;
            var phone = passenger?.Telephone ?? client?.Telephone ?? string.Empty;
            var nomPassager = passenger?.NomComplet?.Trim()
                ?? (!string.IsNullOrWhiteSpace(nomClient) ? nomClient : string.Empty);
            var emailPassager = passenger?.Email ?? string.Empty;

            var siegeCode = !string.IsNullOrWhiteSpace(billet.CodeSiege)
                ? billet.CodeSiege!
                : (billet.Siege?.CodeSiege ?? string.Empty);

            var classeSiege = billet.Siege?.CategorieSiege?.Libelle
                ?? billet.Siege?.CategorieSiege?.CodeCategorieSiege
                ?? string.Empty;

            var kilos = config?.PoidsBagageParKiloOffert ?? 0m;
            var kilosBagage = kilos > 0
                ? $"{kilos.ToString("0.##", CultureInfo.InvariantCulture)} kg"
                : string.Empty;

            var dateVoyage = voyage?.DateDepart.ToString("dd/MM/yyyy") ?? string.Empty;
            var heureDepart = voyage != null
                ? voyage.HeureDepart.ToString(@"hh\:mm")
                : string.Empty;

            return new BilletReportModel
            {
                IdBillet = billet.IdBillet,
                NomClient = nomClient,
                CodeReservation = billet.IdReservation?.ToString() ?? string.Empty,
                Site = billet.Site?.NomSite ?? string.Empty,
                DetailsMessage = $"Veuillez vérifier les détails de votre voyage {societe} ci-dessous.",
                PhoneNumber = phone,
                NomPassager = nomPassager,
                EmailPassager = emailPassager,
                Siege = siegeCode,
                ReferenceBillet = billet.IdReservationPassenger?.ToString() ?? string.Empty,
                DateVoyage = dateVoyage,
                Avion = voyage?.Vehicule?.AliasVehicule ?? string.Empty,
                Provenance = voyage?.Destination?.VilleDepart ?? string.Empty,
                HeureDepart = heureDepart,
                Destination = voyage?.Destination?.VilleArrivee ?? string.Empty,
                HeureArrive = string.Empty,
                Cabin = string.Empty,
                ClasseSiege = classeSiege,
                KilosBagage = kilosBagage,
                NomSociete = societe
            };
        }

        private byte[] RenderPdf(BilletReportModel model)
        {
            using var report = PrepareReport(model);
            using var stream = new MemoryStream();
            using var export = new PDFSimpleExport();
            export.Export(report, stream);
            return stream.ToArray();
        }

        private byte[] RenderHtml(BilletReportModel model)
        {
            using var report = PrepareReport(model);
            using var stream = new MemoryStream();
            using var export = new HTMLExport
            {
                EmbedPictures = true,
                SinglePage = true,
                SubFolder = false,
                Navigator = false
            };
            export.Export(report, stream);

            var html = Encoding.UTF8.GetString(stream.ToArray());
            html = SanitizePreviewHtml(html, model.NomSociete);
            return Encoding.UTF8.GetBytes(html);
        }

        /// <summary>
        /// Retire toute mention visible du moteur de rapport dans la prévisualisation HTML.
        /// </summary>
        private static string SanitizePreviewHtml(string html, string? societeNom)
        {
            if (string.IsNullOrEmpty(html))
                return html;

            // Meta generator / auteur éventuels
            html = Regex.Replace(
                html,
                @"<meta\s+[^>]*(name|content)\s*=\s*[""'][^""']*fast[\s\-]?report[^""']*[""'][^>]*/?>",
                string.Empty,
                RegexOptions.IgnoreCase);

            html = Regex.Replace(
                html,
                @"<meta\s+[^>]*generator[^>]*/?>",
                string.Empty,
                RegexOptions.IgnoreCase);

            // Titre de page neutre
            var pageTitle = string.IsNullOrWhiteSpace(societeNom)
                ? "Billet"
                : $"Billet — {societeNom.Trim()}";
            var encodedTitle = System.Net.WebUtility.HtmlEncode(pageTitle);
            if (Regex.IsMatch(html, @"<title\b[^>]*>.*?</title>", RegexOptions.IgnoreCase | RegexOptions.Singleline))
            {
                html = Regex.Replace(
                    html,
                    @"<title\b[^>]*>.*?</title>",
                    $"<title>{encodedTitle}</title>",
                    RegexOptions.IgnoreCase | RegexOptions.Singleline);
            }
            else if (Regex.IsMatch(html, @"<head\b[^>]*>", RegexOptions.IgnoreCase))
            {
                html = Regex.Replace(
                    html,
                    @"(<head\b[^>]*>)",
                    $"$1<title>{encodedTitle}</title>",
                    RegexOptions.IgnoreCase);
            }

            // Commentaires HTML contenant le nom du moteur
            html = Regex.Replace(
                html,
                @"<!--.*?fast[\s\-]?report.*?-->",
                string.Empty,
                RegexOptions.IgnoreCase | RegexOptions.Singleline);

            return html;
        }

        private Report PrepareReport(BilletReportModel model)
        {
            var reportPath = ResolveReportPath();
            if (!File.Exists(reportPath))
            {
                _logger.LogError("Template FastReport introuvable : {ReportPath}", reportPath);
                throw new FileNotFoundException($"Template de billet introuvable : {reportPath}", reportPath);
            }

            var report = new Report();
            report.Load(reportPath);

            var data = new List<BilletReportModel> { model };
            report.RegisterData(data, DataSourceName);
            var dataSource = report.GetDataSource(DataSourceName);
            if (dataSource != null)
                dataSource.Enabled = true;

            if (report.FindObject("Data1") is DataBand dataBand && dataSource != null)
                dataBand.DataSource = dataSource;

            ApplyObjectBindings(report, model);
            report.Prepare();
            return report;
        }

        private string ResolveReportPath()
        {
            var contentPath = Path.Combine(_environment.ContentRootPath, ReportRelativePath);
            if (File.Exists(contentPath))
                return contentPath;

            return Path.Combine(AppContext.BaseDirectory, ReportRelativePath);
        }

        private static void ApplyObjectBindings(Report report, BilletReportModel model)
        {
            SetTextWithLabelAfter(report, "NomClient", model.NomClient);
            SetTextWithLabelBefore(report, "code_reservation", model.CodeReservation);
            SetTextWithLabelBefore(report, "site", model.Site);
            SetTextWithLabelBefore(report, "phone_number", model.PhoneNumber);
            SetText(report, "Text1", model.DetailsMessage);

            SetCell(report, "nom_passager", model.NomPassager);
            SetCell(report, "email_passager", model.EmailPassager);
            SetCell(report, "siege", model.Siege);
            SetCell(report, "reference_billet", model.ReferenceBillet);

            SetCell(report, "date_voyage", model.DateVoyage);
            SetCell(report, "avion", model.Avion);
            SetCell(report, "provenance", model.Provenance);
            SetCell(report, "heure_depart", model.HeureDepart);
            SetCell(report, "destination", model.Destination);
            SetCell(report, "heure_arrive", model.HeureArrive);
            SetCell(report, "cabin", model.Cabin);
            SetCell(report, "classe_siege", model.ClasseSiege);
            SetCell(report, "kilos_bagage", model.KilosBagage);
        }

        private static void SetTextWithLabelAfter(Report report, string name, string value)
        {
            if (report.FindObject(name) is not TextObject text)
                return;

            var label = text.Text ?? string.Empty;
            text.Text = string.IsNullOrWhiteSpace(value)
                ? label
                : $"{value.Trim()}{label}";
        }

        private static void SetTextWithLabelBefore(Report report, string name, string value)
        {
            if (report.FindObject(name) is not TextObject text)
                return;

            var label = text.Text ?? string.Empty;
            if (string.IsNullOrWhiteSpace(value))
            {
                text.Text = label;
                return;
            }

            var trimmedValue = value.Trim();
            text.Text = label.EndsWith(" ", StringComparison.Ordinal)
                ? $"{label}{trimmedValue}"
                : $"{label.TrimEnd()} {trimmedValue}";
        }

        private static void SetText(Report report, string name, string value)
        {
            if (report.FindObject(name) is TextObject text)
                text.Text = value;
        }

        private static void SetCell(Report report, string name, string value)
        {
            if (report.FindObject(name) is TableCell cell)
                cell.Text = value;
        }
    }
}
