using System.Reflection;

namespace ImpliedReflection
{
    internal static class BindTo
    {
        public static BindingFlags IgnoreCase(this BindingFlags flags)
        {
            if ((flags & BindingFlags.IgnoreCase) != 0)
            {
                return flags;
            }

            return flags | BindingFlags.IgnoreCase;
        }

        public static BindingFlags All =
            BindingFlags.Public |
            BindingFlags.Static |
            BindingFlags.Instance |
            BindingFlags.Static;

        public static class Public
        {
            public static BindingFlags Instance = BindingFlags.Instance | BindingFlags.Public;

            public static BindingFlags Static = BindingFlags.Static | BindingFlags.Public;
        }

        public static class NonPublic
        {
            public static BindingFlags Instance = BindingFlags.Instance | BindingFlags.NonPublic;

            public static BindingFlags Static = BindingFlags.Static | BindingFlags.NonPublic;
        }

        public static class Any
        {
            public static BindingFlags Instance =
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

            public static BindingFlags Static =
                BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public;

            public static BindingFlags Public =
                BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public;

            public static BindingFlags NonPublic =
                BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic;
        }
    }
}
