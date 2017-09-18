<#
	Mursion Upload latest recordings to NAS for backup

	The Video Stitching program (Post Production) must be schedule to run overnight from Windows Task Scheduler. 
	When that is complete, it will call this script to upload them to the NAS.
	
	To upload only the most recent videos, we'll look at the novacam-queue.csv file.
	Any entries that have completed Post Production (stitching), but have not begun updating, will have their video folder copied to the NAS.
	This includes the Raw, cut, and stitched videos.
	
	To execute:
	PowerShell.exe -NoProfile -ExecutionPolicy Bypass "& '[the unity build dir]\NovaCam\Mursion-Upload-to-NAS.ps1'"	
#>

#Change This variable to match location
$NASDirectory = "M:/Recording_Backup/"



#This script must be located in the NovaCam folder
$NovaCamRoot = Split-Path -Parent $PSScriptRoot

# Import Helpers Functions: logging, registry, window
. "$($NovaCamRoot)\NovaCam\modules\mursion\Mursion-Utilities.ps1"

#Current Date to be used for file name
$s = Get-Date -format s 
$date = $s -replace '-', '_'

# Used to store all data related to this session in novacam-queue.csv
$csvRow = {}

#Location that recordings are stored
$srcDir = $env:USERPROFILE + "/Simulation_Recordings/"
$NASDir = $NASDirectory + $date.Substring(0,10) + "/"

#Misc variables
$d = Get-Date -format D

#Each day this script is run, it will create a new log file.
#In the case that it is run multiple times in a day, it will add to the same log file.
$logPath = $NovaCamRoot + "\NovaCam\logs\NASLogs\naslog_" + $date.Substring(0,10) + ".log"

$uploadStatusPath = $NASDirectory

$stationNum

function Main {

	#Create the NASLogs folder if it does not exist
	if(!(Test-Path -Path ($NovaCamRoot + "\NovaCam\logs\NASLogs\"))){
		New-Item -ItemType directory -Path ($NovaCamRoot + "\NovaCam\logs\NASLogs\")
	}
	
	#Create TimeStamped folder for today if it hasn't already been created
	if(!(Test-Path -Path ($uploadStatusPath + $date.Substring(0,10) + "/"))){
		New-Item -ItemType directory -Path ($uploadStatusPath + $date.Substring(0,10) + "/")
	}
	
	#Create TimeStamped folder for today if it hasn't already been created
	if(!(Test-Path -Path ($srcDir + "stationNum.txt"))){
		echo "This Station has not yet been assigned a number."
		$stationNum = Read-Host -Prompt 'Station Number (int only)'
		Add-Content ($srcDir + "stationNum.txt") ($stationNum)
	}else{
		$stationNum = Get-Content ($srcDir + "stationNum.txt")
	}
	
	Add-Content ($uploadStatusPath + $date.Substring(0,10) + "\UploadStatus" + $stationNum + ".txt") ("NASUploadComplete:false")
	
	$formattedDate = $date.Substring(0,10)
	
	echo "Starting Upload to NAS"
	echo "Date: $formattedDate"
	echo " "			
    NASLog "---------------------------------------"
    NASLog " Starting Upload to NAS"
    NASLog "---------------------------------------"
    NASLog " File Name: naslog_$formattedDate.log"
	NASLog " Start Time: $d"
    NASLog "---------------------------------------"
    NASLog " "
	
	#Parse through the CSV file. Each time we call NovaCam-Queue-Get-Next, we get the most recent session that has completed 
	#Post Production and has not started uploading. From here, each is uploaded until NovaCam-Queue-Get-Next returns $false (none left)
	$csvRow = NovaCam-Queue-Get-Next 
	while ($csvRow -ne $false){
		NASUpload-Started
		
		$info = $csvRow.SessionName
		NASLog "** Uploading: $info"		
		
		robocopy ($srcdir + $csvRow.SessionName + '/') ($NASDir + $csvRow.SessionName + '/') *.mp4 /MT:32 /NDL /NP /NJS /NJH /TEE /LOG+:$logPath
		if ($lastexitcode -eq 0) {}
		elseif ($lastexitcode -le 7) {
			echo "$info uploaded"
			}
		else {echo "**** Error! Please verify that all videos have been uploaded to the NAS! ****"}
		NASLog "- Complete!"
		NASLog " "
		NASUpload-Ended
		$csvRow = NovaCam-Queue-Get-Next
	}
	Clear-Content ($uploadStatusPath + $date.Substring(0,10) + "\UploadStatus" + $stationNum + ".txt")
	Add-Content ($uploadStatusPath + $date.Substring(0,10) + "\UploadStatus" + $stationNum + ".txt") ("NASUploadComplete:true")
	
    PAUSE
}

# Append line to naslog_mm-dd-yyyy.log file
function NAS-Write-Log {
	$message = $args[0]
	$newLine = "`n"
	
	Add-Content $logPath ("$message" + $newLine)
}
function NASLog {
	$message = $args[0]
	NAS-Write-Log $message
}

#Update the CSV entry with the start time
function NASUpload-Started {
    # Add PostProductionEndedAt ts to novacam-queue.csv
    $ts = [Math]::Floor([decimal](Get-Date(Get-Date).ToUniversalTime()-uformat "%s"))
    NovaCam-Queue-Update $csvRow.SessionName $csvRow.fileName $csvRow.RecordingStartedAt "UploadStartedAt" $ts
}

#Update the CSV entry with the end time
function NASUpload-Ended {
    # Add PostProductionEndedAt ts to novacam-queue.csv
    $ts = [Math]::Floor([decimal](Get-Date(Get-Date).ToUniversalTime()-uformat "%s"))
    NovaCam-Queue-Update $csvRow.SessionName $csvRow.fileName $csvRow.RecordingStartedAt "UploadEndedAt" $ts
}

# Return a Row in NovaCam Queue CSV file
# returns the most recently added row that has a complete Post Production (value -ne 0) and an incomplete upload (-eq 0)
function NovaCam-Queue-Get-Next {
	$csvFile = $srcDir + "\novacam-queue.csv"

	#NASLog "Retrieving Completed session from csvFile: $csvFile"

	# args
	$col = "PostProductionEndedAt"
	$col2 = "UploadEndedAt"
	$csv = Import-Csv $csvFile -Delimiter ','

	# If one row (not an array) make array
    if ($csv.GetType().BaseType.Name -ne "Array") {
        $csv = $csv, ""
    }
	
	$rows = $csv.Where({$PSItem.$col -ne '0' -and $PSItem.$col2 -eq '0'}) | Sort-Object -Property @{Expression = "RecordingStartedAt"; Descending = $true}

	#NASLog "novacam-queue.csv SELECTED ROW(S):"
	#NASLog $rows
	
	# Return the first (latest) row
	if ($rows -is [system.array]) {
		return $rows[0]
	} else {
		if ($rows.SessionName -ne $null) { 
			return $rows
		} else {
			return $false
		}
	}
}

Main