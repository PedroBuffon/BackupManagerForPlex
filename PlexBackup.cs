/*
 * Backup Manager For Plex
 * Copyright (c) 2025 Pedro Buffon
 * Licensed under MIT License - see LICENSE file for details
 * 
 * This software is not affiliated with Plex, Inc.
 */

using System;
using System.IO;
using System.Diagnostics;
using System.Windows.Forms;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using System.Threading.Tasks;

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
    public partial class PlexBackupForm : Form
    {
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
        private ComboBox cmbRetentionDays;
        private ProgressBar progressBar;
        private RichTextBox txtLog;
        private Label lblStatus;
        private Button btnViewBackups;
        private Button btnRestoreBackup;
        private Button btnSettings;

        [STAThread]
        public static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new PlexBackupForm());
        }

        public PlexBackupForm()
        {
            InitializeComponent();
            LoadConfiguration();
        }

        private void InitializeComponent()
        {
            this.Text = "Backup Manager For Plex v1.2.1";
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
                Height = 120,
                Dock = DockStyle.Fill
            };

            chkIncludeRegistry = new CheckBox { Text = "Include Registry Backup", Checked = true, AutoSize = true };
            chkIncludeFiles = new CheckBox { Text = "Include File Backup", Checked = true, AutoSize = true };
            chkIncludeLogs = new CheckBox { Text = "Include Previous Logs", Checked = false, AutoSize = true };
            chkStopPlex = new CheckBox { Text = "Stop Plex During Backup", Checked = true, AutoSize = true };

            optionsPanel.Controls.AddRange(new Control[] { chkIncludeRegistry, chkIncludeFiles, chkIncludeLogs, chkStopPlex });

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
            LogMessage("Backup Manager For Plex v1.2.1 - Ready for operation", Color.White);
        }

        private void PerformPlexBackup()
        {
            try
            {
                // Create root backup folder if it doesn't exist
                if (!Directory.Exists(rootBackup))
                {
                    Directory.CreateDirectory(rootBackup);
                }

                // Determine the day and date
                var weekday = DateTime.Now.DayOfWeek.ToString();
                var day = DateTime.Now.ToString("dd-MM-yyyy");

                // Create day folder path
                var weekdayDestination = Path.Combine(rootBackup, $"{weekday} {day}-Backup");

                // Create day folder if it doesn't exist
                if (!Directory.Exists(weekdayDestination))
                {
                    Directory.CreateDirectory(weekdayDestination);
                }

                // Set path for log backup folder
                var logDestination = Path.Combine(weekdayDestination, "Logs");

                // Create log folder if it doesn't exist
                if (!Directory.Exists(logDestination))
                {
                    Directory.CreateDirectory(logDestination);
                }

                // Registry backup
                BackupRegistry(weekdayDestination, weekday);

                // File backup
                BackupFiles(weekdayDestination, logDestination, weekday);

                // Show completion message
                MessageBox.Show(
                    $"Plex Backup has finished successfully, see logs in {logDestination}",
                    "Plex Backup",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error during backup: {ex.Message}",
                    "Plex Backup Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
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

                // Stop Plex Server
                StopPlexServer();

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

                // Start Plex Server
                StartPlexServer();
            }
            catch (Exception ex)
            {
                // Try to restart Plex Server even if backup failed
                try
                {
                    StartPlexServer();
                }
                catch { }

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
            }
            catch (Exception ex)
            {
                // Continue even if stopping Plex fails
                Console.WriteLine($"Warning: Failed to stop Plex Server: {ex.Message}");
            }
        }

        private void StartPlexServer()
        {
            try
            {
                if (File.Exists(PlexExecutablePath))
                {
                    Process.Start(PlexExecutablePath);
                }
                else
                {
                    throw new Exception($"Plex executable not found at: {PlexExecutablePath}");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to start Plex Server: {ex.Message}");
            }
        }

        public void GetConfiguration()
        {
            try
            {
                // Full path of the config file
                var configFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.ini");

                // If the config file does not exist, create it
                if (!File.Exists(configFile))
                {
                    CreateConfiguration(configFile);
                }
                else
                {
                    // If the config file already exists, execute the backup
                    LoadConfiguration(configFile);
                    PerformPlexBackup();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Configuration error: {ex.Message}",
                    "Plex Backup Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private void CreateConfiguration(string configFile)
        {
            MessageBox.Show(
                "Choose a folder where the backup will be saved",
                "Plex Backup",
                MessageBoxButtons.OK,
                MessageBoxIcon.Question);

            using (var browser = new FolderBrowserDialog())
            {
                var result = browser.ShowDialog();
                
                if (result != DialogResult.OK || string.IsNullOrEmpty(browser.SelectedPath))
                {
                    MessageBox.Show(
                        "You can always run the script to backup your Plex Server ;)",
                        "Plex Backup",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                    return;
                }

                var path = browser.SelectedPath;

                var confirmResult = MessageBox.Show(
                    $"Do you still want to backup to {path}",
                    "Plex Backup",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (confirmResult == DialogResult.Yes)
                {
                    try
                    {
                        File.WriteAllText(configFile, $"[Locations]\nBackupPath={path}");
                        
                        MessageBox.Show(
                            $"The config file has been created at [{configFile}].",
                            "Plex Backup",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Information);

                        rootBackup = path;
                        PerformPlexBackup();
                    }
                    catch (Exception ex)
                    {
                        throw new Exception($"Failed to create config file: {ex.Message}");
                    }
                }
            }
        }

        private void LoadConfiguration(string configFile)
        {
            try
            {
                var configLines = File.ReadAllLines(configFile);
                var config = new Dictionary<string, string>();

                foreach (var line in configLines)
                {
                    if (string.IsNullOrWhiteSpace(line) || line.StartsWith("["))
                        continue;

                    var parts = line.Split('=');
                    if (parts.Length == 2)
                    {
                        config[parts[0].Trim()] = parts[1].Trim();
                    }
                }

                if (config.ContainsKey("BackupPath"))
                {
                    rootBackup = config["BackupPath"];
                }
                else
                {
                    throw new Exception("BackupPath not found in config file");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to load configuration: {ex.Message}");
            }
        }
    }
}
