######################################################################################

## Title: Plex Backup Script

## Author: Pedro Buffon

## Purpose: Backs up Plex Database Files & Reg Keys

## Useage: Run as Scheduled Task

## Version: 2.0

## Last Updated: 13/05/2023

######################################################################################

Add-Type -AssemblyName System.Windows.Forms
[System.Windows.Forms.Application]::EnableVisualStyles();

function PlexBackup {

    IF (!(Test-Path -Path $RootBackup)) { New-Item -Path $RootBackup -ItemType Directory -Force } ### Create Folder if it doesn't exist

    $WEEKDAY = (Get-date).DayOfWeek ### Determine the Day

    $DAY = Get-Date
    $DAY = $DAY.ToString("dd-MM-yyyy") ### Determine the date that the backup was created

    $WEEKDAYDestination = -join($RootBackup,"\","$WEEKDAY " + $DAY,"-Backup") ### Create Day Folder Path

    IF (!(Test-Path -Path $WEEKDAYDestination)) { New-Item -Path $WEEKDAYDestination -ItemType Directory -Force } ### Create Day Folder if it doesn't exist

    $LogDestination = -join ($WEEKDAYDestination,"\Logs") ### Set Path for Log Backup Folder

    IF (!(Test-Path -Path $LogDestination)) { New-Item -Path $LogDestination -Force -ItemType Directory} ### Create Log Folder if it doesn't exist

    $PlexReg = "HKEY_CURRENT_USER\Software\Plex, Inc." ### Variable for Registry Path

    $RegDestination = -join ($WEEKDAYDestination,"\RegBackup") ### Set Path for Registry Backup Folder

    IF (!(Test-Path -Path $RegDestination)) { New-Item -Path $RegDestination -Force -ItemType Directory} ### Create Registry Backup Folder if it doesn't exist

    $RegDestination = -join ($RegDestination,"\Regbackup-",$WEEKDAY,".reg") ### Set Path for Backup .Reg File

    IF ((Test-Path -Path $RegDestination)) { Remove-Item -Path $RegDestination -Force } ### If a previous backup exists, delete it.

    Invoke-Command { Reg Export $($PlexReg.Replace(":","")) $RegDestination } ### Backup The Registry Key

    $FileDestination = -join ($WEEKDAYDestination,"\FileBackup") ### Create Appdata Folder Backup Path

    IF (!(Test-Path -Path $FileDestination)) { New-Item -Path $FileDestination -ItemType Directory -Force } ### If Appdata Backup Folder Doesn't Exist, Create it.

    $Source = "$($env:LOCALAPPDATA)\Plex Media Server" ### Set Source for Robocopy Command

    $Exclude = "$($Source)\Cache" ### Exclude Cache Folder

    Stop-Process -Name 'Plex Media Server' -Force -ErrorAction SilentlyContinue ### Stop Plex Server

    Robocopy.exe $Source $FileDestination /Mir /R:1 /W:1 /XD $Exclude /log:$LogDestination\LogBackup-$WEEKDAY.txt ### Perform a Mirror Style Backup, Excluing the Cache Directory.

    $Plexsvr = -join ("C:\Program Files (x86)\Plex\Plex Media Server","\Plex Media Server.exe") ### Add .exe to Variable

    Start-Process -FilePath $PlexSvr ### Start Plex Server

    [System.Windows.Forms.MessageBox]::Show("Plex Backup has finished sucessfully, see logs in $LogDestination ", "Plex Backup", [System.Windows.Forms.MessageBoxButtons]::Ok, [System.Windows.Forms.MessageBoxIcon]::Question)
}

function  getConfiguration {
    # Full path of the file
    $file = "$PSScriptRoot\config.ini"

    #If the config file does not exist, create it.
    if (-not(Test-Path -Path $file -PathType Leaf)) {
        try {

            [System.Windows.Forms.MessageBox]::Show("Choose a folder where the backup will be saved", "Plex Backup", [System.Windows.Forms.MessageBoxButtons]::OK, [System.Windows.Forms.MessageBoxIcon]::Question)
            $browser = New-Object System.Windows.Forms.FolderBrowserDialog
            $null = $browser.ShowDialog()
            $path = $browser.SelectedPath
        
            if (!$path){
                [System.Windows.Forms.MessageBox]::Show("You can always run the script to backup your Plex Server ;)", "Plex Backup", [System.Windows.Forms.MessageBoxButtons]::OK, [System.Windows.Forms.MessageBoxIcon]::Question)
                exit
            }
        
            $result = [System.Windows.Forms.MessageBox]::Show("Do you still want to backup to $path", "Plex Backup", [System.Windows.Forms.MessageBoxButtons]::YesNo, [System.Windows.Forms.MessageBoxIcon]::Question)
        
            if ($result -eq "Yes") {
                $null = New-Item -ItemType File -Path $file -Force -ErrorAction Stop
                echo > $file "[Locations]
BackupPath=""$path"""
                [System.Windows.Forms.MessageBox]::Show("The config file has been created on [$file].", "Plex Backup", [System.Windows.Forms.MessageBoxButtons]::OK, [System.Windows.Forms.MessageBoxIcon]::Question)
            }
        }
        catch {
            throw $_.Exception.Message
        }
    }
    # If the config file already exists, execute the backup instead of asking for the path
    else {
        Get-Content "config.ini" | foreach-object -begin {$h=@{}} -process { $k = [regex]::split($_,'='); if(($k[0].CompareTo("") -ne 0) -and ($k[0].StartsWith("[") -ne $True)) { $h.Add($k[0], $k[1]) } }
        $RootBackup = $h.Get_Item("BackupPath")
        PlexBackup
    }
}

getConfiguration