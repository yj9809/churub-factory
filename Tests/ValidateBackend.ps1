param([string]$Artifacts = 'C:\Workspace\backend-refactor-artifacts')
$ErrorActionPreference = 'Stop'
$project = Split-Path $PSScriptRoot -Parent
New-Item -ItemType Directory -Force $Artifacts | Out-Null
$targets = @'
<Project>
 <Target Name="IncludeBackendRefactorSources" BeforeTargets="BeforeCompile">
  <PropertyGroup Condition="'$(MSBuildProjectName)' == 'Assembly-CSharp' and '$(ValidateAndroid)' == 'true'">
   <DefineConstants>$([System.Text.RegularExpressions.Regex]::Replace('$(DefineConstants)', 'UNITY_EDITOR[^;]*;?', ''))</DefineConstants>
  </PropertyGroup>
  <ItemGroup Condition="'$(MSBuildProjectName)' == 'Churub.Core'">
   <Compile Include="$(MSBuildProjectDirectory)/Assets/1. Scripts/Core/BalanceTable.cs" />
   <Compile Include="$(MSBuildProjectDirectory)/Assets/1. Scripts/Core/ServerOperation.cs" />
  </ItemGroup>
  <ItemGroup Condition="'$(MSBuildProjectName)' == 'Assembly-CSharp'">
   <Compile Include="$(MSBuildProjectDirectory)/Assets/1. Scripts/System/BackendService.cs" />
   <Compile Include="$(MSBuildProjectDirectory)/Assets/1. Scripts/System/GameDataRepository.cs" />
   <Compile Include="$(MSBuildProjectDirectory)/Assets/1. Scripts/System/StartupCoordinator.cs" />
  </ItemGroup>
 </Target>
</Project>
'@
$targetPath = Join-Path $Artifacts 'validation.targets'
[IO.File]::WriteAllText($targetPath, $targets)
Push-Location $project
try {
    & dotnet build Assembly-CSharp.csproj --no-restore -v:q "/p:CustomAfterMicrosoftCommonTargets=$targetPath" *> (Join-Path $Artifacts 'compile.log')
    if ($LASTEXITCODE -ne 0) { throw 'Game compilation failed; see compile.log' }
    [xml]$game = Get-Content Assembly-CSharp.csproj
    [xml]$coreTests = Get-Content Churub.Core.Tests.csproj
    $references = @(
        (Join-Path $project 'Temp/Bin/Debug/Assembly-CSharp/Assembly-CSharp.dll'),
        (Join-Path $project 'Temp/Bin/Debug/Churub.Core/Churub.Core.dll')
    )
    foreach ($name in 'Backend', 'LitJSON', 'UnityEngine', 'UnityEngine.CoreModule') {
        $reference = $game.Project.ItemGroup.Reference | Where-Object Include -eq $name
        if (!$reference) { throw "Missing assembly reference $name" }
        $references += $reference.HintPath
    }
    $references += ($coreTests.Project.ItemGroup.Reference | Where-Object Include -eq 'nunit.framework').HintPath
    $items = foreach ($reference in $references) {
        $escaped = [Security.SecurityElement]::Escape($reference)
        '<Reference Include="' + [IO.Path]::GetFileNameWithoutExtension($reference) + '"><HintPath>' + $escaped + '</HintPath></Reference>'
    }
    $source = [Security.SecurityElement]::Escape((Join-Path $PSScriptRoot 'BackendRefactorValidation.cs'))
    $existing = [Security.SecurityElement]::Escape((Join-Path $project 'Assets/Tests/EditMode/*.cs'))
    $harness = '<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net10.0</TargetFramework><EnableDefaultCompileItems>false</EnableDefaultCompileItems></PropertyGroup><ItemGroup>' +
        ($items -join "`n") + '<Compile Include="' + $source + '"/><Compile Include="' + $existing + '"/></ItemGroup></Project>'
    $harnessPath = Join-Path $Artifacts 'BackendValidation.csproj'
    [IO.File]::WriteAllText($harnessPath, $harness)
    & dotnet run --project $harnessPath *> (Join-Path $Artifacts 'scenarios.log')
    if ($LASTEXITCODE -ne 0) { throw 'Scenario tests failed; see scenarios.log' }
    Get-Content (Join-Path $Artifacts 'scenarios.log') -Tail 4
} finally { Pop-Location }
