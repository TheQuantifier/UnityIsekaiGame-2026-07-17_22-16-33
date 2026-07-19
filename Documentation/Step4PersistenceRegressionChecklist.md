# Step 4 Persistence Regression Checklist

Use this checklist for the final Step 4 manual closeout. Step 4 should not be tagged until this passes.

## Automated Validation

Run:

1. `git status --short`
2. `git diff --check`
3. `dotnet build Assembly-CSharp.csproj`
4. `dotnet build UnityIsekaiGame.EditModeTests.csproj`
5. `dotnet format --verify-no-changes Assembly-CSharp.csproj`
6. Unity batchmode import/compile.
7. All available EditMode tests from Unity Test Runner.
8. Definition validation from `Tools > Game Data > Validate Definitions`.
9. World-entity validation from `Tools > World Entities > Validate Current Scene`.
10. Duplicate `.meta` GUID scan.
11. Missing script/reference scan.
12. Generated save/temp/quarantine/log artifact scan.

If Unity batchmode exits without test XML, do not count that as tests passed. Run tests in the Unity Test Runner and record the result.

## End-To-End Manual Sequence

1. Open `Assets/Scenes/PrototypeScene.unity`.
2. Run definition validation.
3. Run world-entity validation.
4. Run all EditMode tests.
5. Enter Play Mode.
6. Create a complex player state:
   - collect stackable items;
   - collect separate stateful equipment instances;
   - equip items;
   - damage the player;
   - spend Mana and Stamina;
   - apply a timed status;
   - apply a persistent status;
   - start and progress a quest;
   - accept and progress a contract;
   - move to a distinct place and orientation.
7. Open Tab menu > `Test Lab` > Persistence and click `Fingerprint`.
8. Save to `Manual Save 1`.
9. Record the fingerprint and visible state.
10. Mutate every listed system.
11. Load `Manual Save 1`.
12. Confirm fingerprint and visible state match the saved state.
13. Load the same slot five times.
14. Confirm no duplication, drift, repeated rewards, extra listeners, or extra UI rows.
15. Claim one available reward and save.
16. Load and confirm the reward cannot duplicate.
17. Trigger autosaves until all three generations exist.
18. Corrupt newest autosave.
19. Confirm an older generation remains usable and clearly listed.
20. Corrupt `Manual Save 1` primary.
21. Confirm normal load fails without silently using the backup.
22. Validate and load backup explicitly.
23. Promote backup.
24. Confirm primary validates.
25. Inject prepare failure.
26. Confirm live state and fingerprint remain unchanged.
27. Inject commit failure.
28. Confirm rollback is reported honestly.
29. Test blocked-position fallback.
30. Confirm fallback is reported and dirty-state behavior matches documentation.
31. Test reset then load.
32. Verify Test Lab diagnostics.
33. Verify Save/Load page at 1280x720, 1920x1080, 2560x1440, and 3440x1440.
34. Confirm no duplicate rows, listeners, services, or registry entries.
35. Exit Play Mode.
36. Confirm no save files, temp files, quarantine files, test XML, logs, or unrelated scene changes are staged.

## Focus Areas

- Basic save/load: create save, mutate state, load, verify exact player restoration.
- Inventory/equipment: stacks, stateful instances, instance IDs, quality, condition, full inventory, equipment modifiers.
- Vitals/statuses: health, mana, stamina, timed status, persistent status, stacking, clamping, repeated load.
- Quests/contracts: active quest, mid-stage progress, stable objective IDs, contract progress, completion, unclaimed rewards, claimed rewards, no duplicate listeners or rewards.
- Location: exact position, rotation, place, blocked-position fallback, Reach Location suppression.
- Slots/autosave: manual save, overwrite, delete, autosave rotation, dirty warning, play time, timestamps.
- Recovery: corrupt primary, valid backup, backup load, backup promotion, corrupt autosave, stale temp, interrupted marker, quarantine, rollback.
- Identity: duplicate authored-ID validation, runtime entity recreation, item-instance/world-entity distinction.
- UI/tools: Save/Load page, Test Lab, editor commands, modal/input behavior, supported resolutions.

## Pass Criteria

- Player-owned state round-trips.
- Repeated loads do not grow collections, modifiers, listeners, registry entries, or UI rows.
- Corrupt or incompatible saves leave live state unchanged.
- Backups and autosaves remain explicit and usable.
- Rollback succeeds or reports unsafe state accurately.
- Scene/place restoration does not auto-complete reach objectives during load.
- No generated persistence artifacts are staged.
