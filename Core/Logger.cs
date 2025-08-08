/*
 * Backup Manager For Plex - Logger
 * Copyright (c) 2025 Pedro Buffon
 * Licensed under MIT License - see LICENSE file for details
 * 
 * This software is not affiliated with Plex, Inc.
 */

using System;
using System.Collections.Generic;
using System.Linq;

namespace PlexBackupApp
{
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
}
