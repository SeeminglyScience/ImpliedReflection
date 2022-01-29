#if PS5_1
namespace System
{
    internal static class HashCode
    {
        public static int Combine<T0, T1>(T0 arg0, T1 arg1)
        {
            unchecked
            {
                int hash = 17;
                hash = (hash * 23) + (arg0?.GetHashCode() ?? 0);
                hash = (hash * 23) + (arg1?.GetHashCode() ?? 0);
                return hash;
            }
        }
    }
}
#endif
