# Backup Manager For Plex

> [!WARNING]
> This is app is still in development and bugs/errors can occur middle run.
> 
> Use it at your own risk.
>
> THIS APP IS DEVELOP TO BACKUP THE DATABASE AND NOT MEDIA FILES
> 
> MEDIA FILES ARE MANAGED BY THE USER AND THIS APP WILL NEVER TOUCH THOSE FILES

<div id="header" align="center">
  <img src="resources/icon.png" width="180"/>
</div>

A modern and complete Windows Forms application for backing up Plex Media Server database files and registry keys.

## 📋 Future Roadmap

- [X] Restore functionality
- [X] Backup compression
- [ ] Dark/light theme
- [ ] Complete scheduling implementation
- [ ] Retention Management
- [ ] Incremental backup
- [ ] Cloud backup (OneDrive, Google Drive)
- [ ] Linux/Mac Support

## 🚀 Features

### Complete Graphical Interface

- **Intuitive main window** with all options organized
- **Visual folder selector** to choose backup destination
- **Real-time log** with colors for different message types
- **Progress bar** during backup operations

### Advanced Backup Options

- ✅ **Registry Backup** - Exports Plex registry keys
- ✅ **File Backup** - Mirrors Plex files (excluding cache)
- ✅ **Include Previous Logs** - Optional to include old logs
- ✅ **Stop Plex During Backup** - Control over service stopping

### Extra Features

- 🗂️ **View Backups** - Lists all existing backups with details
- 📅 **Schedule Backup** - Interface to configure automatic backups
- 🔄 **Restore Backup** - Restore functionality
- ⚙️ **Advanced Settings** - Complete configuration management
  - Log level configuration (Debug, Info, Warning, Error)
  - UI preferences (minimize to tray)
  - Auto-start with Windows option
  - Backup statistics tracking

## 📋 Technical Features

- **Async/Await** for non-blocking operations
- **Robust error handling** with detailed logs
- **Persistent configuration** saved automatically
- **Responsive interface** that doesn't freeze during backups
- **Multiple paths** to find Plex executable

## 🛠️ Requirements

- .NET 9.0 or higher
- Windows (for Windows Forms and Windows-specific commands)
- Plex Media Server running on the same machine

## 📥 How to Build and Run

1. **Clone or download** the project
2. **Open PowerShell terminal** in the project folder or execute the .exe file
3. **Build**: `dotnet build`
4. **Run**: `dotnet run`

## 🎯 How to Use

### First Run

1. Run the application
2. Click **"Browse..."** to select backup folder
3. Configure desired backup options
4. Click **"Backup Now"** to perform the first backup

### Interface Features

#### 🎛️ Main Panel

- **Backup Path**: Path where backups will be saved
- **Backup Options**: Checkboxes to customize what to include
- **Keep Backups For**: Automatic retention configuration

#### 🔘 Action Buttons

- **Backup Now**: Execute immediate backup
- **Schedule Backup**: Schedule automatic backups
- **View Backups**: View existing backups
- **Restore**: Restore selected backup
- **Settings**: Advanced settings (in development)

#### 📊 Log and Status

- **Colored log** with real-time operations
- **Progress bar** during backups
- **Current status** of the application

## 📁 Backup Structure

```text
BackupPath/
├── Monday 22-07-2025-Backup/
│   ├── Logs/
│   │   └── LogBackup-Monday.txt
│   ├── RegBackup/
│   │   └── Regbackup-Monday.reg
│   └── FileBackup/
│       └── [Plex Media Server Files]
├── Tuesday 23-07-2025-Backup/
│   └── ...
```

## 🔧 Configuration

The application automatically saves configuration in JSON format:

### JSON Configuration

- **Modern structured format** with enhanced data organization
- **Complete settings** including statistics and advanced options
- **Automatic backup** of all configuration settings
- **File**: `config.json`

### Configuration Features

- Selected backup path and all UI options
- **Backup statistics** (last backup date, total backups created)
- **Advanced settings** (custom Plex paths, log levels, UI preferences)
- **System tray settings** (minimize behavior, notifications)
- **Automatic creation** when no configuration exists

### Sample JSON Configuration

```json
{
  "backupPath": "C:\\Backups\\Plex",
  "includeRegistry": true,
  "includeFiles": true,
  "includeLogs": false,
  "stopPlex": true,
  "retentionDays": 2,
  "lastBackup": "2025-07-22T14:30:00",
  "totalBackupsCreated": 15,
  "customPlexPaths": [
    "C:\\Program Files\\Plex\\Plex Media Server\\Plex Media Server.exe"
  ],
  "minimizeToTray": false,
  "showNotifications": true,
  "autoStartWithWindows": false,
  "logLevel": "Info",
  "hasShownTrayNotification": false
}
```

```text
BackupPath/
├── Monday 22-07-2025-Backup/
│   ├── Logs/
│   │   └── LogBackup-Monday.txt
│   ├── RegBackup/
│   │   └── Regbackup-Monday.reg
│   └── FileBackup/
│       └── [Plex Files]
```

## Configuration for Scheduled Task

To use as a scheduled task, compile the project and use the generated .exe file:
`PlexBackup.exe`

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Disclaimer

This is an independent third-party tool. See [DISCLAIMER](DISCLAIMER.md) for important legal information.
