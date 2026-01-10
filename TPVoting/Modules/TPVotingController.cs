using UnityEngine.Networking;
using RoR2;
using System.Linq;
using System.Collections;
using System;

namespace TPVoting
{
    public class TPVotingController : NetworkBehaviour
    {
        private VotingMachine<NetworkUserId> votingMachine = new VotingMachine<NetworkUserId>();
        private IEnumerator majorityTPVotingTimer;
        private bool isMajorityTPVotingTimerRunning;

        public delegate void TPVotingStarted();
        public event TPVotingStarted OnTPVotingStarted;

        public delegate void TPVotingEnded();
        public event TPVotingEnded OnTPVotingEnded;

        public bool IsVotingStarted()
        {
            return votingMachine.IsVotingStarted;
        }

        public bool HasUserVoted(NetworkUser user)
        {
            return user && votingMachine.CheckIfVoted(user.id);
        }

        public bool Vote(NetworkUser user)
        {
            return user && votingMachine.IsVotingStarted && votingMachine.Vote(user.id);
        }

        public void StartVoting(bool showInstruction = true)
        {
            if (votingMachine.IsVotingStarted)
            {
                return;
            }

            votingMachine.StartVoting(NetworkUser.readOnlyInstancesList.Select(l => l.id).ToList());

            if (showInstruction)
            {
                StartCoroutine(ShowVotingInstruction());
                IEnumerator ShowVotingInstruction()
                {
                    yield return new UnityEngine.WaitForSeconds(3);
                    ChatHelper.VotingInstruction();
                }
            }
        }

        public void EndVoting()
        {
            if (votingMachine.IsVotingStarted)
            {
                votingMachine.EndVoting();
            }
        }

        public void Awake()
        {
            votingMachine.CheckVotingEndCondition = (VotingMachine<NetworkUserId> votingMachine) => 
            {
                if (votingMachine.CheckIfHalfOrMoreVoted())
                {
                    if (!isMajorityTPVotingTimerRunning)
                    {
                        var unlockTime = PluginConfig.MajorityVotesCountdownTime.Value;

                        ChatHelper.TPCountdown(unlockTime);
                        StartCoroutine(majorityTPVotingTimer = WaitAndEndVoting());

                        IEnumerator WaitAndEndVoting()
                        {
                            isMajorityTPVotingTimerRunning = true;
                            yield return new UnityEngine.WaitForSeconds(unlockTime);
                            votingMachine.EndVoting();
                            isMajorityTPVotingTimerRunning = false;
                        }
                    }
                }

                return votingMachine.CheckIfAllVoted() || votingMachine.CheckIfThereIsOnlyOneVoter();
            };

            votingMachine.OnVotingStarted += VotingMachine_OnVotingStarted;
            votingMachine.OnVotingEnded += VotingMachine_OnVotingEnded;
            votingMachine.OnVoterVoted += VotingMachine_OnVoterVoted;

            On.RoR2.CharacterMaster.OnBodyDeath += CharacterMaster_OnBodyDeath;
            On.RoR2.NetworkUser.OnDestroy += NetworkUser_OnDestroy;
            On.RoR2.Chat.SendBroadcastChat_ChatMessageBase += Chat_SendBroadcastChat_ChatMessageBase;
        }

        public void OnDestroy()
        {
            votingMachine.OnVotingStarted -= VotingMachine_OnVotingStarted;
            votingMachine.OnVotingEnded -= VotingMachine_OnVotingEnded;
            votingMachine.OnVoterVoted -= VotingMachine_OnVoterVoted;

            On.RoR2.CharacterMaster.OnBodyDeath -= CharacterMaster_OnBodyDeath;
            On.RoR2.NetworkUser.OnDestroy -= NetworkUser_OnDestroy;
            On.RoR2.Chat.SendBroadcastChat_ChatMessageBase -= Chat_SendBroadcastChat_ChatMessageBase;
        }

        private void VotingMachine_OnVotingStarted()
        {
            OnTPVotingStarted?.Invoke();
        }

        private void VotingMachine_OnVotingEnded()
        {
            if (majorityTPVotingTimer != null)
            {
                StopCoroutine(majorityTPVotingTimer);
            }
            isMajorityTPVotingTimerRunning = false;
            OnTPVotingEnded?.Invoke();
        }

        private void VotingMachine_OnVoterVoted(NetworkUserId voterID)
        {
            var user = NetworkUser.readOnlyInstancesList.FirstOrDefault(l => l.id.Equals(voterID));
            if (user)
            {
                var usersVotes = votingMachine.Votes;
                ChatHelper.UserIsReady(user.userName, usersVotes.Count(ks => ks.Value == true), usersVotes.Count);
            }
        }

        private void CharacterMaster_OnBodyDeath(On.RoR2.CharacterMaster.orig_OnBodyDeath orig, CharacterMaster self, CharacterBody body)
        {
            orig(self, body);

            if (!votingMachine.IsVotingStarted)
            {
                return;
            }

            if (PluginConfig.UserAutoVoteOnDeath.Value && self.IsDeadAndOutOfLivesServer())
            {
                var user = UsersHelper.GetUser(self);
                if (user)
                {
                    votingMachine.Vote(user.id);
                }
            }
        }

        private void NetworkUser_OnDestroy(On.RoR2.NetworkUser.orig_OnDestroy orig, NetworkUser self)
        {
            orig(self);

            if (!votingMachine.IsVotingStarted)
            {
                return;
            }

            votingMachine.RemoveVoter(self.id);
        }

        private void Chat_SendBroadcastChat_ChatMessageBase(On.RoR2.Chat.orig_SendBroadcastChat_ChatMessageBase orig, ChatMessageBase message)
        {
            if (!votingMachine.IsVotingStarted)
            {
                orig(message);
                return;
            }

            if (message is Chat.UserChatMessage userChatMessage)
            {
                var user = userChatMessage.sender.GetComponent<NetworkUser>();
                if (user)
                {
                    var preparedMessage = userChatMessage.text.ToLower().Trim();

                    if (CheckIfReadyMessage(preparedMessage))
                    {
                        if (votingMachine.Vote(user.id))
                        {
                            return;
                        }
                    }
                }
            }

            orig(message);
        }

        private bool CheckIfReadyMessage(string message)
        {
            return PluginConfig.PlayerIsReadyMessages.Value.Split(',').Contains(message);
        }
    }
}
