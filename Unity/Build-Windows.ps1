param([string]$Editor = 'C:\Program Files\Unity\Hub\Editor\6000.3.17f1\Editor\Unity.exe')
$ErrorActionPreference = 'Stop'
if (!(Test-Path -LiteralPath $Editor)) { throw "Unity 6000.3.17f1 not found. Pass -Editor with its Unity.exe path." }
$buildLog = Join-Path $PSScriptRoot 'Builds/build.log'
New-Item -ItemType Directory -Force (Split-Path $buildLog) | Out-Null
$buildProcess = Start-Process -FilePath $Editor -ArgumentList @('-batchmode', '-projectPath', ('"' + $PSScriptRoot + '"'), '-executeMethod', 'MoonlitRide.Editor.ProjectSetup.BuildWindows', '-quit', '-logFile', ('"' + $buildLog + '"')) -WindowStyle Hidden -PassThru -Wait
if ($buildProcess.ExitCode -ne 0) { throw "Unity build failed ($($buildProcess.ExitCode)). See $buildLog" }
Write-Host "Built: $PSScriptRoot\Builds\Windows\MoonlitRide.exe"
