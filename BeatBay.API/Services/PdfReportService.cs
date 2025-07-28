using iTextSharp.text;
using iTextSharp.text.pdf;
using System.IO;
using BeatBay.DTOs;

namespace BeatBay.API.Services
{
    public class PdfReportService
    {
        public byte[] GenerateArtistStatisticsReport(
            string artistName,
            object summary,
            List<SongStatsDto> songs,
            List<SongStatsDto> topSongs)
        {
            using (var memoryStream = new MemoryStream())
            {
                // Create PDF document
                var document = new Document(PageSize.A4, 50, 50, 25, 25);
                var writer = PdfWriter.GetInstance(document, memoryStream);

                document.Open();

                // Fonts - Fixed for iTextSharp.LGPLv2.Core
                var titleFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 18, BaseColor.Gray);
                var headerFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 14, BaseColor.Gray);
                var normalFont = FontFactory.GetFont(FontFactory.HELVETICA, 12, BaseColor.Black);
                var smallFont = FontFactory.GetFont(FontFactory.HELVETICA, 10, BaseColor.Gray);

                // Main title
                var title = new Paragraph($"Statistics Report - {artistName}", titleFont);
                title.Alignment = Element.ALIGN_CENTER;
                title.SpacingAfter = 20;
                document.Add(title);

                // Generation date
                var date = new Paragraph($"Generated on: {DateTime.Now:MM/dd/yyyy HH:mm}", smallFont);
                date.Alignment = Element.ALIGN_RIGHT;
                date.SpacingAfter = 20;
                document.Add(date);

                // Separator line
                document.Add(new Paragraph("_________________________________________________"));
                document.Add(new Paragraph("\n")); // Replaced Chunk.NEWLINE

                // Summary
                var summaryTitle = new Paragraph("GENERAL SUMMARY", headerFont);
                summaryTitle.SpacingAfter = 10;
                document.Add(summaryTitle);

                if (summary != null)
                {
                    var summaryDict = summary.GetType().GetProperties()
                        .ToDictionary(p => p.Name, p => p.GetValue(summary));

                    var summaryTable = new PdfPTable(2);
                    summaryTable.WidthPercentage = 100;
                    summaryTable.SetWidths(new float[] { 1, 1 });

                    // Style for summary cells
                    var cellStyle = new PdfPCell();
                    cellStyle.BackgroundColor = new BaseColor(245, 245, 245);
                    cellStyle.Padding = 10;
                    cellStyle.Border = Rectangle.BOX;

                    // Total songs
                    var totalSongsLabel = new PdfPCell(new Phrase("Total Songs:", normalFont));
                    totalSongsLabel.BackgroundColor = new BaseColor(245, 245, 245);
                    totalSongsLabel.Padding = 10;
                    summaryTable.AddCell(totalSongsLabel);

                    var totalSongsValue = new PdfPCell(new Phrase(summaryDict.GetValueOrDefault("TotalSongs", 0).ToString(), normalFont));
                    totalSongsValue.Padding = 10;
                    summaryTable.AddCell(totalSongsValue);

                    // Total plays
                    var totalPlaysLabel = new PdfPCell(new Phrase("Total Plays:", normalFont));
                    totalPlaysLabel.BackgroundColor = new BaseColor(245, 245, 245);
                    totalPlaysLabel.Padding = 10;
                    summaryTable.AddCell(totalPlaysLabel);

                    var totalPlaysValue = new PdfPCell(new Phrase(summaryDict.GetValueOrDefault("TotalPlays", 0).ToString(), normalFont));
                    totalPlaysValue.Padding = 10;
                    summaryTable.AddCell(totalPlaysValue);

                    // Active songs
                    var activeSongsLabel = new PdfPCell(new Phrase("Active Songs:", normalFont));
                    activeSongsLabel.BackgroundColor = new BaseColor(245, 245, 245);
                    activeSongsLabel.Padding = 10;
                    summaryTable.AddCell(activeSongsLabel);

                    var activeSongsValue = new PdfPCell(new Phrase(summaryDict.GetValueOrDefault("ActiveSongs", 0).ToString(), normalFont));
                    activeSongsValue.Padding = 10;
                    summaryTable.AddCell(activeSongsValue);

                    // Total time played
                    var totalDurationLabel = new PdfPCell(new Phrase("Total Time Played:", normalFont));
                    totalDurationLabel.BackgroundColor = new BaseColor(245, 245, 245);
                    totalDurationLabel.Padding = 10;
                    summaryTable.AddCell(totalDurationLabel);

                    var totalDuration = Convert.ToInt32(summaryDict.GetValueOrDefault("TotalDurationPlayed", 0));
                    var timeSpan = TimeSpan.FromSeconds(totalDuration);
                    var totalDurationValue = new PdfPCell(new Phrase(timeSpan.ToString(@"hh\:mm\:ss"), normalFont));
                    totalDurationValue.Padding = 10;
                    summaryTable.AddCell(totalDurationValue);

                    document.Add(summaryTable);
                }

                document.Add(new Paragraph("\n")); // Replaced Chunk.NEWLINE

                // Top songs
                if (topSongs != null && topSongs.Any())
                {
                    var topSongsTitle = new Paragraph("TOP 10 MOST PLAYED SONGS", headerFont);
                    topSongsTitle.SpacingAfter = 10;
                    document.Add(topSongsTitle);

                    var topSongsTable = new PdfPTable(3);
                    topSongsTable.WidthPercentage = 100;
                    topSongsTable.SetWidths(new float[] { 3, 1, 1.5f });

                    // Headers
                    var headerCell1 = new PdfPCell(new Phrase("Title", headerFont));
                    headerCell1.BackgroundColor = new BaseColor(70, 130, 180);
                    headerCell1.Padding = 8;
                    topSongsTable.AddCell(headerCell1);

                    var headerCell2 = new PdfPCell(new Phrase("Plays", headerFont));
                    headerCell2.BackgroundColor = new BaseColor(70, 130, 180);
                    headerCell2.Padding = 8;
                    topSongsTable.AddCell(headerCell2);

                    var headerCell3 = new PdfPCell(new Phrase("Total Time", headerFont));
                    headerCell3.BackgroundColor = new BaseColor(70, 130, 180);
                    headerCell3.Padding = 8;
                    topSongsTable.AddCell(headerCell3);

                    // Data
                    int position = 1;
                    foreach (var song in topSongs.Take(10))
                    {
                        var titleCell = new PdfPCell(new Phrase($"{position}. {song.Title}", normalFont));
                        titleCell.Padding = 8;
                        if (position % 2 == 0)
                            titleCell.BackgroundColor = new BaseColor(250, 250, 250);
                        topSongsTable.AddCell(titleCell);

                        var playsCell = new PdfPCell(new Phrase(song.PlayCount.ToString(), normalFont));
                        playsCell.Padding = 8;
                        playsCell.HorizontalAlignment = Element.ALIGN_CENTER;
                        if (position % 2 == 0)
                            playsCell.BackgroundColor = new BaseColor(250, 250, 250);
                        topSongsTable.AddCell(playsCell);

                        var durationCell = new PdfPCell(new Phrase(
                            TimeSpan.FromSeconds(song.TotalDurationPlayed).ToString(@"hh\:mm\:ss"),
                            normalFont));
                        durationCell.Padding = 8;
                        durationCell.HorizontalAlignment = Element.ALIGN_CENTER;
                        if (position % 2 == 0)
                            durationCell.BackgroundColor = new BaseColor(250, 250, 250);
                        topSongsTable.AddCell(durationCell);

                        position++;
                    }

                    document.Add(topSongsTable);
                }

                // New page for all songs
                document.NewPage();

                // All songs
                if (songs != null && songs.Any())
                {
                    var allSongsTitle = new Paragraph("ALL SONGS", headerFont);
                    allSongsTitle.SpacingAfter = 10;
                    document.Add(allSongsTitle);

                    var allSongsTable = new PdfPTable(3);
                    allSongsTable.WidthPercentage = 100;
                    allSongsTable.SetWidths(new float[] { 3, 1, 1.5f });

                    // Headers
                    var headerCell1 = new PdfPCell(new Phrase("Title", headerFont));
                    headerCell1.BackgroundColor = new BaseColor(70, 130, 180);
                    headerCell1.Padding = 8;
                    allSongsTable.AddCell(headerCell1);

                    var headerCell2 = new PdfPCell(new Phrase("Plays", headerFont));
                    headerCell2.BackgroundColor = new BaseColor(70, 130, 180);
                    headerCell2.Padding = 8;
                    allSongsTable.AddCell(headerCell2);

                    var headerCell3 = new PdfPCell(new Phrase("Total Time", headerFont));
                    headerCell3.BackgroundColor = new BaseColor(70, 130, 180);
                    headerCell3.Padding = 8;
                    allSongsTable.AddCell(headerCell3);

                    // Data
                    int count = 1;
                    foreach (var song in songs)
                    {
                        var titleCell = new PdfPCell(new Phrase(song.Title, normalFont));
                        titleCell.Padding = 8;
                        if (count % 2 == 0)
                            titleCell.BackgroundColor = new BaseColor(250, 250, 250);
                        allSongsTable.AddCell(titleCell);

                        var playsCell = new PdfPCell(new Phrase(song.PlayCount.ToString(), normalFont));
                        playsCell.Padding = 8;
                        playsCell.HorizontalAlignment = Element.ALIGN_CENTER;
                        if (count % 2 == 0)
                            playsCell.BackgroundColor = new BaseColor(250, 250, 250);
                        allSongsTable.AddCell(playsCell);

                        var durationCell = new PdfPCell(new Phrase(
                            TimeSpan.FromSeconds(song.TotalDurationPlayed).ToString(@"hh\:mm\:ss"),
                            normalFont));
                        durationCell.Padding = 8;
                        durationCell.HorizontalAlignment = Element.ALIGN_CENTER;
                        if (count % 2 == 0)
                            durationCell.BackgroundColor = new BaseColor(250, 250, 250);
                        allSongsTable.AddCell(durationCell);

                        count++;
                    }

                    document.Add(allSongsTable);
                }

                // Footer
                document.Add(new Paragraph("\n")); // Replaced Chunk.NEWLINE
                document.Add(new Paragraph("_________________________________________________"));
                var footer = new Paragraph($"BeatBay - Automatically generated report", smallFont);
                footer.Alignment = Element.ALIGN_CENTER;
                document.Add(footer);

                document.Close();
                return memoryStream.ToArray();
            }
        }
    }
}