[CmdletBinding()]
param(
    [ValidateSet('EditMode', 'PlayMode')][string]$Mode = 'EditMode',
    [string]$UnityEditor = $env:UNITY_EDITOR_PATH,
    [string]$Filter,
    [ValidateRange(1, 120)][int]$TimeoutMinutes = 20
)
$ErrorActionPreference = 'Stop'
$project = Split-Path $PSScriptRoot -Parent
$process = $null
try {
    $versionText = Get-Content (Join-Path $project 'ProjectSettings/ProjectVersion.txt') -Raw
    $version = [regex]::Match($versionText, '(?m)^m_EditorVersion: (\S+)').Groups[1].Value
    if (!$version) { throw 'Cannot read the project Editor version.' }
    if (!$UnityEditor) {
        $UnityEditor = Join-Path $env:ProgramFiles "Unity/Hub/Editor/$version/Editor/Unity.exe"
    }
    if (!(Test-Path -LiteralPath $UnityEditor -PathType Leaf)) {
        throw "Unity $version not found. Pass -UnityEditor or set UNITY_EDITOR_PATH to Editor/Unity.exe."
    }
    $UnityEditor = (Resolve-Path -LiteralPath $UnityEditor).Path
    $installedVersion = (Get-Item -LiteralPath $UnityEditor).VersionInfo.ProductVersion
    if ($installedVersion -notlike "$version*") {
        throw "Editor version '$installedVersion' does not match project version '$version'."
    }
    $lockPath = Join-Path $project 'Temp/UnityLockfile'
    if (Test-Path -LiteralPath $lockPath) {
        try {
            $lock = [IO.File]::Open($lockPath, 'Open', 'ReadWrite', 'None')
            $lock.Dispose()
        } catch { throw 'Project is open in Unity. Close the Editor before running batch tests.' }
    }
    $runId = (Get-Date -Format 'yyyyMMdd-HHmmss') + '-' + [guid]::NewGuid().ToString('N').Substring(0, 8)
    $artifacts = Join-Path $project "Logs/Tests/$runId/$Mode"
    New-Item -ItemType Directory -Path $artifacts -Force | Out-Null
    $resultPath = Join-Path $artifacts 'results.xml'
    $logPath = Join-Path $artifacts 'editor.log'
    $arguments = @('-batchmode', '-runTests', '-projectPath', $project,
        '-testPlatform', $Mode, '-testResults', $resultPath, '-logFile', $logPath)
    if ($Filter) { $arguments += @('-testFilter', $Filter) }
    foreach ($argument in $arguments) {
        if ($argument.Contains('"')) { throw 'Arguments cannot contain double quotes.' }
    }
    $commandLine = ($arguments | ForEach-Object { '"' + $_ + '"' }) -join ' '
    Write-Host "Running $Mode tests with Unity $version. Artifacts: $artifacts"
    $process = Start-Process -FilePath $UnityEditor -ArgumentList $commandLine -PassThru -WindowStyle Hidden
    if (!$process.WaitForExit($TimeoutMinutes * 60000)) {
        Stop-Process -Id $process.Id -Force -ErrorAction SilentlyContinue
        throw "Test run exceeded $TimeoutMinutes minutes. See $logPath"
    }
    $process.Refresh()
    $editorExitCode = $process.ExitCode
    if (!(Test-Path -LiteralPath $resultPath)) {
        throw "Unity produced no test report (exit $editorExitCode). See $logPath"
    }
    $report = New-Object System.Xml.XmlDocument
    $report.XmlResolver = $null
    $report.Load($resultPath)
    $run = $report.SelectSingleNode('/test-run')
    if (!$run -or !$run.HasAttribute('total') -or !$run.HasAttribute('failed')) {
        throw "Invalid NUnit report: $resultPath"
    }
    $total = [int]$run.GetAttribute('total')
    $passed = [int]$run.GetAttribute('passed')
    $failed = [int]$run.GetAttribute('failed')
    $summary = [ordered]@{
        mode = $Mode; editorVersion = $version; editorExitCode = $editorExitCode
        result = $run.GetAttribute('result'); total = $total; passed = $passed
        failed = $failed; skipped = $run.GetAttribute('skipped')
        report = $resultPath; log = $logPath
    }
    $summary | ConvertTo-Json | Set-Content (Join-Path $artifacts 'summary.json') -Encoding UTF8
    Write-Host "$Mode : total=$total passed=$passed failed=$failed result=$($summary.result)"
    foreach ($test in $report.SelectNodes('//test-case[@result="Failed"]')) {
        Write-Host "FAILED: $($test.fullname)"
        Write-Host $test.failure.message.InnerText
    }
    if ($editorExitCode -ne 0 -or $total -eq 0 -or $passed -eq 0 -or $failed -gt 0 -or $summary.result -ne 'Passed') {
        throw "Test run did not pass. See $resultPath and $logPath"
    }
    exit 0
} catch {
    Write-Error $_ -ErrorAction Continue
    exit 1
} finally {
    if ($process) { $process.Dispose() }
}
