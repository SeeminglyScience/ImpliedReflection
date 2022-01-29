[CmdletBinding()]
param(
    [string] $Version = '6.1.100',

    [switch] $Unix
)
begin {
    function TestDotNetVersion([System.Management.Automation.CommandInfo] $command) {
        $existingVersion = ((& $command --version) -split '-')[0]
        if ($existingVersion -and ([version]$existingVersion) -ge ([version]$Version)) {
            return $true
        }

        return $false
    }
}
end {
    $targetFolder = "$PSScriptRoot/dotnet"
    $executableName = 'dotnet.exe'
    if ($Unix.IsPresent) {
        $executableName = 'dotnet'
    }

    if (($dotnet = Get-Command dotnet -ea 0) -and (TestDotNetVersion $dotnet)) {
        return $dotnet
    }


    if ($dotnet = Get-Command $targetFolder/$executableName -ea 0) {
        if (TestDotNetVersion $dotnet) {
            return $dotnet
        }

        Write-Host -ForegroundColor Yellow Found dotnet $found but require $Version, replacing...
        Remove-Item $targetFolder -Recurse
        $dotnet = $null
    }

    Write-Host -ForegroundColor Green Downloading dotnet version $Version

    if ($Unix.IsPresent) {
        $uri = "https://raw.githubusercontent.com/dotnet/cli/v2.0.0/scripts/obtain/dotnet-install.sh"
        $installerPath = [System.IO.Path]::GetTempPath() + 'dotnet-install.sh'
        $scriptText = [System.Net.WebClient]::new().DownloadString($uri)
        Set-Content $installerPath -Value $scriptText -Encoding UTF8
        $installer = { param($Version, $InstallDir) & (Get-Command bash) $installerPath -Version $Version -InstallDir $InstallDir }
    } else {
        $uri = "https://raw.githubusercontent.com/dotnet/cli/v2.0.0/scripts/obtain/dotnet-install.ps1"
        $scriptText = [System.Net.WebClient]::new().DownloadString($uri)

        # Stop the official script from hard exiting at times...
        $safeScriptText = $scriptText -replace 'exit 0', 'return'
        $installer = [scriptblock]::Create($safeScriptText)
    }

    $null = & $installer -Version $Version -InstallDir $targetFolder

    return Get-Command $targetFolder/$executableName
}
