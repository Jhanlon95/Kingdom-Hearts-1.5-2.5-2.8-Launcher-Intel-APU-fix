using System;
using System.Drawing;
using System.Windows.Forms;
using System.Diagnostics;
using System.IO;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.IO.MemoryMappedFiles;
using System.Threading;

namespace KingdomHeartsLauncher
{
    public class KHLauncherForm : Form
    {
        // Candidate executable paths per game, relative to the game folder (first match found wins).
        // If the exact name isn't found, a keyword search of the folder is used as a fallback
        // (see ResolveExe), so tweak these if your install uses different file names.
        private static readonly string[][] GameExeCandidates = new string[][]
        {
            new[] { "KINGDOM HEARTS Dream Drop Distance.exe", "KINGDOM HEARTS Dream Drop Distance HD.exe" },
            new[]
            {
                @"KINGDOM HEARTS 0.2 Birth by Sleep\Binaries\Win64\KINGDOM HEARTS 0.2 Birth by Sleep.exe",
                "KINGDOM HEARTS 0.2 Birth by Sleep.exe",
                "KINGDOM HEARTS 0.2 Birth by Sleep A fragmentary passage.exe"
            },
            // Back Cover: no exe guessed here. The user locates the ORIGINAL (non-patched) launcher
            // with the "Locate Original Launcher" button (see originalLauncherPath).
            new string[0]
        };

        // Keywords used for the fallback search (all must appear in the exe name, case-insensitive)
        private static readonly string[][] GameExeKeywords = new string[][]
        {
            new[] { "dream", "drop" },
            new[] { "0.2" },
            new string[0]
        };

        private const int GameCount = 3;
        private const int BirthBySleepCode = 1;
        private const int BackCoverCode = 2;

        // 0.2 Birth by Sleep refuses to log in when started directly on the Epic version
        // even with valid AUTH arguments, unless the Mare_mem shared memory is created.
        // We therefore launch it directly (with Mare_mem) whenever real Epic args are present.
        // Back Cover still always goes through the original launcher.
        private bool UsesOriginalLauncher(int code)
        {
            if (code == BackCoverCode)
                return true;

            if (code == BirthBySleepCode)
                return !usingEpicArgs;   // only fall back when placeholder args are in use

            return false;
        }

        private string launchArguments = "";
        private string gameDir = "";
        private Label lblStatus;
        private Label lblOriginalStatus;
        private Label lblLoginStatus;
        private bool usingEpicArgs = false;
        private readonly string configPath;
        private readonly string originalLauncherConfigPath;
        private string originalLauncherPath = "";

        private List<Panel> gameCards = new List<Panel>();
        private int selectedIndex = 0;

        // XInput
        private System.Windows.Forms.Timer inputTimer;
        private XInputState prevState;

        public KHLauncherForm(string[] rawArgs)
        {
            configPath = Path.Combine(Application.StartupPath, "kh_launcher_path.txt");

            originalLauncherConfigPath = Path.Combine(Application.StartupPath, "kh_original_launcher_path.txt");
            try
            {
                if (File.Exists(originalLauncherConfigPath))
                    originalLauncherPath = File.ReadAllText(originalLauncherConfigPath).Trim();
            }
            catch { }

            KillWaitScreen();

            // Simple argument handling (no longer saved to file)
            if (rawArgs != null && rawArgs.Length > 0)
            {
                launchArguments = string.Join(" ", rawArgs);
            }

            usingEpicArgs = !string.IsNullOrEmpty(launchArguments) && launchArguments.Contains("-AUTH_");

            if (!usingEpicArgs)
            {
                launchArguments = "-AUTH_LOGIN=unused -AUTH_PASSWORD=unused -AUTH_TYPE=exchangecode -epicusername=Player -epicuserid=00000000000000000000000000000000 -epiclocale=en -EpicPortal";
            }

            // Window setup
            this.Text = "Kingdom Hearts HD 2.8 Final Chapter Prologue";
            this.Size = new Size(760, 615);
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
                Text = "- HD 2.8 Final Chapter Prologue -",
                Font = new Font("Segoe UI", 11F),
                ForeColor = Color.FromArgb(160, 180, 215),
                AutoSize = false,
                Size = new Size(720, 25),
                Location = new Point(20, 58),
                TextAlign = ContentAlignment.MiddleCenter
            };
            this.Controls.Add(lblSub);

            // Cards (single column)
            CreateGameCard("💤 Dream Drop Distance HD", "Sora and Riku take the Mark of Mastery exam", 0, 30, 100);
            CreateGameCard("🌌 0.2 Birth by Sleep - A fragmentary passage -", "Aqua's journey through the Realm of Darkness (direct launch + Mare_mem when started from Epic)", 1, 30, 210);
            CreateGameCard("🎬 χ Back Cover (HD Cinematic)", "Opens the original launcher to play the Back Cover movie", 2, 30, 320);

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

            lblOriginalStatus = new Label()
            {
                Font = new Font("Segoe UI", 8.5F),
                AutoSize = false,
                Size = new Size(430, 42),
                Location = new Point(30, 485),
                TextAlign = ContentAlignment.MiddleLeft
            };
            this.Controls.Add(lblOriginalStatus);

            lblLoginStatus = new Label()
            {
                Font = new Font("Segoe UI", 8.5F),
                AutoSize = false,
                Size = new Size(430, 24),
                Location = new Point(30, 530),
                TextAlign = ContentAlignment.MiddleLeft
            };
            if (usingEpicArgs)
            {
                lblLoginStatus.Text = "✓ Epic login arguments received from the Epic Games Launcher";
                lblLoginStatus.ForeColor = Color.FromArgb(100, 210, 140);
            }
            else
            {
                lblLoginStatus.Text = "⚠ Placeholder login in use (not started from Epic Games Launcher)";
                lblLoginStatus.ForeColor = Color.FromArgb(255, 180, 80);
            }
            this.Controls.Add(lblLoginStatus);

            Button btnOriginal = new Button()
            {
                Text = "Locate Original Launcher",
                Size = new Size(250, 36),
                Location = new Point(480, 485),
                BackColor = Color.FromArgb(40, 45, 60),
                ForeColor = Color.FromArgb(220, 220, 230),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnOriginal.FlatAppearance.BorderSize = 0;
            btnOriginal.Click += (s, e) => SelectOriginalLauncher();
            this.Controls.Add(btnOriginal);

            LoadGameFolder();
            UpdateOriginalStatusLabel();
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
                Size = new Size(680, 95),
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
                Size = new Size(650, 28),
                Location = new Point(12, 12),
                Cursor = Cursors.Hand
            };

            Label lblDesc = new Label()
            {
                Text = desc,
                Font = new Font("Segoe UI", 8.5F),
                ForeColor = Color.FromArgb(140, 155, 180),
                AutoSize = false,
                Size = new Size(650, 45),
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

            if (up && !prevUp) { selectedIndex = Math.Max(0, selectedIndex - 1); UpdateSelection(); }
            if (down && !prevDown) { selectedIndex = Math.Min(GameCount - 1, selectedIndex + 1); UpdateSelection(); }
            if (left && !prevLeft) { selectedIndex = Math.Max(0, selectedIndex - 1); UpdateSelection(); }
            if (right && !prevRight) { selectedIndex = Math.Min(GameCount - 1, selectedIndex + 1); UpdateSelection(); }

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
                case Keys.Up: selectedIndex = Math.Max(0, selectedIndex - 1); UpdateSelection(); break;
                case Keys.Down: selectedIndex = Math.Min(GameCount - 1, selectedIndex + 1); UpdateSelection(); break;
                case Keys.Left: selectedIndex = Math.Max(0, selectedIndex - 1); UpdateSelection(); break;
                case Keys.Right: selectedIndex = Math.Min(GameCount - 1, selectedIndex + 1); UpdateSelection(); break;
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

        private bool FolderHasGameExes(string dir)
        {
            if (string.IsNullOrWhiteSpace(dir) || !Directory.Exists(dir))
                return false;

            // The folder is valid if at least one of the 2.8 game executables (Dream Drop Distance
            // or 0.2 Birth by Sleep) is found. Back Cover uses the user-selected original launcher.
            for (int i = 0; i < GameCount - 1; i++)
            {
                if (ResolveExe(dir, i) != null)
                    return true;
            }
            return false;
        }

        // Finds the executable for a game: exact candidate names first, then a keyword search
        private string ResolveExe(string dir, int code)
        {
            if (code == BackCoverCode)
                return GetOriginalLauncherIfValid();

            if (string.IsNullOrWhiteSpace(dir) || !Directory.Exists(dir))
                return null;

            foreach (string name in GameExeCandidates[code])
            {
                string path = Path.Combine(dir, name);
                if (File.Exists(path) && !IsSelf(path))
                    return path;
            }

            // Fallback: keyword search of the folder and its subfolders (limited depth)
            return FindExeByKeywords(dir, GameExeKeywords[code], 4);
        }

        // Never resolve to this launcher itself (it may share the original launcher's file name)
        private static bool IsSelf(string path)
        {
            try
            {
                return string.Equals(Path.GetFullPath(path), Path.GetFullPath(Application.ExecutablePath),
                    StringComparison.OrdinalIgnoreCase);
            }
            catch { return false; }
        }

        private static string FindExeByKeywords(string dir, string[] keywords, int depth)
        {
            try
            {
                foreach (string file in Directory.GetFiles(dir, "*.exe", SearchOption.TopDirectoryOnly))
                {
                    string fileName = Path.GetFileName(file);
                    bool allMatch = true;
                    foreach (string kw in keywords)
                    {
                        if (fileName.IndexOf(kw, StringComparison.OrdinalIgnoreCase) < 0)
                        {
                            allMatch = false;
                            break;
                        }
                    }
                    if (allMatch && !IsSelf(file))
                        return file;
                }

                if (depth > 0)
                {
                    foreach (string sub in Directory.GetDirectories(dir))
                    {
                        string found = FindExeByKeywords(sub, keywords, depth - 1);
                        if (found != null)
                            return found;
                    }
                }
            }
            catch { }

            return null;
        }

        private void SelectGameFolder()
        {
            using (var dialog = new FolderBrowserDialog())
            {
                dialog.Description = "Select the folder where Kingdom Hearts HD 2.8 Final Chapter Prologue is installed";
                dialog.ShowNewFolderButton = false;
                if (!string.IsNullOrEmpty(gameDir) && Directory.Exists(gameDir))
                    dialog.SelectedPath = gameDir;

                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    string selected = dialog.SelectedPath;
                    if (FolderHasGameExes(selected))
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

        private string GetOriginalLauncherIfValid()
        {
            if (!string.IsNullOrWhiteSpace(originalLauncherPath) && File.Exists(originalLauncherPath) && !IsSelf(originalLauncherPath))
                return originalLauncherPath;
            return null;
        }

        private bool SelectOriginalLauncher()
        {
            using (var dialog = new OpenFileDialog())
            {
                dialog.Title = "Select the ORIGINAL (non-patched) Kingdom Hearts HD 2.8 launcher";
                dialog.Filter = "Executable files (*.exe)|*.exe";
                dialog.CheckFileExists = true;

                string startDir = !string.IsNullOrEmpty(originalLauncherPath)
                    ? Path.GetDirectoryName(originalLauncherPath)
                    : gameDir;
                if (!string.IsNullOrEmpty(startDir) && Directory.Exists(startDir))
                    dialog.InitialDirectory = startDir;

                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    if (IsSelf(dialog.FileName))
                    {
                        MessageBox.Show("That is this launcher. Please select the original launcher instead.",
                            "Wrong File", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return false;
                    }

                    originalLauncherPath = dialog.FileName;
                    try { File.WriteAllText(originalLauncherConfigPath, originalLauncherPath); } catch { }
                    UpdateOriginalStatusLabel();
                    return true;
                }
            }
            return false;
        }

        private void UpdateOriginalStatusLabel()
        {
            if (string.IsNullOrWhiteSpace(originalLauncherPath))
            {
                lblOriginalStatus.Text = "Original launcher not set (needed for Back Cover and for 0.2 when not started from Epic).\nClick \"Locate Original Launcher\"";
                lblOriginalStatus.ForeColor = Color.FromArgb(255, 180, 80);
            }
            else if (File.Exists(originalLauncherPath))
            {
                lblOriginalStatus.Text = "✓ Original launcher found\n" + originalLauncherPath;
                lblOriginalStatus.ForeColor = Color.FromArgb(100, 210, 140);
            }
            else
            {
                lblOriginalStatus.Text = "✗ Original launcher missing at saved location\n" + originalLauncherPath;
                lblOriginalStatus.ForeColor = Color.FromArgb(240, 110, 110);
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

            bool viaOriginal = UsesOriginalLauncher(code);
            string exePath = viaOriginal ? GetOriginalLauncherIfValid() : ResolveExe(gameDir, code);

            if (exePath == null && viaOriginal)
            {
                MessageBox.Show("This game is started through the original launcher.\nPlease select it now.",
                    "Original Launcher Not Set", MessageBoxButtons.OK, MessageBoxIcon.Information);
                if (SelectOriginalLauncher())
                    exePath = GetOriginalLauncherIfValid();
                if (exePath == null)
                    return;
            }
            if (exePath == null)
            {
                MessageBox.Show("Game executable not found in:\n" + gameDir, "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            KillWaitScreen();

            // 0.2 needs the Mare_mem shared memory that the original launcher normally creates.
            bool needsMareMem = (code == BirthBySleepCode);

            try
            {
                if (needsMareMem)
                {
                    // Keep Mare_mem alive
                    var mmf = MemoryMappedFile.CreateOrOpen("Mare_mem", 2048, MemoryMappedFileAccess.ReadWrite);
                    using (var stream = mmf.CreateViewStream())
                    {
                        byte[] init = new byte[2048];
                        stream.Write(init, 0, init.Length);
                    }

                    var psi = new ProcessStartInfo
                    {
                        FileName = exePath,
                        WorkingDirectory = Path.GetDirectoryName(exePath) ?? gameDir,
                        Arguments = launchArguments,
                        UseShellExecute = false
                    };

                    Process gameProc = Process.Start(psi);

                    // Hide and disable all input
                    this.Hide();
                    this.ShowInTaskbar = false;
                    this.KeyPreview = false;

                    if (inputTimer != null)
                    {
                        inputTimer.Stop();
                    }

                    // Wait for the game to exit, then close the launcher
                    ThreadPool.QueueUserWorkItem(_ =>
                    {
                        try
                        {
                            if (gameProc != null)
                                gameProc.WaitForExit();

                            // Extra safety for possible stub processes
                            string[] possibleNames =
                            {
                "KINGDOM HEARTS 0.2 Birth by Sleep",
                "KINGDOM HEARTS 0.2 Birth by Sleep A fragmentary passage"
            };

                            foreach (string name in possibleNames)
                            {
                                foreach (var p in Process.GetProcessesByName(name))
                                {
                                    try { p.WaitForExit(); } catch { }
                                }
                            }
                        }
                        catch { }
                        finally
                        {
                            try { Environment.Exit(0); } catch { }
                        }
                    });
                }
                else
                {
                    // Dream Drop Distance and Back Cover (via original)
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = exePath,
                        WorkingDirectory = viaOriginal ? Path.GetDirectoryName(exePath) : gameDir,
                        Arguments = launchArguments,
                        UseShellExecute = false
                    });
                    this.Close();
                }
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
            // Prevent a second instance of the patched launcher from showing a window.
            // The game / EOS often re-launches the main 2.8 executable.
            bool createdNew;
            using (var mutex = new Mutex(true, "Global\\KH28_PatchedLauncher_SingleInstance", out createdNew))
            {
                if (!createdNew)
                {
                    // Another instance is already running → exit silently
                    return 0;
                }

                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new KHLauncherForm(args));
                return 0;
            }
        }
    }
}