$moduleName   = 'ImpliedReflection'
$manifestPath = "$PSScriptRoot\..\module\$moduleName.psd1"

Import-Module $manifestPath

Describe 'Disable-ImpliedReflection operation' {
    BeforeAll {
        # Disable before tests if already enabled.
        try { Disable-ImpliedReflection -ErrorAction Ignore } catch {}
    }
    It 'throws if not enabled' {
        { Disable-ImpliedReflection } | Should Throw 'is not enabled'
    }
    It 'runs successfully if was enabled' {
        Enable-ImpliedReflection -Force
        Disable-ImpliedReflection
    }
    It 'stops adding members after disable' {
        $ps = [powershell]::Create()
        try {
            $result = $ps.AddScript('
                    Import-Module $args[0]

                    Enable-ImpliedReflection -Force

                    $Host | Out-Default
                    $Host

                    Disable-ImpliedReflection
                    $ExecutionContext').
                AddArgument($manifestPath).
                Invoke()
            if ($ps.HadErrors) {
                throw $ps.Streams.Error
            }
        } finally {
            if ($ps) { $ps.Dispose() }
        }
        $result[0].Context.GetType().Name | Should Be 'ExecutionContext'
        $result[1]._context | Should Be $null
    }
}
