using System;
using System.Management.Automation;
using System.Reflection;

namespace ImpliedReflection
{
    /// <summary>
    /// Represents the real PSVersionInfo class in SMA. Although it recently became public, it
    /// still isn't in PSv5 which is what we're preferencing, so more reflection.
    /// </summary>
    internal static class PSVersionInfo
    {
        internal static readonly Version Empty = new Version(0, 0);

        static PSVersionInfo()
        {
            Type psVersionInfoType =
                typeof(PSObject).Assembly.GetType("System.Management.Automation.PSVersionInfo");

            if (psVersionInfoType == null)
            {
                PSVersion = Empty;
                return;
            }

            PropertyInfo psVersionProperty = psVersionInfoType.GetProperty(
                nameof(PSVersion),
                Bind.Any.Static);

            if (psVersionProperty == null || psVersionProperty.PropertyType != typeof(Version))
            {
                PSVersion = Empty;
                return;
            }

            object version = null;
            try
            {
                version = psVersionProperty.GetValue(null);
            }
            catch
            {
            }

            if (version == null)
            {
                PSVersion = Empty;
                return;
            }

            PSVersion = (Version)version;
        }

        internal static Version PSVersion { get; }
    }
}
