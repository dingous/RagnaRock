[CmdletBinding()]
param(
    [string]$UnityPath = 'C:\Program Files\Unity\Hub\Editor\6000.3.12f1\Editor\Unity.exe',
    [switch]$Build
)
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
if (-not (Test-Path $UnityPath)) { throw "Unity executable not found: $UnityPath" }
$Root = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$Results = Join-Path $Root 'TestResults'
New-Item -ItemType Directory -Path $Results -Force | Out-Null
foreach ($mode in @('EditMode','PlayMode')) {
    $xml = Join-Path $Results "$mode.xml"
    if (Test-Path $xml) { Remove-Item $xml }
    $log = Join-Path $Results "$mode.log"
    # -quit is intentionally absent for -runTests; the Test Runner terminates after writing results.
    $arguments = @('-batchmode','-projectPath',('"'+$Root+'"'),'-runTests','-testPlatform',$mode,
        '-testResults',('"'+$xml+'"'),'-logFile',('"'+$log+'"'))
    $process = Start-Process -FilePath $UnityPath -ArgumentList $arguments -Wait -PassThru
    if ($process.ExitCode -ne 0 -or -not (Test-Path $xml)) { throw "$mode failed. Inspect $log" }
    [xml]$results = Get-Content $xml -Raw
    $run = $results.'test-run'
    if ($null -eq $run -or [int]$run.failed -gt 0 -or [int]$run.total -le 0 -or $run.result -ne 'Passed') {
        throw "$mode did not pass or no tests ran. Inspect $xml"
    }
    Write-Host "$mode passed: $($run.total) tests."
}
if ($Build) {
    $log = Join-Path $Results 'WindowsBuild.log'
    $arguments = @('-batchmode','-quit','-projectPath',('"'+$Root+'"'),'-executeMethod',
        'RagnaRock.Editor.BuildTools.BuildWindows','-logFile',('"'+$log+'"'))
    $process = Start-Process -FilePath $UnityPath -ArgumentList $arguments -Wait -PassThru
    if ($process.ExitCode -ne 0 -or -not (Test-Path (Join-Path $Root 'Builds/Windows/RagnaRock.exe'))) {
        throw "Windows build failed. Inspect $log"
    }
    Write-Host 'Windows build created. Manual gameplay, performance and release checks are still required.'
}
