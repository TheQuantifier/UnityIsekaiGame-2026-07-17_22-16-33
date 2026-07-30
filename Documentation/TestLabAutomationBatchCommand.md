# Test Lab Automation Batch Command

The in-game Test Lab automation runner remains the authoritative scene-hosted UI runner. The batch command reuses the same suite registry, runner, result model, definition fallback source, and report exporter so command-line runs do not drift from the in-game runner.

Automation suite registration is owned by `PrototypeTestLabAutomationCatalog`. Step provider classes declare themselves with `PrototypeTestLabAutomationProviderAttribute`, and both the command runner and in-game runner build their registry from that catalog. To add a new scenario to an existing suite, edit the owning `PrototypeStep*AutomationSuites` file only. To add a new Step provider, create the provider class, add the provider attribute, and implement `public static RegisterDefaults(TestLabAutomationRegistry registry)`.

## Unity Command

```powershell
Unity.exe -batchmode -projectPath "C:\Users\jhand\Documents\Github\UnityIsekaiGame" -executeMethod UnityIsekaiGame.Editor.Tools.TestLabAutomation.TestLabAutomationBatchCommand.Run -testLabMode quick -testLabOutputDir Logs/TestLabAutomation -testLabFormat both -quit
```

## Arguments

- `-testLabMode quick|all|suite|scenario|list|compatibility`
- `-testLabStep <step>` limits catalog, compatibility, or run selection to one Step provider, such as `9`
- `-testLabSuite <suiteId>` required for `suite` and `scenario`
- `-testLabScenario <scenarioId>` required for `scenario`
- `-testLabOutputDir <directory>` writes timestamped reports into a directory
- `-testLabOutput <path>` writes deterministic `.json` and/or `.md` files using this path as a base
- `-testLabFormat json|markdown|junit|both|all`
- `-testLabOrder normal|reverse|shuffled`
- `-testLabSeed <int>`
- `-testLabStopOnFail true|false`
- `-testLabScene <scene asset path>` optionally opens a scene before the run

`list` mode exports the discovered provider/suite/scenario catalog without running automation. `compatibility` mode exports the same compatibility preview used by the runner before execution. JUnit XML is emitted only for run results; list and compatibility exports normalize JUnit-only requests to JSON.

## Exit Codes

- `0`: command parsed and automation passed, or help was requested
- `1`: automation completed with failed, errored, or cancelled scenarios
- `2`: invalid command-line arguments or missing scene path
- `3`: unhandled command failure

## Host Boundary

The command registers a scene-independent batch host for fixture-owned Knowledge, History, Item, Profession, and Economy automation. Character, Combat, Biology, Persistence, or UI scenarios that require a live Prototype scene host should still be run through the in-game Test Lab runner unless an explicit editor-capable scene host is added later.
