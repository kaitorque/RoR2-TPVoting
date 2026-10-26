using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace TPVoting
{
    public class VotingMachine<VoterID>
    {
        private Dictionary<VoterID, bool> votes = new Dictionary<VoterID, bool>();
        public IReadOnlyDictionary<VoterID, bool> Votes => new ReadOnlyDictionary<VoterID, bool>(votes);

        public bool IsVotingStarted { get; private set; }

        public delegate bool VotingEndCondition(VotingMachine<VoterID> votingMachine);
        public VotingEndCondition CheckVotingEndCondition{ get; set; } = (VotingMachine<VoterID> votingMachine) => { return votingMachine.CheckIfAllVoted(); };

        public delegate void VotingUpdated();
        public event VotingUpdated OnVotingUpdated;

        public delegate void VotingStarted();
        public event VotingStarted OnVotingStarted;

        public delegate void VotingEnded();
        public event VotingEnded OnVotingEnded;

        public delegate void VoterVoted(VoterID voterID);
        public event VoterVoted OnVoterVoted;

        public delegate void VoterUnvoted(VoterID voterID);
        public event VoterUnvoted OnVoterUnvoted;

        public delegate void VoterRemoved(VoterID voterID);
        public event VoterRemoved OnVoterRemoved;

        public void StartVoting(IReadOnlyCollection<VoterID> voterIDs, IEnumerable<VoterID> initialVoterIDs = null)
        {
            votes.Clear();
            foreach (var voterID in voterIDs)
            {
                votes[voterID] = false;
            }

            if (initialVoterIDs != null)
            {
                foreach (var voterID in initialVoterIDs.Where(votes.ContainsKey))
                {
                    votes[voterID] = true;
                }
            }

            IsVotingStarted = true;
            OnVotingStarted?.Invoke();
            PostVotingUpdate();
        }

        public void EndVoting()
        {
            if (!IsVotingStarted)
            {
                UnityEngine.Debug.LogWarning("VotingMachine::EndVoting: Trying to end voting while there is no voting started");
                return;
            }

            votes.Clear();
            IsVotingStarted = false;
            OnVotingEnded?.Invoke();
            PostVotingUpdate();
        }

        public bool Vote(VoterID voterID)
        {
            if (!IsVotingStarted)
            {
                UnityEngine.Debug.LogWarning("VotingMachine::Vote: Trying to set vote while voting is not started");
                return false;
            }

            if (!votes.TryGetValue(voterID, out bool vote))
            {
                UnityEngine.Debug.LogWarning($"VotingMachine::Vote: Failed to find voter ID {voterID} in voter list");
                return false;
            }

            if (vote)
            {
                return false;
            }

            votes[voterID] = true;

            OnVoterVoted?.Invoke(voterID);
            PostVotingUpdate();
            return true;
        }

        public bool Unvote(VoterID voterID)
        {
            if (!IsVotingStarted)
            {
                return false;
            }

            if (votes.TryGetValue(voterID, out bool vote) && vote)
            {
                votes[voterID] = false;

                OnVoterUnvoted?.Invoke(voterID);
                PostVotingUpdate();
                return true;
            }

            return false;
        }

        public bool RemoveVoter(VoterID voterID)
        {
            if (!IsVotingStarted)
            {
                return false;
            }

            if (votes.ContainsKey(voterID))
            {
                votes.Remove(voterID);

                OnVoterRemoved?.Invoke(voterID);
                PostVotingUpdate();
                return true;
            }

            return false;
        }

        private void PostVotingUpdate()
        {
            OnVotingUpdated?.Invoke();

            if (IsVotingStarted && CheckVotingEndCondition(this))
            {
                EndVoting();
            }
        }

        public bool CheckIfVoted(VoterID voterID)
        {
            return votes.TryGetValue(voterID, out bool vote) && vote;
        }

        public int CountVotes()
        {
            return votes.Count(kv => kv.Value == true);
        }

        public bool CheckIfAllVoted()
        {
            return votes.All(kv => kv.Value == true);
        }

        public bool CheckIfThereIsOnlyOneVoter()
        {
            return votes.Count == 1;
        }

        public bool CheckIfHalfOrMoreVoted()
        {
            return CountVotes() >= UnityEngine.Mathf.CeilToInt((float)votes.Count / 2);
        }
    }
}
