using UnityEngine.Networking;
using RoR2;

namespace TPVoting
{
    public class TPVotingSystem : NetworkBehaviour
    {
        public TPVotingController TPVotingController { get; private set; }
        private TPLocker tpLocker;

        public bool IsTPUnlocked()
        {
            return tpLocker.IsTPUnlocked;
        }

        public void Awake()
        {
            TPVotingController = gameObject.AddComponent<TPVotingController>();
            TPVotingController.OnTPVotingStarted += TPVotingController_OnTPVotingStarted;
            TPVotingController.OnTPVotingEnded += TPVotingController_OnTPVotingEnded;

            tpLocker = gameObject.AddComponent<TPLocker>();
            tpLocker.IsLockedTPInteractable = (NetworkUser user) => { return !TPVotingController.HasUserVoted(user); };
            tpLocker.OnLockedTPInteractionAttempt += TpLocker_OnLockedTPInteractionAttempt;

            On.RoR2.Run.OnServerSceneChanged += Run_OnServerSceneChanged;
            On.RoR2.TeleporterInteraction.ChargedState.OnEnter += TeleporterInteraction_ChargedState_OnEnter;
        }

        public void OnDestroy()
        {
            TPVotingController.OnTPVotingStarted -= TPVotingController_OnTPVotingStarted;
            TPVotingController.OnTPVotingEnded -= TPVotingController_OnTPVotingEnded;
            Destroy(TPVotingController);

            tpLocker.OnLockedTPInteractionAttempt -= TpLocker_OnLockedTPInteractionAttempt;
            Destroy(tpLocker);

            On.RoR2.Run.OnServerSceneChanged -= Run_OnServerSceneChanged;
            On.RoR2.TeleporterInteraction.ChargedState.OnEnter -= TeleporterInteraction_ChargedState_OnEnter;
        }

        private void TPVotingController_OnTPVotingStarted()
        {
            tpLocker.IsTPUnlocked = false;
        }

        private void TPVotingController_OnTPVotingEnded()
        {
            tpLocker.IsTPUnlocked = true;
            ChatHelper.TPUnlocked();
        }

        private void TpLocker_OnLockedTPInteractionAttempt(NetworkUser interactingUser)
        {
            if (!TPVotingController.Vote(interactingUser))
            {
                ChatHelper.PlayersNotReady();
            }
        }

        private void Run_OnServerSceneChanged(On.RoR2.Run.orig_OnServerSceneChanged orig, Run self, string sceneName)
        {
            orig(self, sceneName);

            TryStartVoting(true);
        }

        private void TeleporterInteraction_ChargedState_OnEnter(On.RoR2.TeleporterInteraction.ChargedState.orig_OnEnter orig, RoR2.TeleporterInteraction.ChargedState self)
        {
            orig(self);

            if (PluginConfig.VoteAfterTPEvent.Value)
            {
                TryStartVoting(false);
            }
        }

        private void TryStartVoting(bool showInstruction)
        {
            tpLocker.IsTPUnlocked = true;
            TPVotingController.EndVoting();
            if (UsersHelper.IsOneUserOnly() || !CheckIfCurrentStageQualifyForTPVoting())
            {
                return;
            }

            TPVotingController.StartVoting(showInstruction);
        }

        private bool CheckIfCurrentStageQualifyForTPVoting()
        {
            return !PluginGlobals.IgnoredStages.Contains(SceneCatalog.GetSceneDefForCurrentScene().baseSceneName);
        }
    }
}
