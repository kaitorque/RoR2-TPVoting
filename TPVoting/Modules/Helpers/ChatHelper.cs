namespace TPVoting
{
    public static class ChatHelper
    {
        private const string GrayColor = "7e91af";
        private const string RedColor = "ff0000";
        private const string YellowColor = "ffff00";
        private const string GreenColor = "32cd32";
        private const string SilverColor = "c0c0c0";

        private static string ReadyMessage => FirstEntry(PluginConfig.PlayerIsReadyMessages.Value, "r");
        private static string CancelMessage => FirstEntry(PluginConfig.PlayerCancelMessages.Value, "f");

        private static string FirstEntry(string commaSeparated, string fallback)
        {
            var first = commaSeparated.Split(',')[0].Trim();
            return string.IsNullOrEmpty(first) ? fallback : first;
        }

        private static void Send(string message)
        {
            RoR2.Chat.SendBroadcastChat(new RoR2.Chat.SimpleChatMessage { baseToken = message });
        }

        private static string Exit(string exitName) => $"<color=#{RedColor}>{exitName}</color>";
        private static string Command(string command) => $"<color=#{SilverColor}>\"{command}\"</color>";
        private static string Count(int numberOfUsersWhoVoted, int numberOfVotingUsers) => $"<color=#{GrayColor}>({numberOfUsersWhoVoted}/{numberOfVotingUsers})</color>";

        public static void VotingStarted(string userName, string exitName, int numberOfUsersWhoVoted, int numberOfVotingUsers)
        {
            Send($"<color=#{GreenColor}>{userName}</color> wants to leave through {Exit(exitName)}. Write {Command(ReadyMessage)} or press it to ready up, {Command(CancelMessage)} to cancel your vote. {Count(numberOfUsersWhoVoted, numberOfVotingUsers)}");
        }

        public static void UserIsReady(string userName, int numberOfUsersWhoVoted, int numberOfVotingUsers)
        {
            Send($"<color=#{GreenColor}>{userName}</color> is ready! {Count(numberOfUsersWhoVoted, numberOfVotingUsers)}");
        }

        public static void UserIsNotReady(string userName, int numberOfUsersWhoVoted, int numberOfVotingUsers)
        {
            Send($"<color=#{YellowColor}>{userName}</color> is no longer ready. {Count(numberOfUsersWhoVoted, numberOfVotingUsers)}");
        }

        public static void ExitUnlocked(string exitName)
        {
            Send($"{Exit(exitName)} is unlocked! <color=#{GreenColor}>Press it to leave.</color>");
        }

        public static void AllReady(string exitName)
        {
            Send($"<color=#{GreenColor}>Everyone is ready!</color> Leaving through {Exit(exitName)}.");
        }

        public static void PlayersNotReady(string exitName)
        {
            Send($"<color=#{RedColor}>Players are not ready</color> to leave through {Exit(exitName)}. Vote by writing {Command(ReadyMessage)} in chat.");
        }

        public static void VoteAlreadyRunning(string exitName)
        {
            Send($"A vote to leave through {Exit(exitName)} is already running. Write {Command(CancelMessage)} to cancel your vote.");
        }

        public static void VotingCancelled()
        {
            Send($"<color=#{YellowColor}>Nobody is ready, vote ended.</color>");
        }

        public static void TPCountdown(uint unlockTime)
        {
            Send($"Starting exit unlock <color=#{YellowColor}>countdown!</color> <color=#{GrayColor}>({unlockTime} seconds left)</color>");
        }

        public static void TPCountdownCancelled()
        {
            Send($"<color=#{YellowColor}>Less than half are ready, countdown cancelled.</color>");
        }
    }
}
