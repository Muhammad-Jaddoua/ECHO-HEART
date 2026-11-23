# Jehad V1.22 — repair the movement audio namespace import

Open `Assets/_Project/Scenes/Jehad/Jehad_V1.22.unity` in Unity 6000.3.27f1.

The V1.21 movement presenter used WorldCueV15.Jump and WorldCueV15.Land without
importing Echoheart.Audio, where that enum is declared. This is a compilation
blocker that the earlier pure-model/syntax checks missed. The fix adds exactly
one `using Echoheart.Audio;` directive to AirborneFeedbackV121.cs.

The existing script is corrected in place because Unity compiles project scripts
regardless of the open scene. Its GUID, class, fields, methods, movement policy
and cue requests are preserved. The new review scene differs from V1.21 only
in version labels and its own scene GUID. No new runtime component, pool, asset,
scene root, gameplay behavior or player implementation is added. Earlier scene
files are unchanged and use the corrected script when applicable.

## Source audit and evidence

The new NamespaceAudit tool uses the Roslyn parser bundled with .NET SDK 8.
It indexes all 152 project scripts, then checks known Echoheart type-name
references in 116 Jehad scripts under Editor and Player preprocessor symbols.
It distinguishes local event-handler method groups such as the UI Health
handler from the Combat Health type. The audit is a source check, not a Unity
compiler or a full C# symbol binder; nested types, aliases, ambiguous value/type
references and external API compatibility still need compiler/manual review.

Before the fix, the audit reported the two WorldCueV15 references in each
configuration: four candidates, all pointing to the missing Echoheart.Audio
import. After the fix, the audit reports no missing-namespace candidates and
no syntax errors in the audited scripts. A SHA256 digest ties the passing audit
to the current project source set. The audit tool itself compiles with warnings
as errors; it does not compile the Unity game or use Unity stubs.

The scene validator checks 131 YAML documents, local/GUID references, hierarchy,
five inherited prefab instances and one listener. Reversing version labels
reproduces V1.21 exactly. Removing the one added import reproduces the verified
original presenter source. All 152 project source blobs match the inspected
main tree after reversing that import. The 976-file staged baseline changes
only in the presenter and contract log. The scene builder is idempotent.

The V1.21 pure gameplay policy suites were not rerun: their source and all
gameplay method bodies are unchanged. Their prior results remain in V1.21's
evidence and do not establish compilation or runtime behavior.

Not run: Unity import/compilation, native input/physics, audio/particles,
acceptance context actions, standalone playtesting or profiling. No Unity
Editor is connected. This checkpoint fixes the identified namespace issue;
it does not certify that the whole game compiles or is ready for submission.

Evidence: `Jehad_V1.22.validation.json`. Commands and audit limitations:
`Tools/Jehad/V1_22/README.md`.

## Next Unity pass

1. Open V1.22, wait for compilation and inspect the Console. Resolve remaining
   errors before starting native acceptance actions.
2. Play through menus, settings, music transitions and the movement/chest checks
   in the V1.21 and V1.20 notes. Run context actions separately.
3. Inspect terrain collisions, secret access, fall recovery, enemy navigation,
   pause/retry and UI at target resolution. Record observed issues with
   reproduction steps; source checks cannot verify these behaviors.
4. With Tariq's production player integrated, verify real attack sounds/effects,
   inhale, camera/reset bindings and the complete boss-to-Heart ending.
