using ClosedXML.Excel;
using Guna.UI2.WinForms;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace WinFormsApp1
{
    public partial class DatabaseManagerForm : Form
    {
        // =========================================================
        // CONTROLS
        // =========================================================
        private ComboBox cmbTables;
        private DataGridView dgvDatabase;
        private Guna2Button btnRefresh;
        private Guna2Button btnAddRow;
        private Guna2Button btnEditRow;
        private Guna2Button btnDeleteRow;
        private Guna2Button btnExport;
        private Guna2Button btnTruncate;
        private Guna2Button btnRunQuery;
        private TextBox txtCustomQuery;
        private Label lblDbStatus;

        private string currentDbTable = "";

        // =========================================================
        // CTOR
        // =========================================================
        public DatabaseManagerForm()
        {
            BuildUi();
            LoadTableList();
        }

        // =========================================================
        // UI
        // =========================================================
        private void BuildUi()
        {
            this.Text = "CDSGA Database Manager";
            this.Size = new Size(1280, 800);
            this.MinimumSize = new Size(1000, 600);
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = Color.FromArgb(245, 245, 248);
            this.ShowIcon = false;

            // ---------------- TOP BAR ----------------
            var topBar = new Guna2Panel
            {
                Dock = DockStyle.Top,
                Height = 110,
                FillColor = Color.White,
                BorderRadius = 0
            };
            this.Controls.Add(topBar);

            var lblTitle = new Label
            {
                Text = "🗄️  Database Manager",
                Font = new Font("Segoe UI Semibold", 20F, FontStyle.Bold),
                ForeColor = Color.Maroon,
                BackColor = Color.Transparent,
                AutoSize = true,
                Location = new Point(24, 18)
            };
            topBar.Controls.Add(lblTitle);

            var lblSub = new Label
            {
                Text = "Browse, edit, add, delete rows — with export and reset utilities",
                Font = new Font("Segoe UI", 9.5F),
                ForeColor = Color.FromArgb(120, 120, 120),
                BackColor = Color.Transparent,
                AutoSize = true,
                Location = new Point(26, 52)
            };
            topBar.Controls.Add(lblSub);

            var lblTable = new Label
            {
                Text = "Table:",
                Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(24, 78),
                ForeColor = Color.FromArgb(80, 80, 80)
            };
            topBar.Controls.Add(lblTable);

            cmbTables = new ComboBox
            {
                Location = new Point(78, 74),
                Size = new Size(260, 30),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 10F)
            };
            cmbTables.SelectedIndexChanged += (s, e) =>
            {
                if (cmbTables.SelectedItem != null)
                    LoadTable(cmbTables.SelectedItem.ToString());
            };
            topBar.Controls.Add(cmbTables);

            // Buttons laid out horizontally
            btnRefresh = MakeTopButton("🔄 Refresh", 360, 72);
            btnRefresh.Click += (s, e) => LoadTable(currentDbTable);
            topBar.Controls.Add(btnRefresh);

            btnAddRow = MakeTopButton("➕ Add Row", 470, 72);
            btnAddRow.FillColor = Color.FromArgb(46, 160, 90);
            btnAddRow.Click += (s, e) => AddRowToTable();
            topBar.Controls.Add(btnAddRow);

            btnEditRow = MakeTopButton("✏️ Edit Row", 580, 72);
            btnEditRow.FillColor = Color.FromArgb(52, 120, 200);
            btnEditRow.Click += (s, e) => EditSelectedRow();
            topBar.Controls.Add(btnEditRow);

            btnDeleteRow = MakeTopButton("🗑 Delete", 690, 72);
            btnDeleteRow.FillColor = Color.FromArgb(200, 50, 50);
            btnDeleteRow.Click += (s, e) => DeleteSelectedRows();
            topBar.Controls.Add(btnDeleteRow);

            btnExport = MakeTopButton("📊 Export", 800, 72);
            btnExport.FillColor = Color.FromArgb(90, 90, 100);
            btnExport.Click += (s, e) => ShowExportMenu();
            topBar.Controls.Add(btnExport);

            btnTruncate = MakeTopButton("⚠ Reset Numbering", 910, 72);
            btnTruncate.Size = new Size(160, 34);
            btnTruncate.FillColor = Color.FromArgb(180, 40, 40);
            btnTruncate.Click += (s, e) => ResetTableNumbering();
            topBar.Controls.Add(btnTruncate);

            // ---------------- GRID ----------------
            var gridHolder = new Guna2Panel
            {
                Location = new Point(20, 130),
                Size = new Size(this.ClientSize.Width - 40, this.ClientSize.Height - 260),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                BorderRadius = 12,
                FillColor = Color.White,
                BorderColor = Color.FromArgb(230, 225, 225),
                BorderThickness = 1,
                Padding = new Padding(10)
            };
            this.Controls.Add(gridHolder);

            dgvDatabase = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = true,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.DisplayedCells,
                EnableHeadersVisualStyles = false,
                ColumnHeadersHeight = 42,
                Font = new Font("Segoe UI", 9.5F)
            };

            dgvDatabase.ColumnHeadersDefaultCellStyle.BackColor = Color.Maroon;
            dgvDatabase.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgvDatabase.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold);
            dgvDatabase.ColumnHeadersDefaultCellStyle.SelectionBackColor = Color.Maroon;
            dgvDatabase.DefaultCellStyle.Font = new Font("Segoe UI", 9.5F);
            dgvDatabase.DefaultCellStyle.SelectionBackColor = Color.FromArgb(250, 235, 235);
            dgvDatabase.DefaultCellStyle.SelectionForeColor = Color.FromArgb(120, 20, 40);
            dgvDatabase.RowTemplate.Height = 32;
            dgvDatabase.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(252, 248, 245);

            dgvDatabase.CellDoubleClick += DgvDatabase_CellDoubleClick;
            dgvDatabase.DataError += DgvDatabase_DataError;

            gridHolder.Controls.Add(dgvDatabase);

            // ---------------- BOTTOM BAR ----------------
            var bottomBar = new Guna2Panel
            {
                Location = new Point(20, this.ClientSize.Height - 120),
                Size = new Size(this.ClientSize.Width - 40, 100),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                BorderRadius = 12,
                FillColor = Color.White,
                BorderColor = Color.FromArgb(230, 225, 225),
                BorderThickness = 1
            };
            this.Controls.Add(bottomBar);

            var lblQuery = new Label
            {
                Text = "Run SELECT query:",
                Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(20, 12),
                ForeColor = Color.FromArgb(80, 80, 80)
            };
            bottomBar.Controls.Add(lblQuery);

            txtCustomQuery = new TextBox
            {
                Location = new Point(20, 40),
                Size = new Size(bottomBar.Width - 220, 34),
                Font = new Font("Consolas", 10F),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                Text = "SELECT * FROM user_credential LIMIT 100;"
            };
            bottomBar.Controls.Add(txtCustomQuery);

            btnRunQuery = new Guna2Button
            {
                Text = "▶  Run Query",
                Size = new Size(150, 40),
                Location = new Point(bottomBar.Width - 170, 36),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                BorderRadius = 8,
                FillColor = Color.Maroon,
                ForeColor = Color.White,
                Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold)
            };
            btnRunQuery.HoverState.FillColor = Color.FromArgb(100, 0, 0);
            btnRunQuery.Click += (s, e) => RunCustomQuery();
            bottomBar.Controls.Add(btnRunQuery);

            lblDbStatus = new Label
            {
                Text = "Ready.",
                Font = new Font("Segoe UI", 8.5F, FontStyle.Italic),
                AutoSize = true,
                Location = new Point(bottomBar.Width - 400, 14),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                ForeColor = Color.FromArgb(120, 120, 120)
            };
            bottomBar.Controls.Add(lblDbStatus);

            // Reflow on form resize
            this.Resize += (s, e) =>
            {
                if (gridHolder != null)
                    gridHolder.Size = new Size(this.ClientSize.Width - 40, this.ClientSize.Height - 260);

                if (bottomBar != null)
                {
                    bottomBar.Location = new Point(20, this.ClientSize.Height - 120);
                    bottomBar.Size = new Size(this.ClientSize.Width - 40, 100);
                    lblDbStatus.Location = new Point(bottomBar.Width - 400, 14);
                }
            };
        }

        private Guna2Button MakeTopButton(string text, int x, int y)
        {
            var btn = new Guna2Button
            {
                Text = text,
                Size = new Size(100, 34),
                Location = new Point(x, y),
                BorderRadius = 8,
                FillColor = Color.Maroon,
                ForeColor = Color.White,
                Font = new Font("Segoe UI Semibold", 8.5F, FontStyle.Bold)
            };
            btn.HoverState.FillColor = ControlPaint.Dark(btn.FillColor, 0.1f);
            return btn;
        }

        // =========================================================
        // TABLE LIST
        // =========================================================
        private void LoadTableList()
        {
            try
            {
                cmbTables.Items.Clear();
                string connStr = SettingsManager.Current.GetConnectionString();

                using (var conn = new MySqlConnection(connStr))
                {
                    conn.Open();
                    var dt = conn.GetSchema("Tables");

                    foreach (DataRow row in dt.Rows)
                    {
                        string type = row["TABLE_TYPE"].ToString();
                        if (type == "BASE TABLE")
                            cmbTables.Items.Add(row["TABLE_NAME"].ToString());
                    }
                }

                if (cmbTables.Items.Count > 0)
                    cmbTables.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                CustomMessageBox.Show("Failed to load table list:\n" + ex.Message,
                    "Error", CustomMessageBoxButtons.OK, CustomMessageBoxIcon.Error);
            }
        }

        private void LoadTable(string tableName)
        {
            if (string.IsNullOrEmpty(tableName)) return;

            currentDbTable = tableName;

            try
            {
                string connStr = SettingsManager.Current.GetConnectionString();
                using (var conn = new MySqlConnection(connStr))
                {
                    conn.Open();

                    // Build a SELECT list that only includes non-binary columns
                    var columnsToLoad = new List<string>();
                    string schemaQuery = @"
                SELECT COLUMN_NAME, DATA_TYPE
                FROM INFORMATION_SCHEMA.COLUMNS
                WHERE TABLE_SCHEMA = DATABASE()
                  AND TABLE_NAME = @table
                ORDER BY ORDINAL_POSITION";

                    using (var cmd = new MySqlCommand(schemaQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@table", tableName);
                        using (var r = cmd.ExecuteReader())
                        {
                            while (r.Read())
                            {
                                string colName = r["COLUMN_NAME"].ToString();
                                string dataType = r["DATA_TYPE"].ToString().ToLower();

                                // Skip binary / blob columns — they crash the grid
                                if (dataType == "blob" || dataType == "mediumblob" ||
                                    dataType == "longblob" || dataType == "tinyblob" ||
                                    dataType == "binary" || dataType == "varbinary")
                                    continue;

                                columnsToLoad.Add($"`{colName}`");
                            }
                        }
                    }

                    string selectList = columnsToLoad.Count > 0
                        ? string.Join(", ", columnsToLoad)
                        : "*";

                    using (var cmd = new MySqlCommand(
                        $"SELECT {selectList} FROM `{tableName}` LIMIT 500", conn))
                    using (var adapter = new MySqlDataAdapter(cmd))
                    {
                        var dt = new DataTable();
                        adapter.Fill(dt);

                        // Detach any previous handler
                        dgvDatabase.DataError -= DgvDatabase_DataError;
                        dgvDatabase.DataError += DgvDatabase_DataError;

                        dgvDatabase.DataSource = dt;

                        lblDbStatus.Text = $"{dt.Rows.Count} row(s) loaded from `{tableName}`.";
                    }
                }
            }
            catch (Exception ex)
            {
                CustomMessageBox.Show("Failed to load table:\n" + ex.Message,
                    "Error", CustomMessageBoxButtons.OK, CustomMessageBoxIcon.Error);
            }
        }

        private void DgvDatabase_DataError(object sender, DataGridViewDataErrorEventArgs e)
        {
            // Silently ignore grid rendering errors (e.g. binary columns)
            e.ThrowException = false;
            e.Cancel = true;
        }

        // =========================================================
        // INLINE CELL EDIT
        // =========================================================
        private void DgvDatabase_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;

            var cell = dgvDatabase.Rows[e.RowIndex].Cells[e.ColumnIndex];
            string column = dgvDatabase.Columns[e.ColumnIndex].Name;
            string currentValue = cell.Value == null || cell.Value == DBNull.Value ? "" : cell.Value.ToString();

            string pkColumn = FindPrimaryKeyColumn();

            if (string.IsNullOrEmpty(pkColumn))
            {
                CustomMessageBox.Show("Cannot identify a primary key column (expected something like `*_id`).",
                    "Error", CustomMessageBoxButtons.OK, CustomMessageBoxIcon.Warning);
                return;
            }

            object pkValue = dgvDatabase.Rows[e.RowIndex].Cells[pkColumn].Value;

            string newValue = Prompt(
                $"Edit `{column}` where `{pkColumn}` = {pkValue}",
                currentValue);

            if (newValue == null) return;

            try
            {
                string connStr = SettingsManager.Current.GetConnectionString();
                using (var conn = new MySqlConnection(connStr))
                {
                    conn.Open();
                    string q = $"UPDATE `{currentDbTable}` SET `{column}` = @val WHERE `{pkColumn}` = @pk LIMIT 1";

                    using (var cmd = new MySqlCommand(q, conn))
                    {
                        cmd.Parameters.AddWithValue("@val", newValue);
                        cmd.Parameters.AddWithValue("@pk", pkValue);

                        int rows = cmd.ExecuteNonQuery();
                        if (rows > 0)
                        {
                            cell.Value = newValue;
                            lblDbStatus.Text = $"Updated 1 row in `{currentDbTable}`.";
                        }
                        else
                        {
                            CustomMessageBox.Show("No row was updated.", "Notice",
                                CustomMessageBoxButtons.OK, CustomMessageBoxIcon.Information);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                CustomMessageBox.Show("Update failed:\n" + ex.Message,
                    "Error", CustomMessageBoxButtons.OK, CustomMessageBoxIcon.Error);
            }
        }

        private string FindPrimaryKeyColumn()
        {
            if (dgvDatabase.Columns.Contains("user_id")) return "user_id";
            if (dgvDatabase.Columns.Contains("id")) return "id";

            foreach (DataGridViewColumn col in dgvDatabase.Columns)
            {
                if (col.Name.EndsWith("_id", StringComparison.OrdinalIgnoreCase))
                    return col.Name;
            }

            return null;
        }

        // =========================================================
        // EDIT FULL ROW
        // =========================================================
        private void EditSelectedRow()
        {
            if (dgvDatabase.SelectedRows.Count != 1)
            {
                CustomMessageBox.Show("Please select exactly one row to edit.", "Notice",
                    CustomMessageBoxButtons.OK, CustomMessageBoxIcon.Information);
                return;
            }

            var row = dgvDatabase.SelectedRows[0];
            string pkColumn = FindPrimaryKeyColumn();

            if (string.IsNullOrEmpty(pkColumn))
            {
                CustomMessageBox.Show("Cannot identify a primary key column.", "Notice",
                    CustomMessageBoxButtons.OK, CustomMessageBoxIcon.Information);
                return;
            }

            object pkValue = row.Cells[pkColumn].Value;

            using (var dlg = new Form())
            {
                dlg.Text = $"Edit row where `{pkColumn}` = {pkValue}";
                dlg.Size = new Size(520, 640);
                dlg.StartPosition = FormStartPosition.CenterParent;
                dlg.FormBorderStyle = FormBorderStyle.FixedDialog;
                dlg.MaximizeBox = false;
                dlg.MinimizeBox = false;

                var scroll = new Panel
                {
                    Dock = DockStyle.Fill,
                    AutoScroll = true,
                    Padding = new Padding(15)
                };
                dlg.Controls.Add(scroll);

                var editors = new Dictionary<string, TextBox>();
                int top = 15;

                foreach (DataGridViewColumn col in dgvDatabase.Columns)
                {
                    bool isPk = col.Name.Equals(pkColumn, StringComparison.OrdinalIgnoreCase);

                    var lbl = new Label
                    {
                        Text = col.Name + (isPk ? " (PK, read-only)" : ""),
                        Left = 15,
                        Top = top,
                        Width = 150,
                        Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold),
                        ForeColor = isPk ? Color.Gray : Color.Black
                    };
                    scroll.Controls.Add(lbl);

                    var txt = new TextBox
                    {
                        Left = 170,
                        Top = top - 2,
                        Width = 310,
                        Text = row.Cells[col.Index].Value == null || row.Cells[col.Index].Value == DBNull.Value
                               ? ""
                               : row.Cells[col.Index].Value.ToString(),
                        ReadOnly = isPk,
                        Font = new Font("Segoe UI", 9.5F)
                    };
                    scroll.Controls.Add(txt);

                    editors[col.Name] = txt;
                    top += 36;
                }

                var btnSave = new Button
                {
                    Text = "Save",
                    Width = 100,
                    Height = 36,
                    Location = new Point(380, top + 10),
                    DialogResult = DialogResult.OK,
                    BackColor = Color.Maroon,
                    ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat
                };

                var btnCancel = new Button
                {
                    Text = "Cancel",
                    Width = 100,
                    Height = 36,
                    Location = new Point(270, top + 10),
                    DialogResult = DialogResult.Cancel,
                    FlatStyle = FlatStyle.Flat
                };

                scroll.Controls.Add(btnSave);
                scroll.Controls.Add(btnCancel);
                dlg.AcceptButton = btnSave;
                dlg.CancelButton = btnCancel;

                if (dlg.ShowDialog(this) != DialogResult.OK) return;

                try
                {
                    string connStr = SettingsManager.Current.GetConnectionString();
                    using (var conn = new MySqlConnection(connStr))
                    {
                        conn.Open();

                        var setParts = new List<string>();
                        foreach (var kv in editors)
                        {
                            if (kv.Key.Equals(pkColumn, StringComparison.OrdinalIgnoreCase)) continue;
                            setParts.Add($"`{kv.Key}` = @p_{kv.Key}");
                        }

                        string q = $"UPDATE `{currentDbTable}` SET {string.Join(", ", setParts)} WHERE `{pkColumn}` = @pk LIMIT 1";

                        using (var cmd = new MySqlCommand(q, conn))
                        {
                            foreach (var kv in editors)
                            {
                                if (kv.Key.Equals(pkColumn, StringComparison.OrdinalIgnoreCase)) continue;
                                cmd.Parameters.AddWithValue($"@p_{kv.Key}", kv.Value.Text);
                            }
                            cmd.Parameters.AddWithValue("@pk", pkValue);

                            int rows = cmd.ExecuteNonQuery();
                            if (rows > 0)
                            {
                                lblDbStatus.Text = $"Updated 1 row in `{currentDbTable}`.";
                                LoadTable(currentDbTable);
                            }
                            else
                            {
                                CustomMessageBox.Show("No row was updated.", "Notice",
                                    CustomMessageBoxButtons.OK, CustomMessageBoxIcon.Information);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    CustomMessageBox.Show("Update failed:\n" + ex.Message,
                        "Error", CustomMessageBoxButtons.OK, CustomMessageBoxIcon.Error);
                }
            }
        }

        // =========================================================
        // ADD ROW
        // =========================================================
        private void AddRowToTable()
        {
            if (string.IsNullOrEmpty(currentDbTable))
            {
                CustomMessageBox.Show("Select a table first.", "Notice",
                    CustomMessageBoxButtons.OK, CustomMessageBoxIcon.Information);
                return;
            }

            using (var dlg = new Form())
            {
                dlg.Text = $"Add new row to `{currentDbTable}`";
                dlg.Size = new Size(520, 640);
                dlg.StartPosition = FormStartPosition.CenterParent;
                dlg.FormBorderStyle = FormBorderStyle.FixedDialog;
                dlg.MaximizeBox = false;
                dlg.MinimizeBox = false;

                var scroll = new Panel
                {
                    Dock = DockStyle.Fill,
                    AutoScroll = true,
                    Padding = new Padding(15)
                };
                dlg.Controls.Add(scroll);

                var editors = new Dictionary<string, TextBox>();
                int top = 15;

                foreach (DataGridViewColumn col in dgvDatabase.Columns)
                {
                    var lbl = new Label
                    {
                        Text = col.Name,
                        Left = 15,
                        Top = top,
                        Width = 150,
                        Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold)
                    };
                    scroll.Controls.Add(lbl);

                    var txt = new TextBox
                    {
                        Left = 170,
                        Top = top - 2,
                        Width = 310,
                        Font = new Font("Segoe UI", 9.5F)
                    };
                    scroll.Controls.Add(txt);

                    editors[col.Name] = txt;
                    top += 36;
                }

                var btnSave = new Button
                {
                    Text = "Insert",
                    Width = 100,
                    Height = 36,
                    Location = new Point(380, top + 10),
                    DialogResult = DialogResult.OK,
                    BackColor = Color.FromArgb(46, 160, 90),
                    ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat
                };
                var btnCancel = new Button
                {
                    Text = "Cancel",
                    Width = 100,
                    Height = 36,
                    Location = new Point(270, top + 10),
                    DialogResult = DialogResult.Cancel,
                    FlatStyle = FlatStyle.Flat
                };

                scroll.Controls.Add(btnSave);
                scroll.Controls.Add(btnCancel);
                dlg.AcceptButton = btnSave;
                dlg.CancelButton = btnCancel;

                if (dlg.ShowDialog(this) != DialogResult.OK) return;

                try
                {
                    string connStr = SettingsManager.Current.GetConnectionString();
                    using (var conn = new MySqlConnection(connStr))
                    {
                        conn.Open();

                        var cols = new List<string>();
                        var pars = new List<string>();

                        foreach (var kv in editors)
                        {
                            cols.Add($"`{kv.Key}`");
                            pars.Add($"@p_{kv.Key}");
                        }

                        string q = $"INSERT INTO `{currentDbTable}` ({string.Join(", ", cols)}) VALUES ({string.Join(", ", pars)})";

                        using (var cmd = new MySqlCommand(q, conn))
                        {
                            foreach (var kv in editors)
                                cmd.Parameters.AddWithValue($"@p_{kv.Key}",
                                    string.IsNullOrEmpty(kv.Value.Text) ? (object)DBNull.Value : kv.Value.Text);

                            cmd.ExecuteNonQuery();
                            lblDbStatus.Text = $"Inserted 1 row into `{currentDbTable}`.";
                            LoadTable(currentDbTable);
                        }
                    }
                }
                catch (Exception ex)
                {
                    CustomMessageBox.Show("Insert failed:\n" + ex.Message,
                        "Error", CustomMessageBoxButtons.OK, CustomMessageBoxIcon.Error);
                }
            }
        }

        // =========================================================
        // DELETE
        // =========================================================
        private void DeleteSelectedRows()
        {
            if (dgvDatabase.SelectedRows.Count == 0)
            {
                CustomMessageBox.Show("Select one or more rows first.", "Notice",
                    CustomMessageBoxButtons.OK, CustomMessageBoxIcon.Information);
                return;
            }

            string pkColumn = FindPrimaryKeyColumn();
            if (string.IsNullOrEmpty(pkColumn))
            {
                CustomMessageBox.Show("Cannot identify a primary key column.", "Notice",
                    CustomMessageBoxButtons.OK, CustomMessageBoxIcon.Information);
                return;
            }

            int count = dgvDatabase.SelectedRows.Count;
            var confirm = CustomMessageBox.Show(
                $"Delete {count} row(s) from `{currentDbTable}`?\n\nThis cannot be undone.",
                "Confirm Delete",
                CustomMessageBoxButtons.YesNo,
                CustomMessageBoxIcon.Warning);

            if (confirm != CustomMessageBoxResult.Yes) return;

            try
            {
                string connStr = SettingsManager.Current.GetConnectionString();
                using (var conn = new MySqlConnection(connStr))
                {
                    conn.Open();

                    int deleted = 0;

                    foreach (DataGridViewRow row in dgvDatabase.SelectedRows)
                    {
                        object pkValue = row.Cells[pkColumn].Value;
                        if (pkValue == null || pkValue == DBNull.Value) continue;

                        string q = $"DELETE FROM `{currentDbTable}` WHERE `{pkColumn}` = @pk LIMIT 1";

                        using (var cmd = new MySqlCommand(q, conn))
                        {
                            cmd.Parameters.AddWithValue("@pk", pkValue);
                            deleted += cmd.ExecuteNonQuery();
                        }
                    }

                    CustomMessageBox.Show($"Deleted {deleted} row(s) from `{currentDbTable}`.",
                        "Deleted", CustomMessageBoxButtons.OK, CustomMessageBoxIcon.Information);
                    lblDbStatus.Text = $"Deleted {deleted} row(s) from `{currentDbTable}`.";
                    LoadTable(currentDbTable);
                }
            }
            catch (Exception ex)
            {
                CustomMessageBox.Show("Delete failed:\n" + ex.Message,
                    "Error", CustomMessageBoxButtons.OK, CustomMessageBoxIcon.Error);
            }
        }

        // =========================================================
        // CUSTOM SELECT QUERY
        // =========================================================
        private void RunCustomQuery()
        {
            string sql = txtCustomQuery.Text.Trim();

            if (string.IsNullOrEmpty(sql))
            {
                CustomMessageBox.Show("Enter a query first.", "Notice",
                    CustomMessageBoxButtons.OK, CustomMessageBoxIcon.Information);
                return;
            }

            if (!sql.TrimStart().StartsWith("SELECT", StringComparison.OrdinalIgnoreCase))
            {
                CustomMessageBox.Show("Only SELECT queries are allowed here for safety.",
                    "Not Allowed", CustomMessageBoxButtons.OK, CustomMessageBoxIcon.Warning);
                return;
            }

            try
            {
                string connStr = SettingsManager.Current.GetConnectionString();
                using (var conn = new MySqlConnection(connStr))
                {
                    conn.Open();
                    using (var cmd = new MySqlCommand(sql, conn))
                    using (var adapter = new MySqlDataAdapter(cmd))
                    {
                        var dt = new DataTable();
                        adapter.Fill(dt);
                        dgvDatabase.DataSource = dt;
                        lblDbStatus.Text = $"{dt.Rows.Count} row(s) returned.";
                    }
                }
            }
            catch (Exception ex)
            {
                CustomMessageBox.Show("Query failed:\n" + ex.Message,
                    "Error", CustomMessageBoxButtons.OK, CustomMessageBoxIcon.Error);
            }
        }

        // =========================================================
        // RESET NUMBERING (TRUNCATE)
        // =========================================================
        private void ResetTableNumbering()
        {
            if (string.IsNullOrEmpty(currentDbTable))
            {
                CustomMessageBox.Show("Select a table first.", "Notice",
                    CustomMessageBoxButtons.OK, CustomMessageBoxIcon.Information);
                return;
            }

            var allowed = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "professor_activity",
                "submitted_activity",
                "professor_class",
                "student_class",
                "quizzes",
                "questions",
                "quiz_attempts",
                "student_answers",
                "professor_attendance",
                "question_answer_security",
                "mainfolderpath"
            };

            if (!allowed.Contains(currentDbTable))
            {
                CustomMessageBox.Show(
                    $"`{currentDbTable}` is not on the whitelist for truncation.\n\n" +
                    "Add it inside `ResetTableNumbering()` if you really need to reset it.",
                    "Not Allowed",
                    CustomMessageBoxButtons.OK,
                    CustomMessageBoxIcon.Warning);
                return;
            }

            long rowCount = 0;
            long maxId = 0;
            string pkColumn = FindPrimaryKeyColumn();

            try
            {
                string connStr = SettingsManager.Current.GetConnectionString();
                using (var conn = new MySqlConnection(connStr))
                {
                    conn.Open();

                    using (var cmd = new MySqlCommand($"SELECT COUNT(*) FROM `{currentDbTable}`", conn))
                        rowCount = Convert.ToInt64(cmd.ExecuteScalar());

                    if (!string.IsNullOrEmpty(pkColumn))
                    {
                        try
                        {
                            using (var cmd = new MySqlCommand(
                                $"SELECT COALESCE(MAX(`{pkColumn}`), 0) FROM `{currentDbTable}`", conn))
                                maxId = Convert.ToInt64(cmd.ExecuteScalar());
                        }
                        catch { }
                    }
                }
            }
            catch (Exception ex)
            {
                CustomMessageBox.Show("Failed to inspect table:\n" + ex.Message,
                    "Error", CustomMessageBoxButtons.OK, CustomMessageBoxIcon.Error);
                return;
            }

            var step1 = CustomMessageBox.Show(
                $"⚠️  You are about to TRUNCATE `{currentDbTable}`.\n\n" +
                $"• Current rows: {rowCount}\n" +
                $"• Highest {pkColumn ?? "ID"}: {maxId}\n" +
                $"• After truncation: 0 rows, auto-increment resets to 1\n\n" +
                $"This CANNOT be undone.\n\nContinue?",
                "Confirm TRUNCATE",
                CustomMessageBoxButtons.YesNo,
                CustomMessageBoxIcon.Warning);

            if (step1 != CustomMessageBoxResult.Yes) return;

            string typed = Prompt(
                $"To confirm, type the exact table name:\n\n    {currentDbTable}\n\n" +
                $"(Copying is allowed — you may paste it.)",
                "");

            if (typed == null) return;
            if (typed.Trim() != currentDbTable)
            {
                CustomMessageBox.Show(
                    "The name you typed does not match. Truncation cancelled.",
                    "Cancelled",
                    CustomMessageBoxButtons.OK,
                    CustomMessageBoxIcon.Information);
                return;
            }

            try
            {
                string connStr = SettingsManager.Current.GetConnectionString();
                using (var conn = new MySqlConnection(connStr))
                {
                    conn.Open();

                    using (var cmd = new MySqlCommand("SET FOREIGN_KEY_CHECKS = 0;", conn))
                        cmd.ExecuteNonQuery();

                    try
                    {
                        using (var cmd = new MySqlCommand($"TRUNCATE TABLE `{currentDbTable}`;", conn))
                            cmd.ExecuteNonQuery();

                        try
                        {
                            using (var cmd = new MySqlCommand(
                                $"ALTER TABLE `{currentDbTable}` AUTO_INCREMENT = 1;", conn))
                                cmd.ExecuteNonQuery();
                        }
                        catch { }
                    }
                    finally
                    {
                        using (var cmd = new MySqlCommand("SET FOREIGN_KEY_CHECKS = 1;", conn))
                            cmd.ExecuteNonQuery();
                    }
                }

                lblDbStatus.Text = $"✔ `{currentDbTable}` truncated. Auto-increment reset to 1.";

                CustomMessageBox.Show(
                    $"`{currentDbTable}` has been truncated.\n\n" +
                    "All rows deleted. Next insert will start at ID = 1.",
                    "TRUNCATE Complete",
                    CustomMessageBoxButtons.OK,
                    CustomMessageBoxIcon.Information);

                LoadTable(currentDbTable);
            }
            catch (Exception ex)
            {
                CustomMessageBox.Show(
                    "TRUNCATE failed:\n\n" + ex.Message,
                    "Error",
                    CustomMessageBoxButtons.OK,
                    CustomMessageBoxIcon.Error);
            }
        }

        // =========================================================
        // EXPORT
        // =========================================================
        private void ShowExportMenu()
        {
            var menu = new ContextMenuStrip();
            menu.Font = new Font("Segoe UI", 10F);
            menu.ShowImageMargin = false;

            var itemUsers = new ToolStripMenuItem("📋  All Users (with info)");
            itemUsers.Click += (s, e) => ExportReport("users_all");

            var itemStudents = new ToolStripMenuItem("🎓  Students Only");
            itemStudents.Click += (s, e) => ExportReport("students_only");

            var itemProfessors = new ToolStripMenuItem("👨‍🏫  Professors Only");
            itemProfessors.Click += (s, e) => ExportReport("professors_only");

            var itemClasses = new ToolStripMenuItem("📚  All Classes (with enrollment)");
            itemClasses.Click += (s, e) => ExportReport("classes_all");

            var itemActivities = new ToolStripMenuItem("📄  All Activities");
            itemActivities.Click += (s, e) => ExportReport("activities_all");

            var itemSubmissions = new ToolStripMenuItem("✅  All Submissions (with scores)");
            itemSubmissions.Click += (s, e) => ExportReport("submissions_all");

            var itemQuizzes = new ToolStripMenuItem("📝  All Quizzes & Exams");
            itemQuizzes.Click += (s, e) => ExportReport("quizzes_all");

            var itemQuizAttempts = new ToolStripMenuItem("📊  All Quiz Attempts (with scores)");
            itemQuizAttempts.Click += (s, e) => ExportReport("quiz_attempts_all");

            var itemAttendance = new ToolStripMenuItem("🗓️  Attendance Summary");
            itemAttendance.Click += (s, e) => ExportReport("attendance_all");

            var itemCurrent = new ToolStripMenuItem("📄  Current Table View");
            itemCurrent.Click += (s, e) => ExportCurrentTable();

            var itemEverything = new ToolStripMenuItem("📦  Everything (multiple sheets)");
            itemEverything.Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold);
            itemEverything.Click += (s, e) => ExportEverything();

            menu.Items.Add(itemUsers);
            menu.Items.Add(itemStudents);
            menu.Items.Add(itemProfessors);
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(itemClasses);
            menu.Items.Add(itemActivities);
            menu.Items.Add(itemSubmissions);
            menu.Items.Add(itemQuizzes);
            menu.Items.Add(itemQuizAttempts);
            menu.Items.Add(itemAttendance);
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(itemCurrent);
            menu.Items.Add(itemEverything);

            menu.Show(btnExport, new Point(0, btnExport.Height));
        }

        private void ExportReport(string reportKey)
        {
            DataTable dt = BuildReport(reportKey);

            if (dt == null || dt.Rows.Count == 0)
            {
                CustomMessageBox.Show("Nothing to export for this report.", "Empty",
                    CustomMessageBoxButtons.OK, CustomMessageBoxIcon.Information);
                return;
            }

            using (var sfd = new SaveFileDialog())
            {
                sfd.Filter = "Excel Files|*.xlsx";
                sfd.FileName = $"{reportKey}_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";

                if (sfd.ShowDialog() != DialogResult.OK) return;

                try
                {
                    using (var wb = new XLWorkbook())
                    {
                        var ws = wb.Worksheets.Add(dt, SheetNameFor(reportKey));

                        ws.Columns().AdjustToContents();

                        var headerRow = ws.Row(1);
                        headerRow.Style.Font.Bold = true;
                        headerRow.Style.Fill.BackgroundColor = XLColor.Maroon;
                        headerRow.Style.Font.FontColor = XLColor.White;
                        headerRow.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                        ws.SheetView.FreezeRows(1);

                        wb.SaveAs(sfd.FileName);
                    }

                    CustomMessageBox.Show(
                        $"Exported successfully.\n\n{dt.Rows.Count} row(s) →\n{sfd.FileName}",
                        "Export Complete",
                        CustomMessageBoxButtons.OK,
                        CustomMessageBoxIcon.Information);

                    lblDbStatus.Text = $"Exported {dt.Rows.Count} row(s) to {Path.GetFileName(sfd.FileName)}.";
                }
                catch (Exception ex)
                {
                    CustomMessageBox.Show("Export failed:\n" + ex.Message, "Error",
                        CustomMessageBoxButtons.OK, CustomMessageBoxIcon.Error);
                }
            }
        }

        private string SheetNameFor(string key)
        {
            switch (key)
            {
                case "users_all": return "All Users";
                case "students_only": return "Students";
                case "professors_only": return "Professors";
                case "classes_all": return "Classes";
                case "activities_all": return "Activities";
                case "submissions_all": return "Submissions";
                case "quizzes_all": return "Quizzes";
                case "quiz_attempts_all": return "Quiz Attempts";
                case "attendance_all": return "Attendance";
                default: return "Data";
            }
        }

        private DataTable BuildReport(string reportKey)
        {
            string connStr = SettingsManager.Current.GetConnectionString();
            string query;

            switch (reportKey)
            {
                case "users_all":
                    query = @"
                        SELECT 
                            u.user_id       AS 'User ID',
                            u.username      AS 'Username',
                            u.roles         AS 'Role',
                            u.user_status   AS 'Status',
                            i.lastname      AS 'Last Name',
                            i.firstname     AS 'First Name',
                            i.middlename    AS 'Middle Name',
                            i.email         AS 'Email',
                            i.school_year   AS 'Year',
                            i.school_section AS 'Section',
                            i.school_semester AS 'Semester',
                            i.school_course AS 'Course'
                        FROM user_credential u
                        LEFT JOIN user_information i ON u.user_id = i.user_id
                        ORDER BY u.roles, i.lastname, i.firstname";
                    break;

                case "students_only":
                    query = @"
                        SELECT 
                            u.user_id       AS 'User ID',
                            u.username      AS 'Username',
                            CONCAT(i.lastname, ', ', i.firstname, ' ', i.middlename) AS 'Full Name',
                            i.email         AS 'Email',
                            i.school_year   AS 'Year',
                            i.school_section AS 'Section',
                            i.school_semester AS 'Semester',
                            i.school_course AS 'Course',
                            u.user_status   AS 'Status'
                        FROM user_credential u
                        LEFT JOIN user_information i ON u.user_id = i.user_id
                        WHERE u.roles = 'Student'
                        ORDER BY i.school_section, i.lastname, i.firstname";
                    break;

                case "professors_only":
                    query = @"
                        SELECT 
                            u.user_id       AS 'User ID',
                            u.username      AS 'Username',
                            CONCAT(i.lastname, ', ', i.firstname, ' ', i.middlename) AS 'Full Name',
                            i.email         AS 'Email',
                            u.user_status   AS 'Status'
                        FROM user_credential u
                        LEFT JOIN user_information i ON u.user_id = i.user_id
                        WHERE u.roles = 'Professor'
                        ORDER BY i.lastname, i.firstname";
                    break;

                case "classes_all":
                    query = @"
                        SELECT 
                            pc.class_id     AS 'Class ID',
                            pc.class_code   AS 'Class Code',
                            pc.class_name   AS 'Class Name',
                            pc.class_section AS 'Section',
                            pc.class_date   AS 'Day',
                            pc.class_time   AS 'Time',
                            CONCAT(i.lastname, ', ', i.firstname) AS 'Professor',
                            (SELECT COUNT(*) FROM student_class sc WHERE sc.professor_id = pc.professor_id AND sc.section = pc.class_section) AS 'Enrolled Students'
                        FROM professor_class pc
                        LEFT JOIN user_information i ON i.user_id = pc.professor_id
                        ORDER BY pc.class_name, pc.class_section";
                    break;

                case "activities_all":
                    query = @"
                        SELECT 
                            pa.activity_id  AS 'Activity ID',
                            pa.title        AS 'Title',
                            pa.activity_subject AS 'Subject',
                            pa.section      AS 'Section',
                            pa.activity_status AS 'Status',
                            pa.start_time   AS 'Posted At',
                            pa.due_date     AS 'Due Date',
                            pa.score        AS 'Max Score',
                            CONCAT(i.lastname, ', ', i.firstname) AS 'Professor'
                        FROM professor_activity pa
                        LEFT JOIN user_information i ON i.user_id = pa.professor_id
                        ORDER BY pa.start_time DESC";
                    break;

                case "submissions_all":
                    query = @"
                        SELECT 
                            sa.user_id      AS 'Student ID',
                            sa.student_name AS 'Student Name',
                            sa.title        AS 'Activity',
                            sa.class_name   AS 'Subject',
                            sa.section      AS 'Section',
                            sa.activity_status AS 'Status',
                            sa.score        AS 'Score',
                            sa.file_path    AS 'File Path',
                            CONCAT(i.lastname, ', ', i.firstname) AS 'Professor'
                        FROM submitted_activity sa
                        LEFT JOIN user_information i ON i.user_id = sa.prof_id
                        ORDER BY sa.section, sa.student_name, sa.title";
                    break;

                case "quizzes_all":
                    query = @"
                        SELECT 
                            q.quiz_id       AS 'Quiz ID',
                            q.quiz_title    AS 'Title',
                            q.subject       AS 'Subject',
                            q.assessment_type AS 'Type',
                            q.exam_period   AS 'Period',
                            q.created_at    AS 'Created At',
                            (SELECT COUNT(*) FROM questions qq WHERE qq.quiz_id = q.quiz_id) AS 'Question Count',
                            COALESCE(qa_agg.total_attempts, 0) AS 'Attempts',
                            COALESCE(qa_agg.avg_score, 0)      AS 'Average Score',
                            COALESCE(qa_agg.submitted_count, 0) AS 'Submitted Count'
                        FROM quizzes q
                        LEFT JOIN (
                            SELECT quiz_id,
                                   COUNT(*) AS total_attempts,
                                   AVG(score) AS avg_score,
                                   SUM(CASE WHEN status = 'SUBMITTED' THEN 1 ELSE 0 END) AS submitted_count
                            FROM quiz_attempts
                            GROUP BY quiz_id
                        ) qa_agg ON qa_agg.quiz_id = q.quiz_id
                        ORDER BY q.created_at DESC";
                    break;

                case "quiz_attempts_all":
                    query = @"
                        SELECT 
                            qa.attempt_id   AS 'Attempt ID',
                            qa.quiz_id      AS 'Quiz ID',
                            q.quiz_title    AS 'Quiz Title',
                            q.subject       AS 'Subject',
                            qa.user_id      AS 'Student ID',
                            qa.student_name AS 'Student Name',
                            qa.score        AS 'Score',
                            qa.total_questions AS 'Total',
                            qa.percentage   AS 'Percentage',
                            qa.status       AS 'Status',
                            qa.started_at   AS 'Started',
                            qa.submitted_at AS 'Submitted'
                        FROM quiz_attempts qa
                        LEFT JOIN quizzes q ON q.quiz_id = qa.quiz_id
                        ORDER BY qa.submitted_at DESC, qa.started_at DESC";
                    break;

                case "attendance_all":
                    query = @"
                        SELECT 
                            pa.student_id   AS 'Student ID',
                            pa.student_name AS 'Student Name',
                            ui.school_section AS 'Section',
                            pa.present      AS 'Total Present',
                            pa.absent       AS 'Total Absent',
                            pa.late         AS 'Total Late',
                            (COALESCE(pa.present,0) + COALESCE(pa.absent,0) + COALESCE(pa.late,0)) AS 'Total Days',
                            CASE 
                                WHEN (COALESCE(pa.present,0) + COALESCE(pa.absent,0) + COALESCE(pa.late,0)) = 0 
                                THEN 0 
                                ELSE ROUND(
                                    (COALESCE(pa.present,0) * 100.0) / 
                                    (COALESCE(pa.present,0) + COALESCE(pa.absent,0) + COALESCE(pa.late,0)), 2)
                            END AS 'Attendance %'
                        FROM professor_attendance pa
                        LEFT JOIN user_information ui ON ui.user_id = pa.student_id
                        ORDER BY ui.school_section, pa.student_name";
                    break;

                default:
                    return null;
            }

            try
            {
                using (var conn = new MySqlConnection(connStr))
                {
                    conn.Open();
                    using (var cmd = new MySqlCommand(query, conn))
                    using (var adapter = new MySqlDataAdapter(cmd))
                    {
                        var dt = new DataTable();
                        adapter.Fill(dt);
                        return dt;
                    }
                }
            }
            catch (Exception ex)
            {
                CustomMessageBox.Show("Report failed:\n\n" + ex.Message,
                    "Report Error", CustomMessageBoxButtons.OK, CustomMessageBoxIcon.Error);
                return null;
            }
        }

        private void ExportCurrentTable()
        {
            if (dgvDatabase.DataSource == null || dgvDatabase.Rows.Count == 0)
            {
                CustomMessageBox.Show("Nothing to export.", "Notice",
                    CustomMessageBoxButtons.OK, CustomMessageBoxIcon.Information);
                return;
            }

            using (var sfd = new SaveFileDialog())
            {
                sfd.Filter = "Excel Files|*.xlsx";
                sfd.FileName = $"{currentDbTable}_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";

                if (sfd.ShowDialog() != DialogResult.OK) return;

                try
                {
                    var dt = (DataTable)dgvDatabase.DataSource;

                    using (var wb = new XLWorkbook())
                    {
                        var ws = wb.Worksheets.Add(dt, currentDbTable);
                        ws.Columns().AdjustToContents();
                        wb.SaveAs(sfd.FileName);
                    }

                    CustomMessageBox.Show("Exported successfully.", "Export Complete",
                        CustomMessageBoxButtons.OK, CustomMessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    CustomMessageBox.Show("Export failed:\n" + ex.Message,
                        "Error", CustomMessageBoxButtons.OK, CustomMessageBoxIcon.Error);
                }
            }
        }

        private void ExportEverything()
        {
            using (var sfd = new SaveFileDialog())
            {
                sfd.Filter = "Excel Files|*.xlsx";
                sfd.FileName = $"CDSGA_Complete_Export_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";

                if (sfd.ShowDialog() != DialogResult.OK) return;

                try
                {
                    var reports = new[]
                    {
                        "users_all",
                        "students_only",
                        "professors_only",
                        "classes_all",
                        "activities_all",
                        "submissions_all",
                        "quizzes_all",
                        "quiz_attempts_all",
                        "attendance_all"
                    };

                    using (var wb = new XLWorkbook())
                    {
                        foreach (var key in reports)
                        {
                            var dt = BuildReport(key);
                            if (dt == null) continue;

                            string sheetName = SheetNameFor(key);
                            if (sheetName.Length > 31)
                                sheetName = sheetName.Substring(0, 31);

                            var ws = wb.Worksheets.Add(dt, sheetName);
                            ws.Columns().AdjustToContents();

                            var headerRow = ws.Row(1);
                            headerRow.Style.Font.Bold = true;
                            headerRow.Style.Fill.BackgroundColor = XLColor.Maroon;
                            headerRow.Style.Font.FontColor = XLColor.White;
                            headerRow.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                            ws.SheetView.FreezeRows(1);
                        }

                        wb.SaveAs(sfd.FileName);
                    }

                    CustomMessageBox.Show(
                        $"Complete export saved.\n\n{sfd.FileName}",
                        "Export Complete",
                        CustomMessageBoxButtons.OK,
                        CustomMessageBoxIcon.Information);

                    lblDbStatus.Text = $"Exported everything to {Path.GetFileName(sfd.FileName)}.";
                }
                catch (Exception ex)
                {
                    CustomMessageBox.Show("Export failed:\n" + ex.Message, "Error",
                        CustomMessageBoxButtons.OK, CustomMessageBoxIcon.Error);
                }
            }
        }

        // =========================================================
        // PROMPT HELPER
        // =========================================================
        private string Prompt(string label, string defaultValue)
        {
            using (var frm = new Form())
            {
                frm.Text = "Input";
                frm.Width = 460;
                frm.Height = 180;
                frm.StartPosition = FormStartPosition.CenterParent;
                frm.FormBorderStyle = FormBorderStyle.FixedDialog;
                frm.MinimizeBox = false;
                frm.MaximizeBox = false;

                var lbl = new Label { Text = label, Left = 12, Top = 12, Width = 420, Height = 60 };
                var txt = new TextBox { Text = defaultValue ?? "", Left = 12, Top = 78, Width = 420 };
                var ok = new Button { Text = "OK", Left = 270, Top = 110, Width = 75, DialogResult = DialogResult.OK };
                var cancel = new Button { Text = "Cancel", Left = 356, Top = 110, Width = 75, DialogResult = DialogResult.Cancel };

                frm.Controls.Add(lbl);
                frm.Controls.Add(txt);
                frm.Controls.Add(ok);
                frm.Controls.Add(cancel);
                frm.AcceptButton = ok;
                frm.CancelButton = cancel;

                return frm.ShowDialog() == DialogResult.OK ? txt.Text : null;
            }
        }
    }
}