using BepInEx.Logging;

namespace TPVoting
{
    internal static class Log
    {
        internal static ManualLogSource Logger;

        internal static void Info(object data) => Logger?.LogInfo(data);
    }
}
