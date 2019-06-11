using System.Management.Automation;

namespace ImpliedReflection.Commands
{
    [Cmdlet(
        VerbsLifecycle.Enable,
        StringLiterals.ImpliedReflection)]
    public class EnableImpliedReflectionCommand : PSCmdlet
    {
        private bool _yesToAll;

        private bool _noToAll;

        [Parameter]
        public SwitchParameter Force { get; set; }

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

            if (!(Force.IsPresent || ShouldContinue()))
            {
                return;
            }

            if (!DelegateController.Enable())
            {
                WriteError(
                    new ErrorRecord(
                        new PSInvalidOperationException(ImpliedReflectionStrings.ImpliedReflectionAlreadyEnabled),
                        nameof(ImpliedReflectionStrings.ImpliedReflectionAlreadyEnabled),
                        ErrorCategory.InvalidOperation,
                        null));
                return;
            }

            ProxyCommandManager.Current.Override((EngineIntrinsics)GetVariableValue("ExecutionContext"));
        }

        private bool ShouldContinue()
        {
            return ShouldContinue(
                ImpliedReflectionStrings.ConfirmMessage,
                ImpliedReflectionStrings.ConfirmTitle,
                hasSecurityImpact: false,
                ref _yesToAll,
                ref _noToAll);
        }
    }
}
