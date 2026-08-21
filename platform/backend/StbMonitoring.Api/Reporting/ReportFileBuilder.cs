using System.IO.Compression;
using System.Text;
using System.Security;

namespace StbMonitoring.Api.Reporting;

internal static class ReportFileBuilder
{
    public static byte[] Pdf(string title,IReadOnlyList<string[]> rows)
    {
        static string E(string s)=>s.Replace("\\","\\\\").Replace("(","\\(").Replace(")","\\)");
        var critical=rows.Count(x=>x.Length>2&&x[2]=="P1Critical");var resolved=rows.Count(x=>x.Length>3&&(x[3]=="Resolved"||x[3]=="Closed"));var open=rows.Count-resolved;
        var lines=new List<string>{title,$"Généré le {DateTime.UtcNow:dd/MM/yyyy HH:mm} UTC",$"SYNTHÈSE — Total: {rows.Count} | Ouverts: {open} | Résolus/clos: {resolved} | P1 critiques: {critical}","", "DÉTAIL DES INCIDENTS"};
        lines.AddRange(rows.Take(35).Select(x=>string.Join(" | ",x).Length>105?string.Join(" | ",x)[..105]:string.Join(" | ",x)));
        var stream=new StringBuilder("BT /F1 10 Tf 40 800 Td 14 TL ");foreach(var line in lines)stream.Append('(').Append(E(line)).Append(") Tj T* ");stream.Append("ET");
        var objects=new[]{"<< /Type /Catalog /Pages 2 0 R >>","<< /Type /Pages /Kids [3 0 R] /Count 1 >>","<< /Type /Page /Parent 2 0 R /MediaBox [0 0 595 842] /Resources << /Font << /F1 5 0 R >> >> /Contents 4 0 R >>",$"<< /Length {Encoding.UTF8.GetByteCount(stream.ToString())} >>\nstream\n{stream}\nendstream","<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>"};
        using var ms=new MemoryStream();void W(string s){var b=Encoding.UTF8.GetBytes(s);ms.Write(b);}W("%PDF-1.4\n");var offsets=new List<long>{0};for(var i=0;i<objects.Length;i++){offsets.Add(ms.Position);W($"{i+1} 0 obj\n{objects[i]}\nendobj\n");}var xref=ms.Position;W($"xref\n0 {objects.Length+1}\n0000000000 65535 f \n");for(var i=1;i<offsets.Count;i++)W($"{offsets[i]:D10} 00000 n \n");W($"trailer << /Size {objects.Length+1} /Root 1 0 R >>\nstartxref\n{xref}\n%%EOF");return ms.ToArray();
    }

    public static byte[] Xlsx(IReadOnlyList<string[]> rows)
    {
        using var ms=new MemoryStream();using(var zip=new ZipArchive(ms,ZipArchiveMode.Create,true))
        {
            Add(zip,"[Content_Types].xml","<?xml version=\"1.0\"?><Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\"><Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\"/><Default Extension=\"xml\" ContentType=\"application/xml\"/><Override PartName=\"/xl/workbook.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml\"/><Override PartName=\"/xl/worksheets/sheet1.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml\"/></Types>");
            Add(zip,"_rels/.rels","<?xml version=\"1.0\"?><Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\"><Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument\" Target=\"xl/workbook.xml\"/></Relationships>");
            Add(zip,"xl/workbook.xml","<?xml version=\"1.0\"?><workbook xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\"><sheets><sheet name=\"Incidents\" sheetId=\"1\" r:id=\"rId1\"/></sheets></workbook>");
            Add(zip,"xl/_rels/workbook.xml.rels","<?xml version=\"1.0\"?><Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\"><Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet\" Target=\"worksheets/sheet1.xml\"/></Relationships>");
            var critical=rows.Count(x=>x.Length>2&&x[2]=="P1Critical");var resolved=rows.Count(x=>x.Length>3&&(x[3]=="Resolved"||x[3]=="Closed"));
            var all=new[]{new[]{"STB Sentinel — Rapport incidents"},new[]{"Généré le",DateTime.UtcNow.ToString("dd/MM/yyyy HH:mm")+" UTC"},new[]{"Total",rows.Count.ToString()},new[]{"Ouverts",(rows.Count-resolved).ToString()},new[]{"Résolus / clos",resolved.ToString()},new[]{"P1 critiques",critical.ToString()},Array.Empty<string>(),new[]{"Numéro","Titre","Priorité","Statut","Création","Résolution"}}.Concat(rows);var sheet=new StringBuilder("<?xml version=\"1.0\"?><worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><cols><col min=\"1\" max=\"1\" width=\"22\" customWidth=\"1\"/><col min=\"2\" max=\"2\" width=\"48\" customWidth=\"1\"/><col min=\"3\" max=\"6\" width=\"20\" customWidth=\"1\"/></cols><sheetData>");var rn=1;foreach(var row in all){sheet.Append($"<row r=\"{rn++}\">");foreach(var cell in row)sheet.Append("<c t=\"inlineStr\"><is><t>").Append(SecurityElement.Escape(cell)).Append("</t></is></c>");sheet.Append("</row>");}sheet.Append("</sheetData><autoFilter ref=\"A8:F1048576\"/></worksheet>");Add(zip,"xl/worksheets/sheet1.xml",sheet.ToString());
        }return ms.ToArray();
    }
    private static void Add(ZipArchive zip,string name,string content){var e=zip.CreateEntry(name,CompressionLevel.Fastest);using var w=new StreamWriter(e.Open(),new UTF8Encoding(false));w.Write(content);}
}
