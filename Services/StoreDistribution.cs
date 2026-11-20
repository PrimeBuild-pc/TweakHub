namespace TweakHub.Services;

internal static class StoreDistribution
{
    internal static bool IsStoreBuild
    {
        get
        {
#if MSIX_STORE
            return true;
#else
            return false;
#endif
        }
    }
}
