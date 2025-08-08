/*
 * Backup Manager For Plex - Linux SSH Restore
 * Copyright (c) 2025 Pedro Buffon
 * Licensed under MIT License - see LICENSE file for details
 * 
 * This software is not affiliated with Plex, Inc.
 */

using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Diagnostics;
using System.Windows.Forms;
using System.Drawing;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using Renci.SshNet;
using Renci.SshNet.Common;

namespace PlexBackupApp
{
    // SSH Restore Progress Form
    public class LinuxSSHRestoreForm : Form
    {
        private TextBox txtHostIP;
        private NumericUpDown nudPort;
        private TextBox txtUsername;
        private TextBox txtPassword;
        private TextBox txtPrivateKeyPath;
        private CheckBox chkUsePrivateKey;
        private TextBox txtPlexDataPath;
        private TextBox txtTempRestorePath;
        private CheckBox chkStopPlexService;
        private TextBox txtPlexServiceName;
        private Button btnBrowsePrivateKey;
        private Button btnTestConnection;
        private Button btnStartRestore;
        private Button btnCancel;
        private RichTextBox rtbProgress;
        private ProgressBar progressBar;
        
        private BackupInfo backupInfo;
        private SSHConnectionConfig sshConfig;
        private bool isRestoreInProgress = false;

        public LinuxSSHRestoreForm(BackupInfo backup)
        {
            backupInfo = backup;
            sshConfig = new SSHConnectionConfig();
            InitializeForm();
        }

        private void InitializeForm()
        {
            Text = $"Linux SSH Restore - {backupInfo.BackupName}";
            Size = new Size(600, 700);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;

            var mainPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(10),
                ColumnCount = 3,
                RowCount = 15
            };

            // Set column styles
            mainPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150));
            mainPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 70));
            mainPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30));

            int row = 0;

            // Title
            var lblTitle = new Label
            {
                Text = "Configure Linux SSH Connection",
                Font = new Font("Arial", 12F, FontStyle.Bold),
                AutoSize = true,
                Anchor = AnchorStyles.Left
            };
            mainPanel.Controls.Add(lblTitle, 0, row);
            mainPanel.SetColumnSpan(lblTitle, 3);
            row++;

            // Host IP
            mainPanel.Controls.Add(new Label { Text = "Host IP:", AutoSize = true, Anchor = AnchorStyles.Left }, 0, row);
            txtHostIP = new TextBox { Dock = DockStyle.Fill, Text = "192.168.1.100" };
            mainPanel.Controls.Add(txtHostIP, 1, row);
            mainPanel.SetColumnSpan(txtHostIP, 2);
            row++;

            // Port
            mainPanel.Controls.Add(new Label { Text = "Port:", AutoSize = true, Anchor = AnchorStyles.Left }, 0, row);
            nudPort = new NumericUpDown { Dock = DockStyle.Fill, Minimum = 1, Maximum = 65535, Value = 22 };
            mainPanel.Controls.Add(nudPort, 1, row);
            row++;

            // Username
            mainPanel.Controls.Add(new Label { Text = "Username:", AutoSize = true, Anchor = AnchorStyles.Left }, 0, row);
            txtUsername = new TextBox { Dock = DockStyle.Fill, Text = "plex" };
            mainPanel.Controls.Add(txtUsername, 1, row);
            mainPanel.SetColumnSpan(txtUsername, 2);
            row++;

            // Authentication method checkbox
            chkUsePrivateKey = new CheckBox 
            { 
                Text = "Use Private Key Authentication", 
                AutoSize = true, 
                Anchor = AnchorStyles.Left 
            };
            chkUsePrivateKey.CheckedChanged += ChkUsePrivateKey_CheckedChanged;
            mainPanel.Controls.Add(chkUsePrivateKey, 0, row);
            mainPanel.SetColumnSpan(chkUsePrivateKey, 3);
            row++;

            // Password
            mainPanel.Controls.Add(new Label { Text = "Password:", AutoSize = true, Anchor = AnchorStyles.Left }, 0, row);
            txtPassword = new TextBox { Dock = DockStyle.Fill, UseSystemPasswordChar = true };
            mainPanel.Controls.Add(txtPassword, 1, row);
            mainPanel.SetColumnSpan(txtPassword, 2);
            row++;

            // Private Key Path
            mainPanel.Controls.Add(new Label { Text = "Private Key:", AutoSize = true, Anchor = AnchorStyles.Left }, 0, row);
            txtPrivateKeyPath = new TextBox { Dock = DockStyle.Fill, Enabled = false };
            mainPanel.Controls.Add(txtPrivateKeyPath, 1, row);
            btnBrowsePrivateKey = new Button { Text = "Browse", Enabled = false };
            btnBrowsePrivateKey.Click += BtnBrowsePrivateKey_Click;
            mainPanel.Controls.Add(btnBrowsePrivateKey, 2, row);
            row++;

            // Separator
            var separator1 = new Label 
            { 
                Text = "Plex Server Configuration", 
                Font = new Font("Arial", 10F, FontStyle.Bold), 
                AutoSize = true, 
                Anchor = AnchorStyles.Left 
            };
            mainPanel.Controls.Add(separator1, 0, row);
            mainPanel.SetColumnSpan(separator1, 3);
            row++;

            // Plex Data Path
            mainPanel.Controls.Add(new Label { Text = "Plex Data Path:", AutoSize = true, Anchor = AnchorStyles.Left }, 0, row);
            txtPlexDataPath = new TextBox 
            { 
                Dock = DockStyle.Fill, 
                Text = "/var/lib/plexmediaserver/Library/Application Support/Plex Media Server" 
            };
            mainPanel.Controls.Add(txtPlexDataPath, 1, row);
            mainPanel.SetColumnSpan(txtPlexDataPath, 2);
            row++;

            // Temp Restore Path
            mainPanel.Controls.Add(new Label { Text = "Temp Path:", AutoSize = true, Anchor = AnchorStyles.Left }, 0, row);
            txtTempRestorePath = new TextBox { Dock = DockStyle.Fill, Text = "/tmp/plex_restore" };
            mainPanel.Controls.Add(txtTempRestorePath, 1, row);
            mainPanel.SetColumnSpan(txtTempRestorePath, 2);
            row++;

            // Stop Plex Service
            chkStopPlexService = new CheckBox 
            { 
                Text = "Stop Plex Service During Restore", 
                AutoSize = true, 
                Anchor = AnchorStyles.Left,
                Checked = true
            };
            mainPanel.Controls.Add(chkStopPlexService, 0, row);
            mainPanel.SetColumnSpan(chkStopPlexService, 3);
            row++;

            // Plex Service Name
            mainPanel.Controls.Add(new Label { Text = "Service Name:", AutoSize = true, Anchor = AnchorStyles.Left }, 0, row);
            txtPlexServiceName = new TextBox { Dock = DockStyle.Fill, Text = "plexmediaserver" };
            mainPanel.Controls.Add(txtPlexServiceName, 1, row);
            mainPanel.SetColumnSpan(txtPlexServiceName, 2);
            row++;

            // Buttons
            var buttonPanel = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.RightToLeft,
                Height = 35,
                Dock = DockStyle.Fill
            };

            btnCancel = new Button { Text = "Cancel", Width = 80, DialogResult = DialogResult.Cancel };
            btnStartRestore = new Button { Text = "Start Restore", Width = 100, Enabled = false };
            btnTestConnection = new Button { Text = "Test Connection", Width = 120 };

            btnTestConnection.Click += BtnTestConnection_Click;
            btnStartRestore.Click += BtnStartRestore_Click;

            buttonPanel.Controls.AddRange(new Control[] { btnCancel, btnStartRestore, btnTestConnection });

            mainPanel.Controls.Add(buttonPanel, 0, row);
            mainPanel.SetColumnSpan(buttonPanel, 3);
            row++;

            // Progress section
            var lblProgress = new Label 
            { 
                Text = "Progress:", 
                Font = new Font("Arial", 10F, FontStyle.Bold), 
                AutoSize = true, 
                Anchor = AnchorStyles.Left 
            };
            mainPanel.Controls.Add(lblProgress, 0, row);
            mainPanel.SetColumnSpan(lblProgress, 3);
            row++;

            progressBar = new ProgressBar { Dock = DockStyle.Fill, Style = ProgressBarStyle.Continuous };
            mainPanel.Controls.Add(progressBar, 0, row);
            mainPanel.SetColumnSpan(progressBar, 3);
            row++;

            // Progress log
            rtbProgress = new RichTextBox
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                BackColor = Color.Black,
                ForeColor = Color.LightGreen,
                Font = new Font("Consolas", 9F),
                Height = 150
            };
            mainPanel.Controls.Add(rtbProgress, 0, row);
            mainPanel.SetColumnSpan(rtbProgress, 3);
            mainPanel.SetRowSpan(rtbProgress, 2);

            // Set row styles
            for (int i = 0; i < mainPanel.RowCount - 2; i++)
            {
                mainPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            }
            mainPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Progress bar
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); // Log textbox

            Controls.Add(mainPanel);
        }

        private void ChkUsePrivateKey_CheckedChanged(object sender, EventArgs e)
        {
            bool useKey = chkUsePrivateKey.Checked;
            txtPrivateKeyPath.Enabled = useKey;
            btnBrowsePrivateKey.Enabled = useKey;
            txtPassword.Enabled = !useKey;
        }

        private void BtnBrowsePrivateKey_Click(object sender, EventArgs e)
        {
            using (var openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Title = "Select Private Key File";
                openFileDialog.Filter = "All Files (*.*)|*.*|PEM Files (*.pem)|*.pem|Key Files (*.key)|*.key";
                openFileDialog.FilterIndex = 1;

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    txtPrivateKeyPath.Text = openFileDialog.FileName;
                }
            }
        }

        private async void BtnTestConnection_Click(object sender, EventArgs e)
        {
            if (!ValidateSSHConfig())
                return;

            btnTestConnection.Enabled = false;
            LogProgress("Testing SSH connection...", Color.Yellow);

            try
            {
                UpdateConfigFromUI();
                bool connectionSuccess = await TestSSHConnection();
                
                if (connectionSuccess)
                {
                    LogProgress("SSH connection successful!", Color.LightGreen);
                    btnStartRestore.Enabled = true;
                }
                else
                {
                    LogProgress("SSH connection failed!", Color.Red);
                }
            }
            catch (Exception ex)
            {
                LogProgress($"Connection test error: {ex.Message}", Color.Red);
            }
            finally
            {
                btnTestConnection.Enabled = true;
            }
        }

        private async void BtnStartRestore_Click(object sender, EventArgs e)
        {
            if (!ValidateSSHConfig() || isRestoreInProgress)
                return;

            var result = MessageBox.Show(
                $"This will restore the backup '{backupInfo.BackupName}' to the Linux server:\n\n" +
                $"Host: {txtHostIP.Text}:{nudPort.Value}\n" +
                $"Target Path: {txtPlexDataPath.Text}\n\n" +
                "WARNING: This will overwrite existing Plex data on the Linux server!\n" +
                "Make sure you have a backup of the current Linux Plex installation.\n\n" +
                "Continue with restore?",
                "Confirm Linux SSH Restore",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (result != DialogResult.Yes)
                return;

            isRestoreInProgress = true;
            btnStartRestore.Enabled = false;
            btnTestConnection.Enabled = false;
            progressBar.Value = 0;
            
            LogProgress("Starting Linux SSH restore operation...", Color.Cyan);

            try
            {
                UpdateConfigFromUI();
                await PerformSSHRestore();
                LogProgress("Restore completed successfully!", Color.LightGreen);
                progressBar.Value = 100;
                
                MessageBox.Show("Restore completed successfully!", "Restore Complete", 
                              MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                LogProgress($"Restore failed: {ex.Message}", Color.Red);
                MessageBox.Show($"Restore failed: {ex.Message}", "Restore Error", 
                              MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                isRestoreInProgress = false;
                btnStartRestore.Enabled = true;
                btnTestConnection.Enabled = true;
            }
        }

        private bool ValidateSSHConfig()
        {
            if (string.IsNullOrWhiteSpace(txtHostIP.Text))
            {
                MessageBox.Show("Please enter the host IP address.", "Validation Error", 
                              MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtHostIP.Focus();
                return false;
            }

            if (string.IsNullOrWhiteSpace(txtUsername.Text))
            {
                MessageBox.Show("Please enter the username.", "Validation Error", 
                              MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtUsername.Focus();
                return false;
            }

            if (chkUsePrivateKey.Checked)
            {
                if (string.IsNullOrWhiteSpace(txtPrivateKeyPath.Text) || !File.Exists(txtPrivateKeyPath.Text))
                {
                    MessageBox.Show("Please select a valid private key file.", "Validation Error", 
                                  MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtPrivateKeyPath.Focus();
                    return false;
                }
            }
            else
            {
                if (string.IsNullOrWhiteSpace(txtPassword.Text))
                {
                    MessageBox.Show("Please enter the password.", "Validation Error", 
                                  MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtPassword.Focus();
                    return false;
                }
            }

            if (string.IsNullOrWhiteSpace(txtPlexDataPath.Text))
            {
                MessageBox.Show("Please enter the Plex data path.", "Validation Error", 
                              MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtPlexDataPath.Focus();
                return false;
            }

            return true;
        }

        private void UpdateConfigFromUI()
        {
            sshConfig.HostIP = txtHostIP.Text.Trim();
            sshConfig.Port = (int)nudPort.Value;
            sshConfig.Username = txtUsername.Text.Trim();
            sshConfig.Password = txtPassword.Text;
            sshConfig.PrivateKeyPath = txtPrivateKeyPath.Text.Trim();
            sshConfig.UsePrivateKey = chkUsePrivateKey.Checked;
            sshConfig.PlexDataPath = txtPlexDataPath.Text.Trim();
            sshConfig.TempRestorePath = txtTempRestorePath.Text.Trim();
            sshConfig.StopPlexService = chkStopPlexService.Checked;
            sshConfig.PlexServiceName = txtPlexServiceName.Text.Trim();
        }

        private async Task<bool> TestSSHConnection()
        {
            return await Task.Run(() =>
            {
                try
                {
                    // Test SSH connection using SSH.NET
                    string errorMessage;
                    bool success = SSHRestoreUtilities.TestSSHConnection(sshConfig, out errorMessage);
                    
                    if (!success)
                    {
                        LogProgress($"SSH test failed: {errorMessage}", Color.Red);
                    }
                    
                    return success;
                }
                catch (Exception ex)
                {
                    LogProgress($"SSH test exception: {ex.Message}", Color.Red);
                    return false;
                }
            });
        }

        private async Task PerformSSHRestore()
        {
            await Task.Run(() =>
            {
                try
                {
                    // Step 1: Prepare backup for transfer
                    LogProgress("Step 1/6: Preparing backup for transfer...", Color.Yellow);
                    string localBackupPath = PrepareBackupForTransfer();
                    progressBar.Invoke((Action)(() => progressBar.Value = 15));

                    // Step 2: Create temp directory on remote server
                    LogProgress("Step 2/6: Creating temporary directory on remote server...", Color.Yellow);
                    CreateRemoteTempDirectory();
                    progressBar.Invoke((Action)(() => progressBar.Value = 25));

                    // Step 3: Transfer backup to remote server
                    LogProgress("Step 3/6: Transferring backup to remote server...", Color.Yellow);
                    TransferBackupToRemote(localBackupPath);
                    progressBar.Invoke((Action)(() => progressBar.Value = 50));

                    // Step 4: Stop Plex service (if requested)
                    if (sshConfig.StopPlexService)
                    {
                        LogProgress("Step 4/6: Stopping Plex service on remote server...", Color.Yellow);
                        StopRemotePlexService();
                    }
                    else
                    {
                        LogProgress("Step 4/6: Skipping Plex service stop (user preference)...", Color.Yellow);
                    }
                    progressBar.Invoke((Action)(() => progressBar.Value = 65));

                    // Step 5: Extract and restore backup
                    LogProgress("Step 5/6: Extracting and restoring backup on remote server...", Color.Yellow);
                    ExtractAndRestoreRemoteBackup();
                    progressBar.Invoke((Action)(() => progressBar.Value = 85));

                    // Step 6: Start Plex service and cleanup
                    if (sshConfig.StopPlexService)
                    {
                        LogProgress("Step 6/6: Starting Plex service and cleaning up...", Color.Yellow);
                        StartRemotePlexService();
                    }
                    else
                    {
                        LogProgress("Step 6/6: Cleaning up temporary files...", Color.Yellow);
                    }
                    CleanupRemoteTemp();
                    progressBar.Invoke((Action)(() => progressBar.Value = 100));

                    LogProgress("Restore operation completed successfully!", Color.LightGreen);
                }
                catch (Exception ex)
                {
                    LogProgress($"Restore operation failed: {ex.Message}", Color.Red);
                    throw;
                }
            });
        }

        private string PrepareBackupForTransfer()
        {
            string sourceBackupPath = backupInfo.FullPath;
            
            if (backupInfo.IsCompressed)
            {
                // Backup is already compressed, use as-is
                LogProgress($"Backup is already compressed: {Path.GetFileName(sourceBackupPath)}", Color.Cyan);
                return sourceBackupPath;
            }
            else
            {
                // Need to compress the directory backup for transfer
                string tempZipPath = Path.Combine(Path.GetTempPath(), $"plex_backup_transfer_{DateTime.Now:yyyyMMdd_HHmmss}.zip");
                LogProgress($"Compressing directory backup to: {Path.GetFileName(tempZipPath)}", Color.Cyan);
                
                try
                {
                    ZipFile.CreateFromDirectory(sourceBackupPath, tempZipPath);
                    LogProgress("Backup compression completed", Color.LightGreen);
                    return tempZipPath;
                }
                catch (Exception ex)
                {
                    throw new Exception($"Failed to compress backup: {ex.Message}");
                }
            }
        }

        private void CreateRemoteTempDirectory()
        {
            string command = $"mkdir -p {sshConfig.TempRestorePath}";
            ExecuteRemoteCommand(command, "Failed to create temporary directory");
            LogProgress($"Created temporary directory: {sshConfig.TempRestorePath}", Color.LightGreen);
        }

        private string DetectPlexServiceName()
        {
            string[] possibleServices = { "plexmediaserver", "plex", "pms" };
            
            foreach (string serviceName in possibleServices)
            {
                try
                {
                    ExecuteRemoteCommand($"systemctl is-active {serviceName} || systemctl is-enabled {serviceName}", "Service check", allowFailure: true);
                    return serviceName; // If no exception, this service exists
                }
                catch
                {
                    continue; // Try next service name
                }
            }
            
            // Fallback to configured service name
            return sshConfig.PlexServiceName;
        }

        private void StopRemotePlexService()
        {
            string serviceName = DetectPlexServiceName();
            string command = $"sudo systemctl stop {serviceName}";
            ExecuteRemoteCommand(command, "Failed to stop Plex service", allowFailure: true);
            
            // Wait a moment for service to stop
            System.Threading.Thread.Sleep(3000);
            LogProgress("Plex service stopped", Color.LightGreen);
        }

        private void StartRemotePlexService()
        {
            string serviceName = DetectPlexServiceName();
            string command = $"sudo systemctl start {serviceName}";
            ExecuteRemoteCommand(command, "Failed to start Plex service", allowFailure: true);
            
            LogProgress("Plex service started", Color.LightGreen);
        }

        private void ExtractAndRestoreRemoteBackup()
        {
            string tempBackupPath = $"{sshConfig.TempRestorePath}/backup.zip";
            string extractPath = $"{sshConfig.TempRestorePath}/extracted";
            
            // Create extraction directory
            ExecuteRemoteCommand($"mkdir -p {extractPath}", "Failed to create extraction directory");
            
            // Extract backup
            ExecuteRemoteCommand($"cd {extractPath} && unzip -q {tempBackupPath}", "Failed to extract backup");
            LogProgress("Backup extracted successfully", Color.LightGreen);
            
            // Create backup of current Plex data (if it exists)
            string backupTimestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string currentBackupPath = $"{sshConfig.TempRestorePath}/current_backup_{backupTimestamp}";
            ExecuteRemoteCommand($"mkdir -p {currentBackupPath}", "Failed to create current backup directory");
            
            // Check if Plex data path exists before backing up
            ExecuteRemoteCommand($"test -d \"{sshConfig.PlexDataPath}\" && cp -r \"{sshConfig.PlexDataPath}\" {currentBackupPath}/ || echo 'No existing Plex data to backup'", "Failed to backup current Plex data", allowFailure: true);
            LogProgress("Current Plex data backed up", Color.LightGreen);
            
            // Create Plex data directory if it doesn't exist, then clear it
            ExecuteRemoteCommand($"sudo mkdir -p \"{sshConfig.PlexDataPath}\"", "Failed to create Plex data directory", allowFailure: true);
            ExecuteRemoteCommand($"sudo rm -rf \"{sshConfig.PlexDataPath}\"/*", "Failed to clear current Plex data", allowFailure: true);
            LogProgress("Current Plex data cleared", Color.Yellow);
            
            // Copy restored data
            // Find the actual Plex data directory in the extracted backup
            string copyCommand = $"find {extractPath} -type d -name 'Plex Media Server' -exec cp -r {{}}/* \"{sshConfig.PlexDataPath}/\" \\;";
            ExecuteRemoteCommand(copyCommand, "Failed to restore Plex data");
            LogProgress("Plex data restored successfully", Color.LightGreen);
            
            // Set proper permissions - try to detect the correct user
            // First try common Plex users
            string[] plexUsers = { "plex", "plexmediaserver", "pms" };
            bool permissionsSet = false;
            
            foreach (string user in plexUsers)
            {
                ExecuteRemoteCommand($"id {user}", "User check", allowFailure: true);
                ExecuteRemoteCommand($"sudo chown -R {user}:{user} \"{sshConfig.PlexDataPath}\"", $"Failed to set permissions for {user}", allowFailure: true);
                permissionsSet = true;
                break; // If we get here without exception, it worked
            }
            
            if (!permissionsSet)
            {
                // Fallback to current user
                ExecuteRemoteCommand($"sudo chown -R $USER:$USER \"{sshConfig.PlexDataPath}\"", "Failed to set fallback permissions", allowFailure: true);
            }
            
            LogProgress("Permissions updated", Color.LightGreen);
        }

        private void CleanupRemoteTemp()
        {
            ExecuteRemoteCommand($"rm -rf {sshConfig.TempRestorePath}", "Failed to cleanup temporary files", allowFailure: true);
            LogProgress("Temporary files cleaned up", Color.LightGreen);
        }

        private ConnectionInfo CreateConnectionInfo()
        {
            AuthenticationMethod authMethod;
            
            if (sshConfig.UsePrivateKey && !string.IsNullOrEmpty(sshConfig.PrivateKeyPath))
            {
                var keyFile = new PrivateKeyFile(sshConfig.PrivateKeyPath, sshConfig.Password);
                authMethod = new PrivateKeyAuthenticationMethod(sshConfig.Username, keyFile);
            }
            else
            {
                authMethod = new PasswordAuthenticationMethod(sshConfig.Username, sshConfig.Password);
            }

            return new ConnectionInfo(sshConfig.HostIP, sshConfig.Port, sshConfig.Username, authMethod);
        }

        private void TransferBackupToRemote(string localBackupPath)
        {
            string remoteBackupPath = $"{sshConfig.TempRestorePath}/backup.zip";
            
            try
            {
                var connectionInfo = CreateConnectionInfo();
                
                using (var scpClient = new ScpClient(connectionInfo))
                {
                    scpClient.Connect();
                    
                    LogProgress("Starting backup file transfer...", Color.Yellow);
                    
                    var fileInfo = new FileInfo(localBackupPath);
                    long totalBytes = fileInfo.Length;
                    long uploadedBytes = 0;
                    
                    scpClient.Uploading += (sender, e) =>
                    {
                        uploadedBytes = e.Uploaded;
                        int progressPercent = (int)((uploadedBytes * 100) / totalBytes);
                        
                        // Update progress on UI thread
                        if (progressBar.InvokeRequired)
                        {
                            progressBar.Invoke((Action)(() => {
                                progressBar.Value = Math.Min(progressPercent, 100);
                            }));
                        }
                    };
                    
                    scpClient.Upload(fileInfo, remoteBackupPath);
                    scpClient.Disconnect();
                }
                
                LogProgress("Backup transfer completed successfully", Color.LightGreen);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to transfer backup: {ex.Message}");
            }
        }

        private void ExecuteRemoteCommand(string command, string errorMessage, bool allowFailure = false)
        {
            try
            {
                var connectionInfo = CreateConnectionInfo();
                
                using (var sshClient = new SshClient(connectionInfo))
                {
                    sshClient.Connect();
                    
                    // Handle sudo commands by providing password via echo
                    if (command.StartsWith("sudo ") && !string.IsNullOrEmpty(sshConfig.Password))
                    {
                        command = $"echo '{sshConfig.Password}' | sudo -S {command.Substring(5)}";
                    }
                    
                    using (var cmd = sshClient.CreateCommand(command))
                    {
                        cmd.CommandTimeout = TimeSpan.FromMinutes(1);
                        string result = cmd.Execute();
                        
                        if (cmd.ExitStatus != 0 && !allowFailure)
                        {
                            string error = cmd.Error;
                            throw new Exception($"{errorMessage}: {error}");
                        }
                        
                        if (!string.IsNullOrEmpty(result))
                        {
                            LogProgress($"Command output: {result.Trim()}", Color.LightBlue);
                        }
                        
                        if (!string.IsNullOrEmpty(cmd.Error) && allowFailure)
                        {
                            LogProgress($"Command warning: {cmd.Error.Trim()}", Color.Orange);
                        }
                    }
                    
                    sshClient.Disconnect();
                }
            }
            catch (Exception ex)
            {
                if (!allowFailure)
                {
                    throw new Exception($"{errorMessage}: {ex.Message}");
                }
                else
                {
                    LogProgress($"Warning - {errorMessage}: {ex.Message}", Color.Orange);
                }
            }
        }

        private void LogProgress(string message, Color color)
        {
            if (rtbProgress.InvokeRequired)
            {
                rtbProgress.Invoke((Action)(() => LogProgress(message, color)));
                return;
            }

            rtbProgress.SelectionStart = rtbProgress.TextLength;
            rtbProgress.SelectionLength = 0;
            rtbProgress.SelectionColor = color;
            rtbProgress.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}\n");
            rtbProgress.ScrollToCaret();
        }
    }

    // SSH Restore Utilities

}
