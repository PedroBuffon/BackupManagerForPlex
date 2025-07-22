using System;
using System.IO;
using System.Diagnostics;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using System.Threading.Tasks;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PlexBackupApp
{
    /// <summary>
    /// Title: Plex Backup Application
    /// Author: Pedro Buffon
    /// Purpose: Backs up Plex Database Files & Registry Keys
    /// Usage: GUI Application with advanced features
    /// Version: 3.0 (GUI Version)
    /// Last Updated: 22/07/2025
    /// </summary>
    
    // Configuration class for JSON serialization
    public class PlexBackupConfig
    {
        public string BackupPath { get; set; } = "";
        public bool IncludeRegistry { get; set; } = true;
        public bool IncludeFiles { get; set; } = true;
        public bool IncludeLogs { get; set; } = false;
        public bool StopPlex { get; set; } = true;
        public int RetentionDays { get; set; } = 2; // Index for combobox (30 days default)
        public bool UseJsonConfig { get; set; } = true;
        public DateTime LastBackup { get; set; } = DateTime.MinValue;
        public int TotalBackupsCreated { get; set; } = 0;
        public List<string> CustomPlexPaths { get; set; } = new List<string>();
        public bool MinimizeToTray { get; set; } = false;
        public bool ShowNotifications { get; set; } = true;
        public bool AutoStartWithWindows { get; set; } = false;
        public string LogLevel { get; set; } = "Info"; // Debug, Info, Warning, Error
        public bool HasShownTrayNotification { get; set; } = false;
    }

    public partial class PlexBackupForm : Form
    {
        private PlexBackupConfig config;
        private string configJsonPath;
        private string configIniPath;
        private string rootBackup;
        private const string PlexRegistryPath = @"HKEY_CURRENT_USER\Software\Plex, Inc.";
        private readonly string[] PlexExecutablePaths = {
            @"C:\Program Files (x86)\Plex\Plex Media Server\Plex Media Server.exe",
            @"C:\Program Files\Plex\Plex Media Server\Plex Media Server.exe"
        };

        // UI Controls
        private TextBox txtBackupPath;
        private Button btnBrowse;
        private Button btnBackupNow;
        private Button btnScheduleBackup;
        private CheckBox chkIncludeRegistry;
        private CheckBox chkIncludeFiles;
        private CheckBox chkIncludeLogs;
        private CheckBox chkStopPlex;
        private CheckBox chkMinimizeToTray;
        private ComboBox cmbRetentionDays;
        private ProgressBar progressBar;
        private RichTextBox txtLog;
        private Label lblStatus;
        private Button btnViewBackups;
        private Button btnRestoreBackup;
        private Button btnSettings;
        
        // System Tray Components
        private NotifyIcon notifyIcon;
        private ContextMenuStrip trayContextMenu;
        private ToolStripMenuItem trayShowItem;
        private ToolStripMenuItem trayBackupNowItem;
        private ToolStripMenuItem trayExitItem;

        [STAThread]
        public static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new PlexBackupForm());
        }

        public PlexBackupForm()
        {
            // Initialize configuration paths
            configJsonPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.json");
            configIniPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.ini");
            config = new PlexBackupConfig();
            
            InitializeComponent();
            InitializeSystemTray();
            LoadConfiguration();
        }

        private void InitializeComponent()
        {
            this.Text = "Plex Backup Manager v3.0";
            this.Size = new Size(800, 700);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.Icon = SystemIcons.Application;

            // Main container
            var mainPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 8,
                Padding = new Padding(10)
            };

            // Column styles
            mainPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30F));
            mainPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 70F));

            // Row styles
            for (int i = 0; i < 8; i++)
            {
                mainPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            }

            // Backup Path Section
            var lblBackupPath = new Label
            {
                Text = "Backup Path:",
                AutoSize = true,
                Anchor = AnchorStyles.Left,
                Font = new Font("Arial", 10F, FontStyle.Bold)
            };

            var pathPanel = new Panel { Height = 35, Dock = DockStyle.Fill };
            txtBackupPath = new TextBox
            {
                Width = 400,
                Height = 25,
                Anchor = AnchorStyles.Left | AnchorStyles.Top
            };

            btnBrowse = new Button
            {
                Text = "Browse...",
                Width = 80,
                Height = 25,
                Location = new Point(410, 0)
            };
            btnBrowse.Click += BtnBrowse_Click;

            pathPanel.Controls.Add(txtBackupPath);
            pathPanel.Controls.Add(btnBrowse);

            // Options Section
            var lblOptions = new Label
            {
                Text = "Backup Options:",
                AutoSize = true,
                Anchor = AnchorStyles.Left,
                Font = new Font("Arial", 10F, FontStyle.Bold)
            };

            var optionsPanel = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.TopDown,
                Height = 140,
                Dock = DockStyle.Fill
            };

            chkIncludeRegistry = new CheckBox { Text = "Include Registry Backup", Checked = true, AutoSize = true };
            chkIncludeFiles = new CheckBox { Text = "Include File Backup", Checked = true, AutoSize = true };
            chkIncludeLogs = new CheckBox { Text = "Include Previous Logs", Checked = false, AutoSize = true };
            chkStopPlex = new CheckBox { Text = "Stop Plex During Backup", Checked = true, AutoSize = true };
            chkMinimizeToTray = new CheckBox { Text = "Minimize to System Tray", Checked = false, AutoSize = true };
            chkMinimizeToTray.CheckedChanged += ChkMinimizeToTray_CheckedChanged;

            optionsPanel.Controls.AddRange(new Control[] { chkIncludeRegistry, chkIncludeFiles, chkIncludeLogs, chkStopPlex, chkMinimizeToTray });

            // Retention Section
            var lblRetention = new Label
            {
                Text = "Keep Backups For:",
                AutoSize = true,
                Anchor = AnchorStyles.Left,
                Font = new Font("Arial", 10F, FontStyle.Bold)
            };

            cmbRetentionDays = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Width = 200
            };
            cmbRetentionDays.Items.AddRange(new string[] { "7 days", "14 days", "30 days", "60 days", "Never delete" });
            cmbRetentionDays.SelectedIndex = 2; // 30 days default

            // Action Buttons
            var btnPanel = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.LeftToRight,
                Height = 40,
                Dock = DockStyle.Fill
            };

            btnBackupNow = new Button
            {
                Text = "Backup Now",
                Width = 100,
                Height = 35,
                BackColor = Color.LightGreen
            };
            btnBackupNow.Click += BtnBackupNow_Click;

            btnScheduleBackup = new Button
            {
                Text = "Schedule Backup",
                Width = 120,
                Height = 35,
                BackColor = Color.LightBlue
            };
            btnScheduleBackup.Click += BtnScheduleBackup_Click;

            btnViewBackups = new Button
            {
                Text = "View Backups",
                Width = 100,
                Height = 35
            };
            btnViewBackups.Click += BtnViewBackups_Click;

            btnRestoreBackup = new Button
            {
                Text = "Restore",
                Width = 80,
                Height = 35,
                BackColor = Color.LightCoral
            };
            btnRestoreBackup.Click += BtnRestoreBackup_Click;

            btnSettings = new Button
            {
                Text = "Settings",
                Width = 80,
                Height = 35
            };
            btnSettings.Click += BtnSettings_Click;

            btnPanel.Controls.AddRange(new Control[] { btnBackupNow, btnScheduleBackup, btnViewBackups, btnRestoreBackup, btnSettings });

            // Progress Bar
            progressBar = new ProgressBar
            {
                Dock = DockStyle.Fill,
                Height = 25,
                Visible = false
            };

            // Status Label
            lblStatus = new Label
            {
                Text = "Ready",
                AutoSize = true,
                Anchor = AnchorStyles.Left,
                Font = new Font("Arial", 9F, FontStyle.Italic)
            };

            // Log Section
            var lblLog = new Label
            {
                Text = "Operation Log:",
                AutoSize = true,
                Anchor = AnchorStyles.Left,
                Font = new Font("Arial", 10F, FontStyle.Bold)
            };

            txtLog = new RichTextBox
            {
                Height = 200,
                Dock = DockStyle.Fill,
                ReadOnly = true,
                BackColor = Color.Black,
                ForeColor = Color.LimeGreen,
                Font = new Font("Consolas", 9F)
            };

            // Add controls to main panel
            mainPanel.Controls.Add(lblBackupPath, 0, 0);
            mainPanel.Controls.Add(pathPanel, 1, 0);
            mainPanel.Controls.Add(lblOptions, 0, 1);
            mainPanel.Controls.Add(optionsPanel, 1, 1);
            mainPanel.Controls.Add(lblRetention, 0, 2);
            mainPanel.Controls.Add(cmbRetentionDays, 1, 2);
            mainPanel.Controls.Add(new Label { Text = "Actions:", AutoSize = true, Font = new Font("Arial", 10F, FontStyle.Bold) }, 0, 3);
            mainPanel.Controls.Add(btnPanel, 1, 3);
            mainPanel.Controls.Add(progressBar, 0, 4);
            mainPanel.SetColumnSpan(progressBar, 2);
            mainPanel.Controls.Add(lblStatus, 0, 5);
            mainPanel.SetColumnSpan(lblStatus, 2);
            mainPanel.Controls.Add(lblLog, 0, 6);
            mainPanel.SetColumnSpan(lblLog, 2);
            mainPanel.Controls.Add(txtLog, 0, 7);
            mainPanel.SetColumnSpan(txtLog, 2);

            this.Controls.Add(mainPanel);

            // Add welcome message to log
            LogMessage("Plex Backup Manager v3.0 - Ready for operation", Color.White);
        }

        private void InitializeSystemTray()
        {
            // Create context menu for system tray
            trayContextMenu = new ContextMenuStrip();
            
            trayShowItem = new ToolStripMenuItem("Show Plex Backup Manager");
            trayShowItem.Click += TrayShowItem_Click;
            trayShowItem.Font = new Font(trayShowItem.Font, FontStyle.Bold);
            
            trayBackupNowItem = new ToolStripMenuItem("Backup Now");
            trayBackupNowItem.Click += TrayBackupNowItem_Click;
            
            trayExitItem = new ToolStripMenuItem("Exit");
            trayExitItem.Click += TrayExitItem_Click;
            
            trayContextMenu.Items.AddRange(new ToolStripItem[] {
                trayShowItem,
                new ToolStripSeparator(),
                trayBackupNowItem,
                new ToolStripSeparator(),
                trayExitItem
            });

            // Create system tray icon
            notifyIcon = new NotifyIcon();
            notifyIcon.Icon = SystemIcons.Application;
            notifyIcon.Text = "Plex Backup Manager";
            notifyIcon.ContextMenuStrip = trayContextMenu;
            notifyIcon.DoubleClick += NotifyIcon_DoubleClick;
            
            // Handle form events for minimize to tray
            this.Resize += PlexBackupForm_Resize;
            this.FormClosing += PlexBackupForm_FormClosing;
        }

        // Event Handlers
        private void BtnBrowse_Click(object sender, EventArgs e)
        {
            using (var folderDialog = new FolderBrowserDialog())
            {
                folderDialog.Description = "Select backup destination folder";
                folderDialog.ShowNewFolderButton = true;

                if (folderDialog.ShowDialog() == DialogResult.OK)
                {
                    txtBackupPath.Text = folderDialog.SelectedPath;
                    config.BackupPath = folderDialog.SelectedPath;
                    rootBackup = folderDialog.SelectedPath;
                    SaveConfiguration();
                    LogMessage($"Backup path set to: {rootBackup}", Color.Yellow);
                }
            }
        }

        private async void BtnBackupNow_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(config.BackupPath))
            {
                MessageBox.Show("Please select a backup path first.", "No Backup Path", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            SetControlsEnabled(false);
            progressBar.Visible = true;
            progressBar.Style = ProgressBarStyle.Marquee;
            lblStatus.Text = "Backup in progress...";

            try
            {
                await Task.Run(() => PerformPlexBackup());
                
                // Update statistics
                config.LastBackup = DateTime.Now;
                config.TotalBackupsCreated++;
                SaveConfiguration();
                
                LogMessage("Backup completed successfully!", Color.LightGreen);
                lblStatus.Text = "Backup completed";

                // Clean old backups based on retention setting
                await Task.Run(() => CleanOldBackups());
            }
            catch (Exception ex)
            {
                LogMessage($"Backup failed: {ex.Message}", Color.Red);
                lblStatus.Text = "Backup failed";
                MessageBox.Show($"Backup failed: {ex.Message}", "Backup Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                progressBar.Visible = false;
                SetControlsEnabled(true);
            }
        }

        private void ChkMinimizeToTray_CheckedChanged(object sender, EventArgs e)
        {
            config.MinimizeToTray = chkMinimizeToTray.Checked;
            notifyIcon.Visible = config.MinimizeToTray;
            SaveConfiguration();
            
            if (config.MinimizeToTray)
            {
                LogMessage("Minimize to tray enabled - close button will minimize instead of exit", Color.Yellow);
                
                // Show notification only if it's the first time this option is enabled
                if (!config.HasShownTrayNotification && config.ShowNotifications)
                {
                    ShowTrayNotification("Plex Backup Manager will now minimize to system tray when closed", ToolTipIcon.Info);
                    config.HasShownTrayNotification = true;
                    SaveConfiguration();
                }
            }
            else
            {
                LogMessage("Minimize to tray disabled - close button will exit application", Color.Yellow);
            }
        }

        private void BtnScheduleBackup_Click(object sender, EventArgs e)
        {
            var scheduleForm = new ScheduleBackupForm();
            scheduleForm.ShowDialog();
        }

        private void BtnViewBackups_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(config.BackupPath) || !Directory.Exists(config.BackupPath))
            {
                MessageBox.Show("No backup directory configured or directory doesn't exist.", "No Backups", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var backupViewForm = new BackupViewForm(config.BackupPath);
            backupViewForm.ShowDialog();
        }

        private void BtnRestoreBackup_Click(object sender, EventArgs e)
        {
            var restoreForm = new RestoreBackupForm(config.BackupPath);
            restoreForm.ShowDialog();
        }

        private void BtnSettings_Click(object sender, EventArgs e)
        {
            var settingsForm = new AdvancedSettingsForm(config);
            if (settingsForm.ShowDialog() == DialogResult.OK)
            {
                // Settings were changed, reload UI
                LoadConfigurationToUI();
                SaveConfiguration();
            }
        }

        // System Tray Event Handlers
        private void TrayShowItem_Click(object sender, EventArgs e)
        {
            ShowForm();
        }

        private void TrayBackupNowItem_Click(object sender, EventArgs e)
        {
            if (this.WindowState == FormWindowState.Minimized)
            {
                // Perform backup silently in the background
                _ = Task.Run(async () =>
                {
                    try
                    {
                        if (!string.IsNullOrEmpty(config.BackupPath))
                        {
                            await Task.Run(() => PerformPlexBackup());
                            ShowTrayNotification("Backup completed successfully!", ToolTipIcon.Info);
                        }
                        else
                        {
                            ShowTrayNotification("No backup path configured!", ToolTipIcon.Warning);
                        }
                    }
                    catch (Exception ex)
                    {
                        ShowTrayNotification($"Backup failed: {ex.Message}", ToolTipIcon.Error);
                    }
                });
            }
            else
            {
                // If window is visible, use the regular backup button click
                BtnBackupNow_Click(sender, e);
            }
        }

        private void TrayExitItem_Click(object sender, EventArgs e)
        {
            ExitApplication();
        }

        private void NotifyIcon_DoubleClick(object sender, EventArgs e)
        {
            ShowForm();
        }

        private void PlexBackupForm_Resize(object sender, EventArgs e)
        {
            if (config.MinimizeToTray && this.WindowState == FormWindowState.Minimized)
            {
                this.Hide();
                notifyIcon.Visible = true;
                
                // Don't show notification every time - only when first enabled
                // The notification is now handled in ChkMinimizeToTray_CheckedChanged
            }
        }

        private void PlexBackupForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (config.MinimizeToTray && e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                this.WindowState = FormWindowState.Minimized;
            }
            else
            {
                ExitApplication();
            }
        }

        // System Tray Helper Methods
        private void ShowForm()
        {
            this.Show();
            this.WindowState = FormWindowState.Normal;
            this.BringToFront();
            this.Activate();
            notifyIcon.Visible = config.MinimizeToTray; // Keep visible if minimize to tray is enabled
        }

        private void ShowTrayNotification(string message, ToolTipIcon icon)
        {
            if (config.ShowNotifications && notifyIcon.Visible)
            {
                notifyIcon.ShowBalloonTip(3000, "Plex Backup Manager", message, icon);
            }
        }

        private void ExitApplication()
        {
            notifyIcon.Visible = false;
            notifyIcon.Dispose();
            Application.Exit();
        }

        // Core Backup Methods
        public void PerformPlexBackup()
        {
            try
            {
                LogMessage("Starting Plex backup operation...", Color.White);

                // Create root backup folder if it doesn't exist
                if (!Directory.Exists(config.BackupPath))
                {
                    Directory.CreateDirectory(config.BackupPath);
                    LogMessage("Created backup directory", Color.Yellow);
                }

                // Determine the day and date
                var weekday = DateTime.Now.DayOfWeek.ToString();
                var day = DateTime.Now.ToString("dd-MM-yyyy");

                // Create day folder path
                var weekdayDestination = Path.Combine(config.BackupPath, $"{weekday} {day}-Backup");

                // Create day folder if it doesn't exist
                if (!Directory.Exists(weekdayDestination))
                {
                    Directory.CreateDirectory(weekdayDestination);
                    LogMessage($"Created backup folder: {weekdayDestination}", Color.Yellow);
                }

                // Set path for log backup folder
                var logDestination = Path.Combine(weekdayDestination, "Logs");

                // Create log folder if it doesn't exist
                if (!Directory.Exists(logDestination))
                {
                    Directory.CreateDirectory(logDestination);
                }

                // Perform backups based on selected options
                if (chkStopPlex.Checked)
                {
                    LogMessage("Stopping Plex Media Server...", Color.Yellow);
                    StopPlexServer();
                }

                if (chkIncludeRegistry.Checked)
                {
                    LogMessage("Backing up registry...", Color.Cyan);
                    BackupRegistry(weekdayDestination, weekday);
                }

                if (chkIncludeFiles.Checked)
                {
                    LogMessage("Backing up files...", Color.Cyan);
                    BackupFiles(weekdayDestination, logDestination, weekday);
                }

                if (chkStopPlex.Checked)
                {
                    LogMessage("Restarting Plex Media Server...", Color.Yellow);
                    StartPlexServer();
                }

                LogMessage($"Backup completed successfully! Files saved to: {weekdayDestination}", Color.LightGreen);
            }
            catch (Exception ex)
            {
                LogMessage($"Backup error: {ex.Message}", Color.Red);
                throw;
            }
        }

        private void BackupRegistry(string weekdayDestination, string weekday)
        {
            try
            {
                // Set path for registry backup folder
                var regDestination = Path.Combine(weekdayDestination, "RegBackup");

                // Create registry backup folder if it doesn't exist
                if (!Directory.Exists(regDestination))
                {
                    Directory.CreateDirectory(regDestination);
                }

                // Set path for backup .reg file
                var regFilePath = Path.Combine(regDestination, $"Regbackup-{weekday}.reg");

                // If a previous backup exists, delete it
                if (File.Exists(regFilePath))
                {
                    File.Delete(regFilePath);
                }

                // Backup the registry key
                var regExportProcess = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "reg",
                        Arguments = $"export \"{PlexRegistryPath}\" \"{regFilePath}\"",
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true
                    }
                };

                regExportProcess.Start();
                regExportProcess.WaitForExit();

                if (regExportProcess.ExitCode != 0)
                {
                    throw new Exception($"Registry export failed with exit code: {regExportProcess.ExitCode}");
                }

                LogMessage("Registry backup completed", Color.LightBlue);
            }
            catch (Exception ex)
            {
                throw new Exception($"Registry backup failed: {ex.Message}");
            }
        }

        private void BackupFiles(string weekdayDestination, string logDestination, string weekday)
        {
            try
            {
                // Create appdata folder backup path
                var fileDestination = Path.Combine(weekdayDestination, "FileBackup");

                // If appdata backup folder doesn't exist, create it
                if (!Directory.Exists(fileDestination))
                {
                    Directory.CreateDirectory(fileDestination);
                }

                // Set source for robocopy command
                var source = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Plex Media Server");

                // Exclude cache folder
                var exclude = Path.Combine(source, "Cache");

                // Perform a mirror style backup, excluding the cache directory
                var robocopyProcess = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "robocopy",
                        Arguments = $"\"{source}\" \"{fileDestination}\" /MIR /R:1 /W:1 /XD \"{exclude}\" /log:\"{Path.Combine(logDestination, $"LogBackup-{weekday}.txt")}\"",
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true
                    }
                };

                robocopyProcess.Start();
                robocopyProcess.WaitForExit();

                // Robocopy exit codes 0-7 are considered successful
                if (robocopyProcess.ExitCode > 7)
                {
                    throw new Exception($"Robocopy failed with exit code: {robocopyProcess.ExitCode}");
                }

                LogMessage("File backup completed", Color.LightBlue);
            }
            catch (Exception ex)
            {
                throw new Exception($"File backup failed: {ex.Message}");
            }
        }

        private void StopPlexServer()
        {
            try
            {
                var plexProcesses = Process.GetProcessesByName("Plex Media Server");
                foreach (var process in plexProcesses)
                {
                    process.Kill();
                    process.WaitForExit(5000); // Wait up to 5 seconds for the process to exit
                }
                LogMessage("Plex Media Server stopped", Color.Orange);
            }
            catch (Exception ex)
            {
                LogMessage($"Warning: Failed to stop Plex Server: {ex.Message}", Color.Orange);
            }
        }

        private void StartPlexServer()
        {
            try
            {
                string plexPath = null;
                foreach (var path in PlexExecutablePaths)
                {
                    if (File.Exists(path))
                    {
                        plexPath = path;
                        break;
                    }
                }

                if (plexPath != null)
                {
                    Process.Start(plexPath);
                    LogMessage("Plex Media Server started", Color.Orange);
                }
                else
                {
                    throw new Exception("Plex executable not found in common locations");
                }
            }
            catch (Exception ex)
            {
                LogMessage($"Warning: Failed to start Plex Server: {ex.Message}", Color.Orange);
            }
        }

        private void CleanOldBackups()
        {
            try
            {
                if (cmbRetentionDays.SelectedIndex == 4) // "Never delete"
                    return;

                var retentionDays = new int[] { 7, 14, 30, 60 }[cmbRetentionDays.SelectedIndex];
                var cutoffDate = DateTime.Now.AddDays(-retentionDays);

                var backupFolders = Directory.GetDirectories(config.BackupPath)
                    .Where(dir => Directory.GetCreationTime(dir) < cutoffDate)
                    .ToList();

                foreach (var folder in backupFolders)
                {
                    Directory.Delete(folder, true);
                    LogMessage($"Deleted old backup: {Path.GetFileName(folder)}", Color.Gray);
                }

                if (backupFolders.Count > 0)
                {
                    LogMessage($"Cleaned {backupFolders.Count} old backup(s)", Color.Gray);
                }
            }
            catch (Exception ex)
            {
                LogMessage($"Warning: Failed to clean old backups: {ex.Message}", Color.Orange);
            }
        }

        // Configuration Methods
        private void LoadConfiguration()
        {
            try
            {
                // Try to load JSON config first (modern format)
                if (File.Exists(configJsonPath))
                {
                    LoadJsonConfiguration();
                }
                // Fall back to INI config for backward compatibility
                else if (File.Exists(configIniPath))
                {
                    LoadIniConfiguration();
                    // Convert to JSON and save
                    SaveJsonConfiguration();
                }
                else
                {
                    // No config exists, create default JSON config
                    SaveJsonConfiguration();
                }

                LoadConfigurationToUI();
            }
            catch (Exception ex)
            {
                LogMessage($"Warning: Failed to load configuration: {ex.Message}", Color.Orange);
            }
        }

        private void LoadJsonConfiguration()
        {
            try
            {
                var jsonString = File.ReadAllText(configJsonPath);
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    WriteIndented = true
                };
                
                config = JsonSerializer.Deserialize<PlexBackupConfig>(jsonString, options) ?? new PlexBackupConfig();
                LogMessage("Configuration loaded from JSON file", Color.Gray);
            }
            catch (Exception ex)
            {
                LogMessage($"Warning: Failed to load JSON configuration: {ex.Message}", Color.Orange);
                config = new PlexBackupConfig();
            }
        }

        private void LoadIniConfiguration()
        {
            try
            {
                var configLines = File.ReadAllLines(configIniPath);
                var configDict = new Dictionary<string, string>();

                foreach (var line in configLines)
                {
                    if (string.IsNullOrWhiteSpace(line) || line.StartsWith("["))
                        continue;

                    var parts = line.Split('=');
                    if (parts.Length == 2)
                    {
                        configDict[parts[0].Trim()] = parts[1].Trim();
                    }
                }

                // Map INI values to config object
                if (configDict.ContainsKey("BackupPath"))
                    config.BackupPath = configDict["BackupPath"];
                if (configDict.ContainsKey("IncludeRegistry"))
                    config.IncludeRegistry = bool.Parse(configDict["IncludeRegistry"]);
                if (configDict.ContainsKey("IncludeFiles"))
                    config.IncludeFiles = bool.Parse(configDict["IncludeFiles"]);
                if (configDict.ContainsKey("IncludeLogs"))
                    config.IncludeLogs = bool.Parse(configDict["IncludeLogs"]);
                if (configDict.ContainsKey("StopPlex"))
                    config.StopPlex = bool.Parse(configDict["StopPlex"]);
                if (configDict.ContainsKey("MinimizeToTray"))
                    config.MinimizeToTray = bool.Parse(configDict["MinimizeToTray"]);
                if (configDict.ContainsKey("RetentionDays"))
                    config.RetentionDays = int.Parse(configDict["RetentionDays"]);
                if (configDict.ContainsKey("HasShownTrayNotification"))
                    config.HasShownTrayNotification = bool.Parse(configDict["HasShownTrayNotification"]);

                LogMessage("Configuration loaded from INI file", Color.Gray);
            }
            catch (Exception ex)
            {
                LogMessage($"Warning: Failed to load INI configuration: {ex.Message}", Color.Orange);
                config = new PlexBackupConfig();
            }
        }

        private void LoadConfigurationToUI()
        {
            txtBackupPath.Text = config.BackupPath;
            rootBackup = config.BackupPath;
            
            chkIncludeRegistry.Checked = config.IncludeRegistry;
            chkIncludeFiles.Checked = config.IncludeFiles;
            chkIncludeLogs.Checked = config.IncludeLogs;
            chkStopPlex.Checked = config.StopPlex;
            chkMinimizeToTray.Checked = config.MinimizeToTray;
            
            if (config.RetentionDays >= 0 && config.RetentionDays < cmbRetentionDays.Items.Count)
                cmbRetentionDays.SelectedIndex = config.RetentionDays;

            // Apply system tray configuration
            if (config.MinimizeToTray)
            {
                notifyIcon.Visible = true;
            }

            // Log some statistics if available
            if (config.LastBackup != DateTime.MinValue)
            {
                LogMessage($"Last backup: {config.LastBackup:yyyy-MM-dd HH:mm}", Color.Cyan);
                LogMessage($"Total backups created: {config.TotalBackupsCreated}", Color.Cyan);
            }
        }

        private void SaveConfiguration()
        {
            try
            {
                // Update config from UI
                config.BackupPath = txtBackupPath.Text;
                config.IncludeRegistry = chkIncludeRegistry.Checked;
                config.IncludeFiles = chkIncludeFiles.Checked;
                config.IncludeLogs = chkIncludeLogs.Checked;
                config.StopPlex = chkStopPlex.Checked;
                config.MinimizeToTray = chkMinimizeToTray.Checked;
                config.RetentionDays = cmbRetentionDays.SelectedIndex;

                // Save as JSON (preferred format)
                SaveJsonConfiguration();
                
                // Also maintain INI for backward compatibility
                SaveIniConfiguration();
            }
            catch (Exception ex)
            {
                LogMessage($"Warning: Failed to save configuration: {ex.Message}", Color.Orange);
            }
        }

        private void SaveJsonConfiguration()
        {
            try
            {
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };

                var jsonString = JsonSerializer.Serialize(config, options);
                File.WriteAllText(configJsonPath, jsonString);
            }
            catch (Exception ex)
            {
                LogMessage($"Warning: Failed to save JSON configuration: {ex.Message}", Color.Orange);
            }
        }

        private void SaveIniConfiguration()
        {
            try
            {
                var configLines = new List<string>
                {
                    "[Settings]",
                    $"BackupPath={config.BackupPath}",
                    $"IncludeRegistry={config.IncludeRegistry}",
                    $"IncludeFiles={config.IncludeFiles}",
                    $"IncludeLogs={config.IncludeLogs}",
                    $"StopPlex={config.StopPlex}",
                    $"MinimizeToTray={config.MinimizeToTray}",
                    $"RetentionDays={config.RetentionDays}",
                    $"HasShownTrayNotification={config.HasShownTrayNotification}"
                };

                File.WriteAllLines(configIniPath, configLines);
            }
            catch (Exception ex)
            {
                LogMessage($"Warning: Failed to save INI configuration: {ex.Message}", Color.Orange);
            }
        }

        // Utility Methods
        private void LogMessage(string message, Color color)
        {
            if (txtLog.InvokeRequired)
            {
                txtLog.Invoke(new Action(() => LogMessage(message, color)));
                return;
            }

            txtLog.SelectionStart = txtLog.TextLength;
            txtLog.SelectionLength = 0;
            txtLog.SelectionColor = color;
            txtLog.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}\n");
            txtLog.SelectionColor = txtLog.ForeColor;
            txtLog.ScrollToCaret();
        }

        private void SetControlsEnabled(bool enabled)
        {
            btnBackupNow.Enabled = enabled;
            btnScheduleBackup.Enabled = enabled;
            btnViewBackups.Enabled = enabled;
            btnRestoreBackup.Enabled = enabled;
            btnSettings.Enabled = enabled;
            btnBrowse.Enabled = enabled;
        }
    }

    // Additional Forms (placeholder implementations)
    public class ScheduleBackupForm : Form
    {
        public ScheduleBackupForm()
        {
            this.Text = "Schedule Backup";
            this.Size = new Size(400, 300);
            this.StartPosition = FormStartPosition.CenterParent;
            
            var label = new Label
            {
                Text = "Schedule backup functionality\nwill be implemented here.",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter
            };
            
            this.Controls.Add(label);
        }
    }

    public class BackupViewForm : Form
    {
        public BackupViewForm(string backupPath)
        {
            this.Text = "View Backups";
            this.Size = new Size(600, 400);
            this.StartPosition = FormStartPosition.CenterParent;
            
            var listView = new ListView
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                FullRowSelect = true,
                GridLines = true
            };
            
            listView.Columns.Add("Backup Name", 200);
            listView.Columns.Add("Date Created", 150);
            listView.Columns.Add("Size", 100);
            
            // Populate with backup folders
            if (Directory.Exists(backupPath))
            {
                foreach (var dir in Directory.GetDirectories(backupPath))
                {
                    var dirInfo = new DirectoryInfo(dir);
                    var item = new ListViewItem(dirInfo.Name);
                    item.SubItems.Add(dirInfo.CreationTime.ToString());
                    item.SubItems.Add("N/A"); // Size calculation would go here
                    listView.Items.Add(item);
                }
            }
            
            this.Controls.Add(listView);
        }
    }

    public class RestoreBackupForm : Form
    {
        public RestoreBackupForm(string backupPath)
        {
            this.Text = "Restore Backup";
            this.Size = new Size(500, 350);
            this.StartPosition = FormStartPosition.CenterParent;
            
            var label = new Label
            {
                Text = "Restore backup functionality\nwill be implemented here.",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter
            };
            
            this.Controls.Add(label);
        }
    }

    public class AdvancedSettingsForm : Form
    {
        private PlexBackupConfig config;
        private CheckBox chkMinimizeToTray;
        private CheckBox chkShowNotifications;
        private CheckBox chkAutoStartWithWindows;
        private ComboBox cmbLogLevel;
        private ListBox lstCustomPlexPaths;
        private Button btnAddPath;
        private Button btnRemovePath;
        private Button btnOK;
        private Button btnCancel;

        public AdvancedSettingsForm(PlexBackupConfig configuration)
        {
            config = configuration;
            InitializeAdvancedSettings();
            LoadAdvancedSettings();
        }

        private void InitializeAdvancedSettings()
        {
            this.Text = "Advanced Settings";
            this.Size = new Size(500, 400);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            var mainPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 8,
                Padding = new Padding(10)
            };

            // UI Settings
            var lblUISettings = new Label { Text = "UI Settings:", Font = new Font("Arial", 10F, FontStyle.Bold), AutoSize = true };
            chkMinimizeToTray = new CheckBox { Text = "Minimize to system tray", AutoSize = true };
            chkShowNotifications = new CheckBox { Text = "Show backup notifications", AutoSize = true };
            chkAutoStartWithWindows = new CheckBox { Text = "Auto start with Windows", AutoSize = true };

            // Log Level
            var lblLogLevel = new Label { Text = "Log Level:", Font = new Font("Arial", 10F, FontStyle.Bold), AutoSize = true };
            cmbLogLevel = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 150 };
            cmbLogLevel.Items.AddRange(new string[] { "Debug", "Info", "Warning", "Error" });

            // Custom Plex Paths
            var lblCustomPaths = new Label { Text = "Custom Plex Paths:", Font = new Font("Arial", 10F, FontStyle.Bold), AutoSize = true };
            lstCustomPlexPaths = new ListBox { Height = 100, Width = 350 };
            
            var pathButtonPanel = new FlowLayoutPanel { FlowDirection = FlowDirection.LeftToRight, Height = 30 };
            btnAddPath = new Button { Text = "Add Path", Width = 80, Height = 25 };
            btnRemovePath = new Button { Text = "Remove", Width = 80, Height = 25 };
            btnAddPath.Click += BtnAddPath_Click;
            btnRemovePath.Click += BtnRemovePath_Click;
            pathButtonPanel.Controls.AddRange(new Control[] { btnAddPath, btnRemovePath });

            // Action buttons
            var buttonPanel = new FlowLayoutPanel { FlowDirection = FlowDirection.RightToLeft, Height = 40, Dock = DockStyle.Bottom };
            btnOK = new Button { Text = "OK", Width = 75, Height = 30, DialogResult = DialogResult.OK };
            btnCancel = new Button { Text = "Cancel", Width = 75, Height = 30, DialogResult = DialogResult.Cancel };
            btnOK.Click += BtnOK_Click;
            buttonPanel.Controls.AddRange(new Control[] { btnCancel, btnOK });

            // Add controls to main panel
            mainPanel.Controls.Add(lblUISettings, 0, 0);
            mainPanel.SetColumnSpan(lblUISettings, 2);
            mainPanel.Controls.Add(chkMinimizeToTray, 0, 1);
            mainPanel.SetColumnSpan(chkMinimizeToTray, 2);
            mainPanel.Controls.Add(chkShowNotifications, 0, 2);
            mainPanel.SetColumnSpan(chkShowNotifications, 2);
            mainPanel.Controls.Add(chkAutoStartWithWindows, 0, 3);
            mainPanel.SetColumnSpan(chkAutoStartWithWindows, 2);
            mainPanel.Controls.Add(lblLogLevel, 0, 4);
            mainPanel.Controls.Add(cmbLogLevel, 1, 4);
            mainPanel.Controls.Add(lblCustomPaths, 0, 5);
            mainPanel.SetColumnSpan(lblCustomPaths, 2);
            mainPanel.Controls.Add(lstCustomPlexPaths, 0, 6);
            mainPanel.SetColumnSpan(lstCustomPlexPaths, 2);
            mainPanel.Controls.Add(pathButtonPanel, 0, 7);
            mainPanel.SetColumnSpan(pathButtonPanel, 2);

            this.Controls.Add(mainPanel);
            this.Controls.Add(buttonPanel);
        }

        private void LoadAdvancedSettings()
        {
            chkMinimizeToTray.Checked = config.MinimizeToTray;
            chkShowNotifications.Checked = config.ShowNotifications;
            chkAutoStartWithWindows.Checked = config.AutoStartWithWindows;
            
            var logLevelIndex = new string[] { "Debug", "Info", "Warning", "Error" }.ToList().IndexOf(config.LogLevel);
            cmbLogLevel.SelectedIndex = logLevelIndex >= 0 ? logLevelIndex : 1; // Default to Info

            lstCustomPlexPaths.Items.Clear();
            foreach (var path in config.CustomPlexPaths)
            {
                lstCustomPlexPaths.Items.Add(path);
            }
        }

        private void BtnAddPath_Click(object sender, EventArgs e)
        {
            using (var openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Title = "Select Plex Media Server Executable";
                openFileDialog.Filter = "Executable files (*.exe)|*.exe|All files (*.*)|*.*";
                openFileDialog.FileName = "Plex Media Server.exe";

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    if (!lstCustomPlexPaths.Items.Contains(openFileDialog.FileName))
                    {
                        lstCustomPlexPaths.Items.Add(openFileDialog.FileName);
                    }
                }
            }
        }

        private void BtnRemovePath_Click(object sender, EventArgs e)
        {
            if (lstCustomPlexPaths.SelectedIndex >= 0)
            {
                lstCustomPlexPaths.Items.RemoveAt(lstCustomPlexPaths.SelectedIndex);
            }
        }

        private void BtnOK_Click(object sender, EventArgs e)
        {
            // Save settings back to config
            config.MinimizeToTray = chkMinimizeToTray.Checked;
            config.ShowNotifications = chkShowNotifications.Checked;
            config.AutoStartWithWindows = chkAutoStartWithWindows.Checked;
            config.LogLevel = cmbLogLevel.SelectedItem?.ToString() ?? "Info";
            
            config.CustomPlexPaths.Clear();
            foreach (var item in lstCustomPlexPaths.Items)
            {
                config.CustomPlexPaths.Add(item.ToString());
            }
        }
    }
}
