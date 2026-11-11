using AiSdlc.Application.Common;

namespace AiSdlc.Application.Abstractions;

/// <summary>Reads the rows of an uploaded team workbook. The adapter hides the spreadsheet library.</summary>
public interface ITeamSheetReader
{
    /// <summary>
    /// Finds the header row and returns the data rows. Fails with a validation error when the workbook cannot be
    /// read or lacks a required column.
    /// </summary>
    Result<IReadOnlyList<RawTeamRow>> Read(byte[] content);
}
