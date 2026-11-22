# Jehad V1.10 — queued discovery and boss notifications

Open **`Assets/_Project/Scenes/Jehad/Jehad_V1.10.unity`** in Unity 6000.3.27f1.

## Changes to review

- Relic, chest and marked-secret messages queue instead of overwriting each
  other. Ordinary discoveries keep their arrival order. The card shows a
  category icon, heading, readable message, pending count and reading-time bar.
- Discovery messages receive 3.2 seconds of reading time. Boss objectives
  receive 4.4 seconds and interrupt discoveries, retaining the interrupted
  message's remaining time. Boss defeat removes the obsolete fight notice
  before presenting the Island Heart objective.
- Pause hides the card and freezes reading time. Resume continues it. Retry,
  death, Title and completion clear active/pending content. A long frame
  expires at most the current message, giving the next message its reading time.
- Duplicate keys are suppressed while active or pending. The total queue is
  capped at 32. At capacity, ordinary new messages are rejected; an objective
  can replace a lower-priority entry. The authored run has at most 22 distinct
  notices: 16 relics, two chests, two marked secret locations and two boss
  objectives. Retry clears the queue before another encounter.
- The 820×104 card keeps the V1.9 safe-area/HUD surface and prompt gap. High
  contrast is retained. Reduced UI motion removes the fade and reading-time
  bar while preserving reading duration. Notifications do not capture input.

## Ownership and reset behavior

WorldToastV110 replaces the old toast and owns its queue/presentation.
WorldNoticesV110 observes the existing relic flags and chest-open states and
listens to existing boss/state/retry events. Its snapshots refresh after the
flow publishes Playing at the end of a retry transaction, so retained relics
and opened chests do not replay notifications on retry.

The two SecretDiscoveryV110 scene components retain the V1.5 location/reset
rules and add only their notification call. Their audio/particles still go
through WorldFeedbackV15.Discover. The existing feedback component's optional
toast field is cleared in this scene so it does not publish duplicate text.
Its health/enemy/footstep/relic sound and particle logic remains unchanged.

Pickups and SecretChestV15 still own collection and chest rewards. The two
chest controllers/prefabs, all 16 relics, audio bank/music/SFX, particles,
population, Guardian, Heart and session flow are reused. The main menus/HUD
remain V1.9. The UI harness's typed notification reference is updated; the
earlier flow and world-polish harnesses remain compatible.

No shared Core, teammate player/combat/AI, previous scene, release scene,
build setting, package or asset-credit file changes. No imported assets or
save system. Production player/ability integration remains outstanding; the
scene still uses the movement-only review player.

## Unity review

1. Collect nearby relics and open both chests. Trigger the Mushroom Circle
   and Hidden Spring discoveries. Confirm each notification can be read and
   that sound/particles occur once at the interaction, independently of when
   queued text becomes visible. If a chest and its relic trigger together,
   the chest notice should precede its relic notice.
2. Enter the Guardian arena while a discovery is showing. Confirm the boss
   objective interrupts it and the discovery resumes with its remaining time.
   Defeat the Guardian and check that the fight notice is removed and the
   Heart interaction objective appears. Restoring the Heart clears notices
   while the completion screen takes over.
3. Pause with active/pending messages, wait several seconds, then resume.
   Repeat with area retry, death, Return to Title and New Game. Confirm no
   retained relic/chest awards or discovery sounds replay on retry.
4. Review at 16:9, ultrawide, 4:3 and with safe-area insets. Test HUD scales
   85%, 100% and 115%, high contrast and reduced UI motion. Inspect two-line
   text, pending counts, icons and separation from the interaction prompt.
5. In Play mode select **Jehad V1.10 UI acceptance harness** and run **World
   feedback → Run notification checks (resets run)**. It checks queue display,
   paused reading time, objective interruption, bounds, retry and Title clear.
   It uses synthetic notices without awarding items or starting a boss.
   Separately run **UI review → Run navigation and settings checks**, then the
   reused flow/world/population harnesses. Actual interactions still require
   a playtest; a synthetic notice is not end-to-end gameplay evidence.

## Verification

**Passed:** 111 assertions compile the actual queue and shared layout model
under C# 9.0/.NET 8 with warnings as errors. Coverage includes FIFO order,
duplicate suppression, invalid/paused time, priority/preemption, interrupted
reading time, stale-objective removal, long frames, capacity/overflow, all 22
authored notices and the larger card's bounds/prompt gap at supported HUD
scales. The existing 335 layout and 1,586 menu/focus/settings regressions pass.

Static checks cover five new C# syntax trees, 115 scene YAML documents,
GUID/component/serialized-field references, the single notification owner,
the disabled legacy text path and unchanged secret-rule source. Only the six
declared presentation bindings/reference changes and version labels differ
from V1.9. The builder is idempotent. The 744-file staged baseline changes only
in the contract log; publication also checks every prior remote blob outside it.

**Not run:** Unity compilation/import, actual rendering/text fit, input,
audio playback, native display switching, supplied harnesses and gameplay/
performance checks. No Unity Editor is connected. Pure C# and static checks
do not prove engine behavior or final-submission readiness.

Evidence: `Jehad_V1.10.validation.json`. Commands: `Tools/Jehad/V1_10/README.md`.
Next: inspect notification timing with actual chest/relic/boss interactions,
then tune audio/VFX and presentation from Unity playtest findings.
