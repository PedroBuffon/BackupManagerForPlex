/*
 * Backup Manager For Plex - Forms
 * Copyright (c) 2025 Pedro Buffon
 * Licensed under MIT License - see LICENSE file for details
 * 
 * This software is not affiliated with Plex, Inc.
 */

using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Reflection;

namespace PlexBackupApp
{
    // Restore Complete Result Window
    public partial class RestoreCompleteForm : Form
    {
        private readonly string restoreMessage;
        private readonly string fullLogContent;
        private readonly bool isSuccess;
        
        public RestoreCompleteForm(string message, string logContent, bool success)
        {
            restoreMessage = message;
            fullLogContent = logContent;
            isSuccess = success;
            InitializeRestoreCompleteForm();
        }
        
        private void InitializeRestoreCompleteForm()
        {
            // Form properties
            Text = isSuccess ? "Restore Complete" : "Restore Failed";
            Size = new Size(500, 300);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;

            // Apply consistent theme with main window (system default)
            BackColor = SystemColors.Control;
            ForeColor = SystemColors.ControlText;

            // Message label
            var lblMessage = new Label
            {
                Text = restoreMessage,
                Location = new Point(20, 20),
                Size = new Size(440, 150),
                Font = new Font("Segoe UI", 9F),
                AutoSize = false,
                ForeColor = SystemColors.ControlText,
                BackColor = SystemColors.Control
            };

            // Show Logs button
            var btnShowLogs = new Button
            {
                Text = "Show Detailed Logs",
                Location = new Point(20, 190),
                Size = new Size(150, 30),
                Font = new Font("Segoe UI", 9F),
                UseVisualStyleBackColor = true
            };
            btnShowLogs.Click += BtnShowLogs_Click;

            // OK button
            var btnOK = new Button
            {
                Text = "OK",
                Location = new Point(390, 190),
                Size = new Size(70, 30),
                Font = new Font("Segoe UI", 9F),
                DialogResult = DialogResult.OK,
                UseVisualStyleBackColor = true
            };

            Controls.AddRange(new Control[] { lblMessage, btnShowLogs, btnOK });
            
            AcceptButton = btnOK;
        }

        private void BtnShowLogs_Click(object sender, EventArgs e)
        {
            var logViewerForm = new RestoreLogViewerForm(fullLogContent);
            logViewerForm.ShowDialog(this);
        }
    }

    // Restore Log Viewer Window
    public partial class RestoreLogViewerForm : Form
    {
        public RestoreLogViewerForm(string logContent)
        {
            InitializeLogViewerForm(logContent);
        }

        private void InitializeLogViewerForm(string logContent)
        {
            // Form properties
            Text = "Restore Process Logs";
            Size = new Size(800, 600);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.Sizable;
            MinimumSize = new Size(600, 400);

            // Apply consistent theme with main window (system default)
            BackColor = SystemColors.Control;
            ForeColor = SystemColors.ControlText;

            // Log text box
            var txtLogs = new RichTextBox
            {
                Location = new Point(10, 10),
                Size = new Size(760, 500),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                ReadOnly = true,
                Font = new Font("Consolas", 9F),
                BackColor = SystemColors.Window,
                ForeColor = SystemColors.WindowText,
                Text = logContent,
                WordWrap = true,
                BorderStyle = BorderStyle.Fixed3D
            };

            // Format the log content with colors
            FormatLogContent(txtLogs);

            // Copy Logs button
            var btnCopyLogs = new Button
            {
                Text = "Copy to Clipboard",
                Location = new Point(10, 520),
                Size = new Size(120, 30),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left,
                Font = new Font("Segoe UI", 9F),
                UseVisualStyleBackColor = true
            };
            btnCopyLogs.Click += (s, e) => {
                try
                {
                    if (!string.IsNullOrEmpty(logContent))
                    {
                        Clipboard.SetText(logContent);
                        MessageBox.Show("Logs copied to clipboard!", "Copy Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to copy logs: {ex.Message}", "Copy Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };

            // Close button
            var btnClose = new Button
            {
                Text = "Close",
                Location = new Point(690, 520),
                Size = new Size(80, 30),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
                Font = new Font("Segoe UI", 9F),
                DialogResult = DialogResult.OK,
                UseVisualStyleBackColor = true
            };

            Controls.AddRange(new Control[] { txtLogs, btnCopyLogs, btnClose });
            
            AcceptButton = btnClose;
        }

        private void FormatLogContent(RichTextBox txtLogs)
        {
            if (string.IsNullOrEmpty(txtLogs.Text)) return;

            var lines = txtLogs.Text.Split('\n');
            txtLogs.Clear();

            foreach (var line in lines)
            {
                Color textColor = SystemColors.WindowText;
                FontStyle fontStyle = FontStyle.Regular;

                // Apply colors based on log content
                if (line.Contains("[ERROR]") || line.Contains("ERROR:") || line.Contains("Failed"))
                {
                    textColor = Color.Red;
                    fontStyle = FontStyle.Bold;
                }
                else if (line.Contains("[WARNING]") || line.Contains("WARNING:") || line.Contains("Warning"))
                {
                    textColor = Color.Orange;
                }
                else if (line.Contains("[SUCCESS]") || line.Contains("SUCCESS:") || line.Contains("completed successfully"))
                {
                    textColor = Color.Green;
                }
                else if (line.Contains("[INFO]") || line.Contains("Starting") || line.Contains("Completed"))
                {
                    textColor = Color.Blue;
                }

                txtLogs.SelectionColor = textColor;
                txtLogs.SelectionFont = new Font("Consolas", 9F, fontStyle);
                txtLogs.AppendText(line + "\n");
            }

            // Scroll to top
            txtLogs.SelectionStart = 0;
            txtLogs.ScrollToCaret();
        }
    }

    // License Agreement Form
    public partial class LicenseAgreementForm : Form
    {
        private RichTextBox txtLicense;
        private Button btnAccept;
        private Button btnDecline;
        private CheckBox chkAgree;
        
        public bool LicenseAccepted { get; private set; } = false;
        
        public LicenseAgreementForm()
        {
            InitializeLicenseForm();
        }
        
        private void InitializeLicenseForm()
        {
            // Form properties
            Text = "License Agreement - Backup Manager For Plex";
            Size = new Size(700, 600);
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            BackColor = SystemColors.Control;
            ForeColor = SystemColors.ControlText;
            
            // Main container
            var mainPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 4,
                Padding = new Padding(20)
            };
            
            // Row styles
            mainPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Title
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F)); // License text
            mainPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Checkbox
            mainPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Buttons
            
            // Title label
            var lblTitle = new Label
            {
                Text = "SOFTWARE LICENSE AGREEMENT",
                Font = new Font("Arial", 14F, FontStyle.Bold),
                ForeColor = SystemColors.ControlText,
                AutoSize = true,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                TextAlign = ContentAlignment.MiddleCenter,
                Margin = new Padding(0, 0, 0, 20)
            };
            
            // License text box
            txtLicense = new RichTextBox
            {
                ReadOnly = true,
                BackColor = SystemColors.Window,
                ForeColor = SystemColors.WindowText,
                BorderStyle = BorderStyle.Fixed3D,
                Dock = DockStyle.Fill,
                Font = new Font("Consolas", 9F),
                Margin = new Padding(0, 0, 0, 20)
            };
            
            // Load license text
            LoadLicenseText();
            
            // Agreement checkbox
            chkAgree = new CheckBox
            {
                Text = "I have read and agree to the terms of this license agreement",
                Font = new Font("Arial", 10F),
                ForeColor = SystemColors.ControlText,
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 20)
            };
            chkAgree.CheckedChanged += ChkAgree_CheckedChanged;
            
            // Button panel
            var buttonPanel = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.RightToLeft,
                AutoSize = true,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            
            // Decline button
            btnDecline = new Button
            {
                Text = "Decline",
                Size = new Size(100, 35),
                FlatStyle = FlatStyle.System,
                DialogResult = DialogResult.Cancel,
                Margin = new Padding(10, 0, 0, 0)
            };
            btnDecline.Click += BtnDecline_Click;
            
            // Accept button
            btnAccept = new Button
            {
                Text = "Accept",
                Size = new Size(100, 35),
                FlatStyle = FlatStyle.System,
                Enabled = false,
                DialogResult = DialogResult.OK,
                Margin = new Padding(10, 0, 0, 0)
            };
            btnAccept.Click += BtnAccept_Click;
            
            // Add buttons to panel
            buttonPanel.Controls.Add(btnDecline);
            buttonPanel.Controls.Add(btnAccept);
            
            // Add controls to main panel
            mainPanel.Controls.Add(lblTitle, 0, 0);
            mainPanel.Controls.Add(txtLicense, 0, 1);
            mainPanel.Controls.Add(chkAgree, 0, 2);
            mainPanel.Controls.Add(buttonPanel, 0, 3);
            
            Controls.Add(mainPanel);
        }
        
        private void LoadLicenseText()
        {
            try
            {
                string licensePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "LICENSE");
                if (File.Exists(licensePath))
                {
                    string licenseContent = File.ReadAllText(licensePath);
                    txtLicense.Text = licenseContent;
                }
                else
                {
                    // Fallback license text
                    txtLicense.Text = @"MIT License

Copyright (c) 2025 Pedro Buffon

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the ""Backup Manager For Plex""), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED ""AS IS"", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.

===============================================================================

DISCLAIMER:

This software is an independent third-party tool and is not affiliated with, 
endorsed by, or sponsored by Plex, Inc. 

""Plex"" and ""Plex Media Server"" are trademarks of Plex, Inc. This software 
interacts with Plex Media Server installations through standard file system 
operations and publicly available interfaces, but does not redistribute, 
modify, or reverse engineer any Plex, Inc. proprietary software.

Users are responsible for ensuring their use of this software complies with 
Plex, Inc.'s Terms of Service and any applicable licensing agreements.

The authors of this software assume no responsibility for any data loss, 
system damage, or violations of third-party terms of service that may result 
from the use of this software.

USE AT YOUR OWN RISK.";
                }
            }
            catch (Exception ex)
            {
                txtLicense.Text = "Error loading license file: " + ex.Message + "\n\nPlease contact the developer for license information.";
            }
        }
        
        private void ChkAgree_CheckedChanged(object sender, EventArgs e)
        {
            btnAccept.Enabled = chkAgree.Checked;
        }
        
        private void BtnAccept_Click(object sender, EventArgs e)
        {
            LicenseAccepted = true;
            Close();
        }
        
        private void BtnDecline_Click(object sender, EventArgs e)
        {
            LicenseAccepted = false;
            Close();
        }
    }

    // Schedule Backup Form (placeholder implementation)
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
}
