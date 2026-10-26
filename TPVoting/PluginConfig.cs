using BepInEx.Configuration;
using System.Collections.Generic;

namespace TPVoting
{
    class PluginConfig
    {
        public static ConfigEntry<string>
            PlayerIsReadyMessages,
            PlayerCancelMessages,
            IgnoredGameModes;

        public static ConfigEntry<uint>
            MajorityVotesCountdownTime;

        public static ConfigEntry<bool>
            UserAutoVoteOnDeath;
    }
}
