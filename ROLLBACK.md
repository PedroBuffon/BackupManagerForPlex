# Backup Manager For Plex - Rollback Feature

## Overview

The Backup Manager For Plex includes an advanced rollback system that automatically restores the system to its previous state if a backup operation fails. This ensures that your Plex Media Server remains in a consistent state even when backup operations encounter errors.

## How It Works

### Automatic State Tracking

The rollback system automatically tracks:

- **Plex Server State**: Whether Plex was running or stopped before the backup began
- **Created Files**: All files created during the backup process
- **Created Directories**: All directories created during the backup process  
- **Modified Files**: Backup copies of any files that were modified during backup
- **Temporary Files**: Location of rollback temporary files

### Rollback Process

When a backup fails, the system automatically:

1. **Restores Plex Service State**
   - If Plex was running before backup, it will be restarted
   - If Plex was stopped before backup, it will remain stopped

2. **Restores Modified Files**
   - Any files that were overwritten during backup are restored from backup copies
   - Original file states are preserved in temporary rollback directory

3. **Cleans Up Created Files**
   - Removes any files that were created during the failed backup
   - Prevents partial backup files from remaining on the system

4. **Removes Created Directories**
   - Removes any empty directories that were created during backup
   - Maintains clean directory structure

5. **Cleanup**
   - Removes temporary rollback files
   - Logs rollback completion status

## Configuration Options

### Enable/Disable Rollback

- **Location**: Main interface checkbox "Enable Automatic Rollback on Failure"
- **Default**: Enabled
- **Description**: Master switch to enable or disable the rollback feature

### Maximum Rollback Attempts

- **Location**: Advanced Settings → Rollback Settings
- **Default**: 3 attempts
- **Range**: 1-10 attempts
- **Description**: Number of times the system will attempt rollback operations if they fail

## Rollback Scenarios

### Registry Backup Failure

- Restores any registry files that were overwritten
- Removes incomplete registry backup files
- Maintains registry consistency

### File Backup Failure

- Restores Plex data directory to original state
- Removes incomplete file backups
- Ensures Plex database integrity

### Plex Service Issues

- Restores Plex Media Server to original running state
- Handles cases where Plex fails to stop or start during backup

## Safety Features

### Non-Destructive Operations

- Rollback never deletes existing user data
- Only removes files that were created during the failed backup
- Preserves all original Plex configurations and databases

### Logging

- All rollback operations are logged with timestamps
- Success/failure status for each rollback step
- Warning messages for partial rollback completion

### Error Handling

- Rollback continues even if individual steps fail
- Reports partial completion status
- Provides guidance for manual intervention when needed

## Best Practices

### When to Enable Rollback

- **Recommended**: Always enabled for production systems
- **Critical**: When running automated/scheduled backups
- **Essential**: For systems with complex Plex configurations

### When to Disable Rollback

- **Testing environments**: When testing backup procedures
- **Manual oversight**: When you want to manually handle failures
- **Storage constraints**: If temporary disk space is extremely limited

### Monitoring

- Review backup logs regularly for rollback activities
- Investigate frequent rollback occurrences
- Ensure adequate disk space for temporary rollback files

## Troubleshooting

### Rollback Fails

If rollback itself fails:

1. Check log messages for specific error details
2. Verify file system permissions
3. Ensure adequate disk space
4. Manually restore Plex service state if needed

### Partial Rollback

If rollback completes with warnings:

1. Review log for specific failed operations
2. Manually verify Plex Media Server functionality
3. Check for leftover temporary files
4. Consider manual cleanup if necessary

### Prevention

- Ensure backup destination has adequate space
- Verify Plex Media Server is accessible before backup
- Run backups during low-usage periods
- Regularly test backup and restore procedures

## Technical Details

### Temporary Files Location

- Windows: `%TEMP%\PlexBackupRollback_[timestamp]`
- Created at backup start, cleaned up at completion
- Contains backup copies of modified files

### Rollback Validation

- Checks directory emptiness before removal
- Verifies file existence before restoration
- Validates Plex service state before changes

### Performance Impact

- Minimal overhead during normal backup operations
- Temporary disk space usage for file backups
- Quick rollback execution (typically under 30 seconds)

## Version History

- **v3.0**: Initial rollback implementation
- Added automatic state tracking
- Implemented comprehensive rollback procedures
- Added configuration options for rollback behavior
