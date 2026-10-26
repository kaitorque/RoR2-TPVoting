using System.Linq;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace TPVoting
{
    public class TPLocker : NetworkBehaviour
    {
        public delegate bool ExitLockCheck(GameObject exit);
        public ExitLockCheck IsExitLocked { get; set; } = (GameObject exit) => { return false; };

        public delegate void LockedExitInteractionAttempt(NetworkUser interactingUser, GameObject exit);
        public event LockedExitInteractionAttempt OnLockedExitInteractionAttempt;

        private bool bypassLock;

        public void Awake()
        {
            On.RoR2.Interactor.PerformInteraction += Interactor_PerformInteraction;
            On.RoR2.SceneExitController.OnEnable += SceneExitController_OnEnable;
            On.RoR2.SceneExitController.Begin += SceneExitController_Begin;
        }

        public void OnDestroy()
        {
            On.RoR2.Interactor.PerformInteraction -= Interactor_PerformInteraction;
            On.RoR2.SceneExitController.OnEnable -= SceneExitController_OnEnable;
            On.RoR2.SceneExitController.Begin -= SceneExitController_Begin;
        }

        public static bool IsStageExit(GameObject interactableObject)
        {
            if (!interactableObject || interactableObject.GetComponent<SeerStationController>())
            {
                return false;
            }

            if (interactableObject.TryGetComponent<TeleporterInteraction>(out var teleporter))
            {
                return teleporter.activationState == TeleporterInteraction.ActivationState.Charged;
            }

#pragma warning disable CS0618
            return interactableObject.GetComponentInParent<SceneExitController>() || interactableObject.GetComponentInChildren<SceneExitController>()
                || interactableObject.GetComponentInParent<SceneExitControllerColossusPortal>() || interactableObject.GetComponentInChildren<SceneExitControllerColossusPortal>();
#pragma warning restore CS0618
        }

        public static bool IsAvailableFor(GameObject interactableObject, Interactor interactor)
        {
            return interactableObject && interactor
                && interactableObject.GetComponents<IInteractable>().Any(i => i.GetInteractability(interactor) == Interactability.Available);
        }

        public static string GetExitName(GameObject exit)
        {
            if (!exit)
            {
                return "exit";
            }

            var displayName = exit.GetComponent<IDisplayNameProvider>()?.GetDisplayName();
            return string.IsNullOrEmpty(displayName) ? exit.name : displayName;
        }

        public void InteractWithoutLock(Interactor interactor, GameObject exit)
        {
            bypassLock = true;
            try
            {
                interactor.AttemptInteraction(exit);
            }
            finally
            {
                bypassLock = false;
            }
        }

        private void Interactor_PerformInteraction(On.RoR2.Interactor.orig_PerformInteraction orig, Interactor self, GameObject interactableObject)
        {
            if (bypassLock || !NetworkServer.active || !IsStageExit(interactableObject) || !IsExitLocked(interactableObject) || !IsAvailableFor(interactableObject, self))
            {
                orig(self, interactableObject);
                return;
            }

            var user = UsersHelper.GetUser(self);
            if (!user)
            {
                orig(self, interactableObject);
                return;
            }

            OnLockedExitInteractionAttempt?.Invoke(user, interactableObject);
        }

        private void SceneExitController_OnEnable(On.RoR2.SceneExitController.orig_OnEnable orig, SceneExitController self)
        {
            orig(self);

            var interactables = self.GetComponentsInParent<IInteractable>().Concat(self.GetComponentsInChildren<IInteractable>()).Distinct();
            Log.Info($"Protected stage exit: {self.name} (interactables: {string.Join(", ", interactables.Select(i => i.GetType().Name))})");
        }

        private void SceneExitController_Begin(On.RoR2.SceneExitController.orig_Begin orig, SceneExitController self)
        {
            Log.Info($"Stage exit begun: {self.name}");
            orig(self);
        }
    }
}
