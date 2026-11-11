using System.IO.Compression;
using System.Text;
using ClosedXML.Excel;

namespace AiSdlc.IntegrationTests.Infrastructure;

/// <summary>Small uploads that pass the server's file checks.</summary>
public static class TestFiles
{
    public static byte[] Docx(string marker = "") => Package(marker, "word/document.xml", "[Content_Types].xml");

    /// <summary>A real team workbook. By default the five people from the sample sheet.</summary>
    public static byte[] Xlsx(params string[][] people) => TeamSheet(people.Length == 0 ? SamplePeople : people);

    public static readonly string[][] SamplePeople =
    [
        ["Asha Rao", "Developer", "C#, ASP.NET Core, SQL, EF Core", "40", "30"],
        ["Ravi Kumar", "Developer", "Angular, TypeScript, CSS", "40", "40"],
        ["Priya Nair", "Developer", "C#, Security, Azure, DevOps", "32", "32"],
        ["Meera Iyer", "QA", "Testing, Selenium, API Testing", "40", "40"],
        ["Arjun Das", "QA", "Testing, Security Testing", "30", "30"],
    ];

    public static readonly string[] TeamHeaders = ["Name", "Role", "Skills", "Capacity", "Availability"];

    public static byte[] TeamSheet(IEnumerable<string[]> people, string[]? headers = null)
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.AddWorksheet("Team");
        var columns = headers ?? TeamHeaders;
        for (var c = 0; c < columns.Length; c++)
        {
            sheet.Cell(1, c + 1).Value = columns[c];
        }

        var row = 2;
        foreach (var person in people)
        {
            for (var c = 0; c < person.Length; c++)
            {
                // Hours are numbers in a real sheet, so write them as numbers where they parse.
                if (double.TryParse(person[c], System.Globalization.CultureInfo.InvariantCulture, out var number) && c >= 3)
                {
                    sheet.Cell(row, c + 1).Value = number;
                }
                else
                {
                    sheet.Cell(row, c + 1).Value = person[c];
                }
            }

            row++;
        }

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    public static byte[] Text(string content = "The system shall support express checkout.") =>
        Encoding.UTF8.GetBytes(content);

    public static byte[] Package(string marker, params string[] entryNames)
    {
        using var stream = new MemoryStream();
        using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var name in entryNames)
            {
                using var writer = new StreamWriter(archive.CreateEntry(name).Open());
                writer.Write($"<xml>{marker}</xml>");
            }
        }

        return stream.ToArray();
    }
}
