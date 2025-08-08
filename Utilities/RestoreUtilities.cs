using System;
using System.IO;
using System.IO.Compression;
using System.Linq;

namespace PlexBackupApp
{
    // Validation and safety methods for restore operations
    public static class RestoreUtilities
    {
        public static bool ValidateBackupIntegrity(string backupPath)
        {
            try
            {
                // Check if the path exists as a file (ZIP) or directory
                bool isZipFile = File.Exists(backupPath) && Path.GetExtension(backupPath).ToLower() == ".zip";
                bool isDirectory = Directory.Exists(backupPath);
                
                if (!isZipFile && !isDirectory)
                    return false;
                
                // Check if it's a ZIP file (compressed backup)
                if (isZipFile)
                {
                    return ValidateZipBackupIntegrity(backupPath);
                }
                
                // Handle directory backup (uncompressed backup)
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

        private static bool ValidateZipBackupIntegrity(string zipPath)
        {
            try
            {
                using (var archive = ZipFile.OpenRead(zipPath))
                {
                    // Basic validation: ZIP file should have entries and be readable
                    if (archive.Entries.Count == 0)
                        return false;
                    
                    // Check for backup-related content (more flexible approach)
                    var hasBackupContent = archive.Entries.Any(e => 
                        e.FullName.Contains("RegBackup") || 
                        e.FullName.Contains("FileBackup") ||
                        e.FullName.Contains("Preferences.xml") ||
                        e.FullName.Contains("library.db") ||
                        e.FullName.EndsWith(".reg"));
                    
                    return hasBackupContent;
                }
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
                long backupSize;
                
                // Check if it's a ZIP file (compressed backup)
                if (File.Exists(backupPath) && Path.GetExtension(backupPath).ToLower() == ".zip")
                {
                    // For ZIP files, we need to estimate uncompressed size
                    // Use ZIP file size * 3 as a conservative estimate for uncompressed size
                    backupSize = new FileInfo(backupPath).Length * 3;
                }
                else if (Directory.Exists(backupPath))
                {
                    // Calculate backup size for directory
                    backupSize = GetDirectorySize(new DirectoryInfo(backupPath));
                }
                else
                {
                    return false; // Path doesn't exist
                }
                
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
}
