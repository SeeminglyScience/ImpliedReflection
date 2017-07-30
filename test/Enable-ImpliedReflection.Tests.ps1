$moduleName   = 'ImpliedReflection'
$manifestPath = "$PSScriptRoot\..\module\$moduleName.psd1"

Import-Module $manifestPath

function OutConsoleProxy {
    param([Parameter(ValueFromPipeline)]$InputObject)
    process {
        $ps = [powershell]::Create()
        try {
            $ps.AddScript('
                param($modulePath, $inputObject)
                Import-Module $modulePath
                Enable-ImpliedReflection -Force
                $inputObject | Out-Default').
                AddArgument($manifestPath).
                AddArgument($InputObject).
                Invoke()
            if ($ps.HadErrors) {
                throw $ps.Streams.Error
            }
        } finally {
            if ($ps) { $ps.Dispose() }
        }
    }
}

Describe 'Enable-ImpliedReflection operation' {
    It 'can be enabled' {
        (Get-Command Out-Default).Module.Name | Should Not Be 'ImpliedReflection'
        Enable-ImpliedReflection -Force
        (Get-Command Out-Default).Module.Name | Should Be 'ImpliedReflection'
    }
    It 'throws if already enabled' {
        { Enable-ImpliedReflection -Force
          Enable-ImpliedReflection -Force } | Should Throw 'already enabled'
    }
    It 'can implicitly add to $ExecutionContext' {
        $ExecutionContext | OutConsoleProxy
        $ExecutionContext._context.GetType().Name | Should Be 'ExecutionContext'
    }
    It 'implicitly adds to $Host' {
        $ps = [powershell]::Create()
        try {
            $ps.AddScript('
                    Import-Module $args[0]

                    Enable-ImpliedReflection -Force

                    $Host | Out-Default
                    $Host').
                AddArgument($manifestPath).
                Invoke().
                Context.
                GetType().
                Name | Should Be 'ExecutionContext'
            if ($ps.HadErrors) {
                throw $ps.Streams.Error
            }
        } finally {
            if ($ps) { $ps.Dispose() }
        }
    }
}
