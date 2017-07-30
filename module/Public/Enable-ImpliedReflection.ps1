using namespace System.Management.Automation

function Enable-ImpliedReflection {
    <#
    .EXTERNALHELP ImpliedReflection-help.xml
    #>
    [CmdletBinding(SupportsShouldProcess=$true, ConfirmImpact='High')]
    param(
        [switch]
        $Force
    )
    end {
        if ($script:OriginalOutDefault) {
            $PSCmdlet.ThrowTerminatingError(
                [ErrorRecord]::new(
                    [InvalidOperationException]::new('Implied reflection is already enabled.'),
                    'ImpliedReflectionAlreadyEnabled',
                    [ErrorCategory]::InvalidOperation,
                    $null))
        }
        $outDefault = $ExecutionContext.InvokeCommand.GetCommand('Out-Default', 'All')
        $script:OriginalOutDefault = $outDefault
        $proxy = [ProxyCommand]::Create($outDefault)
        $injectedProxy = $proxy -replace
            '(process(\r?\n){\s+try {\r?\n)',
            '$1        if ($null -ne $PSItem) { $null = Add-PrivateMember -InputObject $PSItem$2 }'

        $function = 'function global:Out-Default {',
                    $injectedProxy,
                    '}' -join [Environment]::NewLine

        if (-not $Force.IsPresent) {
            $shouldProcess = $PSCmdlet.ShouldProcess(
                $Strings.EnableIRWhatIf,
                $Strings.EnableIRConfirmMessage,
                $Strings.EnableIRConfirmTitle)
        }
        $definer = [scriptblock]::Create($function)

        if ($shouldProcess -or $Force.IsPresent) {
            . $definer
        }
    }
}
