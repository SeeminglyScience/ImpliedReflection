using namespace System.Management.Automation
using namespace System.Reflection

function Disable-ImpliedReflection {
    <#
    .EXTERNALHELP ImpliedReflection-help.xml
    #>
    [CmdletBinding()]
    param()
    end {
        if (-not $script:OriginalOutDefault) {
            $PSCmdlet.ThrowTerminatingError(
                [ErrorRecord]::new(
                    [InvalidOperationException]::new('Implied reflection is not enabled.'),
                    'ImpliedReflectionDisabled',
                    [ErrorCategory]::InvalidOperation,
                    $null))
        }
        foreach ($visibilityScope in 'static', 'instance') {
            foreach ($memberType in 'Property', 'Method') {
                [ref].Assembly.
                    GetType('System.Management.Automation.DotNetAdapter').
                    GetField("${visibilityScope}${memberType}CacheTable", [BindingFlags]'Static, NonPublic').
                    GetValue($null).
                    Clear()
            }
        }

        if ($script:OriginalOutDefault -is [CmdletInfo]) {
            Get-Item function:\Out-Default | Remove-Item
        } elseif ($script:OriginalOutDefault -is [FunctionInfo]) {
            Set-Content function:\Out-Default -Value $script:OriginalOutDefault.ScriptBlock
        }
        Remove-Variable -Scope Script -Name OriginalOutDefault
    }
}
