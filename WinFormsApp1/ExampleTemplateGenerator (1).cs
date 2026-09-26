using System;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace WinFormsApp1
{
    /// <summary>
    /// Builds a sample DOCX file that shows professors exactly how to format
    /// their exam so DocxQuizImporter can read it correctly (PART headers,
    /// numbered questions, A/B/C/D choices, and the ANSWER KEY layout).
    ///
    /// This writes the .docx by hand as a plain ZIP of the minimal Word XML
    /// parts (no DocumentFormat.OpenXml SDK types needed), so it works
    /// regardless of which version of that package is referenced.
    /// </summary>
    public static class ExampleTemplateGenerator
    {
        public static void Generate(string filePath)
        {
            StringBuilder body = new StringBuilder();

            body.Append(Centered("SUBJECT NAME HERE", bold: true, halfPtSize: 28));
            body.Append(Centered("QUIZ / EXAMINATION", bold: true, halfPtSize: 24));

            body.Append(Line("Name: ________________________________________________"));
            body.Append(Line("Section: ____________________    Date: ________________"));
            body.Append(Line("Score: __________ / 50"));

            body.Append(Line("GENERAL INSTRUCTIONS", bold: true));
            body.Append(Line("Read each question carefully before answering."));
            body.Append(Line("For Multiple Choice, choose the letter of the best answer."));
            body.Append(Line("For True or False, write TRUE if the statement is correct and FALSE if it is incorrect."));
            body.Append(Line("For Identification, write the correct term or concept."));
            body.Append(Line("For Essay, answer clearly and briefly using complete sentences."));

            // PART I - MULTIPLE CHOICE
            body.Append(Line("PART I \u2013 MULTIPLE CHOICE (15 points)", bold: true));
            body.Append(Line("1. Sample multiple choice question goes here?"));
            body.Append(Line("A. First choice"));
            body.Append(Line("B. Second choice (this is the correct one in this example)"));
            body.Append(Line("C. Third choice"));
            body.Append(Line("D. Fourth choice"));
            body.Append(Line("2. Add as many questions as you need, just keep numbering them 1, 2, 3..."));
            body.Append(Line("A. Choice A"));
            body.Append(Line("B. Choice B"));
            body.Append(Line("C. Choice C"));
            body.Append(Line("D. Choice D"));

            // PART II - TRUE OR FALSE
            body.Append(Line("PART II \u2013 TRUE OR FALSE (10 points)", bold: true));
            body.Append(Line("1. Sample true-or-false statement goes here.  __________"));
            body.Append(Line("2. Numbering restarts at 1 for every new PART.  __________"));

            // PART III - IDENTIFICATION
            body.Append(Line("PART III \u2013 IDENTIFICATION (10 points)", bold: true));
            body.Append(Line("1. Sample identification question goes here."));
            body.Append(Line("Answer: ______________________________________________"));
            body.Append(Line("2. Add more identification items the same way."));
            body.Append(Line("Answer: ______________________________________________"));

            // PART IV - ESSAY
            body.Append(Line("PART IV \u2013 ESSAY (15 points)", bold: true));
            body.Append(Line("1. Sample essay question goes here. Explain your answer in complete sentences."));
            body.Append(Line("________________________________________________________________________________"));
            body.Append(Line("________________________________________________________________________________"));
            body.Append(Line("________________________________________________________________________________"));

            // Page break before the answer key.
            body.Append("<w:p><w:r><w:br w:type=\"page\"/></w:r></w:p>");

            // ANSWER KEY
            body.Append(Line("ANSWER KEY \u2013 FOR PROFESSOR", bold: true));
            body.Append(Line("1. B"));
            body.Append(Line("2. A"));

            body.Append(Line("TRUE OR FALSE", bold: true));
            body.Append(Line("1. TRUE"));
            body.Append(Line("2. FALSE"));

            body.Append(Line("IDENTIFICATION", bold: true));
            body.Append(Line("1. Sample Answer One"));
            body.Append(Line("2. Sample Answer Two"));

            body.Append(Line("ESSAY", bold: true));
            body.Append(Line("Answers may vary. Check based on accuracy, completeness, relevance, and clarity."));

            string documentXml =
                "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" +
                "<w:document xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\">" +
                "<w:body>" +
                body.ToString() +
                "<w:sectPr>" +
                "<w:pgSz w:w=\"12240\" w:h=\"15840\"/>" +
                "<w:pgMar w:top=\"1440\" w:right=\"1440\" w:bottom=\"1440\" w:left=\"1440\"/>" +
                "</w:sectPr>" +
                "</w:body>" +
                "</w:document>";

            const string contentTypesXml =
                "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" +
                "<Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\">" +
                "<Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\"/>" +
                "<Default Extension=\"xml\" ContentType=\"application/xml\"/>" +
                "<Override PartName=\"/word/document.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.wordprocessingml.document.main+xml\"/>" +
                "</Types>";

            const string rootRelsXml =
                "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" +
                "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">" +
                "<Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument\" Target=\"word/document.xml\"/>" +
                "</Relationships>";

            const string docRelsXml =
                "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" +
                "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">" +
                "</Relationships>";

            if (File.Exists(filePath))
                File.Delete(filePath);

            using (FileStream fs = new FileStream(filePath, FileMode.Create))
            using (ZipArchive archive = new ZipArchive(fs, ZipArchiveMode.Create))
            {
                WriteEntry(archive, "[Content_Types].xml", contentTypesXml);
                WriteEntry(archive, "_rels/.rels", rootRelsXml);
                WriteEntry(archive, "word/document.xml", documentXml);
                WriteEntry(archive, "word/_rels/document.xml.rels", docRelsXml);
            }
        }

        private static void WriteEntry(ZipArchive archive, string entryPath, string xmlContent)
        {
            ZipArchiveEntry entry = archive.CreateEntry(entryPath, CompressionLevel.Optimal);
            using (Stream stream = entry.Open())
            using (StreamWriter writer = new StreamWriter(stream, new UTF8Encoding(false)))
            {
                writer.Write(xmlContent);
            }
        }

        private static string Line(string text, bool bold = false, int halfPtSize = 22)
        {
            string runProps = "<w:rPr>" + (bold ? "<w:b/>" : "") + "<w:sz w:val=\"" + halfPtSize + "\"/></w:rPr>";
            return "<w:p><w:r>" + runProps + "<w:t xml:space=\"preserve\">" + Escape(text) + "</w:t></w:r></w:p>";
        }

        private static string Centered(string text, bool bold = false, int halfPtSize = 22)
        {
            string runProps = "<w:rPr>" + (bold ? "<w:b/>" : "") + "<w:sz w:val=\"" + halfPtSize + "\"/></w:rPr>";
            return "<w:p><w:pPr><w:jc w:val=\"center\"/></w:pPr><w:r>" + runProps +
                   "<w:t xml:space=\"preserve\">" + Escape(text) + "</w:t></w:r></w:p>";
        }

        private static string Escape(string text)
        {
            if (string.IsNullOrEmpty(text)) return "";
            return text
                .Replace("&", "&amp;")
                .Replace("<", "&lt;")
                .Replace(">", "&gt;");
        }
    }
}