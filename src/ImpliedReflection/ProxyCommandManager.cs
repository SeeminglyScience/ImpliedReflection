using System.Collections.Generic;
using System.Diagnostics;
using System.Management.Automation;
using System.Management.Automation.Provider;
using System.Management.Automation.Runspaces;

namespace ImpliedReflection
{
    internal class ProxyCommandManager
    {
        private const string OutDefaultAlias = "Microsoft.PowerShell.Core\\Alias::Out-Default";

        private const string OutDefaultFunction = "Microsoft.PowerShell.Core\\Function::Out-Default";

        private const string OutDefaultCommandName = "Out-Default";

        private static Dictionary<Runspace, ProxyCommandManager> s_managerTable =
            new Dictionary<Runspace, ProxyCommandManager>();

        internal static ProxyCommandManager Current
        {
            get
            {
                Runspace currentRunspace = Runspace.DefaultRunspace;
                Debug.Assert(
                    currentRunspace != null,
                    "Caller should verify ProxyCommandManager is referenced on the correct thread.");

                lock (s_managerTable)
                {
                    ProxyCommandManager manager;
                    if (s_managerTable.TryGetValue(currentRunspace, out manager))
                    {
                        return manager;
                    }

                    manager = new ProxyCommandManager();
                    s_managerTable.Add(currentRunspace, manager);
                    return manager;
                }
            }
        }

        internal ScriptBlock OriginalOutDefaultFunction { get; set; }

        internal string OriginalOutDefaultAlias { get; set; }

        private ProxyCommandManager()
        {
        }

        internal void Override(EngineIntrinsics engine)
        {
            ItemCmdletProviderIntrinsics itemIntrinsics = engine.InvokeProvider.Item;
            if (itemIntrinsics.Exists(OutDefaultAlias))
            {
                OriginalOutDefaultAlias = ReadSingle<string>(engine, OutDefaultAlias);
                itemIntrinsics.Remove(OutDefaultAlias, recurse: false);
            }

            ScriptBlock outDefaultProxy = engine.SessionState.InvokeCommand
                .NewScriptBlock(StringLiterals.OutDefaultProxy);
            if (itemIntrinsics.Exists(OutDefaultFunction))
            {
                OriginalOutDefaultFunction = ReadSingle<ScriptBlock>(engine, OutDefaultFunction);
            }
            else
            {
                itemIntrinsics.New(OutDefaultFunction, OutDefaultCommandName, null, outDefaultProxy);
                return;
            }

            WriteSingle<ScriptBlock>(engine, OutDefaultFunction, outDefaultProxy);
        }

        internal void Undo(EngineIntrinsics engine)
        {
            ItemCmdletProviderIntrinsics itemIntrinsics = engine.InvokeProvider.Item;
            if (OriginalOutDefaultAlias != null)
            {
                if (itemIntrinsics.Exists(OutDefaultAlias))
                {
                    WriteSingle<string>(engine, OutDefaultAlias, OriginalOutDefaultAlias);
                }
                else
                {
                    itemIntrinsics.New(
                        OutDefaultAlias,
                        OutDefaultCommandName,
                        string.Empty,
                        OriginalOutDefaultAlias);
                }

                OriginalOutDefaultAlias = null;
            }

            if (OriginalOutDefaultFunction != null)
            {
                WriteSingle<ScriptBlock>(engine, OutDefaultFunction, OriginalOutDefaultFunction);
                OriginalOutDefaultFunction = null;
                return;
            }

            itemIntrinsics.Remove(OutDefaultFunction, recurse: false);
        }

        private TContentType ReadSingle<TContentType>(EngineIntrinsics engine, string path)
            where TContentType : class
        {
            using (IContentReader reader = engine.InvokeProvider.Content.GetReader(path)[0])
            {
                object content = reader.Read(1)[0];
                if (content is PSObject pso)
                {
                    return pso.BaseObject as TContentType;
                }

                return content as TContentType;
            }
        }

        private void WriteSingle<TContentType>(EngineIntrinsics engine, string path, TContentType content)
        {
            engine.InvokeProvider.Content.Clear(path);
            using (IContentWriter writer = engine.InvokeProvider.Content.GetWriter(path)[0])
            {
                writer.Write(new[] { content });
            }
        }
    }
}
