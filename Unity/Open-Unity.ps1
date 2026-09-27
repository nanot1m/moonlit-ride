param([string]$Editor = 'C:\Program Files\Unity\Hub\Editor\6000.3.17f1\Editor\Unity.exe')
$ErrorActionPreference = 'Stop'
if (!(Test-Path -LiteralPath $Editor)) { throw "Unity 6000.3.17f1 not found. Pass -Editor with its Unity.exe path." }
Start-Process -FilePath $Editor -ArgumentList @('-projectPath', ('"' + $PSScriptRoot + '"'))
