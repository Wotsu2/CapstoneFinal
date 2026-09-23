using DevExpress.XtraBars.Navigation;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.StartPanel;

namespace WinFormsApp1
{
    public partial class QandAForm : Form
    {
        private string StudentUsername;
        private int UserId;
        private string Section;
        private string question;
        private string answer;

        private string Afirst;
        private string Asecond;
        private string Athird;

        public QandAForm(int Userid, string section, string Username)
        {
            InitializeComponent();

            StudentUsername = Username;
            UserId = Userid;
            Section = section;
            InitializeGetQuestioner();
        }

        private void InitializeGetQuestioner()
        {
            string connStr = SettingsManager.Current.GetConnectionString();
            try
            {
                using (var conn = new MySqlConnection(connStr))
                {
                    conn.Open();
                    string query = "SELECT question, answer FROM question_answer_security WHERE username = @username";
                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@username", StudentUsername);
                        using (var reader = cmd.ExecuteReader())
                        {
                            var questions = new List<string>();
                            var answer = new List<string>();

                            while (reader.Read())
                            {
                                string q = reader.GetString("question");
                                questions.Add(q);
                                string a = reader.IsDBNull(reader.GetOrdinal("answer"))
                                    ? ""
                                    : reader.GetString("answer");

                                answer.Add(a);
                            }

                            Afirst = answer[0];
                            Asecond = answer[1];
                            Athird = answer[2];

                            cmbFirstQuestion.Items.Add(questions[0]);
                            cmbSecondQuestion.Items.Add(questions[1]);
                            cmbThirdQuestion.Items.Add(questions[2]);
                            cmbFirstQuestion.SelectedIndex = 0;
                            cmbSecondQuestion.SelectedIndex = 0;
                            cmbThirdQuestion.SelectedIndex = 0;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("An error occurred while fetching the question and answer." + ex.Message);
            }
        }

        private void btnSubmitSecurityQuestion_Click(object sender, EventArgs e)
        {
            string connStr = SettingsManager.Current.GetConnectionString();
            try
            {
                using (var conn = new MySqlConnection(connStr))
                {
                    conn.Open();
                    string query = "SELECT question, answer FROM question_answer_security WHERE username = @username";
                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@username", StudentUsername);
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                string answer = reader.GetString("answer");

                                if (string.IsNullOrEmpty(txtFirstAnswer.Text) || string.IsNullOrEmpty(txtSecondAnswer.Text) || string.IsNullOrEmpty(txtThirdAnswer.Text))
                                {
                                    MessageBox.Show("Please Fill the Blank");
                                    return;
                                }

                                if (Afirst == txtFirstAnswer.Text && Asecond == txtSecondAnswer.Text && Athird == txtThirdAnswer.Text)
                                {
                                    this.Hide();

                                    // ✅ Open StudentForm BEFORE closing QandA
                                    StudentForm studentForm = new StudentForm(UserId, Section, StudentUsername);
                                    studentForm.Show();
                                    studentForm.Refresh();
                                    Application.DoEvents();

                                }
                                else
                                {
                                    MessageBox.Show("Please Enter The Correct Answer");
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("An error occurred while fetching the remaining limit." + ex.Message);
            }
        }
    }
}