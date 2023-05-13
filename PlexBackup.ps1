######################################################################################

## Title: Plex Backup Script

## Purpose: Backs up Plex Database Files & Reg Keys

## Useage: Run as Scheduled Task

## Version: 1.2

## Last Updated: 2022-10-06

######################################################################################

### Variables to Change if necessary

$RootBackup = "D:\PlexBackups" ### Adjust this to the path of the backup storage

$PlexSvr = "C:\Program Files (x86)\Plex\Plex Media Server" ### Plex .exe location

######################################################################################

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

$PlexDataPath = "C:\Users\Pedro Buffon\AppData\Local\" ### Get Plex Appdata Path from Registry

$FileDestination = -join ($WEEKDAYDestination,"\FileBackup") ### Create Appdata Folder Backup Path

IF (!(Test-Path -Path $FileDestination)) { New-Item -Path $FileDestination -ItemType Directory -Force } ### If Appdata Backup Folder Doesn't Exist, Create it.

$Source = "$($PlexDataPath)\Plex Media Server" ### Set Source for Robocopy Command

$Exclude = "$($Source)\Cache" ### Exclude Cache Folder

Stop-Process -Name 'Plex Media Server' -Force -ErrorAction SilentlyContinue ### Stop Plex Server

Invoke-Command {Robocopy.exe $Source $FileDestination /Mir /R:1 /W:1 /XD $Exclude /log:$LogDestination\LogBackup-$WEEKDAY.txt} ### Perform a Mirror Style Backup, Excluing the Cache Directory.

$Plexsvr = -join ($PlexSvr,"\Plex Media Server.exe") ### Add .exe to Variable

Start-Process -FilePath $PlexSvr ### Start Plex Server

### Windows popup
# Add-Type -AssemblyName System.Windows.Forms
# $global:balloon = New-Object System.Windows.Forms.NotifyIcon
# $path = (Get-Process -id $pid).Path
# $balloon.Icon = [System.Drawing.Icon]::ExtractAssociatedIcon($path)
# $balloon.BalloonTipIcon = [System.Windows.Forms.ToolTipIcon]::Warning
# $balloon.BalloonTipText = 'Plex Backup has finished sucessfully'
# $balloon.BalloonTipTitle = "$Env:USERNAME"
# $balloon.Visible = $true
# $balloon.ShowBalloonTip(5000)

$wshell = New-Object -ComObject Wscript.Shell 
$Output = $wshell.Popup("Plex Backup has finished sucessfully")