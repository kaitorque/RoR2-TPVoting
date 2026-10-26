# TPVoting (Kaitorque fork)

Stops players from **accidentally leaving the stage** while others are still picking up boss items.

This is a fork of [TPVoting by Mordrog](https://github.com/Mordrog/RoR2-TPVoting), used under the MIT license (see `LICENSE`). All credit for the original mod goes to Mordrog.

**This fork works differently from the original.** The original locks the teleporter at the start of every stage until players vote. This fork leaves the teleporter boss event alone and only protects the moment you **leave the stage**: the charged teleporter, and every portal. Do not install both; the fork refuses to load alongside the original.

Only the host needs this mod.

## How it works

1. **Starting the boss event is free.** Anyone can activate the teleporter as in vanilla.
2. **Pressing a stage exit starts a vote.** When someone presses the charged teleporter or a portal, nobody teleports. Instead, a vote starts for that exit, and the press counts as their vote.
3. **Join or leave the vote.** Write `r` in chat, or press the same exit, to ready up. Write `f` to take your vote back. Dead players count as ready.
4. **Half or more ready → countdown.** A countdown starts (30 seconds by default). If votes drop below half, the countdown is cancelled. If nobody is ready any more, the vote ends.
5. **Everyone ready → leave immediately.** The exit runs at once, with no extra press.
6. **Countdown finished → exit unlocked.** Nobody is teleported; someone still has to press the exit to leave. The exit stays unlocked for the rest of the stage.

While a vote is running, pressing a different exit only shows a reminder that a vote is already running. Votes reset on every new stage.

**Protected exits:** the charged teleporter and every portal (shop, gold, celestial, void, green, Colossus, DLC portals, and anything else that leads to a new stage). Voting is skipped in single player, on final/special stages (Commencement, the Void Fields, A Moment, Whole, the Planetarium, the ending), and in Simulacrum.

## Config (`kaitorque.TPVoting.cfg`)

| Setting | Default | Description |
|---|---|---|
| PlayerIsReadyMessages | `r,rdy,ready` | Chat messages that ready you up (comma-separated) |
| PlayerCancelMessages | `f` | Chat messages that take your vote back (comma-separated) |
| MajorityVotesCountdownTime | `30` | Seconds until the exit unlocks once half or more are ready |
| UserAutoVoteOnDeath | `true` | Dead players count as ready |
| IgnoredGameModes | `InfiniteTowerRun` | Game modes where voting is off |

## Credits

- Original mod: [Mordrog](https://thunderstore.io/package/Mordrog/) ([source](https://github.com/Mordrog/RoR2-TPVoting))
- Fork: Kaitorque ([source](https://github.com/kaitorque/RoR2-TPVoting))
- Icon: the in-game teleporter icon from Risk of Rain 2

See `CHANGELOG.md` for changes.
