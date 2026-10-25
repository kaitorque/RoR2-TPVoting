# TPVoting fork – Design and Work Plan

This is the working brief for changing this fork of [Mordrog/RoR2-TPVoting](https://github.com/Mordrog/RoR2-TPVoting) (MIT license). Read it fully before changing code.

## 1. Goal

**Today:** pressing a locked teleporter or portal only casts your vote. Once voting passes, the teleporter unlocks and someone has to press it **a second time**.

**Wanted:** press once. Your press is remembered, and when voting passes, the teleporter (or portal) starts automatically for you. No second press.

The owner uses `VoteAfterTPEvent = true`, so voting also locks the charged teleporter after the boss. The point is to stop players from accidentally leaving the stage while others are still picking boss items (they use the Artifact of Potential, so boss items are pick-1-of-3 orbs).

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
| Owner's config | `config\com.Mordrog.TPVoting.cfg` (`VoteAfterTPEvent = true`, `MajorityVotesCountdownTime = 30`) |
| Decompiled game source | `F:\Mod\RoR2\_decomp` |
| Reference build setup | `F:\Mod\RoR2\MonsoonLikeDrizzle` (csproj + `package.ps1`) |

## 3. How the mod works today

| File | Role |
|---|---|
| `TPVotingPlugin.cs` | Plugin entry. Adds `TPVotingSystem` to each run (except ignored game modes). Binds the config. |
| `Modules/TPVotingSystem.cs` | Starts voting on scene change and (with `VoteAfterTPEvent`) when the teleporter finishes charging. Locks/unlocks via `TPLocker`. A locked interaction attempt casts a vote. |
| `Modules/TPLocker.cs` | Hooks `TeleporterInteraction` and portal `GenericInteraction`s. While locked, `GetInteractability` lets only players who haven't voted interact, and `OnInteractionBegin` raises `OnLockedTPInteractionAttempt` **instead of running the original interaction**. |
| `Modules/TPVotingController.cs` | Voting state. Ends voting when everyone has voted, or 30 s (configurable) after half or more have voted. Chat `r`/`rdy`/`ready` also votes. Dead players auto-vote. |
| `Modules/VotingMachine.cs` | Generic vote bookkeeping. |

The voting-passed path: `VotingMachine.EndVoting` → `TPVotingController.VotingMachine_OnVotingEnded` → `OnTPVotingEnded` → `TPVotingSystem.TPVotingController_OnTPVotingEnded`, which sets `IsTPUnlocked = true` and posts "Teleporter is unlocked".

## 4. Changes

### 4.1 Auto-start after voting passes (main feature)

Decisions from the owner:

- **Trigger:** auto-start when voting passes **either way**: everyone ready, or the majority countdown finishing.
- If nobody pressed the teleporter (everyone voted by chat), behave as today: just unlock.
- If fewer than half are ready, no countdown runs and it stays locked. That is the intended protection; don't change it.

Design:

1. In `TPLocker.OnInteractionBegin`, on the locked path, remember the **first** press as a pending interaction: the interactable (`self`), the `Interactor`, and the `orig` delegate. Later presses still vote but do not replace it.
2. When voting ends (`TPVotingSystem.TPVotingController_OnTPVotingEnded`), after unlocking, run the pending interaction if all of these hold:
   - the interactable and the interactor's GameObject still exist,
   - the interactor's body is alive,
   - the interactable's `GetInteractability(interactor)` returns `Available`.

   Otherwise just unlock as today. Clear the pending interaction either way.
3. Clear the pending interaction whenever voting restarts (`TryStartVoting`) or the scene changes, so a press on one stage never fires on the next.
4. Post a chat line when auto-starting, for example `Teleporter starting for <player>`, using `ChatHelper`'s style.
5. Add a config entry, `AutoStartAfterVote` (bool, default `true`), in `Settings`. When `false`, keep the old press-twice behaviour.

Notes:

- `TeleporterInteraction.OnInteractionBegin` covers both starting the boss event (idle state) and leaving the stage (charged state); both should work through the same pending interaction.
- Portals go through `GenericInteraction`; the same pending logic applies.
- Invoke the remembered `orig` (as `TPLocker` already does with `orig.DynamicInvoke`), not the hooked method, so the lock check doesn't run again.

### 4.2 Fix: non-portal `GenericInteraction` handling (existing bug)

In `TPLocker.GenericInteraction_GetInteractability` and `GenericInteraction_OnInteractionBegin`, the non-portal branch calls `orig(self, activator)` but has **no `return`**, so execution falls through into the vote logic:

- While locked, pressing any non-portal `GenericInteraction` (buttons, fans, etc.) counts as a vote and the original interaction runs as well.
- While unlocked, the original interaction runs **twice**.

Add the missing `return` statements (`return orig(self, activator);` for the interactability hook).

### 4.3 Build setup

- `TPVoting.csproj`: the Release `OutputPath` points at Mordrog's own machine. Remove it, or point it at the VP profile through a `package.ps1` like `MonsoonLikeDrizzle`'s.
- Update `RiskOfRain2.GameLibs` from `1.4.0.0-r.0` to the current game (`1.5.0-r.1`, as used in `MapChests/QoLChests/QoLChests.csproj`) and update `MMHOOK.RoR2` to match.
- **Keep the plugin GUID `com.Mordrog.TPVoting`**, so the owner's existing config keeps working and BepInEx never loads the original alongside the fork. Bump `ModVer` and `manifest.json` to `1.5.0`.
- Install by replacing `plugins\Mordrog-TPVoting\TPVoting.dll` in the VP profile. Back up the original DLL first, as `TPVoting.dll.bak`, outside the plugins folder so BepInEx doesn't scan it.

## 5. Test plan (in game, multiplayer)

- [ ] Press the teleporter before the boss; all players type `r`; the boss event starts with no second press.
- [ ] After the boss (`VoteAfterTPEvent = true`): press the charged teleporter; when everyone is ready, the stage exits automatically.
- [ ] Half ready: the countdown runs, then it auto-starts for the first presser.
- [ ] Fewer than half ready: stays locked, nothing starts.
- [ ] Everyone votes by chat only, no press: it just unlocks.
- [ ] First presser dies or leaves before voting passes: it just unlocks, no error.
- [ ] Portals (shop, gold, celestial, green): same behaviour as the teleporter.
- [ ] Buttons and other non-portal interactions work normally while voting is active (4.2).
- [ ] `AutoStartAfterVote = false` restores press-twice.
