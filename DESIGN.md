# TPVoting fork – Design and Work Plan

This is the working brief for changing this fork of [Mordrog/RoR2-TPVoting](https://github.com/Mordrog/RoR2-TPVoting) (MIT license). Read it fully before changing code.

## 1. Goal

Stop players from **accidentally leaving the stage** after the boss is cleared, while others are still picking up boss items. The owner plays with 4 players and the Artifact of Potential, so boss items are pick-1-of-3 orbs that take time to choose.

The teleporter boss event itself should **not** need a vote; anyone can start it freely, like vanilla.

Keep someone near the exit: voting only unlocks the exit, and a player still presses it to leave. The only exception is when everyone readies up before the countdown ends.

### Constraints

- **Local only.** Build and test in the owner's r2modman profile. Do not push to any remote, and do not publish.
- **Keep credit.** The upstream history is preserved; keep `LICENSE` (MIT, Mordrog) and credit Mordrog in `README.md`.
- **Host-only mod.** The plugin is marked `NetworkCompatibility.NoNeedForSync`; only the host needs it. Keep it that way.
- **Ask before adding scope.**

## 2. Environment

| Item | Value |
|---|---|
| OS / shell | Windows, PowerShell |
| .NET SDK | 10.0.203 |
| Game | Risk of Rain 2 1.5.0, at `E:\SteamLibrary\steamapps\common\Risk of Rain 2` |
| r2modman profile | `%APPDATA%\r2modmanPlus-local\RiskOfRain2\profiles\VP\BepInEx` |
| Installed original | `plugins\Mordrog-TPVoting\TPVoting.dll` (1.4.0) |
| Owner's config | `config\com.Mordrog.TPVoting.cfg` (`VoteAfterTPEvent = true`, `MajorityVotesCountdownTime = 30`, `PlayerIsReadyMessages = r,rdy,ready`, `UserAutoVoteOnDeath = true`) |
| Decompiled game source | `F:\Mod\RoR2\_decomp` |
| Reference build setup | `F:\Mod\RoR2\MonsoonLikeDrizzle` (csproj + `package.ps1`) |

## 3. How the mod works today

| File | Role |
|---|---|
| `TPVotingPlugin.cs` | Plugin entry. Adds `TPVotingSystem` to each run (except ignored game modes). Binds the config. |
| `Modules/TPVotingSystem.cs` | Starts voting **automatically** on scene change and (with `VoteAfterTPEvent`) when the teleporter finishes charging. Locks/unlocks via `TPLocker`. A locked interaction attempt casts a vote. |
| `Modules/TPLocker.cs` | Hooks `TeleporterInteraction` and portal `GenericInteraction`s (name contains `portal`). While locked, `GetInteractability` lets only players who haven't voted interact, and `OnInteractionBegin` raises `OnLockedTPInteractionAttempt` **instead of running the original interaction**. One global lock for all exits. |
| `Modules/TPVotingController.cs` | Voting state. Ends voting when everyone has voted, or `MajorityVotesCountdownTime` seconds after half or more have voted. Chat `r`/`rdy`/`ready` votes. Dead players auto-vote. |
| `Modules/VotingMachine.cs` | Generic vote bookkeeping. No way to remove a vote yet. |
| `PluginGlobals.cs` | Stages where voting never applies (`arena`, `moon`, `moon2`, `limbo`, `outro`, `voidraid`). |

Today, voting ending simply unlocks everything, and someone must press the exit again.

## 4. New behaviour

### 4.1 What is protected

- **Owner's rule: anything that takes the players to a new stage is protected.** That includes the teleporter once charged (pressing it leaves the stage), every portal (shop/blue, gold, celestial, void, green, Colossus, and DLC portals), and any other interactable that starts a stage change. Void portals are included, although upstream excludes them on purpose ("void is intentionally excluded" in the README).
- **Detection: use the game's stage-change component, not names.** Every stage change goes through `SceneExitController.Begin()` (`_decomp\RoR2\SceneExitController.cs`). Treat an interactable as a stage exit when its GameObject (or its parent/children) has a `SceneExitController`. The teleporter owns one, and portal prefabs do too. Replace the current `name.Contains("portal")` check with this. The current hooks only cover `TeleporterInteraction` and `GenericInteraction`, so also check the other interactable types that lead to an exit (for example `MeridianEventTriggerInteraction`, `SceneExitControllerColossusPortal`, `PortalDialerController`; search `_decomp` for `SceneExitController` usages), and log the name of each exit the mod protects so missing ones are easy to spot in testing.
- Interactables that only *choose* the next destination without leaving (for example `SeerStationController`) are not exits.
- **Not protected:** starting the teleporter boss event (teleporter idle state). Remove the automatic voting on scene change. `VoteAfterTPEvent` no longer applies; remove it from the config.
- Keep `PluginGlobals.IgnoredStages`, `IgnoredGameModes`, and the single-player bypass.

### 4.2 Flow

1. **Press to start.** The first player to press a protected exit starts a vote **for that exit** (the vote's *target*). That press counts as their vote. Nothing teleports. Post a chat line naming the player and the target, and explaining `r` / `f`.
2. **Join or leave the vote.**
   - Type `r` (`PlayerIsReadyMessages`) or press the target exit → vote ready.
   - Type `f` (new `PlayerCancelMessages`, default `f`) → remove your vote.
   - Dead players auto-vote (`UserAutoVoteOnDeath`), as today.
   - Post the ready count after every change, as today.
3. **Half or more ready → countdown.** Start the `MajorityVotesCountdownTime` (30 s) countdown, as today.
   - If votes drop **below half** (players typed `f`), **cancel the countdown** and say so in chat.
   - If votes drop to **zero**, **end the vote**: clear the target; the next press on any protected exit starts a new vote.
4. **Everyone ready before the countdown ends → leave immediately.** Run the target's original interaction for the **player who started the vote**, with no extra press. If that player is gone, dead, or can't use the target (`GetInteractability` isn't `Available`), use another ready player who is alive; if none qualify, just unlock the target (as in step 5).
5. **Countdown finishes → unlock the target only.** Nobody is teleported. Any player can now press the target to leave, so someone has to be at the exit. Post a chat line saying the exit is unlocked.
6. **Pressing a different exit during a vote** does not start a second vote. Tell the player a vote is already running for the target, and that typing `f` cancels it.
7. Reset all vote state on scene change.

### 4.3 Implementation notes

- Locking becomes **per exit**: while a vote runs, the target behaves as "press = vote"; other exits are blocked with the message from step 4.2.6. After the countdown unlock, only the target is unlocked. Pressing another exit later starts a new vote for it.
- `VotingMachine` needs an unvote operation, and the end-condition check must handle counts going down (countdown cancel, vote reset).
- For step 4.2.4, remember the original-interaction delegate (`orig`) and the `Interactor` of the first press, as `TPLocker.OnInteractionBegin` already receives them, and invoke `orig` directly so the lock check doesn't run again.
- `TeleporterInteraction.OnInteractionBegin` handles both starting the event and leaving the stage. Only the charged state (leaving) is protected; check the teleporter's state (`activationState` / `isCharged`) in `_decomp\RoR2\TeleporterInteraction.cs`.
- Chat matching for `f` reuses `CheckIfReadyMessage`'s approach; swallow the message like `r`.

### 4.4 Fix: non-portal `GenericInteraction` handling (existing bug)

In `TPLocker.GenericInteraction_GetInteractability` and `GenericInteraction_OnInteractionBegin`, the non-portal branch calls `orig(self, activator)` but has **no `return`**, so execution falls through into the vote logic:

- While locked, pressing any non-portal `GenericInteraction` (buttons, fans, etc.) counts as a vote and the original interaction runs as well.
- While unlocked, the original interaction runs **twice**.

Add the missing `return` statements (`return orig(self, activator);` for the interactability hook).

### 4.5 Config

| Setting | Default | Notes |
|---|---|---|
| `PlayerIsReadyMessages` | `r,rdy,ready` | Existing |
| `PlayerCancelMessages` | `f` | New; comma-separated |
| `MajorityVotesCountdownTime` | `30` | Existing |
| `UserAutoVoteOnDeath` | `true` | Existing |
| `IgnoredGameModes` | `InfiniteTowerRun` | Existing |
| `VoteAfterTPEvent` | – | Remove; exits are always protected, the boss event never is |

### 4.6 Build setup

- `TPVoting.csproj`: the Release `OutputPath` points at Mordrog's own machine. Remove it, and add a `package.ps1` like `MonsoonLikeDrizzle`'s that builds and copies into the VP profile.
- Update `RiskOfRain2.GameLibs` from `1.4.0.0-r.0` to the current game (`1.5.0-r.1`, as used in `F:\Mod\RoR2\MapChests\QoLChests\QoLChests.csproj`) and update `MMHOOK.RoR2` to match.
- **Keep the plugin GUID `com.Mordrog.TPVoting`**, so the owner's existing config keeps working and BepInEx never loads the original alongside the fork. Bump `ModVer` and `manifest.json` to `1.5.0`.
- Install by replacing `plugins\Mordrog-TPVoting\TPVoting.dll` in the VP profile. Back up the original DLL first, as `TPVoting.dll.bak`, outside the plugins folder so BepInEx doesn't scan it.

## 5. Test plan (in game, 4 players if possible)

- [ ] Start the boss event: no vote, starts immediately.
- [ ] Press the charged teleporter: a vote starts, nobody teleports.
- [ ] Second player types `r` (2 of 4): countdown starts.
- [ ] One of them types `f` (1 of 4): countdown cancels.
- [ ] Both type `f` (0 of 4): vote ends; pressing a portal now starts a new vote for the portal.
- [ ] Everyone ready before 30 s: stage exits immediately with no extra press.
- [ ] Countdown finishes with 2–3 ready: target unlocks, nobody moves; pressing it leaves.
- [ ] During a vote, pressing a different exit shows the "vote already running" message.
- [ ] Vote starter dies before everyone is ready: everyone ready still exits via another player, or just unlocks.
- [ ] Portals (shop, gold, celestial, void, green): same flow.
- [ ] Buttons and other non-portal interactions work normally during a vote (4.4).
- [ ] Single player: no voting at all.
