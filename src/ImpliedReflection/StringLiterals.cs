namespace ImpliedReflection
{
    internal class StringLiterals
    {
        internal const string ProxyCtorName = "ctor";

        internal const string ReflectionCtorName = ".ctor";

        internal const string PSCtorName = "new";

        internal const string ClrMembersCollectionName = "clr members";

        internal const string DynamicProxyAssemblyName = "ImpliedReflection Generated Types";

        internal const string ImpliedReflection = "ImpliedReflection";

        internal const string OutDefaultProxy = @"
            [CmdletBinding(HelpUri='https://go.microsoft.com/fwlink/?LinkID=113362', RemotingCapability='None')]
            param(
                [switch] $Transcript,

                [Parameter(ValueFromPipeline)]
                [psobject] $InputObject
            )
            begin {
                try {
                    if ($PSBoundParameters.ContainsKey('OutBuffer')) {
                        $PSBoundParameters['OutBuffer'] = 1
                    }

                    $wrappedCmd = $ExecutionContext.InvokeCommand.GetCommand(
                        'Microsoft.PowerShell.Core\Out-Default',
                        [System.Management.Automation.CommandTypes]::Cmdlet)

                    $scriptCmd = { & $wrappedCmd @PSBoundParameters }
                    $steppablePipeline = $scriptCmd.GetSteppablePipeline($MyInvocation.CommandOrigin)
                    $steppablePipeline.Begin($PSCmdlet)
                } catch {
                    throw
                }
            }
            process {
                try {
                    # Ensure the ImpliedReflection delegate has been called and
                    # all members are populated. This is done because the formatter
                    # does not appear to grab CLR members like expected otherwise.
                    $null = $PSItem.psobject.Members[0]
                    $steppablePipeline.Process($PSItem)
                } catch {
                    throw
                }
            }
            end {
                try {
                    $steppablePipeline.End()
                } catch {
                    throw
                }
            }
            <#
                .ForwardHelpTargetName Microsoft.PowerShell.Core\Out-Default
                .ForwardHelpCategory Cmdlet
            #>
        ";
    }
}
