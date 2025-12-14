[CmdletBinding()]
param(
    [ValidateSet('Debug', 'Release')]
    [string] $Configuration = 'Release',

    [Parameter()]
    [switch] $Force,

    [Parameter()]
    [switch] $Publish
)
end {
    & "$PSScriptRoot\tools\AssertRequiredModule.ps1" InvokeBuild 5.12.2 -Force
    $invokeBuildSplat = @{
        Task = 'Build'
        File = "$PSScriptRoot/ImpliedReflection.build.ps1"
        Force = $Force.IsPresent
        Configuration = $Configuration
    }

    if ($Publish) {
        $invokeBuildSplat['Task'] = 'Publish'
    }

    Invoke-Build @invokeBuildSplat
}
