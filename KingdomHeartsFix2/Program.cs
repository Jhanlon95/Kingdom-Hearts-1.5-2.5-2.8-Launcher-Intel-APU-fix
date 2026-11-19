using System;
using System.Drawing;
using System.Windows.Forms;
using System.Diagnostics;
using System.IO;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace KingdomHeartsLauncher
{
    public class KHLauncherForm : Form
    {
        private static readonly string[] GameExes = new string[]
        {
            "KINGDOM HEARTS FINAL MIX.exe",
            "KINGDOM HEARTS Re_Chain of Memories.exe",
            "KINGDOM HEARTS II FINAL MIX.exe",
            "KINGDOM HEARTS Birth by Sleep FINAL MIX.exe",
            "KINGDOM HEARTS Theater.exe",
            "KINGDOM HEARTS Theater.exe"
        };

        private string launchArguments = "";
        private string gameDir = "";
        private Label lblStatus;
        private readonly string configPath;

        private List<Panel> gameCards = new List<Panel>();
        private int selectedIndex = 0;

        // XInput
        private System.Windows.Forms.Timer inputTimer;
        private XInputState prevState;

        public KHLauncherForm(string[] rawArgs)
        {
            configPath = Path.Combine(Application.StartupPath, "kh_launcher_path.txt");

            KillWaitScreen();

            // Simple argument handling (no longer saved to file)
            if (rawArgs != null && rawArgs.Length > 0)
            {
                launchArguments = string.Join(" ", rawArgs);
            }

            if (string.IsNullOrEmpty(launchArguments) || !launchArguments.Contains("-AUTH_"))
            {
                launchArguments = "-AUTH_LOGIN=unused -AUTH_PASSWORD=unused -AUTH_TYPE=exchangecode -epicusername=Player -epicuserid=00000000000000000000000000000000 -epiclocale=en -EpicPortal";
            }

            // Window setup
            this.Text = "Kingdom Hearts HD 1.5 + 2.5 ReMIX";
            this.Size = new Size(760, 590);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.BackColor = Color.FromArgb(14, 16, 24);
            this.ForeColor = Color.White;
            this.Font = new Font("Segoe UI", 9.5F, FontStyle.Regular);
            this.KeyPreview = true;
            this.KeyDown += KHLauncherForm_KeyDown;

            // Title
            Label lblTitle = new Label()
            {
                Text = "KINGDOM HEARTS",
                Font = new Font("Segoe UI", 22F, FontStyle.Bold),
                ForeColor = Color.FromArgb(245, 215, 115),
                AutoSize = false,
                Size = new Size(720, 42),
                Location = new Point(20, 15),
                TextAlign = ContentAlignment.MiddleCenter
            };
            this.Controls.Add(lblTitle);

            Label lblSub = new Label()
            {
                Text = "- HD 1.5 + 2.5 ReMIX -",
                Font = new Font("Segoe UI", 11F),
                ForeColor = Color.FromArgb(160, 180, 215),
                AutoSize = false,
                Size = new Size(720, 25),
                Location = new Point(20, 58),
                TextAlign = ContentAlignment.MiddleCenter
            };
            this.Controls.Add(lblSub);

            // Cards
            CreateGameCard("👑 Kingdom Hearts Final Mix", "The beginning of Sora, Donald and Goofy's adventure", 0, 30, 95);
            CreateGameCard("🃏 Re:Chain of Memories", "The card-based adventure in Castle Oblivion", 1, 380, 95);
            CreateGameCard("🗝️ Kingdom Hearts II Final Mix", "The ultimate battle against Organization XIII", 2, 30, 205);
            CreateGameCard("⚔️ Birth by Sleep Final Mix", "The origin of the saga with Terra, Ventus and Aqua", 3, 380, 205);
            CreateGameCard("🎬 358/2 Days (HD Cinematics)", "The emotional story of Roxas and Axel", 4, 30, 315);
            CreateGameCard("🎬 Re:coded (HD Cinematics)", "The investigation of Jiminy's Journal", 5, 380, 315);

            // Status
            lblStatus = new Label()
            {
                Font = new Font("Segoe UI", 8.5F),
                AutoSize = false,
                Size = new Size(430, 42),
                Location = new Point(30, 435),
                TextAlign = ContentAlignment.MiddleLeft
            };
            this.Controls.Add(lblStatus);

            // Buttons
            Button btnChangeFolder = new Button()
            {
                Text = "Change Folder",
                Size = new Size(130, 36),
                Location = new Point(480, 440),
                BackColor = Color.FromArgb(40, 45, 60),
                ForeColor = Color.FromArgb(220, 220, 230),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnChangeFolder.FlatAppearance.BorderSize = 0;
            btnChangeFolder.Click += (s, e) => SelectGameFolder();
            this.Controls.Add(btnChangeFolder);

            Button btnExit = new Button()
            {
                Text = "Exit",
                Size = new Size(110, 36),
                Location = new Point(620, 440),
                BackColor = Color.FromArgb(40, 45, 60),
                ForeColor = Color.FromArgb(220, 220, 230),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnExit.FlatAppearance.BorderSize = 0;
            btnExit.Click += (s, e) => this.Close();
            this.Controls.Add(btnExit);

            LoadGameFolder();
            UpdateSelection();

            // XInput polling
            inputTimer = new System.Windows.Forms.Timer();
            inputTimer.Interval = 50;
            inputTimer.Tick += InputTimer_Tick;
            inputTimer.Start();
        }

        private void CreateGameCard(string title, string desc, int gameCode, int x, int y)
        {
            Panel card = new Panel()
            {
                Size = new Size(340, 95),
                Location = new Point(x, y),
                BackColor = Color.FromArgb(24, 28, 40),
                Cursor = Cursors.Hand,
                Tag = gameCode
            };

            Label lbl = new Label()
            {
                Text = title,
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                ForeColor = Color.FromArgb(245, 245, 250),
                AutoSize = false,
                Size = new Size(320, 28),
                Location = new Point(12, 12),
                Cursor = Cursors.Hand
            };

            Label lblDesc = new Label()
            {
                Text = desc,
                Font = new Font("Segoe UI", 8.5F),
                ForeColor = Color.FromArgb(140, 155, 180),
                AutoSize = false,
                Size = new Size(320, 45),
                Location = new Point(12, 40),
                Cursor = Cursors.Hand
            };

            card.Controls.Add(lbl);
            card.Controls.Add(lblDesc);

            EventHandler click = (s, e) => LaunchGame(gameCode);
            card.Click += click;
            lbl.Click += click;
            lblDesc.Click += click;

            this.Controls.Add(card);
            gameCards.Add(card);
        }

        private void InputTimer_Tick(object sender, EventArgs e)
        {
            XInputState state;
            if (XInput.XInputGetState(0, out state) != 0) return;

            var buttons = state.Gamepad.wButtons;

            bool up = (buttons & 0x0001) != 0 || state.Gamepad.sThumbLY > 20000;
            bool down = (buttons & 0x0002) != 0 || state.Gamepad.sThumbLY < -20000;
            bool left = (buttons & 0x0004) != 0 || state.Gamepad.sThumbLX < -20000;
            bool right = (buttons & 0x0008) != 0 || state.Gamepad.sThumbLX > 20000;

            bool prevUp = (prevState.Gamepad.wButtons & 0x0001) != 0;
            bool prevDown = (prevState.Gamepad.wButtons & 0x0002) != 0;
            bool prevLeft = (prevState.Gamepad.wButtons & 0x0004) != 0;
            bool prevRight = (prevState.Gamepad.wButtons & 0x0008) != 0;

            if (up && !prevUp) { selectedIndex = Math.Max(0, selectedIndex - 2); UpdateSelection(); }
            if (down && !prevDown) { selectedIndex = Math.Min(5, selectedIndex + 2); UpdateSelection(); }
            if (left && !prevLeft) { selectedIndex = Math.Max(0, selectedIndex - 1); UpdateSelection(); }
            if (right && !prevRight) { selectedIndex = Math.Min(5, selectedIndex + 1); UpdateSelection(); }

            bool aPressed = (buttons & 0x1000) != 0;
            bool prevA = (prevState.Gamepad.wButtons & 0x1000) != 0;
            if (aPressed && !prevA)
            {
                LaunchGame((int)gameCards[selectedIndex].Tag);
            }

            bool bPressed = (buttons & 0x2000) != 0;
            bool prevB = (prevState.Gamepad.wButtons & 0x2000) != 0;
            if (bPressed && !prevB)
            {
                this.Close();
            }

            prevState = state;
        }

        private void KHLauncherForm_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.Up: selectedIndex = Math.Max(0, selectedIndex - 2); UpdateSelection(); break;
                case Keys.Down: selectedIndex = Math.Min(5, selectedIndex + 2); UpdateSelection(); break;
                case Keys.Left: selectedIndex = Math.Max(0, selectedIndex - 1); UpdateSelection(); break;
                case Keys.Right: selectedIndex = Math.Min(5, selectedIndex + 1); UpdateSelection(); break;
                case Keys.Enter:
                case Keys.Space:
                    LaunchGame((int)gameCards[selectedIndex].Tag);
                    break;
                case Keys.Escape:
                    this.Close();
                    break;
            }
        }

        private void UpdateSelection()
        {
            for (int i = 0; i < gameCards.Count; i++)
            {
                gameCards[i].BackColor = (i == selectedIndex)
                    ? Color.FromArgb(50, 80, 140)
                    : Color.FromArgb(24, 28, 40);
            }
        }

        private void LoadGameFolder()
        {
            // 1) Check the folder the launcher lives in (and the working directory), non-recursive
            string[] candidates = new string[]
            {
                Application.StartupPath,
                Environment.CurrentDirectory
            };

            foreach (string dir in candidates)
            {
                if (FolderHasGameExes(dir))
                {
                    gameDir = dir;
                    UpdateStatusLabel();
                    return;
                }
            }

            // 2) Otherwise fall back to the previously saved folder
            if (File.Exists(configPath))
            {
                try { gameDir = File.ReadAllText(configPath).Trim(); } catch { }
            }
            UpdateStatusLabel();
        }

        private static bool FolderHasGameExes(string dir)
        {
            if (string.IsNullOrWhiteSpace(dir) || !Directory.Exists(dir))
                return false;

            // Same check SelectGameFolder uses: the main KH1 exe must be in this exact folder
            return File.Exists(Path.Combine(dir, GameExes[0]));
        }

        private void SelectGameFolder()
        {
            using (var dialog = new FolderBrowserDialog())
            {
                dialog.Description = "Select the folder where Kingdom Hearts HD 1.5 + 2.5 ReMIX is installed";
                dialog.ShowNewFolderButton = false;
                if (!string.IsNullOrEmpty(gameDir) && Directory.Exists(gameDir))
                    dialog.SelectedPath = gameDir;

                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    string selected = dialog.SelectedPath;
                    if (File.Exists(Path.Combine(selected, GameExes[0])))
                    {
                        gameDir = selected;
                        try { File.WriteAllText(configPath, gameDir); } catch { }
                        UpdateStatusLabel();
                    }
                    else
                    {
                        MessageBox.Show("The selected folder does not contain the game executables.", "Incorrect Folder",
                            MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
            }
        }

        private void UpdateStatusLabel()
        {
            if (!string.IsNullOrWhiteSpace(gameDir) && Directory.Exists(gameDir))
            {
                lblStatus.Text = "✓ AMD/Intel Patch active  |  Controller supported (XInput)\nFolder: " + gameDir;
                lblStatus.ForeColor = Color.FromArgb(100, 210, 140);
            }
            else
            {
                lblStatus.Text = "No game folder selected.\nClick \"Change Folder\"";
                lblStatus.ForeColor = Color.FromArgb(255, 180, 80);
            }
        }

        private void LaunchGame(int code)
        {
            if (string.IsNullOrWhiteSpace(gameDir) || !Directory.Exists(gameDir))
            {
                MessageBox.Show("Please select the game folder first using \"Change Folder\".", "Folder Not Set",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string exePath = Path.Combine(gameDir, GameExes[code]);
            if (!File.Exists(exePath))
            {
                MessageBox.Show("Game executable not found:\n" + exePath, "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            KillWaitScreen();

            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = exePath,
                    WorkingDirectory = gameDir,
                    Arguments = launchArguments,
                    UseShellExecute = false
                });
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error starting the game:\n" + ex.Message, "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static void KillWaitScreen()
        {
            try
            {
                foreach (var p in Process.GetProcessesByName("WaitTitleProject"))
                    p.Kill();
            }
            catch { }
        }
    }

    public static class XInput
    {
        [DllImport("xinput1_4.dll")]
        public static extern int XInputGetState(int dwUserIndex, out XInputState pState);
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct XInputState
    {
        public uint dwPacketNumber;
        public XInputGamepad Gamepad;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct XInputGamepad
    {
        public ushort wButtons;
        public byte bLeftTrigger;
        public byte bRightTrigger;
        public short sThumbLX;
        public short sThumbLY;
        public short sThumbRX;
        public short sThumbRY;
    }

    public static class Program
    {
        [STAThread]
        public static int Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new KHLauncherForm(args));
            return 0;
        }
    }
}