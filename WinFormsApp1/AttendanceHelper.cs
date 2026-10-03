using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using MySql.Data.MySqlClient;

namespace WinFormsApp1
{
    public static class AttendanceHelper
    {
        // Grace period bago mag-klase (5 minuto)
        private const int EARLY_GRACE_MINUTES = 5;

        // Grace period pagkatapos mag-klase (15 minuto)
        private const int LATE_GRACE_MINUTES = 15;

        public static void MarkStudentPresentOnLogin(int studentUserId)
        {
            string connStr = SettingsManager.Current.GetConnectionString();
            DateTime now = DateTime.Now;
            string todayName = now.ToString("dddd", CultureInfo.InvariantCulture); // "Saturday"
            string dateCol = now.ToString("MMMdd", CultureInfo.InvariantCulture);  // "Oct04"

            try
            {
                using (var conn = new MySqlConnection(connStr))
                {
                    conn.Open();

                    EnsureAttendanceColumn(conn, dateCol);

                    // 1. Kunin ang lahat ng classes ng student na TODAY
                    //    (class_date = today's day name)
                    var enrollments = GetTodaysClasses(conn, studentUserId, todayName);

                    if (enrollments.Count == 0)
                    {
                        Console.WriteLine($"[Attendance] Walang klase ngayong {todayName} para sa student {studentUserId}.");
                        // Optional: kung gusto mong i-mark na Absent pa rin, i-uncomment ito:
                        // MarkAsAbsent(conn, studentUserId, dateCol);
                        return;
                    }

                    // 2. Para sa bawat class, i-check ang oras
                    foreach (var e in enrollments)
                    {
                        string status = DetermineStatus(now, e.ClassTime);

                        if (status == null)
                        {
                            Console.WriteLine($"[Attendance] Hindi oras ng klase ({e.ClassTime}). Skipping.");
                            continue;
                        }

                        ApplyAttendance(conn, studentUserId, e.StudentName, dateCol, status);
                        Console.WriteLine($"[Attendance] Student {studentUserId} → {status} ({e.ClassTime})");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("[Attendance] MarkStudentPresentOnLogin error: " + ex.Message);
            }
        }

        /// <summary>
        /// Kunin ang mga class ng student na may class_date = today.
        /// </summary>
        private static List<(int ProfessorId, string Section, string StudentName, string ClassTime)>
            GetTodaysClasses(MySqlConnection conn, int studentUserId, string todayName)
        {
            var list = new List<(int, string, string, string)>();

            string query = @"
                SELECT DISTINCT
                    sc.professor_id,
                    sc.section,
                    sc.class_time,
                    CONCAT(ui.lastname, ' ', ui.firstname, ' ', IFNULL(ui.middlename, '')) AS student_name
                FROM student_class sc
                INNER JOIN user_information ui ON ui.user_id = sc.user_id
                WHERE sc.user_id = @student_id
                  AND LOWER(TRIM(sc.class_date)) = LOWER(TRIM(@today))";

            using (var cmd = new MySqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@student_id", studentUserId);
                cmd.Parameters.AddWithValue("@today", todayName);

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        list.Add((
                            Convert.ToInt32(reader["professor_id"]),
                            reader["section"]?.ToString()?.Trim() ?? "",
                            reader["student_name"]?.ToString()?.Trim() ?? "",
                            reader["class_time"]?.ToString()?.Trim() ?? ""
                        ));
                    }
                }
            }

            return list;
        }

        /// <summary>
        /// Tukuyin kung Present, Late, o null (walang mark) base sa oras.
        /// </summary>
        private static string DetermineStatus(DateTime now, string classTimeRange)
        {
            if (string.IsNullOrWhiteSpace(classTimeRange))
                return "Present"; // walang time = assume Present

            // Parse ang "12:00 pm - 3:30 pm" format
            var range = ParseTimeRange(classTimeRange);
            if (range == null)
                return "Present"; // hindi ma-parse = assume Present

            var (start, end) = range.Value;

            // Compute actual start/end with grace periods
            DateTime actualStart = now.Date.Add(start).AddMinutes(-EARLY_GRACE_MINUTES);
            DateTime actualEnd = now.Date.Add(end).AddMinutes(LATE_GRACE_MINUTES);

            if (now < actualStart)
                return null; // masyadong maaga, walang mark

            if (now <= actualEnd)
                return "Present"; // nasa oras ng klase

            return "Late"; // huli na
        }

        /// <summary>
        /// Parse ang "12:00 pm - 3:30 pm" → (TimeSpan start, TimeSpan end)
        /// </summary>
        private static (TimeSpan start, TimeSpan end)? ParseTimeRange(string range)
        {
            try
            {
                // Regex match: "12:00 pm - 3:30 pm"
                var match = Regex.Match(range,
                    @"(\d{1,2}):(\d{2})\s*(am|pm)\s*-\s*(\d{1,2}):(\d{2})\s*(am|pm)",
                    RegexOptions.IgnoreCase);

                if (!match.Success) return null;

                int startHour = int.Parse(match.Groups[1].Value);
                int startMin = int.Parse(match.Groups[2].Value);
                string startAmPm = match.Groups[3].Value.ToLower();

                int endHour = int.Parse(match.Groups[4].Value);
                int endMin = int.Parse(match.Groups[5].Value);
                string endAmPm = match.Groups[6].Value.ToLower();

                // Convert to 24-hour
                if (startAmPm == "pm" && startHour < 12) startHour += 12;
                if (startAmPm == "am" && startHour == 12) startHour = 0;

                if (endAmPm == "pm" && endHour < 12) endHour += 12;
                if (endAmPm == "am" && endHour == 12) endHour = 0;

                return (new TimeSpan(startHour, startMin, 0), new TimeSpan(endHour, endMin, 0));
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// I-apply ang attendance status sa database (may anti-double-count).
        /// </summary>
        private static void ApplyAttendance(MySqlConnection conn, int studentId, string studentName,
            string dateCol, string status)
        {
            string insertIfMissing = @"INSERT INTO professor_attendance_new 
                                 (student_id, professor_id, student_name, section, present, absent, late)
                               SELECT @sid, @pid, @name, @section, 0, 0, 0
                               FROM DUAL
                               WHERE NOT EXISTS (
                                   SELECT 1 FROM professor_attendance_new 
                                   WHERE student_id = @sid AND professor_id = @pid
                               )";
            using (var cmd = new MySqlCommand(insertIfMissing, conn))
            {
                cmd.Parameters.AddWithValue("@sid", studentId);
                cmd.Parameters.AddWithValue("@name", studentName);
                cmd.ExecuteNonQuery();
            }

            // Kunin ang existing status
            string existing = null;
            using (var getCmd = new MySqlCommand(
                $"SELECT `{dateCol}` FROM professor_attendance WHERE student_id = @sid", conn))
            {
                getCmd.Parameters.AddWithValue("@sid", studentId);
                object r = getCmd.ExecuteScalar();
                existing = (r == null || r == DBNull.Value) ? null : r.ToString();
            }

            // Kung may existing na, huwag nang galawin (auto-login lang naman ito)
            if (!string.IsNullOrEmpty(existing))
                return;

            // I-set ang status
            string col = status == "Present" ? "present"
                       : status == "Absent" ? "absent"
                       : "late";

            string update = $@"UPDATE professor_attendance 
                               SET `{dateCol}` = @status,
                                   {col} = COALESCE({col}, 0) + 1
                               WHERE student_id = @sid";

            using (var cmd = new MySqlCommand(update, conn))
            {
                cmd.Parameters.AddWithValue("@status", status);
                cmd.Parameters.AddWithValue("@sid", studentId);
                cmd.ExecuteNonQuery();
            }
        }

        private static void EnsureAttendanceColumn(MySqlConnection conn, string columnName)
        {
            var builder = new MySqlConnectionStringBuilder(conn.ConnectionString);
            string dbName = builder.Database;

            string checkQuery = @"SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS 
                                  WHERE TABLE_SCHEMA = @db 
                                  AND TABLE_NAME = 'professor_attendance' 
                                  AND COLUMN_NAME = @col";

            using (var cmd = new MySqlCommand(checkQuery, conn))
            {
                cmd.Parameters.AddWithValue("@db", dbName);
                cmd.Parameters.AddWithValue("@col", columnName);

                bool exists = Convert.ToInt32(cmd.ExecuteScalar()) > 0;
                if (!exists)
                {
                    string alter = $"ALTER TABLE professor_attendance ADD `{columnName}` VARCHAR(20)";
                    using (var alterCmd = new MySqlCommand(alter, conn))
                        alterCmd.ExecuteNonQuery();
                }
            }
        }
    }
}