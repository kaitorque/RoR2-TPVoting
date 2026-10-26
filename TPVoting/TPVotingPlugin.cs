using BepInEx;
using R2API.Utils;
using RoR2;
using System.Security.Permissions;

[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]

namespace TPVoting
{
    [BepInDependency("com.bepis.r2api")]
    [BepInIncompatibility(OriginalModGuid)]
    [NetworkCompatibility(CompatibilityLevel.NoNeedForSync)]
    [BepInPlugin(ModGuid, ModName, ModVer)]
    public class TPVotingPlugin : BaseUnityPlugin
    {
        public const string ModVer = "1.5.0";
        public const string ModName = "TPVoting";
        public const string ModGuid = "kaitorque.TPVoting";
        public const string OriginalModGuid = "com.Mordrog.TPVoting";

        public TPVotingSystem TPVotingSystem { get; private set; }

        public TPVotingPlugin()
        {
            Log.Logger = Logger;
            InitConfig();
        }

        public void Awake()
        {
            On.RoR2.Run.Awake += Run_Awake;
            On.RoR2.Run.OnDestroy += Run_OnDestroy;
        }

        private void Run_Awake(On.RoR2.Run.orig_Awake orig, Run self)
        {
            orig(self);

            if (PluginConfig.IgnoredGameModes.Value.Contains(GameModeCatalog.GetGameModeName(self.gameModeIndex)))
            {
                return;
            }

            TPVotingSystem = gameObject.AddComponent<TPVotingSystem>();

        }

        private void Run_OnDestroy(On.RoR2.Run.orig_OnDestroy orig, Run self)
        {
            orig(self);

            if (TPVotingSystem)
            {
                Destroy(TPVotingSystem);
            }
        }

        private void InitConfig()
        {
            PluginConfig.PlayerIsReadyMessages = Config.Bind<string>(
                "Settings",
                "PlayerIsReadyMessages",
                "r,rdy,ready",
                "The message the player has to write in the chat to confirm they are ready. Values must be separated by comma."
            );

            PluginConfig.PlayerCancelMessages = Config.Bind<string>(
                "Settings",
                "PlayerCancelMessages",
                "f",
                "The message the player has to write in the chat to remove their vote. Values must be separated by comma."
            );

            PluginConfig.IgnoredGameModes = Config.Bind<string>(
                "Settings",
                "IgnoredGameModes",
                "InfiniteTowerRun",
                "Gamemode in which tp voting should not work."
            );

            PluginConfig.MajorityVotesCountdownTime = Config.Bind<uint>(
                "Settings",
                "MajorityVotesCountdownTime",
                30,
                "Countdown in seconds to unlock the voted exit when half or most of the players are ready."
            );

            PluginConfig.UserAutoVoteOnDeath = Config.Bind<bool>(
                "Settings",
                "UserAutoVoteOnDeath",
                true,
                "Should players auto vote tp when they die."
            );

        }
    }
}
