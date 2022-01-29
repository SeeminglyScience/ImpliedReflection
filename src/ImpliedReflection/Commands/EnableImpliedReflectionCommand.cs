using System.Management.Automation;

namespace ImpliedReflection.Commands
{
    [Cmdlet(VerbsLifecycle.Enable, "ImpliedReflection")]
    public sealed class EnableImpliedReflectionCommand : PSCmdlet
    {
        private bool _yesToAll;

        private bool _noToAll;

        [Parameter]
        [Alias(new[] { "Force", "f" })]
        public SwitchParameter YesIKnowIShouldNotDoThis { get; set; }

        protected override void EndProcessing()
        {
            if (!(YesIKnowIShouldNotDoThis.IsPresent || ShouldContinue()))
            {
                return;
            }

            NonPublicAdapter.Bind();
        }

        private bool ShouldContinue()
        {
            return ShouldContinue(
                Strings.ConfirmMessage,
                Strings.ConfirmTitle,
                hasSecurityImpact: false,
                ref _yesToAll,
                ref _noToAll);
        }
    }
}
