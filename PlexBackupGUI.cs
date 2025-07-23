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
    /// Version: 1.0 (GUI Version)
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
        public DateTime LastBackup { get; set; } = DateTime.MinValue;
        public int TotalBackupsCreated { get; set; } = 0;
        public List<string> CustomPlexPaths { get; set; } = new List<string>();
        public bool MinimizeToTray { get; set; } = false;
        public bool ShowNotifications { get; set; } = true;
        public bool AutoStartWithWindows { get; set; } = false;
        public string LogLevel { get; set; } = "Info"; // Debug, Info, Warning, Error
        public bool HasShownTrayNotification { get; set; } = false;
        public bool EnableRollback { get; set; } = true;
        public int MaxRollbackAttempts { get; set; } = 3;
    }

    // Rollback state management
    public class BackupRollbackState
    {
        public bool PlexWasRunning { get; set; } = false;
        public string BackupDestination { get; set; } = "";
        public List<string> CreatedDirectories { get; set; } = new List<string>();
        public List<string> CreatedFiles { get; set; } = new List<string>();
        public Dictionary<string, string> OriginalFileBackups { get; set; } = new Dictionary<string, string>();
        public DateTime BackupStartTime { get; set; } = DateTime.Now;
        public bool RegistryBackupCompleted { get; set; } = false;
        public bool FileBackupCompleted { get; set; } = false;
        public string TempRollbackPath { get; set; } = "";
    }

    public partial class PlexBackupForm : Form
    {
        private PlexBackupConfig config;
        private string configJsonPath;
        private string rootBackup;
        private BackupRollbackState rollbackState;
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
        private CheckBox chkEnableRollback;
        private ComboBox cmbRetentionDays;
        private ProgressBar progressBar;
        private RichTextBox txtLog;
        private Label lblStatus;
        private Button btnViewBackups;
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
            config = new PlexBackupConfig();
            
            InitializeComponent();
            InitializeSystemTray();
            LoadConfiguration();
        }

        private void InitializeComponent()
        {
            this.Text = "Plex Backup Manager v1.0.0";
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
                Height = 165,
                Dock = DockStyle.Fill
            };

            chkIncludeRegistry = new CheckBox { Text = "Include Registry Backup", Checked = true, AutoSize = true };
            chkIncludeFiles = new CheckBox { Text = "Include File Backup", Checked = true, AutoSize = true };
            chkIncludeLogs = new CheckBox { Text = "Include Previous Logs", Checked = false, AutoSize = true };
            chkStopPlex = new CheckBox { Text = "Stop Plex During Backup", Checked = true, AutoSize = true };
            chkMinimizeToTray = new CheckBox { Text = "Minimize to System Tray", Checked = false, AutoSize = true };
            chkMinimizeToTray.CheckedChanged += ChkMinimizeToTray_CheckedChanged;
            chkEnableRollback = new CheckBox { Text = "Enable Automatic Rollback on Failure", Checked = true, AutoSize = true };

            optionsPanel.Controls.AddRange(new Control[] { chkIncludeRegistry, chkIncludeFiles, chkIncludeLogs, chkStopPlex, chkMinimizeToTray, chkEnableRollback });

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
                Text = "Manage Backups",
                Width = 120,
                Height = 35
            };
            btnViewBackups.Click += BtnViewBackups_Click;

            btnSettings = new Button
            {
                Text = "Settings",
                Width = 80,
                Height = 35
            };
            btnSettings.Click += BtnSettings_Click;

            btnPanel.Controls.AddRange(new Control[] { btnBackupNow, btnScheduleBackup, btnViewBackups, btnSettings });

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
            LogMessage("Plex Backup Manager v1.0.0 - Ready for operation", Color.White);
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
            rollbackState = new BackupRollbackState();
            
            try
            {
                LogMessage("Starting Plex backup operation...", Color.White);
                
                // Initialize rollback system
                if (config.EnableRollback)
                {
                    InitializeRollbackSystem();
                }

                // Check if Plex is running before we start
                rollbackState.PlexWasRunning = IsPlexRunning();
                LogMessage($"Plex server status: {(rollbackState.PlexWasRunning ? "Running" : "Stopped")}", Color.Gray);

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
                rollbackState.BackupDestination = weekdayDestination;

                // Create day folder if it doesn't exist
                if (!Directory.Exists(weekdayDestination))
                {
                    Directory.CreateDirectory(weekdayDestination);
                    rollbackState.CreatedDirectories.Add(weekdayDestination);
                    LogMessage($"Created backup folder: {weekdayDestination}", Color.Yellow);
                }

                // Set path for log backup folder
                var logDestination = Path.Combine(weekdayDestination, "Logs");

                // Create log folder if it doesn't exist
                if (!Directory.Exists(logDestination))
                {
                    Directory.CreateDirectory(logDestination);
                    rollbackState.CreatedDirectories.Add(logDestination);
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
                    rollbackState.RegistryBackupCompleted = true;
                }

                if (chkIncludeFiles.Checked)
                {
                    LogMessage("Backing up files...", Color.Cyan);
                    BackupFiles(weekdayDestination, logDestination, weekday);
                    rollbackState.FileBackupCompleted = true;
                }

                if (chkStopPlex.Checked)
                {
                    LogMessage("Restarting Plex Media Server...", Color.Yellow);
                    StartPlexServer();
                }

                // Clean up rollback temp files on success
                if (config.EnableRollback)
                {
                    CleanupRollbackSystem();
                }

                LogMessage($"Backup completed successfully! Files saved to: {weekdayDestination}", Color.LightGreen);
            }
            catch (Exception ex)
            {
                LogMessage($"Backup error: {ex.Message}", Color.Red);
                
                // Perform rollback if enabled
                if (config.EnableRollback)
                {
                    LogMessage("Initiating rollback due to backup failure...", Color.Orange);
                    PerformRollback();
                }
                
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
                    rollbackState?.CreatedDirectories.Add(regDestination);
                }

                // Set path for backup .reg file
                var regFilePath = Path.Combine(regDestination, $"Regbackup-{weekday}.reg");

                // If a previous backup exists, create rollback copy
                if (File.Exists(regFilePath) && config.EnableRollback && rollbackState != null)
                {
                    var rollbackFilePath = Path.Combine(rollbackState.TempRollbackPath, $"Regbackup-{weekday}_original.reg");
                    File.Copy(regFilePath, rollbackFilePath, true);
                    rollbackState.OriginalFileBackups[regFilePath] = rollbackFilePath;
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

                // Track created file for rollback
                rollbackState?.CreatedFiles.Add(regFilePath);

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
                    rollbackState?.CreatedDirectories.Add(fileDestination);
                }

                // Set source for robocopy command
                var source = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Plex Media Server");

                // Exclude cache folder
                var exclude = Path.Combine(source, "Cache");

                // Create log file path and track for rollback
                var logFilePath = Path.Combine(logDestination, $"LogBackup-{weekday}.txt");
                
                // Perform a mirror style backup, excluding the cache directory
                var robocopyProcess = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "robocopy",
                        Arguments = $"\"{source}\" \"{fileDestination}\" /MIR /R:1 /W:1 /XD \"{exclude}\" /log:\"{logFilePath}\"",
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

                // Track created log file for rollback
                rollbackState?.CreatedFiles.Add(logFilePath);

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

        // Rollback System Methods
        private void InitializeRollbackSystem()
        {
            try
            {
                // Create temporary rollback directory
                rollbackState.TempRollbackPath = Path.Combine(Path.GetTempPath(), $"PlexBackupRollback_{DateTime.Now:yyyyMMdd_HHmmss}");
                Directory.CreateDirectory(rollbackState.TempRollbackPath);
                
                LogMessage("Rollback system initialized", Color.Gray);
            }
            catch (Exception ex)
            {
                LogMessage($"Warning: Failed to initialize rollback system: {ex.Message}", Color.Orange);
            }
        }

        private bool IsPlexRunning()
        {
            try
            {
                var plexProcesses = Process.GetProcessesByName("Plex Media Server");
                return plexProcesses.Length > 0;
            }
            catch
            {
                return false;
            }
        }

        private void PerformRollback()
        {
            try
            {
                LogMessage("Starting rollback operation...", Color.Yellow);
                var rollbackSuccess = true;

                // Restore Plex service state
                if (rollbackState.PlexWasRunning && !IsPlexRunning())
                {
                    try
                    {
                        LogMessage("Restoring Plex server state (starting)...", Color.Yellow);
                        StartPlexServer();
                    }
                    catch (Exception ex)
                    {
                        LogMessage($"Failed to restore Plex server: {ex.Message}", Color.Red);
                        rollbackSuccess = false;
                    }
                }
                else if (!rollbackState.PlexWasRunning && IsPlexRunning())
                {
                    try
                    {
                        LogMessage("Restoring Plex server state (stopping)...", Color.Yellow);
                        StopPlexServer();
                    }
                    catch (Exception ex)
                    {
                        LogMessage($"Failed to restore Plex server state: {ex.Message}", Color.Red);
                        rollbackSuccess = false;
                    }
                }

                // Restore any backed up files (if any were modified during backup)
                foreach (var backup in rollbackState.OriginalFileBackups)
                {
                    try
                    {
                        if (File.Exists(backup.Value))
                        {
                            File.Copy(backup.Value, backup.Key, true);
                            LogMessage($"Restored file: {backup.Key}", Color.Yellow);
                        }
                    }
                    catch (Exception ex)
                    {
                        LogMessage($"Failed to restore file {backup.Key}: {ex.Message}", Color.Red);
                        rollbackSuccess = false;
                    }
                }

                // Remove any files created during failed backup
                foreach (var file in rollbackState.CreatedFiles)
                {
                    try
                    {
                        if (File.Exists(file))
                        {
                            File.Delete(file);
                            LogMessage($"Removed created file: {file}", Color.Yellow);
                        }
                    }
                    catch (Exception ex)
                    {
                        LogMessage($"Failed to remove file {file}: {ex.Message}", Color.Red);
                        rollbackSuccess = false;
                    }
                }

                // Remove any directories created during failed backup (in reverse order)
                for (int i = rollbackState.CreatedDirectories.Count - 1; i >= 0; i--)
                {
                    try
                    {
                        var dir = rollbackState.CreatedDirectories[i];
                        if (Directory.Exists(dir) && IsDirectoryEmpty(dir))
                        {
                            Directory.Delete(dir);
                            LogMessage($"Removed created directory: {dir}", Color.Yellow);
                        }
                    }
                    catch (Exception ex)
                    {
                        LogMessage($"Failed to remove directory {rollbackState.CreatedDirectories[i]}: {ex.Message}", Color.Red);
                        rollbackSuccess = false;
                    }
                }

                // Clean up rollback system
                CleanupRollbackSystem();

                if (rollbackSuccess)
                {
                    LogMessage("Rollback completed successfully", Color.LightGreen);
                }
                else
                {
                    LogMessage("Rollback completed with some warnings - manual intervention may be required", Color.Orange);
                }
            }
            catch (Exception ex)
            {
                LogMessage($"Critical error during rollback: {ex.Message}", Color.Red);
                LogMessage("Manual system recovery may be required", Color.Red);
            }
        }

        private void CleanupRollbackSystem()
        {
            try
            {
                if (!string.IsNullOrEmpty(rollbackState.TempRollbackPath) && Directory.Exists(rollbackState.TempRollbackPath))
                {
                    Directory.Delete(rollbackState.TempRollbackPath, true);
                    LogMessage("Rollback temporary files cleaned up", Color.Gray);
                }
            }
            catch (Exception ex)
            {
                LogMessage($"Warning: Failed to cleanup rollback temp files: {ex.Message}", Color.Orange);
            }
        }

        private bool IsDirectoryEmpty(string path)
        {
            try
            {
                return !Directory.EnumerateFileSystemEntries(path).Any();
            }
            catch
            {
                return false;
            }
        }

        // Configuration Methods
        private void LoadConfiguration()
        {
            try
            {
                // Load JSON configuration
                if (File.Exists(configJsonPath))
                {
                    LoadJsonConfiguration();
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

        private void LoadConfigurationToUI()
        {
            txtBackupPath.Text = config.BackupPath;
            rootBackup = config.BackupPath;
            
            chkIncludeRegistry.Checked = config.IncludeRegistry;
            chkIncludeFiles.Checked = config.IncludeFiles;
            chkIncludeLogs.Checked = config.IncludeLogs;
            chkStopPlex.Checked = config.StopPlex;
            chkMinimizeToTray.Checked = config.MinimizeToTray;
            chkEnableRollback.Checked = config.EnableRollback;
            
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
                config.EnableRollback = chkEnableRollback.Checked;
                config.RetentionDays = cmbRetentionDays.SelectedIndex;

                // Save as JSON
                SaveJsonConfiguration();
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
        private string backupPath;
        private DataGridView dgvBackups;

        public BackupViewForm(string backupPath)
        {
            this.backupPath = backupPath;
            InitializeBackupViewForm();
            LoadBackupData();
        }

        private void InitializeBackupViewForm()
        {
            this.Text = "Manage Backups";
            this.Size = new Size(750, 500);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.Sizable;
            this.MinimumSize = new Size(650, 400);

            // Create main panel
            var mainPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(10)
            };

            // Create title label
            var lblTitle = new Label
            {
                Text = "Available Backups",
                Font = new Font("Arial", 12F, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(0, 0)
            };

            // Create DataGridView
            dgvBackups = new DataGridView
            {
                Location = new Point(0, 30),
                Size = new Size(mainPanel.Width - 20, mainPanel.Height - 80),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                AutoGenerateColumns = false,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = false,
                SelectionMode = DataGridViewSelectionMode.CellSelect,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.Fixed3D,
                EditMode = DataGridViewEditMode.EditProgrammatically
            };

            // Configure columns
            var colBackupName = new DataGridViewTextBoxColumn
            {
                Name = "BackupName",
                HeaderText = "Backup Name (Double-click to rename)",
                DataPropertyName = "BackupName",
                Width = 280,
                ReadOnly = false
            };

            var colDateCreated = new DataGridViewTextBoxColumn
            {
                Name = "DateCreated",
                HeaderText = "Date Created",
                DataPropertyName = "DateCreated",
                Width = 150,
                ReadOnly = true
            };

            var colSize = new DataGridViewTextBoxColumn
            {
                Name = "Size",
                HeaderText = "Size",
                DataPropertyName = "Size",
                Width = 100,
                ReadOnly = true
            };

            var colRestore = new DataGridViewButtonColumn
            {
                Name = "Restore",
                HeaderText = "Action",
                Text = "Restore",
                UseColumnTextForButtonValue = true,
                Width = 80
            };

            var colDelete = new DataGridViewButtonColumn
            {
                Name = "Delete",
                HeaderText = "",
                Text = "Delete",
                UseColumnTextForButtonValue = true,
                Width = 80
            };

            dgvBackups.Columns.AddRange(new DataGridViewColumn[] { colBackupName, colDateCreated, colSize, colRestore, colDelete });

            // Handle events
            dgvBackups.CellClick += DgvBackups_CellClick;
            dgvBackups.CellDoubleClick += DgvBackups_CellDoubleClick;
            dgvBackups.CellEndEdit += DgvBackups_CellEndEdit;
            dgvBackups.CellBeginEdit += DgvBackups_CellBeginEdit;

            // Create bottom panel for additional controls
            var bottomPanel = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.RightToLeft,
                Height = 40,
                Dock = DockStyle.Bottom
            };

            var btnClose = new Button
            {
                Text = "Close",
                Width = 80,
                Height = 30,
                DialogResult = DialogResult.Cancel
            };

            var btnRefresh = new Button
            {
                Text = "Refresh",
                Width = 80,
                Height = 30
            };
            btnRefresh.Click += BtnRefresh_Click;

            bottomPanel.Controls.AddRange(new Control[] { btnClose, btnRefresh });

            mainPanel.Controls.AddRange(new Control[] { lblTitle, dgvBackups });
            this.Controls.AddRange(new Control[] { mainPanel, bottomPanel });
        }

        private void LoadBackupData()
        {
            var backupData = new List<BackupInfo>();

            if (Directory.Exists(backupPath))
            {
                foreach (var dir in Directory.GetDirectories(backupPath))
                {
                    var dirInfo = new DirectoryInfo(dir);
                    var backup = new BackupInfo
                    {
                        BackupName = dirInfo.Name,
                        DateCreated = dirInfo.CreationTime.ToString("dd/MM/yyyy HH:mm"),
                        FullPath = dirInfo.FullName
                    };

                    // Calculate size
                    try
                    {
                        long folderSize = GetDirectorySize(dirInfo);
                        backup.Size = FormatBytes(folderSize);
                    }
                    catch
                    {
                        backup.Size = "N/A";
                    }

                    backupData.Add(backup);
                }
            }

            // Sort by date created (newest first)
            backupData = backupData.OrderByDescending(b => DateTime.Parse(b.DateCreated)).ToList();
            dgvBackups.DataSource = backupData;
        }

        private void DgvBackups_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0 && e.ColumnIndex >= 0)
            {
                var backup = (BackupInfo)dgvBackups.Rows[e.RowIndex].DataBoundItem;
                var columnName = dgvBackups.Columns[e.ColumnIndex].Name;

                if (columnName == "Restore")
                {
                    RestoreBackup(backup);
                }
                else if (columnName == "Delete")
                {
                    DeleteBackup(backup);
                }
            }
        }

        private void DgvBackups_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            // Only allow editing the backup name column
            if (e.RowIndex >= 0 && e.ColumnIndex == 0) // BackupName column is at index 0
            {
                dgvBackups.BeginEdit(true);
            }
        }

        private void DgvBackups_CellBeginEdit(object sender, DataGridViewCellCancelEventArgs e)
        {
            // Store the original value in case we need to revert
            if (e.ColumnIndex == 0) // BackupName column
            {
                var backup = (BackupInfo)dgvBackups.Rows[e.RowIndex].DataBoundItem;
                dgvBackups.Tag = backup.BackupName; // Store original name
            }
        }

        private void DgvBackups_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            if (e.ColumnIndex == 0) // BackupName column
            {
                var backup = (BackupInfo)dgvBackups.Rows[e.RowIndex].DataBoundItem;
                var originalName = dgvBackups.Tag?.ToString();
                var newName = backup.BackupName;

                // Validate the new name
                if (string.IsNullOrWhiteSpace(newName))
                {
                    MessageBox.Show("Backup name cannot be empty!", "Invalid Name", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    backup.BackupName = originalName; // Revert to original name
                    dgvBackups.Refresh();
                    return;
                }

                // Check for invalid characters
                char[] invalidChars = Path.GetInvalidFileNameChars();
                if (newName.IndexOfAny(invalidChars) >= 0)
                {
                    MessageBox.Show("Backup name contains invalid characters!", "Invalid Name", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    backup.BackupName = originalName; // Revert to original name
                    dgvBackups.Refresh();
                    return;
                }

                // If name changed, rename the actual folder
                if (newName != originalName)
                {
                    try
                    {
                        var oldPath = backup.FullPath;
                        var parentPath = Path.GetDirectoryName(oldPath);
                        var newPath = Path.Combine(parentPath, newName);

                        // Check if target folder already exists
                        if (Directory.Exists(newPath))
                        {
                            MessageBox.Show("A backup with this name already exists!", "Name Conflict", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            backup.BackupName = originalName; // Revert to original name
                            dgvBackups.Refresh();
                            return;
                        }

                        // Rename the directory
                        Directory.Move(oldPath, newPath);
                        
                        // Update the backup info
                        backup.FullPath = newPath;
                        
                        MessageBox.Show($"Backup renamed successfully from '{originalName}' to '{newName}'!", 
                                      "Rename Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Failed to rename backup: {ex.Message}", "Rename Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        backup.BackupName = originalName; // Revert to original name
                        dgvBackups.Refresh();
                    }
                }
            }
        }

        private async void RestoreBackup(BackupInfo backup)
        {
            // Pre-restore validation
            if (!RestoreUtilities.ValidateBackupIntegrity(backup.FullPath))
            {
                MessageBox.Show("The selected backup appears to be corrupted or incomplete. Cannot proceed with restore.", 
                              "Invalid Backup", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Check available disk space
            if (!RestoreUtilities.CheckDiskSpaceForRestore(backup.FullPath))
            {
                MessageBox.Show("Insufficient disk space to perform restore operation.", 
                              "Insufficient Space", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            var result = MessageBox.Show(
                $"Are you sure you want to restore the backup '{backup.BackupName}'?\n\n" +
                "This will:\n" +
                "• Create a safety backup of current Plex data\n" +
                "• Stop ALL Plex-related processes\n" +
                "• Restore files and registry from the backup\n" +
                "• Restart Plex Media Server\n\n" +
                "IMPORTANT:\n" +
                "- Make sure no other programs are using Plex files\n" +
                "- Close any media players or streaming clients\n" +
                "- This process may take several minutes\n" +
                "- A safety backup will be created automatically\n\n" +
                "Note: If restore fails, data will be automatically rolled back.",
                "Confirm Restore",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (result == DialogResult.Yes)
            {
                // Create and show progress form
                var progressForm = new RestoreProgressForm();
                progressForm.Show();
                
                var restoreLogger = new RestoreLogger();
                string safetyBackupPath = "";
                
                try
                {
                    // Disable all controls during restore
                    this.Enabled = false;
                    
                    await Task.Run(() => 
                    {
                        // Create a task with timeout monitoring
                        var restoreTask = Task.Run(() => 
                        {
                            safetyBackupPath = PerformBackupRestore(backup.FullPath, progressForm, restoreLogger);
                        });
                        
                        // Wait with timeout (30 minutes)
                        const int timeoutMinutes = 30;
                        if (!restoreTask.Wait(TimeSpan.FromMinutes(timeoutMinutes)))
                        {
                            throw new TimeoutException($"Restore operation timed out after {timeoutMinutes} minutes. This may indicate a stuck process or very large backup.");
                        }
                    });
                    
                    progressForm.Close();
                    
                    var successMessage = "Backup restored successfully!\n\n" +
                                       $"Restore Log:\n{restoreLogger.GetSummary()}\n\n" +
                                       $"Safety backup created at:\n{safetyBackupPath}";
                    
                    MessageBox.Show(successMessage, "Restore Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    progressForm.Close();
                    
                    var errorMessage = $"Restore operation failed: {ex.Message}\n\n" +
                                     $"Error Log:\n{restoreLogger.GetErrorLog()}\n\n";
                    
                    if (!string.IsNullOrEmpty(safetyBackupPath))
                    {
                        errorMessage += $"Safety backup is available at:\n{safetyBackupPath}\n\n" +
                                      "Would you like to automatically rollback to the safety backup?";
                        
                        var rollbackResult = MessageBox.Show(errorMessage, "Restore Failed", 
                                                            MessageBoxButtons.YesNo, MessageBoxIcon.Error);
                        
                        if (rollbackResult == DialogResult.Yes)
                        {
                            try
                            {
                                var rollbackProgress = new RestoreProgressForm();
                                rollbackProgress.Show();
                                await Task.Run(() => RollbackToSafetyBackup(safetyBackupPath, rollbackProgress));
                                rollbackProgress.Close();
                                
                                MessageBox.Show("System successfully rolled back to safety backup.", 
                                              "Rollback Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            }
                            catch (Exception rollbackEx)
                            {
                                MessageBox.Show($"Rollback failed: {rollbackEx.Message}\n\n" +
                                              $"Manual recovery may be required using the safety backup at:\n{safetyBackupPath}", 
                                              "Rollback Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            }
                        }
                    }
                    else
                    {
                        MessageBox.Show(errorMessage, "Restore Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                finally
                {
                    // Re-enable controls
                    this.Enabled = true;
                }
            }
        }

        private void DeleteBackup(BackupInfo backup)
        {
            var result = MessageBox.Show(
                $"Are you sure you want to delete the backup '{backup.BackupName}'?\n\n" +
                "This action cannot be undone!",
                "Confirm Delete",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (result == DialogResult.Yes)
            {
                try
                {
                    Directory.Delete(backup.FullPath, true);
                    MessageBox.Show("Backup deleted successfully!", "Delete Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    LoadBackupData(); // Refresh the list
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to delete backup: {ex.Message}", "Delete Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private string PerformBackupRestore(string backupFolder, RestoreProgressForm progressForm = null, RestoreLogger logger = null)
        {
            string safetyBackupPath = "";
            
            try
            {
                progressForm?.LogInfo("Starting backup restore process");
                progressForm?.LogInfo($"Source backup: {backupFolder}");
                
                // Create safety backup first
                progressForm?.UpdateStatus("Creating safety backup...");
                progressForm?.UpdateOperation("Backing up current Plex data for rollback protection");
                progressForm?.UpdateProgress(2);
                
                safetyBackupPath = RestoreUtilities.CreateSafetyBackup();
                logger?.LogInfo($"Safety backup created at: {safetyBackupPath}");
                progressForm?.LogInfo($"Safety backup created: {safetyBackupPath}");

                progressForm?.UpdateStatus("Stopping Plex Media Server...");
                progressForm?.UpdateOperation("Ensuring all Plex processes are terminated");
                progressForm?.UpdateProgress(5);
                
                // Stop Plex with enhanced verification
                StopPlexServerEnhanced();
                logger?.LogInfo("Plex Media Server stopped successfully");
                progressForm?.LogInfo("All Plex processes terminated successfully");

                progressForm?.UpdateStatus("Waiting for file handles to be released...");
                progressForm?.UpdateOperation("Allowing time for system resources to be freed");
                progressForm?.UpdateProgress(10);
                progressForm?.LogInfo("Waiting 3 seconds for file handles to be released");
                
                // Wait a bit more to ensure all file handles are released
                System.Threading.Thread.Sleep(3000);

                // Restore registry if available
                progressForm?.UpdateStatus("Restoring registry settings...");
                progressForm?.UpdateOperation("Importing Plex registry configuration");
                progressForm?.UpdateProgress(20);
                
                var regBackupPath = Path.Combine(backupFolder, "RegBackup");
                if (Directory.Exists(regBackupPath))
                {
                    progressForm?.LogInfo($"Found registry backup at: {regBackupPath}");
                    var regFiles = Directory.GetFiles(regBackupPath, "*.reg");
                    if (regFiles.Length > 0)
                    {
                        progressForm?.LogInfo($"Importing registry file: {regFiles[0]}");
                        var regProcess = new Process
                        {
                            StartInfo = new ProcessStartInfo
                            {
                                FileName = "reg",
                                Arguments = $"import \"{regFiles[0]}\"",
                                UseShellExecute = false,
                                CreateNoWindow = true,
                                RedirectStandardOutput = true,
                                RedirectStandardError = true
                            }
                        };
                        regProcess.Start();
                        regProcess.WaitForExit();
                        
                        if (regProcess.ExitCode != 0)
                        {
                            var error = regProcess.StandardError.ReadToEnd();
                            logger?.LogError($"Registry import failed: {error}");
                            progressForm?.LogError($"Registry import failed with exit code {regProcess.ExitCode}: {error}");
                            throw new Exception($"Registry import failed with exit code: {regProcess.ExitCode}");
                        }
                        logger?.LogInfo("Registry settings restored successfully");
                        progressForm?.LogInfo("Registry settings imported successfully");
                    }
                    else
                    {
                        progressForm?.LogWarning("No registry files found in backup");
                    }
                }
                else
                {
                    progressForm?.LogInfo("No registry backup found - skipping registry restore");
                }

                // Restore files if available
                var fileBackupPath = Path.Combine(backupFolder, "FileBackup");
                if (Directory.Exists(fileBackupPath))
                {
                    progressForm?.LogInfo($"Found file backup at: {fileBackupPath}");
                    progressForm?.UpdateStatus("Clearing existing Plex data...");
                    progressForm?.UpdateOperation("Removing current database and configuration files");
                    progressForm?.UpdateProgress(30);
                    
                    var destination = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Plex Media Server");
                    progressForm?.LogInfo($"Target directory: {destination}");
                    
                    // Try to clear existing data with retry logic
                    ClearPlexDataWithRetry(destination, progressForm);
                    logger?.LogInfo("Existing Plex data cleared successfully");
                    progressForm?.LogInfo("Existing Plex data cleared successfully");

                    progressForm?.UpdateStatus("Restoring files from backup...");
                    progressForm?.UpdateOperation("Copying database and configuration files - this may take several minutes");
                    progressForm?.UpdateProgress(50);

                    // Try robocopy first with timeout, fallback to manual copy if needed
                    bool robocopySuccess = false;
                    
                    try
                    {
                        progressForm?.LogInfo("Attempting file restore using robocopy...");
                        robocopySuccess = RestoreFilesWithRobocopy(fileBackupPath, destination, progressForm, logger);
                    }
                    catch (Exception robocopyEx)
                    {
                        logger?.LogError($"Robocopy failed: {robocopyEx.Message}");
                        progressForm?.LogError($"Robocopy failed: {robocopyEx.Message}");
                        progressForm?.UpdateOperation("Robocopy failed, switching to manual copy method...");
                    }
                    
                    // If robocopy fails, use manual copy as fallback
                    if (!robocopySuccess)
                    {
                        logger?.LogInfo("Using manual copy method as fallback");
                        progressForm?.LogWarning("Robocopy failed - switching to manual copy method");
                        progressForm?.UpdateOperation("Using alternative copy method - please wait...");
                        RestoreFilesManually(fileBackupPath, destination, progressForm, logger);
                    }

                    logger?.LogInfo("Files restored successfully from backup");
                    progressForm?.LogInfo("All files restored successfully");
                    
                    progressForm?.UpdateStatus("Verifying restored data...");
                    progressForm?.UpdateOperation("Checking integrity of restored files");
                    progressForm?.UpdateProgress(85);
                    progressForm?.LogInfo("Starting data verification...");
                    
                    // Verify restored data
                    if (!RestoreUtilities.VerifyRestoredData(destination))
                    {
                        logger?.LogError("Data verification failed after restore");
                        progressForm?.LogError("Data verification failed - some files may be corrupted");
                        throw new Exception("Restored data verification failed - some files may be corrupted");
                    }
                    logger?.LogInfo("Data verification completed successfully");
                    progressForm?.LogInfo("Data verification completed - all files are intact");
                }
                else
                {
                    progressForm?.LogWarning("No file backup found - skipping file restore");
                }

                progressForm?.UpdateStatus("Restarting Plex Media Server...");
                progressForm?.UpdateOperation("Starting Plex services");
                progressForm?.UpdateProgress(95);
                progressForm?.LogInfo("Starting Plex Media Server...");
                
                logger?.LogInfo("Restore operation completed successfully");
                progressForm?.LogInfo("Restore operation completed successfully!");
                return safetyBackupPath;
            }
            catch (Exception ex)
            {
                logger?.LogError($"Restore operation failed: {ex.Message}");
                progressForm?.LogError($"Restore operation failed: {ex.Message}");
                throw;
            }
            finally
            {
                // Always try to restart Plex
                StartPlexServer();
                
                progressForm?.UpdateStatus("Restore completed!");
                progressForm?.UpdateOperation("Plex Media Server has been restarted");
                progressForm?.UpdateProgress(100);
                progressForm?.LogInfo("Plex Media Server restarted successfully");
                
                // Give a moment to show the completion message
                System.Threading.Thread.Sleep(1000);
            }
        }

        private bool RestoreFilesWithRobocopy(string source, string destination, RestoreProgressForm progressForm, RestoreLogger logger)
        {
            const int timeoutMinutes = 10; // Maximum 10 minutes for robocopy
            
            try
            {
                progressForm?.UpdateOperation("Starting robocopy process...");
                progressForm?.LogInfo($"Starting robocopy: {source} -> {destination}");
                progressForm?.LogInfo($"Robocopy timeout set to {timeoutMinutes} minutes");
                
                var robocopyProcess = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "robocopy",
                        Arguments = $"\"{source}\" \"{destination}\" /MIR /R:3 /W:1 /MT:2 /NP",
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true
                    }
                };

                robocopyProcess.Start();
                progressForm?.LogInfo("Robocopy process started");
                
                // Wait with timeout and progress updates
                var startTime = DateTime.Now;
                var timeoutTime = startTime.AddMinutes(timeoutMinutes);
                
                while (!robocopyProcess.HasExited && DateTime.Now < timeoutTime)
                {
                    var elapsed = DateTime.Now - startTime;
                    var progressPercentage = Math.Min(50 + (int)(elapsed.TotalMinutes / timeoutMinutes * 30), 80);
                    
                    progressForm?.UpdateProgress(progressPercentage);
                    progressForm?.UpdateOperation($"Copying files... ({elapsed.Minutes}m {elapsed.Seconds}s elapsed)");
                    
                    System.Threading.Thread.Sleep(2000); // Update every 2 seconds
                }
                
                if (!robocopyProcess.HasExited)
                {
                    // Timeout - kill the process
                    logger?.LogError($"Robocopy timeout after {timeoutMinutes} minutes");
                    progressForm?.LogError($"Robocopy timeout after {timeoutMinutes} minutes - terminating process");
                    try
                    {
                        robocopyProcess.Kill();
                        robocopyProcess.WaitForExit(5000);
                    }
                    catch { }
                    
                    throw new TimeoutException($"Robocopy operation timed out after {timeoutMinutes} minutes");
                }

                // Check the exit code
                var exitCode = robocopyProcess.ExitCode;
                progressForm?.LogInfo($"Robocopy completed with exit code: {exitCode}");
                
                if (exitCode > 7)
                {
                    var output = robocopyProcess.StandardOutput.ReadToEnd();
                    var error = robocopyProcess.StandardError.ReadToEnd();
                    logger?.LogError($"Robocopy failed with exit code {exitCode}: {output} {error}");
                    progressForm?.LogError($"Robocopy failed with exit code {exitCode}");
                    progressForm?.LogError($"Output: {output}");
                    progressForm?.LogError($"Error: {error}");
                    return false;
                }
                
                logger?.LogInfo($"Robocopy completed successfully with exit code: {exitCode}");
                progressForm?.LogInfo("Robocopy completed successfully");
                return true;
            }
            catch (Exception ex)
            {
                logger?.LogError($"Robocopy exception: {ex.Message}");
                progressForm?.LogError($"Robocopy exception: {ex.Message}");
                return false;
            }
        }

        private void RestoreFilesManually(string source, string destination, RestoreProgressForm progressForm, RestoreLogger logger)
        {
            try
            {
                progressForm?.UpdateOperation("Calculating files to copy...");
                progressForm?.LogInfo("Starting manual file copy process");
                
                // Get all files to copy
                var sourceDir = new DirectoryInfo(source);
                var allFiles = sourceDir.GetFiles("*", SearchOption.AllDirectories).ToList();
                var totalFiles = allFiles.Count;
                var copiedFiles = 0;
                
                logger?.LogInfo($"Found {totalFiles} files to copy");
                progressForm?.LogInfo($"Found {totalFiles} files to copy manually");
                
                // Create directory structure first
                progressForm?.UpdateOperation("Creating directory structure...");
                progressForm?.LogInfo("Creating directory structure...");
                CreateDirectoryStructure(source, destination);
                
                // Copy files with progress
                progressForm?.LogInfo("Starting file copy process...");
                foreach (var file in allFiles)
                {
                    try
                    {
                        var relativePath = Path.GetRelativePath(source, file.FullName);
                        var destFile = Path.Combine(destination, relativePath);
                        var destDir = Path.GetDirectoryName(destFile);
                        
                        if (!Directory.Exists(destDir))
                        {
                            Directory.CreateDirectory(destDir);
                        }
                        
                        File.Copy(file.FullName, destFile, true);
                        copiedFiles++;
                        
                        // Update progress every 10 files or if it's a large file
                        if (copiedFiles % 10 == 0 || file.Length > 1024 * 1024) // Every 10 files or if file > 1MB
                        {
                            var progressPercentage = 50 + (int)((double)copiedFiles / totalFiles * 30);
                            progressForm?.UpdateProgress(progressPercentage);
                            progressForm?.UpdateOperation($"Copying files... {copiedFiles}/{totalFiles} ({file.Name})");
                        }
                        
                        // Log large files or milestones
                        if (file.Length > 10 * 1024 * 1024) // Files > 10MB
                        {
                            progressForm?.LogInfo($"Copied large file: {file.Name} ({file.Length / (1024 * 1024)} MB)");
                        }
                        else if (copiedFiles % 100 == 0) // Every 100 files
                        {
                            progressForm?.LogInfo($"Progress: {copiedFiles}/{totalFiles} files copied");
                        }
                    }
                    catch (Exception ex)
                    {
                        logger?.LogError($"Failed to copy file {file.FullName}: {ex.Message}");
                        progressForm?.LogError($"Failed to copy file {file.Name}: {ex.Message}");
                        // Continue with other files
                    }
                }
                
                logger?.LogInfo($"Manual copy completed: {copiedFiles}/{totalFiles} files copied");
                progressForm?.LogInfo($"Manual copy completed: {copiedFiles}/{totalFiles} files copied");
                
                if (copiedFiles == 0)
                {
                    throw new Exception("No files were copied successfully");
                }
                else if (copiedFiles < totalFiles)
                {
                    var skippedFiles = totalFiles - copiedFiles;
                    logger?.LogError($"Some files failed to copy: {skippedFiles} files skipped");
                    progressForm?.LogWarning($"{skippedFiles} files were skipped due to errors");
                }
            }
            catch (Exception ex)
            {
                logger?.LogError($"Manual copy failed: {ex.Message}");
                progressForm?.LogError($"Manual copy failed: {ex.Message}");
                throw;
            }
        }

        private void CreateDirectoryStructure(string source, string destination)
        {
            var sourceDir = new DirectoryInfo(source);
            var allDirs = sourceDir.GetDirectories("*", SearchOption.AllDirectories);
            
            foreach (var dir in allDirs)
            {
                var relativePath = Path.GetRelativePath(source, dir.FullName);
                var destDir = Path.Combine(destination, relativePath);
                
                if (!Directory.Exists(destDir))
                {
                    Directory.CreateDirectory(destDir);
                }
            }
        }

        private void StopPlexServerEnhanced()
        {
            // Get all Plex-related processes
            var plexProcessNames = new[] { "Plex Media Server", "PlexMediaServer", "Plex", "PlexScriptHost", "PlexDlnaServer", "PlexTranscoder" };
            var processesToKill = new List<Process>();

            foreach (var processName in plexProcessNames)
            {
                var processes = Process.GetProcessesByName(processName);
                processesToKill.AddRange(processes);
            }

            if (processesToKill.Count > 0)
            {
                foreach (var process in processesToKill)
                {
                    try
                    {
                        process.Kill();
                        process.WaitForExit(10000); // Wait up to 10 seconds
                    }
                    catch (Exception)
                    {
                        // Process might already be dead, ignore
                    }
                }

                // Wait additional time for file handles to be released
                System.Threading.Thread.Sleep(5000);

                // Verify all processes are actually stopped
                foreach (var processName in plexProcessNames)
                {
                    var remainingProcesses = Process.GetProcessesByName(processName);
                    if (remainingProcesses.Length > 0)
                    {
                        throw new Exception($"Could not stop all Plex processes. {remainingProcesses.Length} {processName} process(es) still running.");
                    }
                }
            }
        }

        private void ClearPlexDataWithRetry(string destination, RestoreProgressForm progressForm = null)
        {
            const int maxRetries = 5;
            const int baseRetryDelayMs = 1000;
            
            progressForm?.LogInfo($"Starting to clear Plex data directory: {destination}");

            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                try
                {
                    if (Directory.Exists(destination))
                    {
                        progressForm?.UpdateOperation($"Clearing data directory (attempt {attempt} of {maxRetries})");
                        progressForm?.UpdateProgress(30 + attempt * 3); // Progress from 30% to 45%
                        progressForm?.LogInfo($"Attempt {attempt}/{maxRetries} to clear directory");
                        
                        // First, try to clear file attributes that might prevent deletion
                        ClearFileAttributesRecursively(destination, progressForm);
                        
                        // Try to delete the directory with progress feedback
                        DeleteDirectoryWithProgress(destination, progressForm);
                    }
                    else
                    {
                        progressForm?.LogInfo("Target directory does not exist - nothing to clear");
                    }
                    
                    progressForm?.UpdateOperation("Data directory cleared successfully");
                    progressForm?.LogInfo("Data directory cleared successfully");
                    return; // Success, exit the retry loop
                }
                catch (UnauthorizedAccessException ex)
                {
                    if (attempt == maxRetries)
                    {
                        progressForm?.LogError($"Failed to clear directory after {maxRetries} attempts - access denied");
                        throw new Exception($"Could not clear Plex data directory after {maxRetries} attempts. Files may be in use by another process: {ex.Message}");
                    }
                    
                    var retryDelay = baseRetryDelayMs * attempt; // Exponential backoff
                    progressForm?.UpdateOperation($"Access denied. Retrying in {retryDelay/1000} seconds... (attempt {attempt} failed)");
                    progressForm?.LogWarning($"Attempt {attempt} failed with access denied - retrying in {retryDelay/1000} seconds");
                    System.Threading.Thread.Sleep(retryDelay);
                }
                catch (IOException ex)
                {
                    if (attempt == maxRetries)
                    {
                        progressForm?.LogError($"Failed to clear directory after {maxRetries} attempts - I/O error");
                        throw new Exception($"Could not clear Plex data directory after {maxRetries} attempts. Files may be in use by another process: {ex.Message}");
                    }
                    
                    var retryDelay = baseRetryDelayMs * attempt; // Exponential backoff
                    progressForm?.UpdateOperation($"I/O error. Retrying in {retryDelay/1000} seconds... (attempt {attempt} failed)");
                    progressForm?.LogWarning($"Attempt {attempt} failed with I/O error - retrying in {retryDelay/1000} seconds");
                    System.Threading.Thread.Sleep(retryDelay);
                }
                catch (Exception ex)
                {
                    progressForm?.LogError($"Unexpected error while clearing directory: {ex.Message}");
                    throw new Exception($"Unexpected error while clearing Plex data directory: {ex.Message}");
                }
            }
        }

        private void DeleteDirectoryWithProgress(string path, RestoreProgressForm progressForm)
        {
            try
            {
                // Get subdirectories and files for progress tracking
                var dirInfo = new DirectoryInfo(path);
                var subDirs = dirInfo.GetDirectories("*", SearchOption.TopDirectoryOnly);
                var files = dirInfo.GetFiles("*", SearchOption.TopDirectoryOnly);
                
                var totalItems = subDirs.Length + files.Length;
                var processedItems = 0;
                
                // Delete files first
                foreach (var file in files)
                {
                    try
                    {
                        file.Delete();
                        processedItems++;
                        if (totalItems > 0)
                        {
                            progressForm?.UpdateOperation($"Deleting files... {processedItems}/{totalItems}");
                        }
                    }
                    catch
                    {
                        // Try to clear attributes and delete again
                        file.Attributes = FileAttributes.Normal;
                        file.Delete();
                        processedItems++;
                    }
                }
                
                // Delete subdirectories recursively
                foreach (var subDir in subDirs)
                {
                    try
                    {
                        subDir.Delete(true);
                        processedItems++;
                        if (totalItems > 0)
                        {
                            progressForm?.UpdateOperation($"Deleting directories... {processedItems}/{totalItems}");
                        }
                    }
                    catch
                    {
                        // Try to clear attributes recursively and delete again
                        ClearFileAttributesRecursively(subDir.FullName, progressForm);
                        subDir.Delete(true);
                        processedItems++;
                    }
                }
                
                // Finally delete the root directory if it's empty
                if (dirInfo.GetFileSystemInfos().Length == 0)
                {
                    dirInfo.Delete();
                }
            }
            catch (Exception)
            {
                // If granular deletion fails, try the simple approach
                Directory.Delete(path, true);
            }
        }

        private void ClearFileAttributesRecursively(string path, RestoreProgressForm progressForm = null)
        {
            try
            {
                progressForm?.UpdateOperation("Clearing file attributes to enable deletion...");
                
                // Clear attributes on the directory itself
                if (Directory.Exists(path))
                {
                    var dirInfo = new DirectoryInfo(path);
                    if (dirInfo.Attributes.HasFlag(FileAttributes.ReadOnly))
                    {
                        dirInfo.Attributes &= ~FileAttributes.ReadOnly;
                    }

                    // Clear attributes on all files
                    var files = dirInfo.GetFiles("*", SearchOption.AllDirectories);
                    var processedFiles = 0;
                    
                    foreach (var file in files)
                    {
                        try
                        {
                            if (file.Attributes.HasFlag(FileAttributes.ReadOnly) || 
                                file.Attributes.HasFlag(FileAttributes.Hidden) || 
                                file.Attributes.HasFlag(FileAttributes.System))
                            {
                                file.Attributes = FileAttributes.Normal;
                            }
                            
                            processedFiles++;
                            
                            // Update progress every 100 files
                            if (processedFiles % 100 == 0)
                            {
                                progressForm?.UpdateOperation($"Clearing attributes... {processedFiles}/{files.Length} files");
                            }
                        }
                        catch
                        {
                            // If we can't change attributes on a specific file, continue with others
                        }
                    }

                    // Clear attributes on all subdirectories
                    foreach (var dir in dirInfo.GetDirectories("*", SearchOption.AllDirectories))
                    {
                        try
                        {
                            if (dir.Attributes.HasFlag(FileAttributes.ReadOnly))
                            {
                                dir.Attributes &= ~FileAttributes.ReadOnly;
                            }
                        }
                        catch
                        {
                            // If we can't change attributes on a specific directory, continue with others
                        }
                    }
                }
            }
            catch
            {
                // If we can't clear attributes, we'll still try to delete
            }
        }

        private void StopPlexServer()
        {
            var plexProcessNames = new[] { "Plex Media Server", "PlexMediaServer", "Plex" };
            var processesToKill = new List<Process>();

            foreach (var processName in plexProcessNames)
            {
                var processes = Process.GetProcessesByName(processName);
                processesToKill.AddRange(processes);
            }

            foreach (var process in processesToKill)
            {
                try
                {
                    process.Kill();
                    process.WaitForExit(8000); // Wait up to 8 seconds
                }
                catch (Exception)
                {
                    // Process might already be dead or access denied, ignore
                }
            }

            // Additional wait for file handles to be released
            if (processesToKill.Count > 0)
            {
                System.Threading.Thread.Sleep(2000);
            }
        }

        private void StartPlexServer()
        {
            string[] plexPaths = {
                @"C:\Program Files (x86)\Plex\Plex Media Server\Plex Media Server.exe",
                @"C:\Program Files\Plex\Plex Media Server\Plex Media Server.exe"
            };

            foreach (var path in plexPaths)
            {
                if (File.Exists(path))
                {
                    Process.Start(path);
                    break;
                }
            }
        }

        private void BtnRefresh_Click(object sender, EventArgs e)
        {
            LoadBackupData();
        }

        private long GetDirectorySize(DirectoryInfo dirInfo)
        {
            long size = 0;
            foreach (FileInfo file in dirInfo.GetFiles("*", SearchOption.AllDirectories))
            {
                size += file.Length;
            }
            return size;
        }

        private string FormatBytes(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            double len = bytes;
            int order = 0;
            
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }
            
            return $"{len:0.##} {sizes[order]}";
        }

        private void RollbackToSafetyBackup(string safetyBackupPath, RestoreProgressForm progressForm)
        {
            progressForm?.UpdateStatus("Rolling back to safety backup...");
            progressForm?.UpdateOperation("Restoring previous Plex configuration");
            progressForm?.UpdateProgress(10);

            // Stop Plex first
            StopPlexServerEnhanced();
            
            progressForm?.UpdateProgress(30);

            var plexDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Plex Media Server");
            
            // Clear current data
            if (Directory.Exists(plexDataPath))
            {
                progressForm?.UpdateStatus("Clearing corrupted data...");
                progressForm?.UpdateProgress(50);
                
                ClearPlexDataWithRetry(plexDataPath, progressForm);
            }

            // Restore from safety backup
            progressForm?.UpdateStatus("Restoring from safety backup...");
            progressForm?.UpdateOperation("Copying backed up files");
            progressForm?.UpdateProgress(70);
            
            if (Directory.Exists(safetyBackupPath))
            {
                RestoreUtilities.CopyDirectory(safetyBackupPath, plexDataPath);
            }

            progressForm?.UpdateProgress(90);

            // Restart Plex
            StartPlexServer();
            
            progressForm?.UpdateStatus("Rollback completed");
            progressForm?.UpdateProgress(100);

            // Clean up safety backup
            try
            {
                Directory.Delete(safetyBackupPath, true);
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }

    // Helper class for backup data
    public class BackupInfo
    {
        public string BackupName { get; set; }
        public string DateCreated { get; set; }
        public string Size { get; set; }
        public string FullPath { get; set; }
    }

    // Restore operation logger
    public class RestoreLogger
    {
        private List<string> infoLogs = new List<string>();
        private List<string> errorLogs = new List<string>();
        private DateTime startTime;
        
        public RestoreLogger()
        {
            startTime = DateTime.Now;
        }
        
        public void LogInfo(string message)
        {
            var logEntry = $"[{DateTime.Now:HH:mm:ss}] INFO: {message}";
            infoLogs.Add(logEntry);
        }
        
        public void LogError(string message)
        {
            var logEntry = $"[{DateTime.Now:HH:mm:ss}] ERROR: {message}";
            errorLogs.Add(logEntry);
        }
        
        public string GetSummary()
        {
            var duration = DateTime.Now - startTime;
            var summary = $"Operation Duration: {duration:mm\\:ss}\n";
            summary += $"Steps Completed: {infoLogs.Count}\n";
            if (errorLogs.Count > 0)
            {
                summary += $"Errors Encountered: {errorLogs.Count}\n";
            }
            return summary;
        }
        
        public string GetErrorLog()
        {
            return string.Join("\n", errorLogs);
        }
        
        public string GetFullLog()
        {
            var allLogs = new List<string>();
            allLogs.AddRange(infoLogs);
            allLogs.AddRange(errorLogs);
            return string.Join("\n", allLogs.OrderBy(log => log));
        }
    }

    // Validation and safety methods for restore operations
    public static class RestoreUtilities
    {
        public static bool ValidateBackupIntegrity(string backupPath)
        {
            try
            {
                // Check if backup directory exists
                if (!Directory.Exists(backupPath))
                    return false;

                // Check for essential backup components
                var regBackupPath = Path.Combine(backupPath, "RegBackup");
                var fileBackupPath = Path.Combine(backupPath, "FileBackup");
                
                // At least one backup type should exist
                bool hasRegBackup = Directory.Exists(regBackupPath) && Directory.GetFiles(regBackupPath, "*.reg").Length > 0;
                bool hasFileBackup = Directory.Exists(fileBackupPath) && Directory.GetDirectories(fileBackupPath).Length > 0;
                
                if (!hasRegBackup && !hasFileBackup)
                    return false;

                // If file backup exists, verify critical Plex files
                if (hasFileBackup)
                {
                    var criticalFiles = new[] {
                        "Preferences.xml",
                        "Plug-in Support\\Databases\\com.plexapp.plugins.library.db"
                    };

                    foreach (var criticalFile in criticalFiles)
                    {
                        var fullPath = Path.Combine(fileBackupPath, criticalFile);
                        if (File.Exists(fullPath) && new FileInfo(fullPath).Length > 0)
                            return true; // At least one critical file exists and has content
                    }
                }

                return hasRegBackup; // If no file backup verification possible, registry backup is sufficient
            }
            catch
            {
                return false;
            }
        }

        public static bool CheckDiskSpaceForRestore(string backupPath)
        {
            try
            {
                // Calculate backup size
                var backupSize = GetDirectorySize(new DirectoryInfo(backupPath));
                
                // Get available space on target drive (usually C:)
                var targetDrive = new DriveInfo(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData).Substring(0, 1));
                var availableSpace = targetDrive.AvailableFreeSpace;
                
                // Require at least 2x the backup size for safety (temp files, etc.)
                var requiredSpace = backupSize * 2;
                
                return availableSpace > requiredSpace;
            }
            catch
            {
                return true; // If we can't check, assume it's OK
            }
        }

        public static string CreateSafetyBackup()
        {
            var safetyBackupPath = Path.Combine(Path.GetTempPath(), $"PlexSafetyBackup_{DateTime.Now:yyyyMMdd_HHmmss}");
            Directory.CreateDirectory(safetyBackupPath);

            var plexDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Plex Media Server");
            
            if (Directory.Exists(plexDataPath))
            {
                // Create a quick backup of critical files only (for speed)
                var criticalPaths = new[]
                {
                    "Preferences.xml",
                    "Plug-in Support\\Databases",
                    "Media\\localhost"
                };

                foreach (var criticalPath in criticalPaths)
                {
                    var sourcePath = Path.Combine(plexDataPath, criticalPath);
                    var destPath = Path.Combine(safetyBackupPath, criticalPath);
                    
                    try
                    {
                        if (File.Exists(sourcePath))
                        {
                            Directory.CreateDirectory(Path.GetDirectoryName(destPath));
                            File.Copy(sourcePath, destPath, true);
                        }
                        else if (Directory.Exists(sourcePath))
                        {
                            CopyDirectory(sourcePath, destPath);
                        }
                    }
                    catch
                    {
                        // Continue with other files if one fails
                    }
                }
            }

            return safetyBackupPath;
        }

        public static void CopyDirectory(string sourceDir, string destDir)
        {
            Directory.CreateDirectory(destDir);
            
            foreach (var file in Directory.GetFiles(sourceDir))
            {
                var destFile = Path.Combine(destDir, Path.GetFileName(file));
                File.Copy(file, destFile, true);
            }
            
            foreach (var subDir in Directory.GetDirectories(sourceDir))
            {
                var destSubDir = Path.Combine(destDir, Path.GetFileName(subDir));
                CopyDirectory(subDir, destSubDir);
            }
        }

        public static bool VerifyRestoredData(string plexDataPath)
        {
            try
            {
                // Check if critical files exist and have reasonable sizes
                var preferencesFile = Path.Combine(plexDataPath, "Preferences.xml");
                if (File.Exists(preferencesFile) && new FileInfo(preferencesFile).Length > 100)
                    return true;

                var databaseFile = Path.Combine(plexDataPath, "Plug-in Support", "Databases", "com.plexapp.plugins.library.db");
                if (File.Exists(databaseFile) && new FileInfo(databaseFile).Length > 1000)
                    return true;

                return false;
            }
            catch
            {
                return false;
            }
        }

        private static long GetDirectorySize(DirectoryInfo dirInfo)
        {
            long size = 0;
            foreach (FileInfo file in dirInfo.GetFiles("*", SearchOption.AllDirectories))
            {
                size += file.Length;
            }
            return size;
        }
    }

    public class AdvancedSettingsForm : Form
    {
        private PlexBackupConfig config;
        private CheckBox chkMinimizeToTray;
        private CheckBox chkShowNotifications;
        private CheckBox chkAutoStartWithWindows;
        private CheckBox chkEnableRollback;
        private NumericUpDown nudMaxRollbackAttempts;
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
                RowCount = 10,
                Padding = new Padding(10)
            };

            // UI Settings
            var lblUISettings = new Label { Text = "UI Settings:", Font = new Font("Arial", 10F, FontStyle.Bold), AutoSize = true };
            chkMinimizeToTray = new CheckBox { Text = "Minimize to system tray", AutoSize = true };
            chkShowNotifications = new CheckBox { Text = "Show backup notifications", AutoSize = true };
            chkAutoStartWithWindows = new CheckBox { Text = "Auto start with Windows", AutoSize = true };

            // Rollback Settings
            var lblRollbackSettings = new Label { Text = "Rollback Settings:", Font = new Font("Arial", 10F, FontStyle.Bold), AutoSize = true };
            chkEnableRollback = new CheckBox { Text = "Enable automatic rollback on backup failure", AutoSize = true };
            
            var lblMaxAttempts = new Label { Text = "Max Rollback Attempts:", AutoSize = true };
            nudMaxRollbackAttempts = new NumericUpDown 
            { 
                Minimum = 1, 
                Maximum = 10, 
                Value = 3, 
                Width = 60 
            };

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
            
            mainPanel.Controls.Add(lblRollbackSettings, 0, 4);
            mainPanel.SetColumnSpan(lblRollbackSettings, 2);
            mainPanel.Controls.Add(chkEnableRollback, 0, 5);
            mainPanel.SetColumnSpan(chkEnableRollback, 2);
            mainPanel.Controls.Add(lblMaxAttempts, 0, 6);
            mainPanel.Controls.Add(nudMaxRollbackAttempts, 1, 6);
            
            mainPanel.Controls.Add(lblLogLevel, 0, 7);
            mainPanel.Controls.Add(cmbLogLevel, 1, 7);
            mainPanel.Controls.Add(lblCustomPaths, 0, 8);
            mainPanel.SetColumnSpan(lblCustomPaths, 2);
            mainPanel.Controls.Add(lstCustomPlexPaths, 0, 9);
            mainPanel.SetColumnSpan(lstCustomPlexPaths, 2);
            mainPanel.Controls.Add(pathButtonPanel, 0, 10);
            mainPanel.SetColumnSpan(pathButtonPanel, 2);

            this.Controls.Add(mainPanel);
            this.Controls.Add(buttonPanel);
        }

        private void LoadAdvancedSettings()
        {
            chkMinimizeToTray.Checked = config.MinimizeToTray;
            chkShowNotifications.Checked = config.ShowNotifications;
            chkAutoStartWithWindows.Checked = config.AutoStartWithWindows;
            chkEnableRollback.Checked = config.EnableRollback;
            nudMaxRollbackAttempts.Value = config.MaxRollbackAttempts;
            
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
            config.EnableRollback = chkEnableRollback.Checked;
            config.MaxRollbackAttempts = (int)nudMaxRollbackAttempts.Value;
            config.LogLevel = cmbLogLevel.SelectedItem?.ToString() ?? "Info";
            
            config.CustomPlexPaths.Clear();
            foreach (var item in lstCustomPlexPaths.Items)
            {
                config.CustomPlexPaths.Add(item.ToString());
            }
        }
    }

    public class RestoreProgressForm : Form
    {
        private Label lblStatus;
        private ProgressBar progressBar;
        private Label lblOperation;
        private RichTextBox txtLog;
        private Panel logPanel;

        public RestoreProgressForm()
        {
            InitializeProgressForm();
        }

        private void InitializeProgressForm()
        {
            this.Text = "Restoring Backup...";
            this.Size = new Size(600, 500);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.Sizable;
            this.MaximizeBox = true;
            this.MinimizeBox = false;
            this.MinimumSize = new Size(500, 400);
            this.ControlBox = false; // Prevent user from closing during restore

            // Create main container
            var mainContainer = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                Padding = new Padding(10)
            };

            // Progress section (top)
            var progressPanel = new Panel
            {
                Height = 150,
                Dock = DockStyle.Fill
            };

            var progressContainer = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 4,
                Padding = new Padding(5)
            };

            // Title
            var lblTitle = new Label
            {
                Text = "Restoring Plex Backup",
                Font = new Font("Arial", 12F, FontStyle.Bold),
                AutoSize = true,
                Anchor = AnchorStyles.Top,
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill
            };

            // Status
            lblStatus = new Label
            {
                Text = "Preparing restore operation...",
                AutoSize = false,
                Height = 25,
                Anchor = AnchorStyles.Left | AnchorStyles.Right,
                Font = new Font("Arial", 9F, FontStyle.Bold),
                ForeColor = Color.DarkBlue,
                Dock = DockStyle.Fill
            };

            // Progress bar
            progressBar = new ProgressBar
            {
                Style = ProgressBarStyle.Marquee,
                MarqueeAnimationSpeed = 50,
                Height = 25,
                Dock = DockStyle.Fill
            };

            // Operation details
            lblOperation = new Label
            {
                Text = "Please wait while the backup is being restored...",
                AutoSize = false,
                Height = 40,
                Anchor = AnchorStyles.Left | AnchorStyles.Right,
                Font = new Font("Arial", 8F, FontStyle.Italic),
                ForeColor = Color.Gray,
                Dock = DockStyle.Fill
            };

            progressContainer.Controls.Add(lblTitle, 0, 0);
            progressContainer.Controls.Add(lblStatus, 0, 1);
            progressContainer.Controls.Add(progressBar, 0, 2);
            progressContainer.Controls.Add(lblOperation, 0, 3);

            progressPanel.Controls.Add(progressContainer);

            // Log section (bottom)
            logPanel = new Panel
            {
                Dock = DockStyle.Fill
            };

            var logLabel = new Label
            {
                Text = "Restore Log:",
                Font = new Font("Arial", 9F, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(5, 5)
            };

            txtLog = new RichTextBox
            {
                Location = new Point(5, 25),
                Size = new Size(logPanel.Width - 10, logPanel.Height - 30),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                BackColor = Color.Black,
                ForeColor = Color.LightGray,
                Font = new Font("Consolas", 8F),
                ReadOnly = true,
                ScrollBars = RichTextBoxScrollBars.Vertical,
                WordWrap = false
            };

            logPanel.Controls.Add(logLabel);
            logPanel.Controls.Add(txtLog);

            // Add panels to main container
            mainContainer.Controls.Add(progressPanel, 0, 0);
            mainContainer.Controls.Add(logPanel, 0, 1);

            // Set row styles
            mainContainer.RowStyles.Add(new RowStyle(SizeType.Absolute, 150F));
            mainContainer.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            this.Controls.Add(mainContainer);

            // Add initial log entry
            LogMessage("Restore operation initialized", Color.Cyan);
        }

        private void LogMessage(string message, Color color)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action<string, Color>(LogMessage), message, color);
                return;
            }

            txtLog.SelectionStart = txtLog.TextLength;
            txtLog.SelectionLength = 0;
            txtLog.SelectionColor = color;
            txtLog.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}\n");
            txtLog.SelectionColor = txtLog.ForeColor;
            txtLog.ScrollToCaret();
        }

        public void UpdateStatus(string status)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action<string>(UpdateStatus), status);
                return;
            }
            lblStatus.Text = status;
            LogMessage($"STATUS: {status}", Color.LightBlue);
            this.Refresh();
        }

        public void UpdateOperation(string operation)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action<string>(UpdateOperation), operation);
                return;
            }
            lblOperation.Text = operation;
            LogMessage($"OPERATION: {operation}", Color.LightGray);
            this.Refresh();
        }

        public void LogInfo(string message)
        {
            LogMessage($"INFO: {message}", Color.LightGreen);
        }

        public void LogError(string message)
        {
            LogMessage($"ERROR: {message}", Color.Red);
        }

        public void LogWarning(string message)
        {
            LogMessage($"WARNING: {message}", Color.Yellow);
        }

        public void UpdateProgress(int percentage)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action<int>(UpdateProgress), percentage);
                return;
            }
            
            // Switch to continuous style when we have actual progress
            if (progressBar.Style != ProgressBarStyle.Continuous)
            {
                progressBar.Style = ProgressBarStyle.Continuous;
                progressBar.Minimum = 0;
                progressBar.Maximum = 100;
            }
            
            progressBar.Value = Math.Min(percentage, 100);
            LogMessage($"PROGRESS: {percentage}%", Color.Cyan);
            this.Refresh();
        }
    }
}
