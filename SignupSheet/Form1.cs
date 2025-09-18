using System;
using System.Data;
using Microsoft.Data.Sqlite;
using System.Drawing;
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

        public Form1()
        {
            InitializeComponent();
            connectionString = $"Data Source={dbPath}";
            InitializeDatabase();
            InitializeUI();
            LoadMatches();
        }

        private void InitializeDatabase()
        {
            // If schema is wrong, drop and recreate table (for dev/demo only)
            using var conn = new SqliteConnection(connectionString);
            conn.Open();
            string checkSql = "PRAGMA table_info(Matches);";
            using (var checkCmd = new SqliteCommand(checkSql, conn))
            using (var reader = checkCmd.ExecuteReader())
            {
                bool needsRecreate = false;
                bool hasPlayedTimestamp = false;
                bool hasClearedTimestamp = false;
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
                }
                if (needsRecreate || !hasPlayedTimestamp || !hasClearedTimestamp)
                {
                    using var dropCmd = new SqliteCommand("DROP TABLE IF EXISTS Matches;", conn);
                    dropCmd.ExecuteNonQuery();
                }
            }
            string sql = @"CREATE TABLE IF NOT EXISTS Matches (
                matchid INTEGER PRIMARY KEY AUTOINCREMENT,
                date DATE NOT NULL,
                player1 TEXT,
                player2 TEXT,
                player3 TEXT,
                player4 TEXT,
                player1timestamp TEXT,
                player2timestamp TEXT,
                player3timestamp TEXT,
                player4timestamp TEXT,
                played INTEGER,
                playedtimestamp TEXT,
                cleared INTEGER,
                clearedtimestamp TEXT
            );";
            using var cmd = new SqliteCommand(sql, conn);
            cmd.ExecuteNonQuery();
        }

        private void InitializeUI()
        {
            dgv = new DataGridView
            {
                Dock = DockStyle.Top,
                Height = 300,
                AllowUserToAddRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                DefaultCellStyle =
                {
                    SelectionBackColor = SystemColors.Window,
                    SelectionForeColor = SystemColors.ControlText
                }
            };
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "matchid", HeaderText = "Match ID", ReadOnly = true });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "date", HeaderText = "Date", ReadOnly = true });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "player1", HeaderText = "Player 1", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, MinimumWidth = 80 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "player2", HeaderText = "Player 2", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, MinimumWidth = 80 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "player3", HeaderText = "Player 3", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, MinimumWidth = 80 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "player4", HeaderText = "Player 4", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, MinimumWidth = 80 });
            dgv.Columns.Add(new DataGridViewButtonColumn { Name = "playedBtn", HeaderText = "Played", Text = "Played", UseColumnTextForButtonValue = true });
            dgv.Columns["date"].Visible = false; // Hide the date column
            dgv.Columns["matchid"].ReadOnly = true; // Ensure matchid is always read-only
            dgv.CellClick += Dgv_CellClick;
            dgv.CellValueChanged += Dgv_CellValueChanged;
            dgv.CurrentCellDirtyStateChanged += (s, e) =>
            {
                if (dgv.IsCurrentCellDirty)
                    dgv.CommitEdit(DataGridViewDataErrorContexts.Commit);
            };
            Controls.Add(dgv);

            // Panel for buttons
            var buttonPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 50,
                Padding = new Padding(10, 10, 10, 10)
            };

            btnAddMatch = new Button
            {
                Text = "Add a new match",
                Height = 30,
                Width = 150,
                Margin = new Padding(0, 0, 10, 0),
                Dock = DockStyle.Left
            };
            btnAddMatch.Click += BtnAddMatch_Click;

            btnClear = new Button
            {
                Text = "Clear",
                Height = 30,
                Width = 100,
                Margin = new Padding(10, 0, 0, 0),
                Dock = DockStyle.Right
            };
            btnClear.Click += BtnClear_Click;

            buttonPanel.Controls.Add(btnAddMatch);
            buttonPanel.Controls.Add(btnClear);
            Controls.Add(buttonPanel);
        }

        private void LoadMatches()
        {
            dgv.Rows.Clear();
            using var conn = new SqliteConnection(connectionString);
            conn.Open();
            string sql = "SELECT * FROM Matches WHERE cleared IS NULL OR cleared = 0 ORDER BY date DESC";
            using var cmd = new SqliteCommand(sql, conn);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                int idx = dgv.Rows.Add(
                    reader["matchid"],
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
            if (dgv.Columns.Contains("matchid"))
                dgv.Columns["matchid"].ReadOnly = true; // Ensure matchid is always read-only after loading data
        }

        private void BtnAddMatch_Click(object sender, EventArgs e)
        {
            string date = DateTime.Now.ToString("yyyy-MM-dd");
            using var conn = new SqliteConnection(connectionString);
            conn.Open();
            string sql = "INSERT INTO Matches (date, played, cleared) VALUES (@date, 0, 0)";
            using var cmd = new SqliteCommand(sql, conn);
            cmd.Parameters.AddWithValue("@date", date);
            cmd.ExecuteNonQuery();
            LoadMatches();
            // Focus player1 cell in the new row (newest row is at the bottom)
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
            using var conn = new SqliteConnection(connectionString);
            conn.Open();
            string sql = "UPDATE Matches SET cleared = 1, clearedtimestamp = @now WHERE cleared IS NULL OR cleared = 0";
            using var cmd = new SqliteCommand(sql, conn);
            cmd.Parameters.AddWithValue("@now", now);
            cmd.ExecuteNonQuery();
            dgv.Rows.Clear();
        }

        private void Dgv_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex != dgv.Columns["playedBtn"].Index)
                return;
            var row = dgv.Rows[e.RowIndex];
            if (row.DefaultCellStyle.Font != null && row.DefaultCellStyle.Font.Strikeout)
                return; // already played
            int matchid = Convert.ToInt32(row.Cells["matchid"].Value);
            string date = row.Cells["date"].Value.ToString();
            string now = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            using var conn = new SqliteConnection(connectionString);
            conn.Open();
            string sql = @"UPDATE Matches SET played = 1, 
                playedtimestamp = @now,
                player1timestamp = COALESCE(player1timestamp, @now),
                player2timestamp = COALESCE(player2timestamp, @now),
                player3timestamp = COALESCE(player3timestamp, @now),
                player4timestamp = COALESCE(player4timestamp, @now)
                WHERE matchid = @matchid AND date = @date";
            using var cmd = new SqliteCommand(sql, conn);
            cmd.Parameters.AddWithValue("@now", now);
            cmd.Parameters.AddWithValue("@matchid", matchid);
            cmd.Parameters.AddWithValue("@date", date);
            cmd.ExecuteNonQuery();
            MarkRowAsPlayed(row);
        }

        private void MarkRowAsPlayed(DataGridViewRow row)
        {
            row.DefaultCellStyle.Font = new Font(dgv.Font, FontStyle.Strikeout);
            row.DefaultCellStyle.BackColor = Color.LightGray;
            foreach (DataGridViewCell cell in row.Cells)
            {
                if (cell.OwningColumn.Name.StartsWith("player"))
                    cell.ReadOnly = true;
            }
        }

        private void Dgv_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            var row = dgv.Rows[e.RowIndex];
            if (row.DefaultCellStyle.Font != null && row.DefaultCellStyle.Font.Strikeout)
                return; // played, do not update
            int matchid = Convert.ToInt32(row.Cells["matchid"].Value);
            string date = row.Cells["date"].Value.ToString();
            string[] players = new string[4];
            string[] timestampCols = { "player1timestamp", "player2timestamp", "player3timestamp", "player4timestamp" };
            bool updateTimestamps = false;
            string[] timestampValues = new string[4];
            using var conn = new SqliteConnection(connectionString);
            conn.Open();
            // Get current timestamps from DB
            string selectSql = "SELECT player1timestamp, player2timestamp, player3timestamp, player4timestamp FROM Matches WHERE matchid = @matchid AND date = @date";
            using (var selectCmd = new SqliteCommand(selectSql, conn))
            {
                selectCmd.Parameters.AddWithValue("@matchid", matchid);
                selectCmd.Parameters.AddWithValue("@date", date);
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
                // If player name is not empty and timestamp is null, set timestamp
                if (!string.IsNullOrWhiteSpace(players[i]) && string.IsNullOrWhiteSpace(timestampValues[i]))
                {
                    timestampValues[i] = now;
                    updateTimestamps = true;
                }
            }
            // Update player names and timestamps
            string sql = @"UPDATE Matches SET player1 = @p1, player2 = @p2, player3 = @p3, player4 = @p4,
                player1timestamp = @t1, player2timestamp = @t2, player3timestamp = @t3, player4timestamp = @t4
                WHERE matchid = @matchid AND date = @date";
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
            cmd.Parameters.AddWithValue("@date", date);
            cmd.ExecuteNonQuery();
        }
    }
}
