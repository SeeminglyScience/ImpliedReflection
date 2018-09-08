using System.Management.Automation;
using System.Management.Automation.Internal;

namespace ImpliedReflection.Commands
{
    [Cmdlet(VerbsCommon.Add, "PrivateMember")]
    public class AddPrivateMemberCommand : PSCmdlet
    {
        [Parameter(ValueFromPipeline = true)]
        public PSObject InputObject { get; set; }

        [Parameter(Position = 0)]
        [ValidateNotNullOrEmpty()]
        public string ReturnPropertyName { get; set; }

        [Parameter]
        public SwitchParameter PassThru { get; set; }

        protected override void ProcessRecord()
        {
            if (InputObject == null)
            {
                return;
            }

            if (InputObject.BaseObject == null ||
                InputObject == AutomationNull.Value)
            {
                MaybeWriteObject();
                return;
            }

            PSMemberData memberTable = PSMemberData.Get(
                InputObject.BaseObject.GetType(),
                InputObject.BaseObject);

            foreach (PSMemberInfo member in memberTable.Members)
            {
                InputObject.Members.Add(member, preValidated: true);
                if (member is PSPropertyInfo property)
                {
                    InputObject.Properties.Add(property, preValidated: true);
                }

                if (member is PSMethodInfo method)
                {
                    InputObject.Methods.Add(method, preValidated: true);
                }
            }

            MaybeWriteObject();
        }

        private void MaybeWriteObject()
        {
            if (PassThru.IsPresent)
            {
                WriteObject(InputObject);
                return;
            }

            if (ReturnPropertyName == null)
            {
                return;
            }

            WriteObject(InputObject.Members[ReturnPropertyName]);
        }
    }
}
