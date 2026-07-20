# Feature 5.5 Manual Testing

Use `PrototypeScene` and the existing Tab menu.

1. Enter Play Mode.
2. Press Tab and open the Character page.
3. Confirm the Character page shows a Traits section and does not show undiscovered secret Traits by default.
4. Switch to Test Lab, then open the `Traits 5.5` section.
5. Select `trait.reincarnated-memory`, press `Grant Active`, then return to Character. Confirm it does not appear until you use `Discover` from the Test Lab.
6. Select `trait.beastkin`, press `Grant Active`, and confirm the Test Lab summary shows linked `trait.night-vision` and low-light vision capability.
7. Select `trait.brave`, press `Grant Active`, then select `trait.cowardly` and press `Grant Active`. Confirm the second grant is rejected for conflict.
8. With `trait.cowardly` still selected, press `Replace`. Confirm `trait.brave` becomes historical and `trait.cowardly` becomes active in the Test Lab summary.
9. Select `trait.mana-sensitive`, press `Grant Active`, then select `requirement.fire-ritual` and press `Evaluate Req`. Confirm it passes while Mana stays unchanged.
10. Select `trait.frail`, press `Grant Active`, and confirm Character stats/resources update from Trait-owned Calculated Stat contributions. Press `Suppress`, then `Unsuppress`, and confirm effects remove and return.
11. Press `Save Snapshot` in Test Lab or use the Save/Load menu to save. Load the save and confirm active/discovered Traits, linked grants, and visible Character menu output restore.

Expected result: no console errors, Character UI only shows known/discovered Traits, Test Lab can inspect hidden Trait state, conflicts reject unless explicitly replaced, requirement checks do not mutate resources, and save/load restores player Trait records.
