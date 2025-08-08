/*
 * Backup Manager For Plex - Models
 * Copyright (c) 2025 Pedro Buffon
 * Licensed under MIT License - see LICENSE file for details
 * 
 * This software is not affiliated with Plex, Inc.
 */

using System;
using System.Collections.Generic;

namespace PlexBackupApp
{
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
        public bool MinimizeToTray { get; set; } = false;
        public bool ShowNotifications { get; set; } = true;
        public bool AutoStartWithWindows { get; set; } = false;
        public string LogLevel { get; set; } = "Info"; // Debug, Info, Warning, Error
        public bool HasShownTrayNotification { get; set; } = false;
        public bool HasAcceptedLicense { get; set; } = false;
        public bool EnableRollback { get; set; } = true;
        public int MaxRollbackAttempts { get; set; } = 3;
        public string CompressionType { get; set; } = "None"; // None, Zip, 7z, Gzip
        public int CompressionLevel { get; set; } = 6; // 1-9 for compression level
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

    // Helper class for backup data
    public class BackupInfo
    {
        public string BackupName { get; set; } = "";
        public string DateCreated { get; set; } = "";
        public string Size { get; set; } = "";
        public string FullPath { get; set; } = "";
        public bool IsCompressed { get; set; } = false;
    }

    // SSH Connection configuration
    public class SSHConnectionConfig
    {
        public string HostIP { get; set; } = "";
        public int Port { get; set; } = 22;
        public string Username { get; set; } = "";
        public string Password { get; set; } = "";
        public string PrivateKeyPath { get; set; } = "";
        public bool UsePrivateKey { get; set; } = false;
        public string PlexDataPath { get; set; } = "/var/lib/plexmediaserver/Library/Application Support/Plex Media Server";
        public string TempRestorePath { get; set; } = "/tmp/plex_restore";
        public bool StopPlexService { get; set; } = true;
        public string PlexServiceName { get; set; } = "plexmediaserver";
    }
}
