# Plex Backup Manager v3.0 (GUI Version)

A modern and complete Windows Forms application for backing up Plex Media Server database files and registry keys.

## 🚀 New Features v3.0

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

### Retention Management

- **Automatic cleanup** of old backups
- **Retention options**: 7, 14, 30, 60 days or never delete
- **Automatic space saving**

### Extra Features

- 🗂️ **View Backups** - Lists all existing backups with details
- 📅 **Schedule Backup** - Interface to configure automatic backups
- 🔄 **Restore Backup** - Restore functionality (in development)
- ⚙️ **Advanced Settings** - Complete configuration management
  - Custom Plex executable paths
  - Log level configuration (Debug, Info, Warning, Error)
  - UI preferences (minimize to tray, notifications)
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
- Plex Media Server installed

## 📥 How to Build and Run

1. **Clone or download** the project
2. **Open PowerShell terminal** in the project folder
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
- **Schedule Backup**: Schedule automatic backups (in development)
- **View Backups**: View existing backups
- **Restore**: Restore selected backup (in development)
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

## 🔧 Automatic Configuration

The application automatically saves configuration in two formats:

### JSON Configuration (Preferred)
- **Modern format** with structured data
- **Enhanced settings** including statistics and advanced options
- **Automatic backup** of configuration settings
- **File**: `config.json`

### INI Configuration (Legacy Support)
- **Backward compatibility** with older versions
- **Simple key-value** format
- **File**: `config.ini`

### Configuration Features
- Selected backup path and all UI options
- **Backup statistics** (last backup date, total backups created)
- **Advanced settings** (custom Plex paths, log levels, UI preferences)
- **Automatic migration** from INI to JSON format
- **Dual format support** for maximum compatibility

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
  "logLevel": "Info"
}
```

## 🎨 Interface Improvements

### Visual

- **Organized colors** by operation type
- **Responsive layout** with TableLayoutPanel
- **Icons and colors** on buttons for easy identification
- **Terminal-style log** with black background and green text

### Usability

- **Validations** before executing operations
- **Clear messages** for errors and success
- **Non-blocking operations** with async/await
- **Automatic saving** of configurations

## 🚀 Version Evolution

| Version | Description |
|---------|-------------|
| v1.0 | Original PowerShell script |
| v2.0 | Conversion to C# console |
| v3.0 | **Complete graphical interface with advanced features** |

## 📋 Future Roadmap

- [ ] Complete scheduling implementation
- [ ] Automatic restore functionality  
- [ ] Incremental backup
- [ ] Backup compression
- [ ] Cloud backup (OneDrive, Google Drive)
- [ ] Email notifications
- [ ] Dark/light theme

This version represents a significant leap in functionality and usability compared to previous versions!

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

## Differences from PowerShell Version

- Better error handling and exceptions
- Object-oriented structure
- Strong typing
- Better performance
- Easier to maintain and debug

## Configuration for Scheduled Task

To use as a scheduled task, compile the project and use the generated .exe file:
`PlexBackup.exe`
