using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace WinFormsApp1
{
    /// <summary>
    /// Professor calendar na gumagamit ng design ng student calendar
    /// (ExpandedCalendar: orasan, mini month grid na may dots, agenda list).
    ///
    /// Gamit:
    ///   var f = new ProfessorCalendarForm(professorId);
    ///   f.EventOpened += (s, ev) => { /* ev.Kind, ev.Id */ };
    ///   f.ShowDialog();
    /// </summary>
    public partial class ProfessorCalendarForm : Form
    {
        private readonly int professorId;
        private readonly ExpandedCalendar calendar = new ExpandedCalendar();

        public event EventHandler<CalEvent> EventOpened;

        public ProfessorCalendarForm(int profId)
        {
            professorId = profId;

            Text = "My Calendar";
            ClientSize = new Size(340, 720);
            MinimumSize = new Size(356, 500);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.Sizable;
            MaximizeBox = false;
            ShowIcon = false;
            BackColor = Color.FromArgb(30, 36, 46);

            calendar.Dock = DockStyle.Fill;
            calendar.AllClickable = true;                       // tingnan ang note sa ExpandedCalendar
            calendar.CloseRequested += (s, e) => Close();       // "Hide agenda" -> isara
            calendar.EventOpened += (s, ev) => EventOpened?.Invoke(this, ev);
            Controls.Add(calendar);

            Reload();
        }

        // Tawagin ulit kapag may bagong activity / quiz na na-post
        public void Reload()
        {
            var items = new List<CalEvent>();
            items.AddRange(LoadActivities());
            items.AddRange(LoadQuizzes());
            calendar.SetEvents(items);
        }

        // =========================================================
        // DATA
        // =========================================================
        private List<CalEvent> LoadActivities()
        {
            var list = new List<CalEvent>();
            try
            {
                using (var conn = new MySqlConnection(SettingsManager.Current.GetConnectionString()))
                {
                    conn.Open();

                    const string q = @"SELECT * FROM professor_activity
                                       WHERE professor_id = @prof_id AND due_date IS NOT NULL
                                       ORDER BY due_date ASC";

                    using (var cmd = new MySqlCommand(q, conn))
                    {
                        cmd.Parameters.AddWithValue("@prof_id", professorId);
                        using (var r = cmd.ExecuteReader())
                        {
                            while (r.Read())
                            {
                                DateTime d;
                                if (!TryDate(r, "due_date", out d)) continue;

                                list.Add(new CalEvent
                                {
                                    Id = Int(r, "activity_id"),
                                    Date = d.Date,
                                    Kind = "Activity",
                                    Text = Label(Str(r, "title"), Str(r, "activity_subject")),
                                    Done = d.Date < DateTime.Today      // lampas na ang due date = guwang na bilog
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("LoadActivities: " + ex.Message);
            }
            return list;
        }

        private List<CalEvent> LoadQuizzes()
        {
            var list = new List<CalEvent>();
            try
            {
                using (var conn = new MySqlConnection(SettingsManager.Current.GetConnectionString()))
                {
                    conn.Open();

                    // i-filter sa professor kung may professor_id column ang quizzes
                    bool hasProfCol;
                    using (var chk = new MySqlCommand("SHOW COLUMNS FROM quizzes LIKE 'professor_id'", conn))
                        hasProfCol = chk.ExecuteScalar() != null;

                    string q = "SELECT * FROM quizzes" +
                               (hasProfCol ? " WHERE professor_id = @prof_id" : "") +
                               " ORDER BY created_at ASC";

                    using (var cmd = new MySqlCommand(q, conn))
                    {
                        if (hasProfCol) cmd.Parameters.AddWithValue("@prof_id", professorId);

                        using (var r = cmd.ExecuteReader())
                        {
                            while (r.Read())
                            {
                                DateTime d;
                                if (!TryDate(r, "created_at", out d)) continue;

                                string type = Str(r, "assessment_type");
                                bool isExam = type.IndexOf("exam", StringComparison.OrdinalIgnoreCase) >= 0;

                                list.Add(new CalEvent
                                {
                                    Id = Int(r, "quiz_id"),
                                    Date = d.Date,
                                    Kind = isExam ? "Exam" : "Quiz",
                                    Text = Label(Str(r, "quiz_title"), Str(r, "subject")),
                                    Done = d.Date < DateTime.Today
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("LoadQuizzes: " + ex.Message);
            }
            return list;
        }

        // =========================================================
        // HELPERS
        // =========================================================
        private static string Label(string title, string subject)
        {
            return string.IsNullOrWhiteSpace(subject) ? title : title + " (" + subject + ")";
        }

        private static int Ordinal(MySqlDataReader r, string col)
        {
            for (int i = 0; i < r.FieldCount; i++)
                if (string.Equals(r.GetName(i), col, StringComparison.OrdinalIgnoreCase)) return i;
            return -1;
        }

        private static string Str(MySqlDataReader r, string col)
        {
            int i = Ordinal(r, col);
            return i < 0 || r.IsDBNull(i) ? "" : r.GetValue(i).ToString();
        }

        private static int Int(MySqlDataReader r, string col)
        {
            int i = Ordinal(r, col);
            return i < 0 || r.IsDBNull(i) ? 0 : Convert.ToInt32(r.GetValue(i));
        }

        private static bool TryDate(MySqlDataReader r, string col, out DateTime d)
        {
            d = default(DateTime);
            int i = Ordinal(r, col);
            if (i < 0 || r.IsDBNull(i)) return false;
            object raw = r.GetValue(i);
            if (raw is DateTime dt) { d = dt; return true; }
            return DateTime.TryParse(raw.ToString(), out d);
        }
    }
}