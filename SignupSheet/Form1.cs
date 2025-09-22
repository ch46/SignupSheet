using Microsoft.Data.Sqlite;
using System.Windows.Forms;

namespace SignupSheet
{
    public partial class Form1 : Form
    {
        private string dbPath = "matches.db";
        private string connectionString;
        private DataGridView dgv;
        private Button btnAddMatch;
        private Button btnClear;
        private Button btnClearPlayed;
        private ComboBox cmbLocation; // Add ComboBox for location

        private readonly string[] Locations = new[]
        {
            "Thousand Oaks Community Center",
            "Borchard Community Center",
            "Dos Vientos Community Center",
            "Sycamore Canyon School Gym"
        };

        public Form1()
        {
            InitializeComponent();
            connectionString = $"Data Source={dbPath}";
            InitializeDatabase();
            InitializeUI();
            // Do not load matches until a location is selected
        }

        private void InitializeDatabase()
        {
            using var conn = new SqliteConnection(connectionString);
            conn.Open();
            string checkSql = "PRAGMA table_info(Matches);";
            using (var checkCmd = new SqliteCommand(checkSql, conn))
            using (var reader = checkCmd.ExecuteReader())
            {
                bool needsRecreate = false;
                bool hasPlayedTimestamp = false;
                bool hasClearedTimestamp = false;
                bool hasMatchNumber = false;
                bool hasLocation = false;
                while (reader.Read())
                {
                    if (reader["name"].ToString() == "matchid" && reader["type"].ToString().ToUpper() != "INTEGER")
                        needsRecreate = true;
                    if (reader["name"].ToString() == "date" && reader["type"].ToString().ToUpper() != "DATE")
                        needsRecreate = true;
                    if (reader["name"].ToString() == "playedtimestamp")
                        hasPlayedTimestamp = true;
                    if (reader["name"].ToString() == "clearedtimestamp")
                        hasClearedTimestamp = true;
                    if (reader["name"].ToString() == "matchnumber")
                        hasMatchNumber = true;
                    if (reader["name"].ToString() == "Location")
                        hasLocation = true;
                }
                if (needsRecreate || !hasPlayedTimestamp || !hasClearedTimestamp || !hasMatchNumber || !hasLocation)
                {
                    using var dropCmd = new SqliteCommand("DROP TABLE IF EXISTS Matches;", conn);
                    dropCmd.ExecuteNonQuery();
                }
            }
            string sql = @"CREATE TABLE IF NOT EXISTS Matches (
                matchid INTEGER PRIMARY KEY AUTOINCREMENT,
                date DATE NOT NULL,
                matchnumber INTEGER NOT NULL,
                player1 TEXT,
                player1timestamp TEXT,
                player2 TEXT,
                player2timestamp TEXT,
                player3 TEXT,
                player3timestamp TEXT,
                player4 TEXT,
                player4timestamp TEXT,
                played INTEGER,
                playedtimestamp TEXT,
                cleared INTEGER,
                clearedtimestamp TEXT,
                Location TEXT NOT NULL
            );";
            using var cmd = new SqliteCommand(sql, conn);
            cmd.ExecuteNonQuery();
        }

        private void InitializeUI()
        {
            this.WindowState = FormWindowState.Maximized;

            var lblLocation = new Label
            {
                Text = "Location: ",
                AutoSize = true,
                TextAlign = ContentAlignment.MiddleLeft,
                Margin = new Padding(0, 6, 8, 0),
                Font = new Font(FontFamily.GenericSerif, 20)
            };

            // ComboBox for location selection
            cmbLocation = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Margin = new Padding(0, 4, 0, 0),
                Size = new Size(20, 20),
                Font = new Font(FontFamily.GenericSerif, 20)
            };
            cmbLocation.Items.AddRange(Locations);
            cmbLocation.SelectedIndexChanged += CmbLocation_SelectedIndexChanged;

            // Calculate width for ComboBox based on longest location name
            using (var g = cmbLocation.CreateGraphics())
            {
                int maxWidth = 0;
                foreach (string loc in Locations)
                {
                    int w = (int)g.MeasureString(loc, cmbLocation.Font).Width;
                    if (w > maxWidth) maxWidth = w;
                }
                cmbLocation.Width = maxWidth + SystemInformation.VerticalScrollBarWidth + 30;
                cmbLocation.DropDownWidth = cmbLocation.Width;
            }

            // FlowLayoutPanel for label and ComboBox
            var locationPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = cmbLocation.Height + 20,
                FlowDirection = FlowDirection.LeftToRight,
                Padding = new Padding(10, 10, 10, 2),
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink
            };
            locationPanel.Controls.Add(lblLocation);
            locationPanel.Controls.Add(cmbLocation);

            // Button panel
            var buttonPanel = new Panel
            {
                Dock = DockStyle.Top,
                Padding = new Padding(10, 10, 10, 10)
            };

            // Set a fixed height for all buttons
            int fixedButtonHeight = 60;

            btnAddMatch = new Button
            {
                Text = "Add a new match",
                AutoSize = false,
                Font = new Font(FontFamily.GenericSerif, 20),
                Margin = new Padding(0, 0, 10, 0),
                Dock = DockStyle.Left,
                Visible = false,
                Enabled = false,
                Height = fixedButtonHeight,
                Width = 220,
                MinimumSize = new Size(220, fixedButtonHeight),
                MaximumSize = new Size(220, fixedButtonHeight),
                TextAlign = ContentAlignment.MiddleCenter,
                AutoEllipsis = true,
                FlatStyle = FlatStyle.Standard
            };
            btnAddMatch.Click += BtnAddMatch_Click;

            btnClear = new Button
            {
                Text = "Clear",
                AutoSize = false,
                Font = new Font(FontFamily.GenericSerif, 20),
                Margin = new Padding(10, 0, 0, 0),
                Dock = DockStyle.Right,
                Visible = false,
                Enabled = false,
                Height = fixedButtonHeight,
                Width = 120,
                MinimumSize = new Size(120, fixedButtonHeight),
                MaximumSize = new Size(120, fixedButtonHeight),
                TextAlign = ContentAlignment.MiddleCenter,
                AutoEllipsis = true,
                FlatStyle = FlatStyle.Standard
            };
            btnClear.Click += BtnClear_Click;

            btnClearPlayed = new Button
            {
                Text = "Clear played matches",
                AutoSize = false,
                Font = new Font(FontFamily.GenericSerif, 20),
                Margin = new Padding(10, 0, 10, 0),
                Dock = DockStyle.None,
                Anchor = AnchorStyles.Top,
                Visible = false,
                Enabled = false,
                Height = fixedButtonHeight,
                Width = 260,
                MinimumSize = new Size(260, fixedButtonHeight),
                MaximumSize = new Size(260, fixedButtonHeight),
                TextAlign = ContentAlignment.MiddleCenter,
                AutoEllipsis = true,
                FlatStyle = FlatStyle.Standard
            };
            btnClearPlayed.Click += BtnClearPlayed_Click;

            buttonPanel.Height = fixedButtonHeight + buttonPanel.Padding.Top + buttonPanel.Padding.Bottom;

            buttonPanel.Controls.Add(btnAddMatch);
            buttonPanel.Controls.Add(btnClear);
            buttonPanel.Controls.Add(btnClearPlayed);

            buttonPanel.Layout += (s, e) =>
            {
                btnClearPlayed.Left = (buttonPanel.ClientSize.Width - btnClearPlayed.Width) / 2;
                btnClearPlayed.Top = (buttonPanel.ClientSize.Height - btnClearPlayed.Height) / 2;
            };

            // DataGridView
            dgv = new DataGridView
            {
                Dock = DockStyle.Fill,
                AllowUserToAddRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                Font = new Font(FontFamily.GenericSerif, 20),
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize,
                DefaultCellStyle =
                {
                    SelectionBackColor = SystemColors.Window,
                    SelectionForeColor = SystemColors.ControlText
                },
                Visible = false,
                Enabled = false
            };
            dgv.RowTemplate.Height = 50;
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "matchnumber", HeaderText = "Match #", ReadOnly = true, MinimumWidth = 200 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "date", HeaderText = "Date", ReadOnly = true });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "player1", HeaderText = "Player 1", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, MinimumWidth = 80 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "player2", HeaderText = "Player 2", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, MinimumWidth = 80 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "player3", HeaderText = "Player 3", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, MinimumWidth = 80 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "player4", HeaderText = "Player 4", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, MinimumWidth = 80 });
            dgv.Columns.Add(new DataGridViewButtonColumn { Name = "playedBtn", HeaderText = "Played", Text = "Played", UseColumnTextForButtonValue = true });
            dgv.Columns["date"].Visible = false;
            dgv.Columns["matchnumber"].ReadOnly = true;
            dgv.CellClick += Dgv_CellClick;
            dgv.CellValueChanged += Dgv_CellValueChanged;
            dgv.CurrentCellDirtyStateChanged += (s, e) =>
            {
                if (dgv.IsCurrentCellDirty)
                    dgv.CommitEdit(DataGridViewDataErrorContexts.Commit);
            };

            // Add controls in the correct order: locationPanel, buttonPanel, dgv
            Controls.Add(dgv);
            Controls.Add(buttonPanel);
            Controls.Add(locationPanel);
        }

        private void CmbLocation_SelectedIndexChanged(object sender, EventArgs e)
        {
            // Commit any pending edits before changing location
            if (dgv.IsCurrentCellInEditMode)
            {
                dgv.EndEdit();
            }
            // Optionally, force validation to trigger CellValueChanged
            dgv.CurrentCell = null;

            bool locationSelected = cmbLocation.SelectedIndex >= 0;
            btnAddMatch.Visible = locationSelected;
            btnAddMatch.Enabled = locationSelected;
            btnClear.Visible = locationSelected;
            btnClear.Enabled = locationSelected;
            btnClearPlayed.Visible = locationSelected;
            btnClearPlayed.Enabled = locationSelected;
            dgv.Visible = locationSelected;
            dgv.Enabled = locationSelected;
            LoadMatches();
        }

        private void LoadMatches()
        {
            dgv.Rows.Clear();
            if (cmbLocation == null || cmbLocation.SelectedIndex < 0)
                return;

            string selectedLocation = cmbLocation.SelectedItem.ToString();
            using var conn = new SqliteConnection(connectionString);
            conn.Open();
            string sql = "SELECT * FROM Matches WHERE (cleared IS NULL OR cleared = 0) AND Location = @location ORDER BY matchid";
            using var cmd = new SqliteCommand(sql, conn);
            cmd.Parameters.AddWithValue("@location", selectedLocation);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                int idx = dgv.Rows.Add(
                    reader["matchnumber"],
                    Convert.ToDateTime(reader["date"]).ToString("yyyy-MM-dd"),
                    reader["player1"],
                    reader["player2"],
                    reader["player3"],
                    reader["player4"],
                    "Played"
                );
                if (reader["played"] != DBNull.Value && Convert.ToInt32(reader["played"]) == 1)
                {
                    MarkRowAsPlayed(dgv.Rows[idx]);
                }
            }
            if (dgv.Columns.Contains("matchnumber"))
                dgv.Columns["matchnumber"].ReadOnly = true;
        }

        private void BtnAddMatch_Click(object sender, EventArgs e)
        {
            if (cmbLocation.SelectedIndex < 0)
                return;

            string date = DateTime.Now.ToString("yyyy-MM-dd");
            int nextMatchNumber = 1;
            string selectedLocation = cmbLocation.SelectedItem.ToString();
            using (var conn = new SqliteConnection(connectionString))
            {
                conn.Open();
                string getMaxSql = "SELECT MAX(matchnumber) FROM Matches WHERE date = @date AND Location = @location";
                using (var getMaxCmd = new SqliteCommand(getMaxSql, conn))
                {
                    getMaxCmd.Parameters.AddWithValue("@date", date);
                    getMaxCmd.Parameters.AddWithValue("@location", selectedLocation);
                    var result = getMaxCmd.ExecuteScalar();
                    if (result != DBNull.Value && result != null)
                        nextMatchNumber = Convert.ToInt32(result) + 1;
                }
                string insertSql = "INSERT INTO Matches (date, matchnumber, played, cleared, Location) VALUES (@date, @matchnumber, 0, 0, @location)";
                using (var insertCmd = new SqliteCommand(insertSql, conn))
                {
                    insertCmd.Parameters.AddWithValue("@date", date);
                    insertCmd.Parameters.AddWithValue("@matchnumber", nextMatchNumber);
                    insertCmd.Parameters.AddWithValue("@location", selectedLocation);
                    insertCmd.ExecuteNonQuery();
                }
            }
            LoadMatches();
            if (dgv.Rows.Count > 0)
            {
                var newRow = dgv.Rows[dgv.Rows.Count - 1];
                dgv.CurrentCell = newRow.Cells["player1"];
                dgv.BeginEdit(true);
            }
        }

        private void BtnClear_Click(object sender, EventArgs e)
        {
            string now = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            string location = cmbLocation.SelectedItem.ToString();
            using var conn = new SqliteConnection(connectionString);
            conn.Open();
            string sql = "UPDATE Matches SET cleared = 1, clearedtimestamp = @now WHERE (cleared IS NULL OR cleared = 0) AND location = @location";
            using var cmd = new SqliteCommand(sql, conn);
            cmd.Parameters.AddWithValue("@now", now);
            cmd.Parameters.AddWithValue("@location", location);
            cmd.ExecuteNonQuery();
            dgv.Rows.Clear();
        }

        private void BtnClearPlayed_Click(object sender, EventArgs e) // Add this method to your class
        {
            string now = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            using var conn = new SqliteConnection(connectionString);
            conn.Open();
            string sql = "UPDATE Matches SET cleared = 1, clearedtimestamp = @now WHERE played = 1 AND (cleared IS NULL OR cleared = 0)";
            using var cmd = new SqliteCommand(sql, conn);
            cmd.Parameters.AddWithValue("@now", now);
            cmd.ExecuteNonQuery();
            LoadMatches();
        }

        private void Dgv_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex != dgv.Columns["playedBtn"].Index)
                return;
            var row = dgv.Rows[e.RowIndex];
            if (row.DefaultCellStyle.Font != null && row.DefaultCellStyle.Font.Strikeout)
                return; // already played

            int matchnumber = Convert.ToInt32(row.Cells["matchnumber"].Value);
            string date = row.Cells["date"].Value.ToString();

            int matchid = -1;
            using (var conn = new SqliteConnection(connectionString))
            {
                conn.Open();
                string sql = "SELECT matchid FROM Matches WHERE date = @date AND matchnumber = @matchnumber";
                using (var cmd = new SqliteCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@date", date);
                    cmd.Parameters.AddWithValue("@matchnumber", matchnumber);
                    var result = cmd.ExecuteScalar();
                    if (result != null && result != DBNull.Value)
                        matchid = Convert.ToInt32(result);
                }
            }
            if (matchid == -1) return;

            string now = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            using (var conn = new SqliteConnection(connectionString))
            {
                conn.Open();
                string sql = @"UPDATE Matches SET played = 1, 
                    playedtimestamp = @now,
                    player1timestamp = COALESCE(player1timestamp, @now),
                    player2timestamp = COALESCE(player2timestamp, @now),
                    player3timestamp = COALESCE(player3timestamp, @now),
                    player4timestamp = COALESCE(player4timestamp, @now)
                    WHERE matchid = @matchid";
                using (var cmd = new SqliteCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@now", now);
                    cmd.Parameters.AddWithValue("@matchid", matchid);
                    cmd.ExecuteNonQuery();
                }
            }
            MarkRowAsPlayed(row);
            // Set the Played button cell color to grey
            var playedBtnCell = row.Cells["playedBtn"];
            playedBtnCell.Style.BackColor = Color.LightGray;
            playedBtnCell.Style.ForeColor = Color.DarkGray;
        }

        private void MarkRowAsPlayed(DataGridViewRow row)
        {
            row.DefaultCellStyle.Font = new Font(dgv.Font, FontStyle.Strikeout);
            row.DefaultCellStyle.BackColor = Color.LightGray;
            foreach (DataGridViewCell cell in row.Cells)
            {
                if (cell.OwningColumn.Name.StartsWith("player"))
                    cell.ReadOnly = true;
                // Also set Played button cell color to grey if this is the playedBtn column
                if (cell.OwningColumn.Name == "playedBtn")
                {
                    cell.Style.BackColor = Color.LightGray;
                    cell.Style.ForeColor = Color.DarkGray;
                }
            }
        }

        private void Dgv_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            var row = dgv.Rows[e.RowIndex];
            if (row.DefaultCellStyle.Font != null && row.DefaultCellStyle.Font.Strikeout)
                return; // played, do not update

            int matchnumber = Convert.ToInt32(row.Cells["matchnumber"].Value);
            string date = row.Cells["date"].Value.ToString();
            if (cmbLocation.SelectedIndex < 0)
                return;
            string selectedLocation = cmbLocation.SelectedItem.ToString();

            int matchid = -1;
            using (var conn = new SqliteConnection(connectionString))
            {
                conn.Open();
                string sql = "SELECT matchid FROM Matches WHERE date = @date AND matchnumber = @matchnumber and Location = @location";
                using (var cmd = new SqliteCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@date", date);
                    cmd.Parameters.AddWithValue("@matchnumber", matchnumber);
                    cmd.Parameters.AddWithValue("@location", selectedLocation);
                    var result = cmd.ExecuteScalar();
                    if (result != null && result != DBNull.Value)
                        matchid = Convert.ToInt32(result);
                }
            }
            if (matchid == -1) return;

            string[] players = new string[4];
            string[] timestampValues = new string[4];
            using (var conn = new SqliteConnection(connectionString))
            {
                conn.Open();
                string selectSql = "SELECT player1timestamp, player2timestamp, player3timestamp, player4timestamp FROM Matches WHERE matchid = @matchid";
                using (var selectCmd = new SqliteCommand(selectSql, conn))
                {
                    selectCmd.Parameters.AddWithValue("@matchid", matchid);
                    using var reader = selectCmd.ExecuteReader();
                    if (reader.Read())
                    {
                        for (int i = 0; i < 4; i++)
                            timestampValues[i] = reader.IsDBNull(i) ? null : reader.GetString(i);
                    }
                }
                string now = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                for (int i = 0; i < 4; i++)
                {
                    players[i] = row.Cells[$"player{i + 1}"].Value?.ToString() ?? "";
                    // check if this player's name is changed.  if so, set a flag to indicate it is changed.
                    string originalName = "";
                    string getNameSql = $"SELECT player{i + 1} FROM Matches WHERE matchid = @matchid";
                    using (var getNameCmd = new SqliteCommand(getNameSql, conn))
                    {
                        getNameCmd.Parameters.AddWithValue("@matchid", matchid);
                        var result = getNameCmd.ExecuteScalar();
                        if (result != null && result != DBNull.Value)
                            originalName = result.ToString();
                    }
                    if (players[i] != originalName)
                    {
                        timestampValues[i] = now;
                    }

                    if (!string.IsNullOrWhiteSpace(players[i]) && string.IsNullOrWhiteSpace(timestampValues[i]))
                    {
                        timestampValues[i] = now;
                    }
                }
                string sql = @"UPDATE Matches SET player1 = @p1, player2 = @p2, player3 = @p3, player4 = @p4,
                    player1timestamp = @t1, player2timestamp = @t2, player3timestamp = @t3, player4timestamp = @t4
                    WHERE matchid = @matchid";
                using var cmd = new SqliteCommand(sql, conn);
                cmd.Parameters.AddWithValue("@p1", players[0]);
                cmd.Parameters.AddWithValue("@p2", players[1]);
                cmd.Parameters.AddWithValue("@p3", players[2]);
                cmd.Parameters.AddWithValue("@p4", players[3]);
                cmd.Parameters.AddWithValue("@t1", (object)timestampValues[0] ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@t2", (object)timestampValues[1] ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@t3", (object)timestampValues[2] ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@t4", (object)timestampValues[3] ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@matchid", matchid);
                cmd.ExecuteNonQuery();
            }
        }
    }
}
