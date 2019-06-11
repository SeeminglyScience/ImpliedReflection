using System.Management.Automation;

namespace ImpliedReflection.Commands
{
    [Cmdlet(VerbsLifecycle.Disable, StringLiterals.ImpliedReflection)]
    public class DisableImpliedReflectionCommand : PSCmdlet
    {
        protected override void EndProcessing()
        {
            if (PSVersionInfo.PSVersion == PSVersionInfo.Empty)
            {
                ThrowTerminatingError(
                    new ErrorRecord(
                        new PSInvalidOperationException(ImpliedReflectionStrings.UnknownPowerShellVersion),
                        nameof(ImpliedReflectionStrings.UnknownPowerShellVersion),
                        ErrorCategory.InvalidOperation,
                        targetObject: null));

                return;
            }

            if (!DelegateController.Disable())
            {
                WriteError(
                    new ErrorRecord(
                        new PSInvalidOperationException(ImpliedReflectionStrings.ImpliedReflectionDisabled),
                        nameof(ImpliedReflectionStrings.ImpliedReflectionDisabled),
                        ErrorCategory.InvalidOperation,
                        null));
            }

            ProxyCommandManager.Current.Undo((EngineIntrinsics)GetVariableValue("ExecutionContext"));
        }
    }
}
