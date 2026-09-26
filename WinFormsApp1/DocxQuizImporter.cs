using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using DocumentFormat.OpenXml.Packaging;
using Word = DocumentFormat.OpenXml.Wordprocessing;

namespace WinFormsApp1
{
    public class QuizImportResult
    {
        public string Title { get; set; } = "";
        public string Subject { get; set; } = "";
        public List<QuizQuestion> Questions { get; set; } = new List<QuizQuestion>();
    }

    public static class DocxQuizImporter
    {
        // Section headers found in the QUESTION area, e.g.:
        //   "PART I – MULTIPLE CHOICE (15 points)"
        //   "PART II – TRUE OR FALSE (10 points)"
        //   "PART III – IDENTIFICATION (10 points)"
        //   "PART IV – ESSAY (15 points)"
        private static readonly Regex PartHeaderRegex = new Regex(
            @"^\s*(PART\s+[IVXLC]+|[IVXLC]+)\s*[\.\-–—:]?\s*(MULTIPLE\s*CHOICE|TRUE\s*(?:OR|/)?\s*FALSE|IDENTIFICATION|ESSAY)",
            RegexOptions.IgnoreCase);

        // Bare sub-headers found inside the ANSWER KEY area, e.g. "TRUE OR FALSE", "IDENTIFICATION", "ESSAY"
        private static readonly Regex KeySubHeaderRegex = new Regex(
            @"^\s*(MULTIPLE\s*CHOICE|TRUE\s*(?:OR|/)?\s*FALSE|IDENTIFICATION|ESSAY)\s*$",
            RegexOptions.IgnoreCase);

        // Lines that are just blank fill-in space: "Answer: ______" or plain "______________"
        private static readonly Regex BlankAnswerLineRegex = new Regex(
            @"^\s*(Answer\s*:\s*)?_{3,}\s*$", RegexOptions.IgnoreCase);

        // ============================================================
        // MAIN IMPORT
        // ============================================================
        public static QuizImportResult Import(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("DOCX file path is empty.");

            if (!File.Exists(filePath))
                throw new FileNotFoundException("DOCX file was not found.", filePath);

            List<string> lines = ReadAllLines(filePath);

            if (lines.Count == 0)
                throw new Exception("The DOCX file does not contain readable text.");

            var result = new QuizImportResult { Title = ExtractTitle(lines, filePath) };

            // --------------------------------------------------------
            // Split into question area vs answer-key area.
            // --------------------------------------------------------
            int answerKeyStart = lines.FindIndex(l => Regex.IsMatch(l, @"^ANSWER\s*KEY\b", RegexOptions.IgnoreCase));
            int contentEnd = answerKeyStart >= 0 ? answerKeyStart : lines.Count;

            // --------------------------------------------------------
            // Parse the answer key area into per-section dictionaries.
            // Numbering starts over inside each section, so we key
            // answers by (section, number) instead of number alone.
            // --------------------------------------------------------
            var mcAnswers = new Dictionary<int, string>();
            var tfAnswers = new Dictionary<int, string>();
            var idAnswers = new Dictionary<int, string>();

            if (answerKeyStart >= 0)
            {
                string keySection = "mc"; // answers right after "ANSWER KEY" belong to Multiple Choice by default
                for (int i = answerKeyStart + 1; i < lines.Count; i++)
                {
                    string line = lines[i];

                    Match subHeader = KeySubHeaderRegex.Match(line);
                    if (subHeader.Success)
                    {
                        string kw = subHeader.Groups[1].Value.ToUpperInvariant().Replace(" ", "");
                        if (kw.Contains("MULTIPLECHOICE")) keySection = "mc";
                        else if (kw.Contains("TRUE")) keySection = "tf";
                        else if (kw.Contains("IDENTIFICATION")) keySection = "id";
                        else if (kw.Contains("ESSAY")) keySection = "essay";
                        continue;
                    }

                    if (!TryParseNumberedLine(line, out int num, out string val))
                        continue;

                    if (keySection == "mc") mcAnswers[num] = val;
                    else if (keySection == "tf") tfAnswers[num] = val;
                    else if (keySection == "id") idAnswers[num] = val;
                    // essay answers are a rubric note, not per-question -> ignored
                }
            }

            // --------------------------------------------------------
            // Parse the question area into blocks, tracking which
            // PART/section each question belongs to.
            // --------------------------------------------------------
            var blocks = new List<(int Number, string Section, string Question, string A, string B, string C, string D)>();

            string currentSection = null; // "mc" | "tf" | "id" | "essay" | null
            int? curNum = null;
            string curSection = null;
            string curQ = "", curA = "", curB = "", curC = "", curD = "";

            void FlushCurrent()
            {
                if (curNum.HasValue && !string.IsNullOrWhiteSpace(curQ))
                {
                    string cleanQ = Regex.Replace(curQ.Trim(), @"_{2,}\s*$", "").Trim();
                    blocks.Add((curNum.Value, curSection, cleanQ, curA, curB, curC, curD));
                }
                curNum = null;
                curSection = null;
                curQ = curA = curB = curC = curD = "";
            }

            for (int i = 0; i < contentEnd; i++)
            {
                string line = lines[i];

                // Section header ("PART I - MULTIPLE CHOICE ...") -> remember section, skip line entirely.
                if (PartHeaderRegex.IsMatch(line))
                {
                    FlushCurrent();
                    Match m = PartHeaderRegex.Match(line);
                    string kw = m.Groups[2].Value.ToUpperInvariant().Replace(" ", "");
                    if (kw.Contains("MULTIPLECHOICE")) currentSection = "mc";
                    else if (kw.Contains("TRUE")) currentSection = "tf";
                    else if (kw.Contains("IDENTIFICATION")) currentSection = "id";
                    else if (kw.Contains("ESSAY")) currentSection = "essay";
                    continue;
                }

                // Skip header/instruction/blank-fill lines outright.
                if (Regex.IsMatch(line, @"^(NAME|SECTION|DATE|SCORE|TEACHER|PROFESSOR|SUBJECT|COURSE)\s*:", RegexOptions.IgnoreCase))
                    continue;
                if (Regex.IsMatch(line, @"^(GENERAL\s+)?INSTRUCTIONS\s*$", RegexOptions.IgnoreCase))
                    continue;
                if (Regex.IsMatch(line, @"^(Read each|For (Multiple|True|Identification|Essay))", RegexOptions.IgnoreCase))
                    continue;
                if (BlankAnswerLineRegex.IsMatch(line))
                    continue;

                // New numbered question, e.g. "1. What is..." / "1) What is..."
                Match qMatch = Regex.Match(line, @"^\s*(\d+)\s*[\.\)]\s*(.+)$");
                if (qMatch.Success)
                {
                    FlushCurrent();
                    curNum = int.Parse(qMatch.Groups[1].Value);
                    curQ = qMatch.Groups[2].Value.Trim();
                    curSection = currentSection;
                    continue;
                }

                // Choice line, e.g. "A. Something" / "a) Something"
                Match cMatch = Regex.Match(line, @"^\s*([A-Da-d])[\.\)]\s*(.+)$");
                if (curNum.HasValue && cMatch.Success)
                {
                    string text = cMatch.Groups[2].Value.Trim();
                    switch (char.ToUpperInvariant(cMatch.Groups[1].Value[0]))
                    {
                        case 'A': curA = text; break;
                        case 'B': curB = text; break;
                        case 'C': curC = text; break;
                        case 'D': curD = text; break;
                    }
                    continue;
                }

                // Standalone TRUE / FALSE choice lines (no letter prefix), used by some templates.
                if (curNum.HasValue && Regex.IsMatch(line, @"^(TRUE|FALSE)$", RegexOptions.IgnoreCase))
                {
                    if (line.Trim().ToUpperInvariant() == "TRUE" && string.IsNullOrWhiteSpace(curA))
                        curA = "True";
                    else if (line.Trim().ToUpperInvariant() == "FALSE" && string.IsNullOrWhiteSpace(curB))
                        curB = "False";
                    continue;
                }

                // Otherwise, treat as continuation of the current question text.
                if (curNum.HasValue)
                    curQ += " " + line;
            }
            FlushCurrent();

            if (blocks.Count == 0)
                throw new Exception(
                    "No questions were detected in the DOCX file.\n\n" +
                    "Make sure questions are numbered like \"1. ...\" and choices like \"A. ...\".");

            // --------------------------------------------------------
            // Build the final QuizQuestion list.
            // --------------------------------------------------------
            foreach (var b in blocks)
            {
                int choiceCount = new[] { b.A, b.B, b.C, b.D }.Count(c => !string.IsNullOrWhiteSpace(c));

                string type;
                string answer;

                if (b.Section == "mc")
                {
                    type = "multiple_choice";
                    answer = mcAnswers.TryGetValue(b.Number, out string a1) ? a1.Trim() : "";
                }
                else if (b.Section == "tf")
                {
                    type = "true_false";
                    answer = tfAnswers.TryGetValue(b.Number, out string a2) ? a2.Trim() : "";
                }
                else if (b.Section == "id")
                {
                    type = "identification";
                    answer = idAnswers.TryGetValue(b.Number, out string a3) ? a3.Trim() : "";
                }
                else if (b.Section == "essay")
                {
                    type = "essay";
                    answer = "";
                }
                else
                {
                    // No PART header was found for this question (e.g. a simpler docx) -> auto-detect.
                    if (choiceCount >= 2)
                    {
                        type = "multiple_choice";
                    }
                    else if (Regex.IsMatch(b.Question, @"true\s*or\s*false|true/false", RegexOptions.IgnoreCase))
                    {
                        type = "true_false";
                    }
                    else
                    {
                        type = "identification";
                    }
                    answer = mcAnswers.TryGetValue(b.Number, out string a4) ? a4.Trim() : "";
                }

                string correctAnswer = answer;

                if (type == "multiple_choice")
                {
                    Match letterMatch = Regex.Match(answer, @"^\s*([A-Da-d])\b");
                    if (letterMatch.Success)
                    {
                        correctAnswer = letterMatch.Groups[1].Value.ToUpperInvariant();
                    }
                    else if (!string.IsNullOrWhiteSpace(answer))
                    {
                        // Answer key may spell out the choice text instead of a letter.
                        if (SameText(answer, b.A)) correctAnswer = "A";
                        else if (SameText(answer, b.B)) correctAnswer = "B";
                        else if (SameText(answer, b.C)) correctAnswer = "C";
                        else if (SameText(answer, b.D)) correctAnswer = "D";
                    }
                }
                else if (type == "true_false")
                {
                    if (Regex.IsMatch(answer, @"^(true|t)$", RegexOptions.IgnoreCase))
                        correctAnswer = "True";
                    else if (Regex.IsMatch(answer, @"^(false|f)$", RegexOptions.IgnoreCase))
                        correctAnswer = "False";
                }

                result.Questions.Add(new QuizQuestion
                {
                    Question = b.Question,
                    QuestionType = type,
                    ChoiceA = b.A,
                    ChoiceB = b.B,
                    ChoiceC = b.C,
                    ChoiceD = b.D,
                    CorrectAnswer = correctAnswer
                });
            }

            return result;
        }

        // ============================================================
        // READ ALL TEXT LINES FROM THE DOCX (paragraphs + table cells)
        // ============================================================
        private static List<string> ReadAllLines(string filePath)
        {
            var lines = new List<string>();

            using (var document = WordprocessingDocument.Open(filePath, false))
            {
                Word.Body body = document.MainDocumentPart.Document.Body;

                foreach (var element in body.Elements())
                {
                    if (element is Word.Paragraph paragraph)
                    {
                        string text = Clean(paragraph.InnerText);
                        if (!string.IsNullOrWhiteSpace(text))
                            lines.Add(text);
                    }
                    else if (element is Word.Table table)
                    {
                        foreach (var row in table.Elements<Word.TableRow>())
                        {
                            foreach (var cell in row.Elements<Word.TableCell>())
                            {
                                string text = Clean(cell.InnerText);
                                if (!string.IsNullOrWhiteSpace(text))
                                    lines.Add(text);
                            }
                        }
                    }
                }
            }

            return lines;
        }

        // ============================================================
        // TITLE
        // ============================================================
        private static string ExtractTitle(List<string> lines, string filePath)
        {
            string title = "";

            if (lines.Count >= 1 && lines[0].Length < 80 && !Regex.IsMatch(lines[0], @"^\d+\s*[\.\)]"))
                title = lines[0];

            if (lines.Count >= 2 && lines[1].Length < 60 &&
                Regex.IsMatch(lines[1], @"QUIZ|EXAM|TEST", RegexOptions.IgnoreCase))
            {
                title = string.IsNullOrWhiteSpace(title) ? lines[1] : title + " - " + lines[1];
            }

            return string.IsNullOrWhiteSpace(title) ? Path.GetFileNameWithoutExtension(filePath) : title;
        }

        // ============================================================
        // HELPERS
        // ============================================================
        private static string Clean(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return "";
            text = Regex.Replace(text.Replace("\r", " ").Replace("\n", " ").Replace("\t", " "), @"\s+", " ");
            return text.Trim();
        }

        private static bool TryParseNumberedLine(string line, out int number, out string value)
        {
            number = 0;
            value = "";
            Match m = Regex.Match(line, @"^\s*(\d+)\s*[\.\)\:\-]\s*(.+?)\s*$");
            if (!m.Success) return false;
            if (!int.TryParse(m.Groups[1].Value, out number)) return false;
            value = m.Groups[2].Value.Trim();
            return !string.IsNullOrWhiteSpace(value);
        }

        private static bool SameText(string a, string b)
        {
            if (string.IsNullOrWhiteSpace(a) || string.IsNullOrWhiteSpace(b)) return false;
            return string.Equals(a.Trim(), b.Trim(), StringComparison.OrdinalIgnoreCase);
        }
    }
}