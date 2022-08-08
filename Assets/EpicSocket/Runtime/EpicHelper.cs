using PlayEveryWare.EpicOnlineServices;

namespace Mirage.Sockets.EpicSocket
{
    public static class EpicHelper
    {
        public static bool IsLoaded()
        {
            return EOSManager.Instance.GetEOSPlatformInterface() != null;
        }
    }
}

