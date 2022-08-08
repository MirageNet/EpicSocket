using Epic.OnlineServices;
using Mirage.Logging;
using UnityEngine;

namespace Mirage.Sockets.EpicSocket
{
    public static class EpicLogger
    {
        // change default log level based on if we are in debug or release mode.
        // this is only default, if there are log settings they will be used instead of these
#if DEBUG
        private const LogType DEFAULT_LOG = LogType.Warning;
#else
        const LogType DEFAULT_LOG = LogType.Error;
#endif
        internal static readonly ILogger logger = LogFactory.GetLogger("Mirage.Sockets.EpicSocket.Logger", DEFAULT_LOG);
        internal static readonly ILogger verboseLogger = LogFactory.GetLogger("Mirage.Sockets.EpicSocket.Verbose", LogType.Exception);

        public static void WarnResult(this ILogger logger, string tag, Result result)
        {
            if (result == Result.Success) return;
            if (logger.WarnEnabled())
                logger.LogWarning($"{tag} failed with result: {result}");
        }

        [System.Diagnostics.Conditional("DEBUG")]
        internal static void Verbose(string log)
        {
            if (!verboseLogger.LogEnabled())
                return;

#if UNITY_EDITOR
            verboseLogger.Log(log);
#else
            System.Console.WriteLine(log);
#endif
        }
    }
}

