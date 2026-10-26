using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;
using RoR2;

namespace TPVoting
{
    public class TPVotingSystem : NetworkBehaviour
    {
        public TPVotingController TPVotingController { get; private set; }
        private TPLocker tpLocker;

        private GameObject voteTarget;
        private NetworkUserId voteStarterId;
        private readonly HashSet<GameObject> unlockedExits = new HashSet<GameObject>();

        public void Awake()
        {
            TPVotingController = gameObject.AddComponent<TPVotingController>();
            TPVotingController.OnTPVotingEnded += TPVotingController_OnTPVotingEnded;

            tpLocker = gameObject.AddComponent<TPLocker>();
            tpLocker.IsExitLocked = IsExitLocked;
            tpLocker.OnLockedExitInteractionAttempt += TpLocker_OnLockedExitInteractionAttempt;

            On.RoR2.Run.OnServerSceneChanged += Run_OnServerSceneChanged;
        }

        public void OnDestroy()
        {
            TPVotingController.OnTPVotingEnded -= TPVotingController_OnTPVotingEnded;
            Destroy(TPVotingController);

            tpLocker.OnLockedExitInteractionAttempt -= TpLocker_OnLockedExitInteractionAttempt;
            Destroy(tpLocker);

            On.RoR2.Run.OnServerSceneChanged -= Run_OnServerSceneChanged;
        }

        private bool IsExitLocked(GameObject exit)
        {
            return !unlockedExits.Contains(exit) && !UsersHelper.IsOneUserOnly() && CheckIfCurrentStageQualifyForTPVoting();
        }

        private void TpLocker_OnLockedExitInteractionAttempt(NetworkUser interactingUser, GameObject exit)
        {
            if (!TPVotingController.IsVotingStarted())
            {
                voteTarget = exit;
                voteStarterId = interactingUser.id;
                TPVotingController.StartVoting(interactingUser, TPLocker.GetExitName(exit));
            }
            else if (exit == voteTarget)
            {
                if (!TPVotingController.Vote(interactingUser))
                {
                    ChatHelper.PlayersNotReady(TPLocker.GetExitName(voteTarget));
                }
            }
            else
            {
                ChatHelper.VoteAlreadyRunning(TPLocker.GetExitName(voteTarget));
            }
        }

        private void TPVotingController_OnTPVotingEnded(TPVotingResult result)
        {
            var target = voteTarget;
            voteTarget = null;

            switch (result)
            {
                case TPVotingResult.Cancelled:
                    ChatHelper.VotingCancelled();
                    break;
                case TPVotingResult.CountdownFinished:
                    UnlockExit(target);
                    break;
                case TPVotingResult.AllReady:
                    LeaveThroughExit(target);
                    break;
            }
        }

        private void UnlockExit(GameObject exit)
        {
            if (!exit)
            {
                return;
            }

            unlockedExits.Add(exit);
            ChatHelper.ExitUnlocked(TPLocker.GetExitName(exit));
        }

        private void LeaveThroughExit(GameObject exit)
        {
            if (!exit)
            {
                return;
            }

            var starter = NetworkUser.readOnlyInstancesList.FirstOrDefault(u => u.id.Equals(voteStarterId));
            var candidates = new[] { starter }.Concat(TPVotingController.GetReadyUsers().Where(u => u != starter));

            unlockedExits.Add(exit);
            foreach (var user in candidates)
            {
                var interactor = GetLivingInteractor(user);
                if (interactor && TPLocker.IsAvailableFor(exit, interactor))
                {
                    ChatHelper.AllReady(TPLocker.GetExitName(exit));
                    tpLocker.InteractWithoutLock(interactor, exit);
                    return;
                }
            }

            ChatHelper.ExitUnlocked(TPLocker.GetExitName(exit));
        }

        private static Interactor GetLivingInteractor(NetworkUser user)
        {
            if (!user || !user.master)
            {
                return null;
            }

            var body = user.master.GetBody();
            return body && body.healthComponent && body.healthComponent.alive ? body.GetComponent<Interactor>() : null;
        }

        private void Run_OnServerSceneChanged(On.RoR2.Run.orig_OnServerSceneChanged orig, Run self, string sceneName)
        {
            orig(self, sceneName);

            TPVotingController.EndVoting();
            voteTarget = null;
            unlockedExits.Clear();
        }

        private bool CheckIfCurrentStageQualifyForTPVoting()
        {
            var sceneDef = SceneCatalog.GetSceneDefForCurrentScene();
            return sceneDef && !PluginGlobals.IgnoredStages.Contains(sceneDef.baseSceneName);
        }
    }
}
