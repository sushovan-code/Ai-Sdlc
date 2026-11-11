using System.IO.Compression;
using System.Text;

namespace AiSdlc.UnitTests.Support;

/// <summary>Builds small, valid-looking uploads in memory.</summary>
public static class TestFiles
{
    public static byte[] Docx() => Package("word/document.xml", "[Content_Types].xml");

    public static byte[] Xlsx() => Package("xl/workbook.xml", "[Content_Types].xml");

    public static byte[] Text(string content = "The system shall let a customer pay with a stored token.") =>
        Encoding.UTF8.GetBytes(content);

    public static byte[] Package(params string[] entryNames)
    {
        using var stream = new MemoryStream();
        using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var name in entryNames)
            {
                var entry = archive.CreateEntry(name);
                using var writer = new StreamWriter(entry.Open());
                writer.Write("<xml/>");
            }
        }

        return stream.ToArray();
    }
}
