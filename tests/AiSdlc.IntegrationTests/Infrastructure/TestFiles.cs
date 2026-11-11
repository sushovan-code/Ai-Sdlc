using System.IO.Compression;
using System.Text;

namespace AiSdlc.IntegrationTests.Infrastructure;

/// <summary>Small uploads that pass the server's file checks.</summary>
public static class TestFiles
{
    public static byte[] Docx(string marker = "") => Package(marker, "word/document.xml", "[Content_Types].xml");

    public static byte[] Xlsx(string marker = "") => Package(marker, "xl/workbook.xml", "[Content_Types].xml");

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
