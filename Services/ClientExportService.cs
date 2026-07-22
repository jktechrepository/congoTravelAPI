using CongoTravel.Models;
using CongoTravel.Models.DTOs.Client;
using CongoTravel.Data;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using Serilog;

namespace CongoTravel.Services
{
    /// <summary>
    /// Service pour l'export des données clients
    /// </summary>
    public class ClientExportService
    {
        private readonly CongoTravelDbContext _context;
        private readonly ILogger<ClientExportService> _logger;
        private readonly MetricsService _metricsService;

        public ClientExportService(CongoTravelDbContext context, ILogger<ClientExportService> logger, MetricsService metricsService)
        {
            _context = context;
            _logger = logger;
            _metricsService = metricsService;
        }

       
        /// <summary>
        /// Configure les en-têtes de la feuille Excel
        /// </summary>
        private void SetupHeaders(ExcelWorksheet worksheet)
        {
            worksheet.Cells["A1"].Value = "Nom Client";
            worksheet.Cells["B1"].Value = "Adresse";
            worksheet.Cells["C1"].Value = "Téléphone";
            worksheet.Cells["D1"].Value = "Email";
            worksheet.Cells["E1"].Value = "Genre";
            worksheet.Cells["F1"].Value = "Code Cons";
            worksheet.Cells["G1"].Value = "Actif";
            worksheet.Cells["H1"].Value = "Date Création";
            worksheet.Cells["I1"].Value = "Nom Axe";
            worksheet.Cells["J1"].Value = "Nom Cabine";
            worksheet.Cells["K1"].Value = "Usages";
            worksheet.Cells["L1"].Value = "Nombre Bâtiments";
            worksheet.Cells["M1"].Value = "Catégories Usages";
            worksheet.Cells["N1"].Value = "Nombre Usages";
        }

        /// <summary>
        /// Remplit une ligne avec les données d'un client
        /// </summary>
        private void PopulateRow(ExcelWorksheet worksheet, int row, ClientExportDto client)
        {
            worksheet.Cells[$"A{row}"].Value = client.NomClient;
            worksheet.Cells[$"B{row}"].Value = client.AdresseClient;
            worksheet.Cells[$"C{row}"].Value = client.Telephone;
            worksheet.Cells[$"D{row}"].Value = client.EmailClient;
            worksheet.Cells[$"E{row}"].Value = client.GenreClient;
            worksheet.Cells[$"F{row}"].Value = client.CodeCons;
            worksheet.Cells[$"G{row}"].Value = client.IsActif ? "Oui" : "Non";
            worksheet.Cells[$"H{row}"].Value = client.DateCreation.ToString("dd/MM/yyyy HH:mm");
            worksheet.Cells[$"I{row}"].Value = client.NomAxe;
            worksheet.Cells[$"J{row}"].Value = client.NomCabine;
            worksheet.Cells[$"K{row}"].Value = client.UsagesLibelles;
            worksheet.Cells[$"L{row}"].Value = client.UsagesMontants;
            worksheet.Cells[$"M{row}"].Value = client.UsagesCategories;
            worksheet.Cells[$"N{row}"].Value = client.NombreUsages;
        }

        /// <summary>
        /// Applique le formatage à la feuille Excel
        /// </summary>
        private void FormatWorksheet(ExcelWorksheet worksheet, int lastRow)
        {
            // Style des en-têtes
            using (var range = worksheet.Cells[1, 1, 1, 14])
            {
                range.Style.Font.Bold = true;
                range.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
                range.Style.Border.BorderAround(OfficeOpenXml.Style.ExcelBorderStyle.Thin);
            }

            // Bordures pour toutes les données
            using (var range = worksheet.Cells[1, 1, lastRow, 14])
            {
                range.Style.Border.Top.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
                range.Style.Border.Bottom.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
                range.Style.Border.Left.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
                range.Style.Border.Right.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
            }

            // Auto-fit des colonnes
            for (int col = 1; col <= 14; col++)
            {
                worksheet.Column(col).AutoFit();
            }

            // Formatage des colonnes spécifiques
            worksheet.Column(8).Style.Numberformat.Format = "dd/mm/yyyy hh:mm"; // Date Création
            worksheet.Column(12).Style.Numberformat.Format = "0"; // Nombre Bâtiments
            worksheet.Column(14).Style.Numberformat.Format = "0"; // Nombre Usages

            // Congeler les en-têtes
            worksheet.View.FreezePanes(2, 1);
        }
    }
}
