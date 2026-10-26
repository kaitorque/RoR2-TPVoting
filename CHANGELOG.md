# Changelog

## 1.5.0 (Kaitorque fork)

First release of the fork. Behaviour is different from the original mod.

- Voting now protects **leaving the stage** instead of starting the teleporter. The teleporter boss event can be started freely.
- Pressing the charged teleporter or any portal starts a vote for that exit instead of teleporting. Void portals are now included.
- Exits are detected by the game's stage-exit component, so every portal type is covered, not only objects named "portal".
- New `f` chat command (`PlayerCancelMessages`) to take your vote back. The countdown is cancelled if votes drop below half, and the vote ends if nobody is ready.
- When everyone is ready, the stage is left immediately. When the countdown finishes, the exit only unlocks, so someone has to be at the exit to leave.
- Players who are already dead when a vote starts count as ready.
- Fixed buttons and other non-portal interactions counting as votes, or running twice.
- Removed the `VoteAfterTPEvent` setting and the automatic vote at the start of every stage.
- New plugin ID `kaitorque.TPVoting` (config file `kaitorque.TPVoting.cfg`). Will not load if Mordrog's TPVoting is installed.
- Updated for Risk of Rain 2 1.5.0.

## Original mod (Mordrog)

### 1.4.0
- Fix for Alloyed Collective changes

### 1.3.0
- Fix for Seekers of the Storm changes
- Added option for TP voting after TP event ended

### 1.2.3
- Update manifest + rebuild on new patch

### 1.2.2
- Fix for Survivors of the Void changes
- Added option to turn off voting on selected game modes

### 1.2.1
- Fixed issue where players would be auto kicked from dedicated servers if they do not have mod
- Added "outro" stage to ignored Stages for voting
- Added new config to let admin decide whenever players should auto vote on death

### 1.2.0
- Fix for Anniversary Patch changes
- Make portals interaction work same as teleporter interaction
- Players can now vote when they interact with a portal or teleporter for the first time
- Added new Moon stage to ignored Stages for voting

### 1.1.0
- Now works on portals only maps
- Chat messages should not appear for 1 player only

### 1.0.2
- Included all stages without TP to ignored map list
- Fixed activating on interaction items when trying to start locked TP, which made fireworks and squids an actually usefull items. No more.
- Fixed players voting on death on disabled stages

### 1.0.1
- Fixed checking for majority of players
