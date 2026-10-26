using UnityEngine.Networking;
using RoR2;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

namespace TPVoting
{
    public enum TPVotingResult
    {
        Reset,
        Cancelled,
        AllReady,
        CountdownFinished
    }

    public class TPVotingController : NetworkBehaviour
    {
        private VotingMachine<NetworkUserId> votingMachine = new VotingMachine<NetworkUserId>();
        private IEnumerator majorityTPVotingTimer;
        private bool isMajorityTPVotingTimerRunning;
        private TPVotingResult votingResult;

        public delegate void TPVotingEnded(TPVotingResult result);
        public event TPVotingEnded OnTPVotingEnded;

        public bool IsVotingStarted()
        {
            return votingMachine.IsVotingStarted;
        }

        public bool HasUserVoted(NetworkUser user)
        {
            return user && votingMachine.CheckIfVoted(user.id);
        }

        public IEnumerable<NetworkUser> GetReadyUsers()
        {
            return NetworkUser.readOnlyInstancesList.Where(HasUserVoted);
        }

        public bool Vote(NetworkUser user)
        {
            return user && votingMachine.IsVotingStarted && votingMachine.Vote(user.id);
        }

        public void StartVoting(NetworkUser starter, string targetName)
        {
            if (votingMachine.IsVotingStarted)
            {
                return;
            }

            var initialVoters = new List<NetworkUserId> { starter.id };
            if (PluginConfig.UserAutoVoteOnDeath.Value)
            {
                initialVoters.AddRange(NetworkUser.readOnlyInstancesList.Where(u => u.master && u.master.IsDeadAndOutOfLivesServer()).Select(u => u.id));
            }

            var users = NetworkUser.readOnlyInstancesList;
            ChatHelper.VotingStarted(starter.userName, targetName, users.Count(u => initialVoters.Contains(u.id)), users.Count);

            votingMachine.StartVoting(users.Select(u => u.id).ToList(), initialVoters);
        }

        public void EndVoting()
        {
            if (votingMachine.IsVotingStarted)
            {
                votingResult = TPVotingResult.Reset;
                votingMachine.EndVoting();
            }
        }

        public void Awake()
        {
            votingMachine.CheckVotingEndCondition = (VotingMachine<NetworkUserId> votingMachine) =>
            {
                if (votingMachine.CountVotes() == 0)
                {
                    votingResult = TPVotingResult.Cancelled;
                    return true;
                }

                if (votingMachine.CheckIfAllVoted())
                {
                    votingResult = TPVotingResult.AllReady;
                    return true;
                }

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
                            isMajorityTPVotingTimerRunning = false;
                            votingResult = TPVotingResult.CountdownFinished;
                            votingMachine.EndVoting();
                        }
                    }
                }
                else if (isMajorityTPVotingTimerRunning)
                {
                    StopMajorityTPVotingTimer();
                    ChatHelper.TPCountdownCancelled();
                }

                return false;
            };

            votingMachine.OnVotingEnded += VotingMachine_OnVotingEnded;
            votingMachine.OnVoterVoted += VotingMachine_OnVoterVoted;
            votingMachine.OnVoterUnvoted += VotingMachine_OnVoterUnvoted;

            On.RoR2.CharacterMaster.OnBodyDeath += CharacterMaster_OnBodyDeath;
            On.RoR2.NetworkUser.OnDestroy += NetworkUser_OnDestroy;
            On.RoR2.Chat.SendBroadcastChat_ChatMessageBase += Chat_SendBroadcastChat_ChatMessageBase;
        }

        public void OnDestroy()
        {
            votingMachine.OnVotingEnded -= VotingMachine_OnVotingEnded;
            votingMachine.OnVoterVoted -= VotingMachine_OnVoterVoted;
            votingMachine.OnVoterUnvoted -= VotingMachine_OnVoterUnvoted;

            On.RoR2.CharacterMaster.OnBodyDeath -= CharacterMaster_OnBodyDeath;
            On.RoR2.NetworkUser.OnDestroy -= NetworkUser_OnDestroy;
            On.RoR2.Chat.SendBroadcastChat_ChatMessageBase -= Chat_SendBroadcastChat_ChatMessageBase;
        }

        private void StopMajorityTPVotingTimer()
        {
            if (majorityTPVotingTimer != null)
            {
                StopCoroutine(majorityTPVotingTimer);
                majorityTPVotingTimer = null;
            }
            isMajorityTPVotingTimerRunning = false;
        }

        private void VotingMachine_OnVotingEnded()
        {
            StopMajorityTPVotingTimer();
            OnTPVotingEnded?.Invoke(votingResult);
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

        private void VotingMachine_OnVoterUnvoted(NetworkUserId voterID)
        {
            var user = NetworkUser.readOnlyInstancesList.FirstOrDefault(l => l.id.Equals(voterID));
            if (user)
            {
                var usersVotes = votingMachine.Votes;
                ChatHelper.UserIsNotReady(user.userName, usersVotes.Count(ks => ks.Value == true), usersVotes.Count);
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
                    else if (CheckIfCancelMessage(preparedMessage))
                    {
                        if (votingMachine.Unvote(user.id))
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

        private bool CheckIfCancelMessage(string message)
        {
            return PluginConfig.PlayerCancelMessages.Value.Split(',').Contains(message);
        }
    }
}
