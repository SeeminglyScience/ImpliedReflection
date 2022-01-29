#requires -Module InvokeBuild -Version 5.1
[CmdletBinding()]
param(
    [ValidateSet('Debug', 'Release')]
    [string] $Configuration = 'Debug',

    [string[]] $Framework,

    [string[]] $PowerShellVersion = ('5.1', '7.2.0')
)

$moduleName = 'ImpliedReflection'
$manifest = Test-ModuleManifest -Path $PSScriptRoot\module\$moduleName.psd1 -ErrorAction Ignore -WarningAction Ignore

$frameworkSpecified = $PSBoundParameters.ContainsKey('Framework')

$Settings = @{
    Name = $moduleName
    Manifest = $manifest
    Version = $manifest.Version
    ShouldAnalyze = $true
    ShouldTest = $true
    DefaultFrameworks = @()
    TargetFrameworks = $Framework
    PowerShellVersions = $PowerShellVersion
}

$Folders = @{
    PowerShell = "$PSScriptRoot\module"
    Release = '{0}\Release\{1}\{2}' -f $PSScriptRoot, $moduleName, $manifest.Version
    Docs = "$PSScriptRoot\docs"
    Test = "$PSScriptRoot\test"
    PesterCC = "$PSScriptRoot\*.psm1", "$PSScriptRoot\Public\*.ps1", "$PSScriptRoot\Private\*.ps1"
}

$Discovery = @{
    HasDocs = Test-Path ('{0}\{1}\*.md' -f $Folders.Docs, $PSCulture)
    HasTests = Test-Path ('{0}\*.Test.ps1' -f $Folders.Test)
    IsUnix = $PSEdition -eq 'Core' -and -not $IsWindows
}

task Init {
    Remove-Item $Folders.Release -ErrorAction Stop -Force -Recurse
    New-Item -ItemType Directory $Folders.Release -ErrorAction Stop | Out-Null

    $csproj = Get-Content -Raw $PSScriptRoot/src/ImpliedReflection/ImpliedReflection.csproj
    $match = [regex]::Match($csproj, '<TargetFrameworks>(?<Frameworks>[^<]+)</TargetFrameworks>')
    $Settings.DefaultFrameworks = $match.Groups['Frameworks'].Value -split ';'
    if (-not $frameworkSpecified) {
        $Settings.TargetFrameworks = $Settings.DefaultFrameworks
    }

    $Settings.PowerShellVersions = $PowerShellVersion

    if ($Settings.TargetFrameworks.Length -ne $Settings.TargetFrameworks.Length) {
        throw 'The same amount of items must be passed to $Framework and $PowerShellVersion.'
    }
}

task CleanImpl {
    $releaseFolder = $Folders.Release
    if (Test-Path $releaseFolder) {
        Remove-Item $releaseFolder -Recurse
    }

    Remove-Item $PSScriptRoot\lib -Recurse -Force -ErrorAction Stop
    New-Item -ItemType Directory $releaseFolder | Out-Null
    & $dotnet clean
}

task AssertDotNet {
    $script:dotnet = $dotnet = & $PSScriptRoot\tools\GetDotNet.ps1 -Unix:$Discovery.IsUnix
}

task BuildDocs -If { $Discovery.HasDocs } {
    $output = '{0}\{1}' -f $Folders.Release, $PSCulture
    $null = New-ExternalHelp -Path $PSScriptRoot\docs\$PSCulture -OutputPath $output
}

task BuildGenerator {
    & $dotnet publish "$PSScriptRoot/src/IgnoresAccessChecksToGenerator" --framework net46 --configuration Release --verbosity q -nologo
    & $dotnet publish "$PSScriptRoot/src/IgnoresAccessChecksToGenerator" --framework netstandard2.0 --configuration Release  --verbosity q -nologo
}

task BuildManaged {
    for ($i = 0; $i -lt $Settings.TargetFrameworks.Length; $i++) {
        $currentFramework = $Settings.TargetFrameworks[$i]
        $currentPowerShellVersion = $Settings.PowerShellVersions[$i]
        if ($currentPowerShellVersion -eq '5.1' -and $Discovery.IsUnix) {
            continue
        }

        $argList = (
            'publish',
            '--framework', $currentFramework,
            '--configuration', $Configuration,
            '--verbosity', 'q',
            '-nologo')

        if ($frameworkSpecified) {
            $argList += @("-p:SMAVersion=$currentPowerShellVersion")

            if ($currentFramework -notin $Settings.DefaultFrameworks) {
                $argList += @("-p:ExtraFrameworks=$currentFramework")
            }
        }

        & $dotnet @argList
    }
}

task CopyToRelease {
    $releaseFolder = $Folders.Release

    if (-not (Test-Path $releaseFolder)) {
        New-Item -ItemType Directory $releaseFolder | Out-Null
    }

    Copy-Item $PSScriptRoot/module/ImpliedReflection.psd1 -Destination $releaseFolder/ImpliedReflection.psd1 -Recurse

    $fallbackVerison = '5.1'
    for ($i = 0; $i -lt $Settings.TargetFrameworks.Length; $i++) {
        $currentFramework = $Settings.TargetFrameworks[$i]
        $currentPowerShellVersion = $Settings.PowerShellVersions[$i]
        $destination = Join-Path $releaseFolder -ChildPath $currentPowerShellVersion
        if (-not (Test-Path $destination)) {
            New-Item -ErrorAction Stop -ItemType Directory $destination | Out-Null
        }

        $publish = "$PSScriptRoot/src/ImpliedReflection/bin/$Configuration/$currentFramework/publish"
        Copy-Item $publish/ImpliedReflection.* -Destination $destination
        Copy-Item $publish/*Harmony* -Destination $destination
        Copy-Item $publish/*MonoMod* -Destination $destination
        Copy-Item $publish/*Mono.Cecil* -Destination $destination

        if ($currentPowerShellVersion -eq '5.1') {
            Copy-Item $publish/System.Buffers.dll -Destination $destination
            Copy-Item $publish/System.Collections.Immutable.dll -Destination $destination
            Copy-Item $publish/System.Memory.dll -Destination $destination
            Copy-Item $publish/System.Numerics.Vectors.dll -Destination $destination
            Copy-Item $publish/System.Runtime.CompilerServices.Unsafe.dll -Destination $destination
        } else {
            $fallbackVerison = $currentPowerShellVersion
        }
    }

    $null = New-Item -Force -ItemType File -Path $releaseFolder/ImpliedReflection.psm1 -Value ('
        # <auto-generated>
        if ($PSVersionTable.PSVersion.Major -eq 6) {{
            throw "This version of PowerShell is not supported. Please upgrade to at least PowerShell 7"
        }} elseif ($PSVersionTable.PSVersion.Major -eq 5) {{
            Import-Module $PSScriptRoot/5.1/ImpliedReflection.dll -ErrorAction Stop
        }} else {{
            Import-Module $PSScriptRoot/{0}/ImpliedReflection.dll -ErrorAction Stop
        }}

        Export-ModuleMember -Cmdlet Enable-ImpliedReflection' -f $fallbackVerison)
}

task DoInstall {
    $installBase = $Home
    if ($profile) { $installBase = $profile | Split-Path }
    $installPath = '{0}\Modules\{1}\{2}' -f $installBase, $Settings.Name, $Settings.Version

    if (-not (Test-Path $installPath)) {
        $null = New-Item $installPath -ItemType Directory
    }

    Copy-Item -Path ('{0}\*' -f $Folders.Release) -Destination $installPath -Force -Recurse
}

task DoPublish {
    if (-not (Test-Path $env:USERPROFILE\.PSGallery\apikey.xml)) {
        throw 'Could not find PSGallery API key!'
    }

    $apiKey = (Import-Clixml $env:USERPROFILE\.PSGallery\apikey.xml).GetNetworkCredential().Password
    Publish-Module -Name $Folders.Release -NuGetApiKey $apiKey -Confirm
}

task AssertSMA {
    $AssertSMA = "$PSScriptRoot/tools/AssertSMA.ps1"
    foreach ($version in $PowerShellVersion) {
        if ($version -eq '5.1') {
            $libPath = "$PSScriptRoot\lib\System.Management.Automation\5.1"
            if (Test-Path $libPath\System.Management.Automation.dll) {
                continue
            }

            if (-not (Test-Path $libPath)) {
                New-Item -ItemType Directory $libPath | Out-Null
            }

            $gacSma = "$env:WINDIR\Microsoft.Net\assembly\GAC_MSIL\System.Management.Automation\*\System.Management.Automation.dll"
            Copy-Item $gacSma -Destination $libPath\System.Management.Automation.dll -ErrorAction Stop
            continue
        }

        & $AssertSMA -RequiredVersion $version
    }
}

task Clean -Jobs Init, AssertDotNet, CleanImpl

task Build -Jobs Init, AssertDotNet, AssertSMA, BuildGenerator, BuildManaged, CopyToRelease, BuildDocs

task Install -Jobs Build, DoInstall

task Publish -Jobs Build, DoPublish

task . Build
