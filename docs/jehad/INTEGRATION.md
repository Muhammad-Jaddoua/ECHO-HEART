# Production-player and release-scene integration

The inspected main branch still uses JehadFlowExplorerV13, a movement-only
review capsule. This document specifies the handoff; it does not claim that
Tariq's player or the final release scene has been integrated.

| Boundary | Required binding/check |
|---|---|
| PlayerFlowBindingV13 `_player` / `_health` | Assign the production root and its actual player Health |
| `_gameplayInput` | Assign motor/input/inhale/attack input behaviours that flow must disable outside Playing |
| `_onRespawned` | Reset motor velocity and snap/reset the production camera after the existing teleport |
| `_clearAbilityRequested` | Call the real holder's clear method |
| `_equipAbilityRequested(AbilityDefinition)` | Call the real holder's equip method |
| `_publishReviewAbilityChanges` | Turn off when the production holder owns AbilityChanged publishing |
| HealthEventRelay | Keep exactly one Player-mode relay on player Health; do not duplicate HUD/death publishers |
| Collider/layers | Player body on Player layer (8); preserve existing attack/environment masks |
| Flow `_camera` | Assign the actual gameplay camera transform; retain one AudioListener |
| SwordAttack `_onSwing` | Bind successful swings to WorldFeedbackV15.SwordUsed |
| ProjectileAttack `_onFired` | Bind successful Bow/Fire shots to WorldFeedbackV15.RangedUsed |
| Feedback player references | Rebind audio, ambient particles, objective guide and glints to the production root/camera where needed |
| Movement feedback | Recheck CharacterController grounded/velocity sampling against the actual motor |
| Hurt anchor | Recheck the Health transform and 1 m body anchor for the production model |
| Title camera | Keep the scenic title owner limited to Title; gameplay camera takes over in Playing |

The attack hooks are parameterless UnityEvents in the inspected combat sources.
They should follow accepted attacks, so cooldown-rejected clicks do not play
false attack effects. The holder remains the ability owner and the attack
components retain cooldown/damage ownership.

Preserve the original enemy inhale protocol, spawner reset ownership and boss
event bridge. The Guardian is not inhalable. Arena retry must re-arm the real
encounter; the Heart becomes usable only after real boss defeat. Relics persist
through area retries and reset on New Game.

The integrator should transfer the chosen world/presentation bindings into the
release scene, replace the capsule/review HUD as appropriate, and run the full
acceptance route. Keep gameplay objects outside EditorOnly review roots; those
roots are stripped in builds. The current checkpoint preserves the original
`Echoheart.unity` and build settings so this transfer remains explicit.

Complete the release input-driven, including all three powers and repeated
retry/completion. Direct Health damage in development actions verifies contract
paths only; it does not demonstrate production attacks, aim, input or balance.
