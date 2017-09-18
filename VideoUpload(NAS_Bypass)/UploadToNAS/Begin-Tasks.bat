@echo off
START "Post Production" /W "PowerShell.exe" -NoProfile -ExecutionPolicy Bypass -File NovaCam-Post-Production-Start.ps1
START "NAS Upload" /W "PowerShell.exe" -ExecutionPolicy Bypass -File Mursion-Upload-to-NAS.ps1
echo Post Production and Video Upload Complete!
PAUSE