using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using DocumentFormat.OpenXml.Packaging;
using Word = DocumentFormat.OpenXml.Wordprocessing;

namespace WinFormsApp1
{
    public class QuizImportResult
    {
        public string Title { get; set; }
        public string Subject { get; set; }

        public List<QuizQuestion> Questions { get; set; }

        public QuizImportResult()
        {
            Questions = new List<QuizQuestion>();
        }
    }


    public static class DocxQuizImporter
    {
        // =============================================================
        // MAIN IMPORT
        // =============================================================

        public static QuizImportResult Import(string filePath)
        {
            QuizImportResult result =
                new QuizImportResult();

            List<string> paragraphs =
                new List<string>();

            // =========================================================
            // READ ALL DOCX PARAGRAPHS
            // =========================================================

            using (WordprocessingDocument document =
                   WordprocessingDocument.Open(filePath, false))
            {
                Word.Body body =
                    document.MainDocumentPart.Document.Body;

                foreach (Word.Paragraph paragraph
                         in body.Elements<Word.Paragraph>())
                {
                    string text =
                        paragraph.InnerText.Trim();

                    if (!string.IsNullOrWhiteSpace(text))
                    {
                        paragraphs.Add(text);
                    }
                }
            }


            // =========================================================
            // QUESTION NUMBER -> QUESTION
            // =========================================================

            Dictionary<int, QuizQuestion> questionMap =
                new Dictionary<int, QuizQuestion>();


            QuizQuestion currentQuestion = null;

            string currentSectionType = "";

            bool answerKeyReached = false;


            // =========================================================
            // FIRST PASS
            // READ QUESTIONS
            // =========================================================

            foreach (string text in paragraphs)
            {
                // -----------------------------------------------------
                // ANSWER KEY
                //
                // Once detected, stop reading questions.
                // -----------------------------------------------------

                if (IsAnswerKeyHeading(text))
                {
                    answerKeyReached = true;
                    break;
                }


                // -----------------------------------------------------
                // QUIZ TITLE
                // -----------------------------------------------------

                if (text.StartsWith(
                    "QUIZ:",
                    StringComparison.OrdinalIgnoreCase))
                {
                    result.Title =
                        text.Substring(5).Trim();

                    continue;
                }


                // -----------------------------------------------------
                // SUBJECT
                // -----------------------------------------------------

                if (text.StartsWith(
                    "SUBJECT:",
                    StringComparison.OrdinalIgnoreCase))
                {
                    result.Subject =
                        text.Substring(8).Trim();

                    continue;
                }


                // -----------------------------------------------------
                // TYPE:
                //
                // TYPE: MULTIPLE CHOICE
                // TYPE: TRUE/FALSE
                // -----------------------------------------------------

                if (text.StartsWith(
                    "TYPE:",
                    StringComparison.OrdinalIgnoreCase))
                {
                    string type =
                        text.Substring(5).Trim();

                    currentSectionType =
                        ConvertQuestionType(type);

                    if (currentQuestion != null)
                    {
                        currentQuestion.QuestionType =
                            currentSectionType;

                        SetDefaultChoicesForType(
                            currentQuestion,
                            currentSectionType);
                    }

                    continue;
                }


                // -----------------------------------------------------
                // SECTION HEADING
                // -----------------------------------------------------

                string sectionType =
                    DetectSectionHeading(text);

                if (!string.IsNullOrEmpty(sectionType))
                {
                    currentSectionType =
                        sectionType;

                    continue;
                }


                // -----------------------------------------------------
                // NEW QUESTION
                //
                // 1. What is information security?
                // 2. What is confidentiality?
                // -----------------------------------------------------

                int questionNumber;

                if (TryGetQuestionNumber(
                    text,
                    out questionNumber))
                {
                    // -------------------------------------------------
                    // If current line looks like an answer-key line,
                    // do not create a question.
                    // -------------------------------------------------

                    if (LooksLikeAnswerOnly(text))
                    {
                        continue;
                    }


                    // -------------------------------------------------
                    // SAVE PREVIOUS QUESTION
                    // -------------------------------------------------

                    if (currentQuestion != null)
                    {
                        FinalizeQuestion(
                            currentQuestion);

                        if (!result.Questions.Contains(
                            currentQuestion))
                        {
                            result.Questions.Add(
                                currentQuestion);
                        }
                    }


                    // -------------------------------------------------
                    // CREATE NEW QUESTION
                    // -------------------------------------------------

                    currentQuestion =
                        new QuizQuestion();


                    currentQuestion.Question =
                        RemoveQuestionNumber(text);


                    // -------------------------------------------------
                    // DETECT TYPE
                    // -------------------------------------------------

                    string detectedType =
                        DetectQuestionTypeFromText(
                            currentQuestion.Question,
                            currentSectionType);


                    currentQuestion.QuestionType =
                        detectedType;


                    SetDefaultChoicesForType(
                        currentQuestion,
                        detectedType);


                    // -------------------------------------------------
                    // STORE QUESTION NUMBER
                    // -------------------------------------------------

                    if (!questionMap.ContainsKey(
                        questionNumber))
                    {
                        questionMap.Add(
                            questionNumber,
                            currentQuestion);
                    }


                    continue;
                }


                // =====================================================
                // MULTIPLE CHOICE A
                // =====================================================

                if (StartsWithChoice(text, "A"))
                {
                    if (currentQuestion != null)
                    {
                        currentQuestion.QuestionType =
                            "multiple_choice";

                        currentQuestion.ChoiceA =
                            RemoveChoiceLetter(
                                text,
                                "A");
                    }

                    continue;
                }


                // =====================================================
                // MULTIPLE CHOICE B
                // =====================================================

                if (StartsWithChoice(text, "B"))
                {
                    if (currentQuestion != null)
                    {
                        currentQuestion.QuestionType =
                            "multiple_choice";

                        currentQuestion.ChoiceB =
                            RemoveChoiceLetter(
                                text,
                                "B");
                    }

                    continue;
                }


                // =====================================================
                // MULTIPLE CHOICE C
                // =====================================================

                if (StartsWithChoice(text, "C"))
                {
                    if (currentQuestion != null)
                    {
                        currentQuestion.QuestionType =
                            "multiple_choice";

                        currentQuestion.ChoiceC =
                            RemoveChoiceLetter(
                                text,
                                "C");
                    }

                    continue;
                }


                // =====================================================
                // MULTIPLE CHOICE D
                // =====================================================

                if (StartsWithChoice(text, "D"))
                {
                    if (currentQuestion != null)
                    {
                        currentQuestion.QuestionType =
                            "multiple_choice";

                        currentQuestion.ChoiceD =
                            RemoveChoiceLetter(
                                text,
                                "D");
                    }

                    continue;
                }


                // =====================================================
                // TRUE / FALSE
                // =====================================================

                if (IsTrueFalseChoice(text))
                {
                    if (currentQuestion != null)
                    {
                        currentQuestion.QuestionType =
                            "true_false";

                        currentQuestion.ChoiceA =
                            "TRUE";

                        currentQuestion.ChoiceB =
                            "FALSE";

                        currentQuestion.ChoiceC =
                            "";

                        currentQuestion.ChoiceD =
                            "";
                    }

                    continue;
                }


                // =====================================================
                // INLINE ANSWER
                //
                // ANSWER: B
                // ANSWER: Authentication
                // =====================================================

                if (text.StartsWith(
                    "ANSWER:",
                    StringComparison.OrdinalIgnoreCase))
                {
                    if (currentQuestion != null)
                    {
                        string answer =
                            text.Substring(7).Trim();

                        currentQuestion.CorrectAnswer =
                            ConvertAnswerForQuestion(
                                currentQuestion,
                                answer);
                    }

                    continue;
                }


                // =====================================================
                // INLINE CORRECT ANSWER
                // =====================================================

                if (text.StartsWith(
                    "CORRECT ANSWER:",
                    StringComparison.OrdinalIgnoreCase))
                {
                    if (currentQuestion != null)
                    {
                        string answer =
                            text.Substring(15).Trim();

                        currentQuestion.CorrectAnswer =
                            ConvertAnswerForQuestion(
                                currentQuestion,
                                answer);
                    }

                    continue;
                }


                // =====================================================
                // TYPE CLUE
                // =====================================================

                if (currentQuestion != null)
                {
                    string detectedFromLine =
                        DetectTypeFromClue(text);

                    if (!string.IsNullOrEmpty(
                        detectedFromLine))
                    {
                        currentQuestion.QuestionType =
                            detectedFromLine;

                        SetDefaultChoicesForType(
                            currentQuestion,
                            detectedFromLine);
                    }
                }
            }


            // =========================================================
            // SAVE LAST QUESTION
            // =========================================================

            if (currentQuestion != null)
            {
                FinalizeQuestion(
                    currentQuestion);

                if (!result.Questions.Contains(
                    currentQuestion))
                {
                    result.Questions.Add(
                        currentQuestion);
                }
            }


            // =========================================================
            // SECOND PASS
            // READ ANSWER KEY
            //
            // It doesn't matter which page it is on.
            // =========================================================

            ReadAnswerKey(
                paragraphs,
                questionMap);


            // =========================================================
            // FINAL VALIDATION/CLEANUP
            // =========================================================

            foreach (QuizQuestion question
                     in result.Questions)
            {
                FinalizeQuestion(question);
            }


            return result;
        }


        // =============================================================
        // READ ANSWER KEY
        // =============================================================

        private static void ReadAnswerKey(
            List<string> paragraphs,
            Dictionary<int, QuizQuestion> questionMap)
        {
            bool answerKeyMode = false;


            foreach (string text in paragraphs)
            {
                // -----------------------------------------------------
                // START ANSWER KEY
                // -----------------------------------------------------

                if (IsAnswerKeyHeading(text))
                {
                    answerKeyMode = true;
                    continue;
                }


                if (!answerKeyMode)
                    continue;


                // -----------------------------------------------------
                // Ignore empty
                // -----------------------------------------------------

                if (string.IsNullOrWhiteSpace(text))
                    continue;


                // -----------------------------------------------------
                // PARSE:
                //
                // 1. B
                // 2. C
                // 3. Authentication
                //
                // 1) B
                // 2) C
                //
                // 1 - B
                // 2: C
                //
                // 1. Answer: B
                // -----------------------------------------------------

                int number;
                string answer;


                if (!TryParseAnswerKeyLine(
                    text,
                    out number,
                    out answer))
                {
                    continue;
                }


                QuizQuestion question;


                if (!questionMap.TryGetValue(
                    number,
                    out question))
                {
                    continue;
                }


                // -----------------------------------------------------
                // IMPORTANT:
                //
                // Multiple Choice:
                // Convert full answer to A/B/C/D.
                //
                // Identification:
                // Keep actual answer.
                //
                // True/False:
                // Convert to TRUE/FALSE.
                //
                // Essay:
                // Keep answer text.
                // -----------------------------------------------------

                question.CorrectAnswer =
                    ConvertAnswerForQuestion(
                        question,
                        answer);
            }
        }


        // =============================================================
        // CONVERT ANSWER FOR QUESTION
        // =============================================================

        private static string ConvertAnswerForQuestion(
            QuizQuestion question,
            string answer)
        {
            if (question == null)
                return "";


            if (string.IsNullOrWhiteSpace(
                answer))
            {
                return "";
            }


            answer =
                RemoveAnswerPrefix(answer);


            // =========================================================
            // MULTIPLE CHOICE
            // =========================================================

            if (question.QuestionType ==
                "multiple_choice")
            {
                return MatchAnswerToMultipleChoice(
                    question,
                    answer);
            }


            // =========================================================
            // TRUE / FALSE
            // =========================================================

            if (question.QuestionType ==
                "true_false")
            {
                return CleanTrueFalseAnswer(
                    answer);
            }


            // =========================================================
            // IDENTIFICATION
            // =========================================================

            if (question.QuestionType ==
                "identification")
            {
                return answer.Trim();
            }


            // =========================================================
            // ESSAY
            // =========================================================

            if (question.QuestionType ==
                "essay")
            {
                return answer.Trim();
            }


            return answer.Trim();
        }


        // =============================================================
        // MATCH MULTIPLE CHOICE ANSWER
        //
        // Supports:
        //
        // B
        // B.
        // B)
        // B - answer
        // B. answer
        // Full answer:
        // Confidentiality
        // =============================================================

        private static string MatchAnswerToMultipleChoice(
            QuizQuestion question,
            string answer)
        {
            if (question == null)
                return "";


            if (string.IsNullOrWhiteSpace(
                answer))
            {
                return "";
            }


            string value =
                answer.Trim();


            // ---------------------------------------------------------
            // Remove common punctuation
            // ---------------------------------------------------------

            string simple =
                value.Trim(
                    '.',
                    ')',
                    ':',
                    '-')
                .Trim();


            // ---------------------------------------------------------
            // Exact letter
            // ---------------------------------------------------------

            if (simple.Equals(
                "A",
                StringComparison.OrdinalIgnoreCase))
            {
                return "A";
            }


            if (simple.Equals(
                "B",
                StringComparison.OrdinalIgnoreCase))
            {
                return "B";
            }


            if (simple.Equals(
                "C",
                StringComparison.OrdinalIgnoreCase))
            {
                return "C";
            }


            if (simple.Equals(
                "D",
                StringComparison.OrdinalIgnoreCase))
            {
                return "D";
            }


            // ---------------------------------------------------------
            // "C. Confidentiality"
            // "C) Confidentiality"
            // "C - Confidentiality"
            // ---------------------------------------------------------

            Match choiceMatch =
                Regex.Match(
                    value,
                    @"^([ABCDabcd])\s*[\.\)\:\-]\s*(.+)$");


            if (choiceMatch.Success)
            {
                return choiceMatch
                    .Groups[1]
                    .Value
                    .ToUpper();
            }


            // ---------------------------------------------------------
            // "C Confidentiality"
            // ---------------------------------------------------------

            choiceMatch =
                Regex.Match(
                    value,
                    @"^([ABCDabcd])\s+(.+)$");


            if (choiceMatch.Success)
            {
                return choiceMatch
                    .Groups[1]
                    .Value
                    .ToUpper();
            }


            // ---------------------------------------------------------
            // FULL ANSWER MATCHING
            //
            // Example:
            //
            // A. Confidentiality
            // B. Authentication
            // C. Authorization
            // D. Accounting
            //
            // Answer Key:
            //
            // 2. Confidentiality
            //
            // Result:
            //
            // A
            // ---------------------------------------------------------

            if (SameAnswer(
                value,
                question.ChoiceA))
            {
                return "A";
            }


            if (SameAnswer(
                value,
                question.ChoiceB))
            {
                return "B";
            }


            if (SameAnswer(
                value,
                question.ChoiceC))
            {
                return "C";
            }


            if (SameAnswer(
                value,
                question.ChoiceD))
            {
                return "D";
            }


            // ---------------------------------------------------------
            // No matching choice found.
            //
            // Return original value instead of incorrectly assuming
            // its first letter is the answer.
            // ---------------------------------------------------------

            return value.Trim();
        }


        // =============================================================
        // COMPARE ANSWERS
        // =============================================================

        private static bool SameAnswer(
            string answer1,
            string answer2)
        {
            if (string.IsNullOrWhiteSpace(
                answer1) ||
                string.IsNullOrWhiteSpace(
                answer2))
            {
                return false;
            }


            string a =
                NormalizeAnswerText(answer1);


            string b =
                NormalizeAnswerText(answer2);


            return a.Equals(
                b,
                StringComparison.OrdinalIgnoreCase);
        }


        // =============================================================
        // NORMALIZE ANSWER TEXT
        // =============================================================

        private static string NormalizeAnswerText(
            string text)
        {
            if (string.IsNullOrWhiteSpace(
                text))
            {
                return "";
            }


            string value =
                text.Trim();


            // Remove A. / A) / A: / A -
            value =
                Regex.Replace(
                    value,
                    @"^[ABCDabcd]\s*[\.\)\:\-]\s*",
                    "");


            // Remove extra spaces
            value =
                Regex.Replace(
                    value,
                    @"\s+",
                    " ");


            // Remove ending punctuation
            value =
                value.Trim(
                    '.',
                    ')',
                    ':',
                    '-')
                .Trim();


            return value.ToLower();
        }


        // =============================================================
        // REMOVE ANSWER PREFIX
        // =============================================================

        private static string RemoveAnswerPrefix(
            string answer)
        {
            if (string.IsNullOrWhiteSpace(
                answer))
            {
                return "";
            }


            string value =
                answer.Trim();


            if (value.StartsWith(
                "ANSWER:",
                StringComparison.OrdinalIgnoreCase))
            {
                value =
                    value.Substring(7).Trim();
            }


            if (value.StartsWith(
                "CORRECT ANSWER:",
                StringComparison.OrdinalIgnoreCase))
            {
                value =
                    value.Substring(15).Trim();
            }


            return value;
        }


        // =============================================================
        // TRUE/FALSE ANSWER
        // =============================================================

        private static string CleanTrueFalseAnswer(
            string answer)
        {
            if (string.IsNullOrWhiteSpace(
                answer))
            {
                return "";
            }


            string value =
                answer.Trim()
                .ToUpper();


            value =
                value.Trim(
                    '.',
                    ')',
                    ':',
                    '-')
                .Trim();


            if (value == "TRUE" ||
                value == "T")
            {
                return "TRUE";
            }


            if (value == "FALSE" ||
                value == "F")
            {
                return "FALSE";
            }


            return value;
        }


        // =============================================================
        // ANSWER KEY HEADING
        // =============================================================

        private static bool IsAnswerKeyHeading(
            string text)
        {
            if (string.IsNullOrWhiteSpace(
                text))
            {
                return false;
            }


            string value =
                text.Trim()
                .ToUpper();


            value =
                value.Trim(
                    ':',
                    '-',
                    ' ');


            if (value == "ANSWER KEY")
                return true;


            if (value == "ANSWERKEY")
                return true;


            if (value == "ANSWER KEYS")
                return true;


            if (value == "ANSWER SHEET")
                return true;


            if (value == "ANSWERS")
                return true;


            return false;
        }


        // =============================================================
        // PARSE ANSWER KEY LINE
        // =============================================================

        private static bool TryParseAnswerKeyLine(
            string text,
            out int number,
            out string answer)
        {
            number = 0;
            answer = "";


            if (string.IsNullOrWhiteSpace(
                text))
            {
                return false;
            }


            string value =
                text.Trim();


            // ---------------------------------------------------------
            // Supported:
            //
            // 1. B
            // 1) B
            // 1: B
            // 1 - B
            // ---------------------------------------------------------

            Match match =
                Regex.Match(
                    value,
                    @"^(\d+)\s*[\.\)\:\-]\s*(.+)$");


            if (!match.Success)
                return false;


            if (!int.TryParse(
                match.Groups[1].Value,
                out number))
            {
                return false;
            }


            answer =
                match.Groups[2]
                .Value
                .Trim();


            if (string.IsNullOrWhiteSpace(
                answer))
            {
                return false;
            }


            return true;
        }


        // =============================================================
        // CHECK ANSWER-ONLY LINE
        // =============================================================

        private static bool LooksLikeAnswerOnly(
            string text)
        {
            if (string.IsNullOrWhiteSpace(
                text))
            {
                return false;
            }


            Match match =
                Regex.Match(
                    text.Trim(),
                    @"^(\d+)\s*[\.\)\:\-]\s*(.+)$");


            if (!match.Success)
                return false;


            string value =
                match.Groups[2]
                .Value
                .Trim();


            // Short answer
            if (value.Length <= 3)
                return true;


            // Explicit answer
            if (value.StartsWith(
                "ANSWER:",
                StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }


            if (value.StartsWith(
                "CORRECT ANSWER:",
                StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }


            return false;
        }


        // =============================================================
        // FINALIZE QUESTION
        // =============================================================

        private static void FinalizeQuestion(
            QuizQuestion question)
        {
            if (question == null)
                return;


            // ---------------------------------------------------------
            // If A-D exist, definitely Multiple Choice.
            // ---------------------------------------------------------

            if (!string.IsNullOrWhiteSpace(
                    question.ChoiceA) &&
                !string.IsNullOrWhiteSpace(
                    question.ChoiceB) &&
                !string.IsNullOrWhiteSpace(
                    question.ChoiceC) &&
                !string.IsNullOrWhiteSpace(
                    question.ChoiceD))
            {
                question.QuestionType =
                    "multiple_choice";
            }


            // ---------------------------------------------------------
            // TRUE / FALSE
            // ---------------------------------------------------------

            if (question.QuestionType ==
                "true_false")
            {
                question.ChoiceA =
                    "TRUE";

                question.ChoiceB =
                    "FALSE";

                question.ChoiceC =
                    "";

                question.ChoiceD =
                    "";
            }


            // ---------------------------------------------------------
            // CLEAN FINAL ANSWER
            // ---------------------------------------------------------

            if (!string.IsNullOrWhiteSpace(
                question.CorrectAnswer))
            {
                question.CorrectAnswer =
                    ConvertAnswerForQuestion(
                        question,
                        question.CorrectAnswer);
            }
        }


        // =============================================================
        // DETECT SECTION HEADING
        // =============================================================

        private static string DetectSectionHeading(
            string text)
        {
            if (string.IsNullOrWhiteSpace(
                text))
            {
                return "";
            }


            string value =
                text.Trim()
                .ToUpper();


            value =
                RemoveHeadingNumber(value);


            // ---------------------------------------------------------
            // MULTIPLE CHOICE
            // ---------------------------------------------------------

            if (value == "MULTIPLE CHOICE" ||
                value == "MULTIPLE CHOICE QUESTIONS" ||
                value == "MULTIPLE CHOICE QUESTION" ||
                value == "MC")
            {
                return "multiple_choice";
            }


            // ---------------------------------------------------------
            // TRUE / FALSE
            // ---------------------------------------------------------

            if (value == "TRUE OR FALSE" ||
                value == "TRUE/FALSE" ||
                value == "TRUE - FALSE" ||
                value == "TRUE-FALSE" ||
                value == "TRUE FALSE" ||
                value == "TRUE OR FALSE QUESTIONS" ||
                value == "TRUE/FALSE QUESTIONS" ||
                value == "TF")
            {
                return "true_false";
            }


            // ---------------------------------------------------------
            // IDENTIFICATION
            // ---------------------------------------------------------

            if (value == "IDENTIFICATION" ||
                value == "IDENTIFICATION QUESTIONS" ||
                value == "IDENTIFICATION QUESTION" ||
                value == "IDENTIFY" ||
                value == "ID" ||
                value == "FILL IN THE BLANK" ||
                value == "FILL-IN-THE-BLANK" ||
                value == "FILL IN THE BLANKS" ||
                value == "FILL-IN-THE-BLANKS")
            {
                return "identification";
            }


            // ---------------------------------------------------------
            // ESSAY
            // ---------------------------------------------------------

            if (value == "ESSAY" ||
                value == "ESSAY QUESTIONS" ||
                value == "ESSAY QUESTION")
            {
                return "essay";
            }


            return "";
        }


        // =============================================================
        // DETECT TYPE FROM QUESTION TEXT
        // =============================================================

        private static string DetectQuestionTypeFromText(
            string question,
            string sectionType)
        {
            // Section heading has priority.
            if (!string.IsNullOrWhiteSpace(
                sectionType))
            {
                return sectionType;
            }


            if (string.IsNullOrWhiteSpace(
                question))
            {
                return "identification";
            }


            string value =
                question.Trim()
                .ToUpper();


            // ---------------------------------------------------------
            // IDENTIFICATION
            // ---------------------------------------------------------

            if (value.Contains(
                    "IDENTIFY THE") ||
                value.Contains(
                    "IDENTIFY THIS") ||
                value.Contains(
                    "IDENTIFY THE FOLLOWING") ||
                value.Contains(
                    "WHAT TERM") ||
                value.Contains(
                    "NAME THE") ||
                value.Contains(
                    "FILL IN THE BLANK") ||
                value.Contains(
                    "FILL IN THE BLANKS"))
            {
                return "identification";
            }


            // ---------------------------------------------------------
            // ESSAY
            // ---------------------------------------------------------

            if (value.StartsWith("EXPLAIN ") ||
                value.StartsWith("DISCUSS ") ||
                value.StartsWith("DESCRIBE ") ||
                value.StartsWith("ANALYZE ") ||
                value.StartsWith("ELABORATE ") ||
                value.StartsWith("WHY ") ||
                value.StartsWith("HOW ") ||
                value.Contains("IN YOUR OWN WORDS") ||
                value.Contains("ESSAY"))
            {
                return "essay";
            }


            // ---------------------------------------------------------
            // TRUE / FALSE
            // ---------------------------------------------------------

            if (value.Contains(
                    "TRUE OR FALSE") ||
                value.Contains(
                    "TRUE/FALSE"))
            {
                return "true_false";
            }


            // ---------------------------------------------------------
            // UNKNOWN
            //
            // If A-D are later found, FinalizeQuestion changes
            // it automatically to multiple_choice.
            // ---------------------------------------------------------

            return "identification";
        }


        // =============================================================
        // DETECT TYPE FROM CLUE
        // =============================================================

        private static string DetectTypeFromClue(
            string text)
        {
            if (string.IsNullOrWhiteSpace(
                text))
            {
                return "";
            }


            string value =
                text.Trim()
                .ToUpper();


            if (value == "IDENTIFICATION")
                return "identification";


            if (value == "ESSAY")
                return "essay";


            if (value == "MULTIPLE CHOICE")
                return "multiple_choice";


            if (value == "TRUE OR FALSE" ||
                value == "TRUE/FALSE")
            {
                return "true_false";
            }


            return "";
        }


        // =============================================================
        // SET DEFAULT CHOICES
        // =============================================================

        private static void SetDefaultChoicesForType(
            QuizQuestion question,
            string type)
        {
            if (question == null)
                return;


            if (type == "true_false")
            {
                question.ChoiceA =
                    "TRUE";

                question.ChoiceB =
                    "FALSE";

                question.ChoiceC =
                    "";

                question.ChoiceD =
                    "";
            }


            if (type == "identification")
            {
                question.ChoiceA = "";
                question.ChoiceB = "";
                question.ChoiceC = "";
                question.ChoiceD = "";
            }


            if (type == "essay")
            {
                question.ChoiceA = "";
                question.ChoiceB = "";
                question.ChoiceC = "";
                question.ChoiceD = "";
            }
        }


        // =============================================================
        // CONVERT QUESTION TYPE
        // =============================================================

        private static string ConvertQuestionType(
            string type)
        {
            if (string.IsNullOrWhiteSpace(
                type))
            {
                return "identification";
            }


            type =
                type.Trim()
                .ToUpper();


            // ---------------------------------------------------------
            // MULTIPLE CHOICE
            // ---------------------------------------------------------

            if (type == "MULTIPLE CHOICE" ||
                type == "MULTIPLE_CHOICE" ||
                type == "MULTIPLECHOICE" ||
                type == "MC")
            {
                return "multiple_choice";
            }


            // ---------------------------------------------------------
            // TRUE / FALSE
            // ---------------------------------------------------------

            if (type == "TRUE OR FALSE" ||
                type == "TRUE/FALSE" ||
                type == "TRUE_FALSE" ||
                type == "TRUE-FALSE" ||
                type == "TRUEFALSE" ||
                type == "TRUE OR FALSE QUESTION" ||
                type == "TF")
            {
                return "true_false";
            }


            // ---------------------------------------------------------
            // IDENTIFICATION
            // ---------------------------------------------------------

            if (type == "IDENTIFICATION" ||
                type == "IDENTIFICATION QUESTION" ||
                type == "IDENTIFICATION_QUESTION" ||
                type == "IDENTIFY" ||
                type == "ID" ||
                type == "FILL IN THE BLANK" ||
                type == "FILL_IN_THE_BLANK" ||
                type == "FILL-IN-THE-BLANK" ||
                type == "FILLINTHEBLANK")
            {
                return "identification";
            }


            // ---------------------------------------------------------
            // ESSAY
            // ---------------------------------------------------------

            if (type == "ESSAY" ||
                type == "ESSAY QUESTION" ||
                type == "ESSAY_QUESTION")
            {
                return "essay";
            }


            return "identification";
        }


        // =============================================================
        // QUESTION NUMBER
        // =============================================================

        private static bool TryGetQuestionNumber(
            string text,
            out int number)
        {
            number = 0;


            if (string.IsNullOrWhiteSpace(
                text))
            {
                return false;
            }


            Match match =
                Regex.Match(
                    text.Trim(),
                    @"^(\d+)\s*[\.\)]\s+(.+)$");


            if (!match.Success)
                return false;


            return int.TryParse(
                match.Groups[1].Value,
                out number);
        }


        // =============================================================
        // REMOVE QUESTION NUMBER
        // =============================================================

        private static string RemoveQuestionNumber(
            string text)
        {
            if (string.IsNullOrWhiteSpace(
                text))
            {
                return "";
            }


            Match match =
                Regex.Match(
                    text.Trim(),
                    @"^\d+\s*[\.\)]\s*(.+)$");


            if (match.Success)
            {
                return match.Groups[1]
                    .Value
                    .Trim();
            }


            return text.Trim();
        }


        // =============================================================
        // CHECK CHOICE
        // =============================================================

        private static bool StartsWithChoice(
            string text,
            string letter)
        {
            if (string.IsNullOrWhiteSpace(
                text))
            {
                return false;
            }


            string upper =
                text.Trim()
                .ToUpper();


            string l =
                letter.ToUpper();


            if (upper == l)
                return true;


            if (upper.StartsWith(
                l + "."))
                return true;


            if (upper.StartsWith(
                l + ")"))
                return true;


            if (upper.StartsWith(
                l + " -"))
                return true;


            if (upper.StartsWith(
                l + " "))
                return true;


            return false;
        }


        // =============================================================
        // REMOVE CHOICE LETTER
        // =============================================================

        private static string RemoveChoiceLetter(
            string text,
            string letter)
        {
            string value =
                text.Trim();


            if (value.Length == 1)
                return "";


            value =
                value.Substring(1)
                .Trim();


            if (value.StartsWith("."))
            {
                value =
                    value.Substring(1)
                    .Trim();
            }


            if (value.StartsWith(")"))
            {
                value =
                    value.Substring(1)
                    .Trim();
            }


            if (value.StartsWith("-"))
            {
                value =
                    value.Substring(1)
                    .Trim();
            }


            return value.Trim();
        }


        // =============================================================
        // TRUE / FALSE CHOICE
        // =============================================================

        private static bool IsTrueFalseChoice(
            string text)
        {
            if (string.IsNullOrWhiteSpace(
                text))
            {
                return false;
            }


            string value =
                text.Trim()
                .ToUpper();


            if (value == "TRUE" ||
                value == "T" ||
                value == "TRUE." ||
                value == "TRUE)")
            {
                return true;
            }


            if (value == "FALSE" ||
                value == "F" ||
                value == "FALSE." ||
                value == "FALSE)")
            {
                return true;
            }


            return false;
        }


        // =============================================================
        // REMOVE HEADING NUMBER
        // =============================================================

        private static string RemoveHeadingNumber(
            string text)
        {
            if (string.IsNullOrWhiteSpace(
                text))
            {
                return "";
            }


            string value =
                text.Trim();


            // ---------------------------------------------------------
            // 1 MULTIPLE CHOICE
            // ---------------------------------------------------------

            Match numberMatch =
                Regex.Match(
                    value,
                    @"^\d+\s+(.+)$");


            if (numberMatch.Success)
            {
                return numberMatch.Groups[1]
                    .Value
                    .Trim();
            }


            // ---------------------------------------------------------
            // PART I – MULTIPLE CHOICE
            // ---------------------------------------------------------

            int dashIndex =
                value.IndexOf('–');


            if (dashIndex >= 0 &&
                dashIndex < value.Length - 1)
            {
                return value
                    .Substring(
                        dashIndex + 1)
                    .Trim();
            }


            // ---------------------------------------------------------
            // PART I - MULTIPLE CHOICE
            // ---------------------------------------------------------

            dashIndex =
                value.IndexOf('-');


            if (dashIndex >= 0 &&
                dashIndex < value.Length - 1)
            {
                return value
                    .Substring(
                        dashIndex + 1)
                    .Trim();
            }


            return value;
        }
    }
}