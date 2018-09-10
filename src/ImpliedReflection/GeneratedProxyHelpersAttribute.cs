using System;
using System.ComponentModel;

namespace ImpliedReflection.Internal
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public class GeneratedProxyHelpersAttribute : Attribute
    {
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("do not use this method.", error: true)]
        public GeneratedProxyHelpersAttribute()
        {
        }
    }
}
