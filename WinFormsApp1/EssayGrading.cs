using System;
using MySql.Data.MySqlClient;

namespace WinFormsApp1
{
    public static class EssayGrading
    {
        public static void SaveEssayGrade(int answerId, decimal points, bool release)
        {
            string connStr = SettingsManager.Current.GetConnectionString();

            using (var conn = new MySqlConnection(connStr))
            {
                conn.Open();
                using (var tx = conn.BeginTransaction())
                {
                    using (var cmd = new MySqlCommand(@"
                        UPDATE student_answers
                        SET points_earned = @pts, needs_grading = 0
                        WHERE answer_id = @aid", conn, tx))
                    {
                        cmd.Parameters.AddWithValue("@pts", points);
                        cmd.Parameters.AddWithValue("@aid", answerId);
                        cmd.ExecuteNonQuery();
                    }

                    int attemptId;
                    using (var cmd = new MySqlCommand(
                        "SELECT attempt_id FROM student_answers WHERE answer_id = @aid", conn, tx))
                    {
                        cmd.Parameters.AddWithValue("@aid", answerId);
                        attemptId = Convert.ToInt32(cmd.ExecuteScalar());
                    }

                    Recalculate(conn, tx, attemptId, release);
                    tx.Commit();
                }
            }
        }

        private static void Recalculate(MySqlConnection conn, MySqlTransaction tx, int attemptId, bool release)
        {
            int pending;
            using (var cmd = new MySqlCommand(
                "SELECT COUNT(*) FROM student_answers WHERE attempt_id = @id AND needs_grading = 1", conn, tx))
            {
                cmd.Parameters.AddWithValue("@id", attemptId);
                pending = Convert.ToInt32(cmd.ExecuteScalar());
            }

            // Score = tamang auto-graded (x points) + essay points_earned
            // Total = kabuuang points ng quiz
            using (var cmd = new MySqlCommand(@"
                UPDATE quiz_attempts qa
                SET
                  qa.score = (
                    SELECT COALESCE(SUM(CASE
                             WHEN q.question_type = 'essay' THEN COALESCE(sa.points_earned, 0)
                             WHEN sa.is_correct = 1 THEN q.points
                             ELSE 0 END), 0)
                    FROM student_answers sa
                    JOIN questions q ON q.question_id = sa.question_id
                    WHERE sa.attempt_id = qa.attempt_id),
                  qa.total_questions = (
                    SELECT COALESCE(SUM(q.points), 0)
                    FROM questions q
                    WHERE q.quiz_id = qa.quiz_id),
                  qa.grading_status = @status,
                  qa.is_released = @rel
                WHERE qa.attempt_id = @id", conn, tx))
            {
                cmd.Parameters.AddWithValue("@status", pending == 0 ? "graded" : "pending");
                cmd.Parameters.AddWithValue("@rel", (pending == 0 && release) ? 1 : 0);
                cmd.Parameters.AddWithValue("@id", attemptId);
                cmd.ExecuteNonQuery();
            }

            using (var cmd = new MySqlCommand(@"
                UPDATE quiz_attempts
                SET percentage = ROUND(score * 100 / NULLIF(total_questions, 0), 2)
                WHERE attempt_id = @id", conn, tx))
            {
                cmd.Parameters.AddWithValue("@id", attemptId);
                cmd.ExecuteNonQuery();
            }
        }
    }
}