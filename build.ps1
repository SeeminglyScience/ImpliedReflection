[CmdletBinding()]
param(
    [switch] $Force
)
end {
    & "$PSScriptRoot\tools\AssertRequiredModule.ps1" InvokeBuild 5.8.4 -Force:$Force.IsPresent
    $invokeBuildSplat = @{
        Task = 'Build'
        File = "$PSScriptRoot/ImpliedReflection.build.ps1"
        Configuration = 'Release'
    }

    Invoke-Build @invokeBuildSplat
}
