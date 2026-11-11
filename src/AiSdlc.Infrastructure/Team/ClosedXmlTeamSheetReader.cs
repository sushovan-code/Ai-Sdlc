using System.Globalization;
using AiSdlc.Application.Abstractions;
using AiSdlc.Application.Common;
using AiSdlc.Application.Team.Import;
using ClosedXML.Excel;
using Microsoft.Extensions.Logging;

namespace AiSdlc.Infrastructure.Team;

/// <summary>Reads the first worksheet of a team workbook with ClosedXML. Anything unreadable becomes a clear validation error.</summary>
internal sealed class ClosedXmlTeamSheetReader(ILogger<ClosedXmlTeamSheetReader> logger) : ITeamSheetReader
{
    /// <summary>The header row may sit below a title or blank rows, but not further down than this.</summary>
    private const int MaxHeaderSearchRows = 10;

    public Result<IReadOnlyList<RawTeamRow>> Read(byte[] content)
    {
        try
        {
            using var stream = new MemoryStream(content, writable: false);
            using var workbook = new XLWorkbook(stream);
            return ReadFirstSheet(workbook);
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            // An untrusted file can fail inside the library in many ways; the user only needs to know it is not readable.
            logger.LogWarning(exception, "The uploaded team workbook could not be read.");
            return Fail("The file could not be read as an Excel workbook.");
        }
    }

    private static Result<IReadOnlyList<RawTeamRow>> ReadFirstSheet(XLWorkbook workbook)
    {
        var sheet = workbook.Worksheets.FirstOrDefault();
        var used = sheet?.RangeUsed();
        if (sheet is null || used is null)
        {
            return Fail("The first worksheet is empty.");
        }

        var header = FindHeader(sheet, used);
        if (header is null)
        {
            return Fail(
                $"Could not find the header row. The first sheet needs the columns {string.Join(", ", TeamSheetRules.RequiredColumns)}.");
        }

        var missing = TeamSheetRules.RequiredColumns.Where(c => !header.Value.Columns.ContainsKey(c)).ToList();
        if (missing.Count > 0)
        {
            return Fail($"The first sheet is missing the column(s): {string.Join(", ", missing)}.");
        }

        return Result<IReadOnlyList<RawTeamRow>>.Success(ReadRows(sheet, used, header.Value));
    }

    private static (int Row, Dictionary<string, int> Columns)? FindHeader(IXLWorksheet sheet, IXLRange used)
    {
        var lastSearchRow = Math.Min(used.LastRow().RowNumber(), used.FirstRow().RowNumber() + MaxHeaderSearchRows - 1);
        for (var row = used.FirstRow().RowNumber(); row <= lastSearchRow; row++)
        {
            var columns = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (var cell in sheet.Row(row).CellsUsed())
            {
                var text = Text(cell)?.Trim();
                if (!string.IsNullOrEmpty(text))
                {
                    columns.TryAdd(text, cell.Address.ColumnNumber);
                }
            }

            if (TeamSheetRules.RequiredColumns.Count(columns.ContainsKey) >= 3)
            {
                return (row, columns);
            }
        }

        return null;
    }

    private static List<RawTeamRow> ReadRows(IXLWorksheet sheet, IXLRange used, (int Row, Dictionary<string, int> Columns) header)
    {
        var rows = new List<RawTeamRow>();
        for (var row = header.Row + 1; row <= used.LastRow().RowNumber() && rows.Count <= TeamSheetRules.MaxPeople; row++)
        {
            string? Cell(string column) => Text(sheet.Cell(row, header.Columns[column]));

            var raw = new RawTeamRow(
                row,
                Cell(TeamSheetRules.NameColumn),
                Cell(TeamSheetRules.RoleColumn),
                Cell(TeamSheetRules.SkillsColumn),
                Cell(TeamSheetRules.CapacityColumn),
                Cell(TeamSheetRules.AvailabilityColumn));

            // Rows with nothing in any of the five columns are just spacing.
            if (new[] { raw.Name, raw.Role, raw.Skills, raw.Capacity, raw.Availability }.Any(v => !string.IsNullOrWhiteSpace(v)))
            {
                rows.Add(raw);
            }
        }

        return rows;
    }

    /// <summary>Cell text in a culture-independent form, so "40" is read the same on every machine.</summary>
    private static string? Text(IXLCell cell)
    {
        if (cell.IsEmpty())
        {
            return null;
        }

        var value = cell.Value;
        if (value.IsNumber)
        {
            return value.GetNumber().ToString("R", CultureInfo.InvariantCulture);
        }

        return value.IsText ? value.GetText() : cell.GetFormattedString();
    }

    private static Result<IReadOnlyList<RawTeamRow>> Fail(string message) =>
        Result<IReadOnlyList<RawTeamRow>>.Failure(new Error(
            TeamSheetValidator.InvalidCode, "The team sheet has problems.", ErrorType.Validation)
        {
            ValidationErrors = new Dictionary<string, string[]> { ["file"] = [message] },
        });
}
