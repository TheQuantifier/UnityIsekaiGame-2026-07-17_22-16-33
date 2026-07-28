#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityIsekaiGame.Development.Automation.Fixtures.Core;
using UnityIsekaiGame.Development.Automation.Fixtures.History;
using UnityIsekaiGame.GameData;
using UnityIsekaiGame.GameData.Persistence;
using UnityIsekaiGame.Inventory.Crafting;
using UnityIsekaiGame.Inventory.Composition;
using UnityIsekaiGame.Inventory.Durability;
using UnityIsekaiGame.Inventory.Experimentation;
using UnityIsekaiGame.Inventory.Identity;
using UnityIsekaiGame.Inventory.Production;
using UnityIsekaiGame.Inventory.Quality;
using UnityIsekaiGame.Inventory.Recipes;
using UnityIsekaiGame.Knowledge;
using UnityIsekaiGame.Knowledge.Access;
using UnityIsekaiGame.Knowledge.History;
using UnityIsekaiGame.Knowledge.Integration;
using UnityIsekaiGame.Knowledge.Records;
using UnityIsekaiGame.Knowledge.Sharing;
using UnityIsekaiGame.Knowledge.Sources;

namespace UnityIsekaiGame.Development.Automation
{
    public enum TestLabScenarioIsolationMode
    {
        FreshRuntime = 0,
        SnapshotRestore = 1,
        SharedRuntime = 2,
        PersistentFixture = 3
    }

    public enum TestLabFixtureEnsureOutcome
    {
        Created = 0,
        ReusedEquivalent = 1,
        Conflict = 2,
        DependencyFailure = 3,
        ValidationFailure = 4
    }

    public enum TestLabFixtureMutationKind
    {
        Created = 0,
        Reused = 1,
        Modified = 2,
        Deleted = 3,
        Conflict = 4
    }

    public sealed class TestLabFixtureHandle
    {
        public TestLabFixtureHandle(string fixtureId, string kind, string stableId, string signature, TestLabFixtureEnsureOutcome outcome, string message = "")
        {
            FixtureId = Normalize(fixtureId, "fixture");
            Kind = Normalize(kind, "record");
            StableId = Normalize(stableId, string.Empty);
            Signature = signature ?? string.Empty;
            Outcome = outcome;
            Message = message ?? string.Empty;
        }

        public string FixtureId { get; }
        public string Kind { get; }
        public string StableId { get; }
        public string Signature { get; }
        public TestLabFixtureEnsureOutcome Outcome { get; }
        public string Message { get; }
        public bool Succeeded => Outcome == TestLabFixtureEnsureOutcome.Created || Outcome == TestLabFixtureEnsureOutcome.ReusedEquivalent;

        private static string Normalize(string value, string fallback)
        {
            return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
        }
    }

    public sealed class TestLabFixtureHandle<TPayload>
    {
        public TestLabFixtureHandle(TestLabFixtureHandle handle, TPayload payload)
        {
            Handle = handle ?? new TestLabFixtureHandle("fixture", typeof(TPayload).Name, string.Empty, string.Empty, TestLabFixtureEnsureOutcome.ValidationFailure, "Missing fixture handle.");
            Payload = payload;
        }

        public TestLabFixtureHandle Handle { get; }
        public TPayload Payload { get; }
        public bool Succeeded => Handle.Succeeded;
    }

    public sealed class TestLabRuntimeFixturePayload
    {
        public TestLabRuntimeFixturePayload(TestLabRuntimeBundle runtimes, string ownerPersonId, string worldId)
        {
            Runtimes = runtimes;
            OwnerPersonId = ownerPersonId ?? string.Empty;
            WorldId = worldId ?? string.Empty;
        }

        public TestLabRuntimeBundle Runtimes { get; }
        public string OwnerPersonId { get; }
        public string WorldId { get; }
    }

    public sealed class TestLabFixtureMutationRecord
    {
        public TestLabFixtureMutationRecord(string fixtureId, string kind, string stableId, TestLabFixtureMutationKind mutationKind, string beforeSignature, string afterSignature, string message)
        {
            FixtureId = fixtureId ?? string.Empty;
            Kind = kind ?? string.Empty;
            StableId = stableId ?? string.Empty;
            MutationKind = mutationKind;
            BeforeSignature = beforeSignature ?? string.Empty;
            AfterSignature = afterSignature ?? string.Empty;
            Message = message ?? string.Empty;
        }

        public string FixtureId { get; }
        public string Kind { get; }
        public string StableId { get; }
        public TestLabFixtureMutationKind MutationKind { get; }
        public string BeforeSignature { get; }
        public string AfterSignature { get; }
        public string Message { get; }
    }

    public sealed class TestLabFixtureOwnershipLedger
    {
        private readonly List<TestLabFixtureMutationRecord> records = new List<TestLabFixtureMutationRecord>();
        private readonly Dictionary<string, TestLabFixtureHandle> handlesByStableId = new Dictionary<string, TestLabFixtureHandle>(StringComparer.Ordinal);
        private readonly List<string> conflicts = new List<string>();

        public IReadOnlyList<TestLabFixtureMutationRecord> Records => records.ToArray();
        public IReadOnlyList<string> Conflicts => conflicts.ToArray();
        public IReadOnlyList<string> OwnedStableIds => handlesByStableId.Keys.OrderBy(value => value, StringComparer.Ordinal).ToArray();
        public bool HasConflicts => conflicts.Count > 0;

        public TestLabFixtureHandle EnsureEquivalent(string fixtureId, string kind, string stableId, string expectedSignature, bool exists, string actualSignature = "")
        {
            fixtureId = Normalize(fixtureId, "fixture");
            kind = Normalize(kind, "record");
            stableId = Normalize(stableId, string.Empty);
            expectedSignature ??= string.Empty;
            actualSignature ??= string.Empty;

            if (string.IsNullOrWhiteSpace(stableId))
            {
                string message = $"Fixture '{fixtureId}' cannot own a blank {kind} stable ID.";
                conflicts.Add(message);
                TestLabFixtureHandle failed = new TestLabFixtureHandle(fixtureId, kind, stableId, expectedSignature, TestLabFixtureEnsureOutcome.ValidationFailure, message);
                Record(failed, TestLabFixtureMutationKind.Conflict, actualSignature, expectedSignature, message);
                return failed;
            }

            if (handlesByStableId.TryGetValue(stableId, out TestLabFixtureHandle previous)
                && !string.Equals(previous.FixtureId, fixtureId, StringComparison.Ordinal))
            {
                string message = $"Stable ID '{stableId}' is already owned by fixture '{previous.FixtureId}', not '{fixtureId}'.";
                conflicts.Add(message);
                TestLabFixtureHandle failed = new TestLabFixtureHandle(fixtureId, kind, stableId, expectedSignature, TestLabFixtureEnsureOutcome.Conflict, message);
                Record(failed, TestLabFixtureMutationKind.Conflict, previous.Signature, expectedSignature, message);
                return failed;
            }

            if (exists && !string.Equals(expectedSignature, actualSignature, StringComparison.Ordinal))
            {
                string message = $"Fixture '{fixtureId}' found existing {kind} '{stableId}' with a different signature.";
                conflicts.Add(message);
                TestLabFixtureHandle failed = new TestLabFixtureHandle(fixtureId, kind, stableId, expectedSignature, TestLabFixtureEnsureOutcome.Conflict, message);
                Record(failed, TestLabFixtureMutationKind.Conflict, actualSignature, expectedSignature, message);
                return failed;
            }

            TestLabFixtureEnsureOutcome outcome = exists ? TestLabFixtureEnsureOutcome.ReusedEquivalent : TestLabFixtureEnsureOutcome.Created;
            TestLabFixtureHandle handle = new TestLabFixtureHandle(fixtureId, kind, stableId, expectedSignature, outcome);
            handlesByStableId[stableId] = handle;
            Record(handle, exists ? TestLabFixtureMutationKind.Reused : TestLabFixtureMutationKind.Created, actualSignature, expectedSignature, string.Empty);
            return handle;
        }

        public void RecordModified(string fixtureId, string kind, string stableId, string beforeSignature, string afterSignature, string message = "")
        {
            records.Add(new TestLabFixtureMutationRecord(fixtureId, kind, stableId, TestLabFixtureMutationKind.Modified, beforeSignature, afterSignature, message));
        }

        public TestLabFixtureLedgerSnapshot CreateSnapshot()
        {
            return new TestLabFixtureLedgerSnapshot(records.ToArray(), conflicts.ToArray());
        }

        private void Record(TestLabFixtureHandle handle, TestLabFixtureMutationKind mutationKind, string beforeSignature, string afterSignature, string message)
        {
            records.Add(new TestLabFixtureMutationRecord(handle.FixtureId, handle.Kind, handle.StableId, mutationKind, beforeSignature, afterSignature, message));
        }

        private static string Normalize(string value, string fallback)
        {
            return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
        }
    }

    public sealed class TestLabFixtureLedgerSnapshot
    {
        public TestLabFixtureLedgerSnapshot(IReadOnlyList<TestLabFixtureMutationRecord> records, IReadOnlyList<string> conflicts)
        {
            Records = (records ?? Array.Empty<TestLabFixtureMutationRecord>()).ToArray();
            Conflicts = (conflicts ?? Array.Empty<string>()).ToArray();
        }

        public IReadOnlyList<TestLabFixtureMutationRecord> Records { get; }
        public IReadOnlyList<string> Conflicts { get; }
    }

    public interface ITestLabFixtureProvider
    {
        string FixtureId { get; }
        IReadOnlyList<string> Dependencies { get; }
        TestLabFixtureHandle Prepare(TestLabScenarioContext context);
        TestLabFixtureHandle Validate(TestLabScenarioContext context);
    }

    public sealed class TestLabFixtureProvider : ITestLabFixtureProvider
    {
        private readonly Func<TestLabScenarioContext, TestLabFixtureHandle> prepare;
        private readonly Func<TestLabScenarioContext, TestLabFixtureHandle> validate;
        private readonly IReadOnlyList<string> dependencies;

        public TestLabFixtureProvider(
            string fixtureId,
            IEnumerable<string> dependencies,
            Func<TestLabScenarioContext, TestLabFixtureHandle> prepare,
            Func<TestLabScenarioContext, TestLabFixtureHandle> validate = null)
        {
            FixtureId = fixtureId ?? string.Empty;
            this.dependencies = (dependencies ?? Array.Empty<string>()).Where(value => !string.IsNullOrWhiteSpace(value)).ToArray();
            this.prepare = prepare;
            this.validate = validate;
        }

        public string FixtureId { get; }
        public IReadOnlyList<string> Dependencies => dependencies;

        public TestLabFixtureHandle Prepare(TestLabScenarioContext context)
        {
            return prepare == null
                ? new TestLabFixtureHandle(FixtureId, "fixture", FixtureId, string.Empty, TestLabFixtureEnsureOutcome.ValidationFailure, $"Fixture '{FixtureId}' has no prepare action.")
                : prepare(context);
        }

        public TestLabFixtureHandle Validate(TestLabScenarioContext context)
        {
            return validate == null ? Prepare(context) : validate(context);
        }
    }

    public sealed class TestLabFixtureRegistry
    {
        private readonly Dictionary<string, ITestLabFixtureProvider> providersById = new Dictionary<string, ITestLabFixtureProvider>(StringComparer.Ordinal);
        private readonly Dictionary<string, TestLabFixtureHandle> preparedHandles = new Dictionary<string, TestLabFixtureHandle>(StringComparer.Ordinal);

        public IReadOnlyList<string> RegisteredFixtureIds => providersById.Keys.OrderBy(value => value, StringComparer.Ordinal).ToArray();

        public bool TryRegister(ITestLabFixtureProvider provider, out string failure)
        {
            failure = string.Empty;
            if (provider == null)
            {
                failure = "Fixture provider is null.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(provider.FixtureId))
            {
                failure = "Fixture provider has no stable fixture ID.";
                return false;
            }

            if (providersById.ContainsKey(provider.FixtureId))
            {
                failure = $"Duplicate fixture provider ID '{provider.FixtureId}'.";
                return false;
            }

            providersById.Add(provider.FixtureId, provider);
            return true;
        }

        public TestLabFixtureHandle Require(string fixtureId, TestLabScenarioContext context)
        {
            fixtureId = fixtureId ?? string.Empty;
            if (preparedHandles.TryGetValue(fixtureId, out TestLabFixtureHandle existing))
            {
                return existing;
            }

            if (!providersById.TryGetValue(fixtureId, out ITestLabFixtureProvider provider))
            {
                return new TestLabFixtureHandle(fixtureId, "fixture", fixtureId, string.Empty, TestLabFixtureEnsureOutcome.DependencyFailure, $"Fixture provider '{fixtureId}' is not registered.");
            }

            foreach (string dependency in provider.Dependencies ?? Array.Empty<string>())
            {
                TestLabFixtureHandle dependencyHandle = Require(dependency, context);
                if (!dependencyHandle.Succeeded)
                {
                    return new TestLabFixtureHandle(fixtureId, "fixture", fixtureId, string.Empty, TestLabFixtureEnsureOutcome.DependencyFailure, dependencyHandle.Message);
                }
            }

            TestLabFixtureHandle handle = provider.Prepare(context) ?? new TestLabFixtureHandle(fixtureId, "fixture", fixtureId, string.Empty, TestLabFixtureEnsureOutcome.ValidationFailure, $"Fixture provider '{fixtureId}' returned no handle.");
            preparedHandles[fixtureId] = handle;
            return handle;
        }

        public IReadOnlyList<string> ValidateDependencyGraph()
        {
            List<string> errors = new List<string>();
            HashSet<string> visiting = new HashSet<string>(StringComparer.Ordinal);
            HashSet<string> visited = new HashSet<string>(StringComparer.Ordinal);

            foreach (ITestLabFixtureProvider provider in providersById.Values.OrderBy(provider => provider.FixtureId, StringComparer.Ordinal))
            {
                Visit(provider, visiting, visited, errors);
            }

            return errors;
        }

        private void Visit(ITestLabFixtureProvider provider, HashSet<string> visiting, HashSet<string> visited, List<string> errors)
        {
            if (provider == null || visited.Contains(provider.FixtureId))
            {
                return;
            }

            if (!visiting.Add(provider.FixtureId))
            {
                errors.Add($"Fixture dependency cycle includes '{provider.FixtureId}'.");
                return;
            }

            foreach (string dependency in provider.Dependencies ?? Array.Empty<string>())
            {
                if (!providersById.TryGetValue(dependency, out ITestLabFixtureProvider dependencyProvider))
                {
                    errors.Add($"Fixture '{provider.FixtureId}' depends on missing fixture '{dependency}'.");
                    continue;
                }

                Visit(dependencyProvider, visiting, visited, errors);
            }

            visiting.Remove(provider.FixtureId);
            visited.Add(provider.FixtureId);
        }
    }

    public sealed class TestLabRuntimeBundle : IDisposable
    {
        private readonly GameObject ownedKnowledgeObject;

        private TestLabRuntimeBundle(
            DefinitionRegistry definitionRegistry,
            string personId,
            string worldId,
            IReadOnlyList<string> knownPersonIds,
            IReadOnlyList<string> knownBodyIds,
            PersonKnowledgeRuntime knowledge,
            AuthoritativeHistoryRuntime history,
            PersonMemoryRuntime memory,
            InformationSourceRuntime sources,
            InformationTransferRuntime transfers,
            InformationAccessRuntime access,
            KnowledgeRecordRuntime records,
            ItemInstanceIdentityRuntime itemInstances,
            ItemCompositionRuntime itemCompositions,
            ItemQualityAffixRuntime itemQualityAffixes,
            ItemDurabilityRuntime itemDurability,
            ProductionRequirementRuntime productionRequirements,
            RecipeKnowledgeRuntime recipeKnowledge,
            CraftingExecutionRuntime craftingExecution,
            ProductionWorkflowRuntime productionWorkflow,
            ExperimentationRuntime experimentation,
            GameObject ownedKnowledgeObject)
        {
            DefinitionRegistry = definitionRegistry;
            PersonId = personId ?? string.Empty;
            WorldId = worldId ?? string.Empty;
            KnownPersonIds = (knownPersonIds ?? Array.Empty<string>()).ToArray();
            KnownBodyIds = (knownBodyIds ?? Array.Empty<string>()).ToArray();
            Knowledge = knowledge;
            History = history;
            Memory = memory;
            Sources = sources;
            Transfers = transfers;
            Access = access;
            Records = records;
            ItemInstances = itemInstances;
            ItemCompositions = itemCompositions;
            ItemQualityAffixes = itemQualityAffixes;
            ItemDurability = itemDurability;
            ProductionRequirements = productionRequirements;
            RecipeKnowledge = recipeKnowledge;
            CraftingExecution = craftingExecution;
            ProductionWorkflow = productionWorkflow;
            Experimentation = experimentation;
            this.ownedKnowledgeObject = ownedKnowledgeObject;
            Facade = new KnowledgeHistoryFacade(CreateRuntimeSet());
        }

        public DefinitionRegistry DefinitionRegistry { get; }
        public string PersonId { get; }
        public string WorldId { get; }
        public IReadOnlyList<string> KnownPersonIds { get; }
        public IReadOnlyList<string> KnownBodyIds { get; }
        public PersonKnowledgeRuntime Knowledge { get; }
        public AuthoritativeHistoryRuntime History { get; }
        public PersonMemoryRuntime Memory { get; }
        public InformationSourceRuntime Sources { get; }
        public InformationTransferRuntime Transfers { get; }
        public InformationAccessRuntime Access { get; }
        public KnowledgeRecordRuntime Records { get; }
        public ItemInstanceIdentityRuntime ItemInstances { get; }
        public ItemCompositionRuntime ItemCompositions { get; }
        public ItemQualityAffixRuntime ItemQualityAffixes { get; }
        public ItemDurabilityRuntime ItemDurability { get; }
        public ProductionRequirementRuntime ProductionRequirements { get; }
        public RecipeKnowledgeRuntime RecipeKnowledge { get; }
        public CraftingExecutionRuntime CraftingExecution { get; }
        public ProductionWorkflowRuntime ProductionWorkflow { get; }
        public ExperimentationRuntime Experimentation { get; }
        public KnowledgeHistoryFacade Facade { get; }

        public static TestLabRuntimeBundle FromExisting(
            DefinitionRegistry definitionRegistry,
            string personId,
            string worldId,
            IReadOnlyList<string> knownPersonIds,
            IReadOnlyList<string> knownBodyIds,
            PersonKnowledgeRuntime knowledge,
            AuthoritativeHistoryRuntime history,
            PersonMemoryRuntime memory,
            InformationSourceRuntime sources,
            InformationTransferRuntime transfers,
            InformationAccessRuntime access,
            KnowledgeRecordRuntime records,
            ItemInstanceIdentityRuntime itemInstances = null,
            ItemCompositionRuntime itemCompositions = null,
            ItemQualityAffixRuntime itemQualityAffixes = null,
            ItemDurabilityRuntime itemDurability = null,
            ProductionRequirementRuntime productionRequirements = null,
            RecipeKnowledgeRuntime recipeKnowledge = null,
            CraftingExecutionRuntime craftingExecution = null,
            ProductionWorkflowRuntime productionWorkflow = null,
            ExperimentationRuntime experimentation = null)
        {
            return new TestLabRuntimeBundle(definitionRegistry, personId, worldId, knownPersonIds, knownBodyIds, knowledge, history, memory, sources, transfers, access, records, itemInstances ?? new ItemInstanceIdentityRuntime(), itemCompositions ?? new ItemCompositionRuntime(), itemQualityAffixes ?? new ItemQualityAffixRuntime(), itemDurability ?? new ItemDurabilityRuntime(), productionRequirements ?? new ProductionRequirementRuntime(), recipeKnowledge ?? new RecipeKnowledgeRuntime(), craftingExecution ?? new CraftingExecutionRuntime(), productionWorkflow ?? new ProductionWorkflowRuntime(), experimentation ?? new ExperimentationRuntime(), null);
        }

        public static TestLabRuntimeBundle CreateFresh(
            DefinitionRegistry definitionRegistry,
            string personId,
            string worldId,
            IReadOnlyList<string> knownPersonIds,
            IReadOnlyList<string> knownBodyIds,
            string objectName = "Test Lab Fresh Knowledge Runtime")
        {
            GameObject knowledgeObject = new GameObject(string.IsNullOrWhiteSpace(objectName) ? "Test Lab Fresh Knowledge Runtime" : objectName);
            knowledgeObject.hideFlags = HideFlags.HideAndDontSave;
            PersonKnowledgeRuntime knowledge = knowledgeObject.AddComponent<PersonKnowledgeRuntime>();
            AuthoritativeHistoryRuntime history = new AuthoritativeHistoryRuntime();
            PersonMemoryRuntime memory = new PersonMemoryRuntime();
            InformationSourceRuntime sources = new InformationSourceRuntime();
            InformationTransferRuntime transfers = new InformationTransferRuntime();
            InformationAccessRuntime access = new InformationAccessRuntime();
            KnowledgeRecordRuntime records = new KnowledgeRecordRuntime();
            ItemInstanceIdentityRuntime itemInstances = new ItemInstanceIdentityRuntime();
            ItemCompositionRuntime itemCompositions = new ItemCompositionRuntime();
            ItemQualityAffixRuntime itemQualityAffixes = new ItemQualityAffixRuntime();
            ItemDurabilityRuntime itemDurability = new ItemDurabilityRuntime();
            ProductionRequirementRuntime productionRequirements = new ProductionRequirementRuntime();
            RecipeKnowledgeRuntime recipeKnowledge = new RecipeKnowledgeRuntime();
            CraftingExecutionRuntime craftingExecution = new CraftingExecutionRuntime();
            ProductionWorkflowRuntime productionWorkflow = new ProductionWorkflowRuntime();
            ExperimentationRuntime experimentation = new ExperimentationRuntime();

            string[] persons = (knownPersonIds ?? Array.Empty<string>()).Concat(new[] { personId }).Where(value => !string.IsNullOrWhiteSpace(value)).Distinct(StringComparer.Ordinal).ToArray();
            string[] bodies = (knownBodyIds ?? Array.Empty<string>()).Where(value => !string.IsNullOrWhiteSpace(value)).Distinct(StringComparer.Ordinal).ToArray();
            history.Configure(definitionRegistry, string.IsNullOrWhiteSpace(worldId) ? PersistenceService.LocalWorldId : worldId, persons, bodies);
            knowledge.Configure(definitionRegistry, personId);
            memory.Configure(personId, definitionRegistry, history, persons);
            sources.Configure(definitionRegistry, personId);
            transfers.Configure(definitionRegistry, personId);
            access.Configure(definitionRegistry, personId);
            records.Configure(definitionRegistry, personId);

            return new TestLabRuntimeBundle(definitionRegistry, personId, worldId, persons, bodies, knowledge, history, memory, sources, transfers, access, records, itemInstances, itemCompositions, itemQualityAffixes, itemDurability, productionRequirements, recipeKnowledge, craftingExecution, productionWorkflow, experimentation, knowledgeObject);
        }

        public KnowledgeHistoryRuntimeSet CreateRuntimeSet()
        {
            return new KnowledgeHistoryRuntimeSet
            {
                DefinitionRegistry = DefinitionRegistry,
                PersonId = PersonId,
                WorldId = WorldId,
                KnownPersonIds = KnownPersonIds,
                KnownBodyIds = KnownBodyIds,
                KnowledgeRuntime = Knowledge,
                HistoryRuntime = History,
                MemoryRuntime = Memory,
                SourceRuntime = Sources,
                TransferRuntime = Transfers,
                AccessRuntime = Access,
                RecordRuntime = Records
            };
        }

        public TestLabRuntimeBundleSnapshot CreateSnapshot()
        {
            return new TestLabRuntimeBundleSnapshot(
                Knowledge?.CreateSaveData(),
                History?.CreateSaveData(),
                Memory?.CreateSaveData(),
                Sources?.CreateSaveData(),
                Transfers?.CreateSaveData(),
                Access?.CreateSaveData(),
                Records?.CreateSaveData(),
                ItemInstances?.CreateSaveData(),
                ItemCompositions?.CreateSaveData(),
                ItemQualityAffixes?.CreateSaveData(),
                ItemDurability?.CreateSaveData(),
                ProductionRequirements?.CreateSaveData(),
                RecipeKnowledge?.CreateSaveData(),
                CraftingExecution?.CreateSaveData(),
                ProductionWorkflow?.CreateSaveData(),
                Experimentation?.CreateSaveData());
        }

        public TestLabRuntimeBundleFingerprint CreateFingerprint()
        {
            return new TestLabRuntimeBundleFingerprint(new[]
            {
                TestLabRuntimeFingerprintSection.FromObject("Knowledge", Knowledge?.KnowledgeRevision ?? 0L, Knowledge?.CreateSaveData()),
                TestLabRuntimeFingerprintSection.FromObject("History", History?.HistoryRevision ?? 0L, History?.CreateSaveData(), History?.NextSequence ?? 0L),
                TestLabRuntimeFingerprintSection.FromObject("Memory", Memory?.MemoryRevision ?? 0L, Memory?.CreateSaveData()),
                TestLabRuntimeFingerprintSection.FromObject("Sources", Sources?.SourceRevision ?? 0L, Sources?.CreateSaveData()),
                TestLabRuntimeFingerprintSection.FromObject("Transfers", Transfers?.TransferRevision ?? 0L, Transfers?.CreateSaveData()),
                TestLabRuntimeFingerprintSection.FromObject("Access", Access?.AccessRevision ?? 0L, Access?.CreateSaveData()),
                TestLabRuntimeFingerprintSection.FromObject("Records", Records?.RecordRevision ?? 0L, Records?.CreateSaveData()),
                TestLabRuntimeFingerprintSection.FromObject("Items", ItemInstances?.Revision ?? 0L, ItemInstances?.CreateSaveData()),
                TestLabRuntimeFingerprintSection.FromObject("ItemCompositions", ItemCompositions?.Revision ?? 0L, ItemCompositions?.CreateSaveData()),
                TestLabRuntimeFingerprintSection.FromObject("ItemQualityAffixes", ItemQualityAffixes?.Revision ?? 0L, ItemQualityAffixes?.CreateSaveData()),
                TestLabRuntimeFingerprintSection.FromObject("ItemDurability", ItemDurability?.Revision ?? 0L, ItemDurability?.CreateSaveData()),
                TestLabRuntimeFingerprintSection.FromObject("ProductionRequirements", ProductionRequirements?.Revision ?? 0L, ProductionRequirements?.CreateSaveData()),
                TestLabRuntimeFingerprintSection.FromObject("RecipeKnowledge", RecipeKnowledge?.Revision ?? 0L, RecipeKnowledge?.CreateSaveData()),
                TestLabRuntimeFingerprintSection.FromObject("CraftingExecution", CraftingExecution?.Revision ?? 0L, CraftingExecution?.CreateSaveData()),
                TestLabRuntimeFingerprintSection.FromObject("ProductionWorkflow", ProductionWorkflow?.Revision ?? 0L, ProductionWorkflow?.CreateSaveData()),
                TestLabRuntimeFingerprintSection.FromObject("Experimentation", Experimentation?.Revision ?? 0L, Experimentation?.CreateSaveData())
            });
        }

        public bool RestoreSnapshot(TestLabRuntimeBundleSnapshot snapshot, out string failure)
        {
            failure = string.Empty;
            if (snapshot == null)
            {
                return true;
            }

            if (Knowledge != null && snapshot.Knowledge != null)
            {
                KnowledgeOperationResult result = Knowledge.RestoreFromSaveData(snapshot.Knowledge, DefinitionRegistry, PersonId, restoring: true);
                if (!result.Succeeded)
                {
                    failure = $"Knowledge restore failed: {result.Message}";
                    return false;
                }
            }

            if (History != null && snapshot.History != null)
            {
                HistoryOperationResult result = History.RestoreFromSaveData(snapshot.History, DefinitionRegistry, KnownPersonIds, KnownBodyIds, restoring: true);
                if (!result.Succeeded)
                {
                    failure = $"History restore failed: {result.Message}";
                    return false;
                }
            }

            if (Memory != null && snapshot.Memory != null)
            {
                HistoryOperationResult result = Memory.RestoreFromSaveData(snapshot.Memory, DefinitionRegistry, History, KnownPersonIds, restoring: true);
                if (!result.Succeeded)
                {
                    failure = $"Memory restore failed: {result.Message}";
                    return false;
                }
            }

            if (Sources != null && snapshot.Sources != null)
            {
                InformationSourceOperationResult result = Sources.RestoreFromSaveData(snapshot.Sources, DefinitionRegistry, PersonId, restoring: true);
                if (!result.Succeeded)
                {
                    failure = $"Information source restore failed: {result.Message}";
                    return false;
                }
            }

            if (Transfers != null && snapshot.Transfers != null)
            {
                InformationTransferResult result = Transfers.RestoreFromSaveData(snapshot.Transfers, DefinitionRegistry, PersonId, restoring: true);
                if (!result.Succeeded)
                {
                    failure = $"Information transfer restore failed: {result.Message}";
                    return false;
                }
            }

            if (Access != null && snapshot.Access != null)
            {
                InformationAccessOperationResult result = Access.RestoreFromSaveData(snapshot.Access, DefinitionRegistry, PersonId, restoring: true);
                if (!result.Succeeded)
                {
                    failure = $"Information access restore failed: {result.Message}";
                    return false;
                }
            }

            if (Records != null && snapshot.Records != null)
            {
                KnowledgeRecordOperationResult result = Records.RestoreFromSaveData(snapshot.Records, DefinitionRegistry, PersonId, restoring: true);
                if (!result.Succeeded)
                {
                    failure = $"Knowledge record restore failed: {result.Message}";
                    return false;
                }
            }

            if (ItemInstances != null && snapshot.ItemInstances != null)
            {
                ItemInstanceOperationResult result = ItemInstances.RestoreFromSaveData(snapshot.ItemInstances, DefinitionRegistry);
                if (!result.Succeeded)
                {
                    failure = $"Item instance restore failed: {result.Message}";
                    return false;
                }
            }

            if (ItemCompositions != null && snapshot.ItemCompositions != null)
            {
                ItemCompositionOperationResult result = ItemCompositions.RestoreFromSaveData(snapshot.ItemCompositions, DefinitionRegistry, ItemInstances);
                if (!result.Succeeded)
                {
                    failure = $"Item composition restore failed: {result.Message}";
                    return false;
                }
            }

            if (ItemQualityAffixes != null && snapshot.ItemQualityAffixes != null)
            {
                ItemQualityAffixOperationResult result = ItemQualityAffixes.RestoreFromSaveData(snapshot.ItemQualityAffixes, DefinitionRegistry, ItemInstances);
                if (!result.Succeeded)
                {
                    failure = $"Item quality restore failed: {result.Message}";
                    return false;
                }
            }

            if (ItemDurability != null && snapshot.ItemDurability != null)
            {
                ItemDurabilityOperationResult result = ItemDurability.RestoreFromSaveData(snapshot.ItemDurability, DefinitionRegistry, ItemInstances, ItemCompositions);
                if (!result.Succeeded)
                {
                    failure = $"Item durability restore failed: {result.Message}";
                    return false;
                }
            }

            if (ProductionRequirements != null && snapshot.ProductionRequirements != null)
            {
                ProductionRequirementEvaluationResult result = ProductionRequirements.RestoreFromSaveData(snapshot.ProductionRequirements);
                if (!result.Succeeded)
                {
                    failure = $"Production requirement restore failed: {result.Message}";
                    return false;
                }
            }

            if (RecipeKnowledge != null && snapshot.RecipeKnowledge != null)
            {
                if (!RecipeKnowledge.RestoreFromSaveData(snapshot.RecipeKnowledge, DefinitionRegistry, out string recipeFailure))
                {
                    failure = $"Recipe knowledge restore failed: {recipeFailure}";
                    return false;
                }
            }

            if (CraftingExecution != null && snapshot.CraftingExecution != null)
            {
                CraftingExecutionResult result = CraftingExecution.RestoreFromSaveData(snapshot.CraftingExecution, DefinitionRegistry);
                if (!result.Succeeded)
                {
                    failure = $"Crafting execution restore failed: {result.Message}";
                    return false;
                }
            }

            if (ProductionWorkflow != null && snapshot.ProductionWorkflow != null)
            {
                ProductionWorkflowResult result = ProductionWorkflow.RestoreFromSaveData(snapshot.ProductionWorkflow, DefinitionRegistry);
                if (!result.Succeeded)
                {
                    failure = $"Production workflow restore failed: {result.Message}";
                    return false;
                }
            }

            if (Experimentation != null && snapshot.Experimentation != null)
            {
                ExperimentationResult result = Experimentation.RestoreFromSaveData(snapshot.Experimentation, DefinitionRegistry);
                if (!result.Succeeded)
                {
                    failure = $"Experimentation restore failed: {result.Message}";
                    return false;
                }
            }

            return true;
        }

        public void Dispose()
        {
            if (ownedKnowledgeObject == null)
            {
                return;
            }

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                UnityEngine.Object.DestroyImmediate(ownedKnowledgeObject);
                return;
            }
#endif
            UnityEngine.Object.Destroy(ownedKnowledgeObject);
        }
    }

    public sealed class TestLabRuntimeBundleSnapshot
    {
        public TestLabRuntimeBundleSnapshot(
            PersonKnowledgeSaveData knowledge,
            AuthoritativeHistorySaveData history,
            PersonMemorySaveData memory,
            InformationSourceSaveData sources,
            InformationTransferSaveData transfers,
            InformationAccessSaveData access,
            KnowledgeRecordSaveData records,
            ItemInstanceRuntimeSaveData itemInstances,
            ItemCompositionRuntimeSaveData itemCompositions,
            ItemQualityAffixRuntimeSaveData itemQualityAffixes,
            ItemDurabilityRuntimeSaveData itemDurability,
            ProductionRequirementRuntimeSaveData productionRequirements,
            RecipeKnowledgeSaveData recipeKnowledge,
            CraftingExecutionRuntimeSaveData craftingExecution,
            ProductionWorkflowRuntimeSaveData productionWorkflow,
            ExperimentationRuntimeSaveData experimentation)
        {
            Knowledge = knowledge;
            History = history;
            Memory = memory;
            Sources = sources;
            Transfers = transfers;
            Access = access;
            Records = records;
            ItemInstances = itemInstances;
            ItemCompositions = itemCompositions;
            ItemQualityAffixes = itemQualityAffixes;
            ItemDurability = itemDurability;
            ProductionRequirements = productionRequirements;
            RecipeKnowledge = recipeKnowledge;
            CraftingExecution = craftingExecution;
            ProductionWorkflow = productionWorkflow;
            Experimentation = experimentation;
        }

        public PersonKnowledgeSaveData Knowledge { get; }
        public AuthoritativeHistorySaveData History { get; }
        public PersonMemorySaveData Memory { get; }
        public InformationSourceSaveData Sources { get; }
        public InformationTransferSaveData Transfers { get; }
        public InformationAccessSaveData Access { get; }
        public KnowledgeRecordSaveData Records { get; }
        public ItemInstanceRuntimeSaveData ItemInstances { get; }
        public ItemCompositionRuntimeSaveData ItemCompositions { get; }
        public ItemQualityAffixRuntimeSaveData ItemQualityAffixes { get; }
        public ItemDurabilityRuntimeSaveData ItemDurability { get; }
        public ProductionRequirementRuntimeSaveData ProductionRequirements { get; }
        public RecipeKnowledgeSaveData RecipeKnowledge { get; }
        public CraftingExecutionRuntimeSaveData CraftingExecution { get; }
        public ProductionWorkflowRuntimeSaveData ProductionWorkflow { get; }
        public ExperimentationRuntimeSaveData Experimentation { get; }
    }

    public sealed class TestLabRuntimeBundleFingerprint
    {
        private readonly IReadOnlyList<TestLabRuntimeFingerprintSection> sections;

        public TestLabRuntimeBundleFingerprint(IEnumerable<TestLabRuntimeFingerprintSection> sections)
        {
            this.sections = (sections ?? Array.Empty<TestLabRuntimeFingerprintSection>())
                .Where(section => section != null)
                .OrderBy(section => section.Area, StringComparer.Ordinal)
                .ToArray();
        }

        public IReadOnlyList<TestLabRuntimeFingerprintSection> Sections => sections;

        public IReadOnlyList<TestLabRuntimeFingerprintDiff> Compare(TestLabRuntimeBundleFingerprint other)
        {
            Dictionary<string, TestLabRuntimeFingerprintSection> before = Sections.ToDictionary(section => section.Area, StringComparer.Ordinal);
            Dictionary<string, TestLabRuntimeFingerprintSection> after = (other?.Sections ?? Array.Empty<TestLabRuntimeFingerprintSection>()).ToDictionary(section => section.Area, StringComparer.Ordinal);
            List<TestLabRuntimeFingerprintDiff> diffs = new List<TestLabRuntimeFingerprintDiff>();
            foreach (string area in before.Keys.Concat(after.Keys).Distinct(StringComparer.Ordinal).OrderBy(value => value, StringComparer.Ordinal))
            {
                before.TryGetValue(area, out TestLabRuntimeFingerprintSection prior);
                after.TryGetValue(area, out TestLabRuntimeFingerprintSection current);
                if (!TestLabRuntimeFingerprintSection.SemanticallyEquals(prior, current))
                {
                    diffs.Add(new TestLabRuntimeFingerprintDiff(area, prior, current));
                }
            }

            return diffs;
        }
    }

    public sealed class TestLabRuntimeFingerprintSection
    {
        public TestLabRuntimeFingerprintSection(string area, long revision, long sequence, string contentHash, int contentLength)
        {
            Area = area ?? string.Empty;
            Revision = revision;
            Sequence = sequence;
            ContentHash = contentHash ?? string.Empty;
            ContentLength = contentLength;
        }

        public string Area { get; }
        public long Revision { get; }
        public long Sequence { get; }
        public string ContentHash { get; }
        public int ContentLength { get; }

        public string ToDiagnostic()
        {
            return $"{Area}:rev={Revision};seq={Sequence};hash={ContentHash};len={ContentLength}";
        }

        public static bool SemanticallyEquals(TestLabRuntimeFingerprintSection left, TestLabRuntimeFingerprintSection right)
        {
            if (ReferenceEquals(left, right))
            {
                return true;
            }

            if (left == null || right == null)
            {
                return false;
            }

            return string.Equals(left.Area, right.Area, StringComparison.Ordinal)
                && left.Revision == right.Revision
                && left.Sequence == right.Sequence
                && left.ContentLength == right.ContentLength
                && string.Equals(left.ContentHash, right.ContentHash, StringComparison.Ordinal);
        }

        public static TestLabRuntimeFingerprintSection FromObject(string area, long revision, object state, long sequence = 0L)
        {
            string json = state == null ? string.Empty : JsonUtility.ToJson(state);
            return FromText(area, revision, json, sequence);
        }

        public static TestLabRuntimeFingerprintSection FromText(string area, long revision, string state, long sequence = 0L)
        {
            string text = state ?? string.Empty;
            return new TestLabRuntimeFingerprintSection(area, revision, sequence, DeterministicHash(text), text.Length);
        }

        private static string DeterministicHash(string value)
        {
            unchecked
            {
                uint hash = 2166136261;
                string text = value ?? string.Empty;
                for (int i = 0; i < text.Length; i++)
                {
                    hash ^= text[i];
                    hash *= 16777619;
                }

                return hash.ToString("x8");
            }
        }
    }

    public sealed class TestLabRuntimeFingerprintDiff
    {
        public TestLabRuntimeFingerprintDiff(string area, TestLabRuntimeFingerprintSection before, TestLabRuntimeFingerprintSection after)
        {
            Area = area ?? string.Empty;
            Before = before;
            After = after;
        }

        public string Area { get; }
        public TestLabRuntimeFingerprintSection Before { get; }
        public TestLabRuntimeFingerprintSection After { get; }

        public string ToDiagnostic()
        {
            return $"{Area} [{Before?.ToDiagnostic() ?? "missing"}] -> [{After?.ToDiagnostic() ?? "missing"}]";
        }
    }

    public sealed class TestLabScenarioContext : IDisposable
    {
        public const string RuntimeBaselineFixtureId = "automation.runtime-baseline";
        public const string MutableStateScopeFixtureId = "automation.scenario-mutable-state";
        public static readonly IReadOnlyList<string> DefaultRequiredFixtureIds = new[] { RuntimeBaselineFixtureId, MutableStateScopeFixtureId };
        private readonly TestLabRuntimeBundle ownedFreshRuntime;
        private readonly IReadOnlyList<string> requiredFixtureIds;
        private readonly Dictionary<string, object> fixturePayloads = new Dictionary<string, object>(StringComparer.Ordinal);
        private readonly Func<TestLabRuntimeArea, IEnumerable<TestLabRuntimeFingerprintSection>> additionalFingerprintSections;
        private readonly TestLabRuntimeBundleSnapshot restoreSnapshot;
        private readonly TestLabRuntimeBundleFingerprint baselineFingerprint;
        private string cleanupFailure;
        private bool isolationRestored;

        public TestLabScenarioContext(
            string runId,
            string suiteId,
            string scenarioId,
            TestLabScenarioIsolationMode isolationMode,
            TestLabRuntimeBundle runtimeBundle,
            TestLabRuntimeBundle ownedFreshRuntime = null,
            TestLabRuntimeArea requiredRuntimeAreas = TestLabRuntimeArea.KnowledgeHistory,
            IEnumerable<string> requiredFixtureIds = null,
            Func<TestLabRuntimeArea, IEnumerable<TestLabRuntimeFingerprintSection>> additionalFingerprintSections = null)
        {
            RunId = runId ?? string.Empty;
            SuiteId = suiteId ?? string.Empty;
            ScenarioId = scenarioId ?? string.Empty;
            IsolationMode = isolationMode;
            RequiredRuntimeAreas = requiredRuntimeAreas;
            Runtimes = runtimeBundle;
            this.ownedFreshRuntime = ownedFreshRuntime;
            Fixtures = new TestLabFixtureRegistry();
            Ledger = new TestLabFixtureOwnershipLedger();
            this.additionalFingerprintSections = additionalFingerprintSections;
            this.requiredFixtureIds = (requiredFixtureIds ?? DefaultRequiredFixtureIds).Where(value => !string.IsNullOrWhiteSpace(value)).Distinct(StringComparer.Ordinal).ToArray();
            restoreSnapshot = isolationMode == TestLabScenarioIsolationMode.SnapshotRestore ? runtimeBundle?.CreateSnapshot() : null;
            baselineFingerprint = CreateCurrentFingerprint();
            RegisterCoreFixtures();
        }

        public string RunId { get; }
        public string SuiteId { get; }
        public string ScenarioId { get; }
        public TestLabScenarioIsolationMode IsolationMode { get; }
        public TestLabRuntimeArea RequiredRuntimeAreas { get; }
        public TestLabRuntimeBundle Runtimes { get; }
        public TestLabFixtureRegistry Fixtures { get; }
        public TestLabFixtureOwnershipLedger Ledger { get; }
        public IReadOnlyList<string> RequiredFixtureIds => requiredFixtureIds;
        public string Namespace => Sanitize($"{SuiteId}.{ScenarioId}.{RunId}");

        public string ScopedId(string category, string slug)
        {
            string normalizedCategory = Sanitize(category);
            string normalizedSlug = Sanitize(slug);
            return $"{normalizedCategory}.fixture.{Namespace}.{normalizedSlug}";
        }

        public void SetFixturePayload<TPayload>(string fixtureId, TPayload payload)
        {
            if (string.IsNullOrWhiteSpace(fixtureId))
            {
                return;
            }

            fixturePayloads[fixtureId] = payload;
        }

        public bool TryGetFixturePayload<TPayload>(string fixtureId, out TPayload payload)
        {
            if (!string.IsNullOrWhiteSpace(fixtureId)
                && fixturePayloads.TryGetValue(fixtureId, out object value)
                && value is TPayload typed)
            {
                payload = typed;
                return true;
            }

            payload = default;
            return false;
        }

        public TestLabAutomationStepResult Preflight()
        {
            List<string> errors = new List<string>();
            if (string.IsNullOrWhiteSpace(RunId))
            {
                errors.Add("Scenario run ID is missing.");
            }

            if (string.IsNullOrWhiteSpace(SuiteId))
            {
                errors.Add("Scenario suite ID is missing.");
            }

            if (string.IsNullOrWhiteSpace(ScenarioId))
            {
                errors.Add("Scenario ID is missing.");
            }

            if (RequiredFixtureIds.Count == 0)
            {
                errors.Add("Scenario declares no fixture requirements.");
            }

            if (!CanIsolate(RuntimeIsolationSupportedAreas, RequiredRuntimeAreas)
                && (IsolationMode == TestLabScenarioIsolationMode.FreshRuntime
                    || IsolationMode == TestLabScenarioIsolationMode.SnapshotRestore))
            {
                errors.Add($"{IsolationMode} cannot isolate required runtime areas '{RequiredRuntimeAreas}'. Supported isolated areas: {RuntimeIsolationSupportedAreas}.");
            }

            errors.AddRange(Fixtures.ValidateDependencyGraph());
            if (errors.Count > 0)
            {
                return TestLabAssertions.Fail("fixture.preflight", "Fixture preflight", "CleanFixtureScope", "valid", "invalid", string.Join(" | ", errors));
            }

            foreach (string fixtureId in RequiredFixtureIds)
            {
                TestLabFixtureHandle handle = Fixtures.Require(fixtureId, this);
                if (!handle.Succeeded)
                {
                    string failureDiagnostics = $"Suite={SuiteId} Scenario={ScenarioId} Run={RunId} Isolation={IsolationMode} Fixture={fixtureId} Code={handle.Outcome} {handle.Message}";
                    return TestLabAssertions.Fail("fixture.preflight", "Fixture preflight", "FixturePrepared", "Succeeded", handle.Outcome, failureDiagnostics);
                }
            }

            string diagnostics = $"Isolation={IsolationMode} RuntimeAreas={RequiredRuntimeAreas} Namespace={Namespace} RequiredFixtures={string.Join(",", RequiredFixtureIds)} Fixtures={Fixtures.RegisteredFixtureIds.Count} RuntimeBundle={(Runtimes == null ? "None" : "Available")}.";
            return TestLabAssertions.Pass("fixture.preflight", "Fixture preflight", diagnostics);
        }

        public TestLabAutomationStepResult AuditMutationsBeforeRestore()
        {
            TestLabFixtureLedgerSnapshot snapshot = Ledger.CreateSnapshot();
            if (snapshot.Conflicts.Count > 0)
            {
                return TestLabAssertions.Fail("fixture.audit", "Fixture mutation audit", "NoFixtureConflicts", "0", snapshot.Conflicts.Count, string.Join(" | ", snapshot.Conflicts));
            }

            IReadOnlyList<TestLabRuntimeFingerprintDiff> diffs = baselineFingerprint?.Compare(CreateCurrentFingerprint()) ?? Array.Empty<TestLabRuntimeFingerprintDiff>();
            bool mutableScopeDeclared = RequiredFixtureIds.Any(id => string.Equals(id, MutableStateScopeFixtureId, StringComparison.Ordinal));
            int ownedMutationCount = snapshot.Records.Count(record => !IsCoreScopeRecord(record));
            bool changesAllowed = diffs.Count == 0
                || (mutableScopeDeclared && (IsolationMode == TestLabScenarioIsolationMode.FreshRuntime
                    || IsolationMode == TestLabScenarioIsolationMode.SharedRuntime
                    || IsolationMode == TestLabScenarioIsolationMode.PersistentFixture
                    || ownedMutationCount > 0));

            string diagnostics = $"Suite={SuiteId} Scenario={ScenarioId} Run={RunId} Isolation={IsolationMode} Diffs={diffs.Count} OwnedMutations={ownedMutationCount} LedgerRecords={snapshot.Records.Count} OwnedIds={string.Join(",", Ledger.OwnedStableIds)}";
            if (diffs.Count > 0)
            {
                diagnostics += $" Changes={string.Join(" | ", diffs.Select(diff => diff.ToDiagnostic()))}";
            }

            return changesAllowed
                ? TestLabAssertions.Pass("fixture.audit", "Fixture mutation audit", diagnostics)
                : TestLabAssertions.Fail("fixture.audit", "Fixture mutation audit", "DeclaredMutationsOnly", "declared", "undeclared", diagnostics);
        }

        public TestLabAutomationStepResult RestoreIsolation()
        {
            if (isolationRestored)
            {
                return TestLabAssertions.Pass("fixture.restore", "Fixture restore", $"Isolation={IsolationMode} already restored.");
            }

            isolationRestored = true;
            if (restoreSnapshot != null && Runtimes != null && !Runtimes.RestoreSnapshot(restoreSnapshot, out cleanupFailure))
            {
                cleanupFailure = $"Snapshot restore failed for {SuiteId}/{ScenarioId}: {cleanupFailure}";
                return TestLabAssertions.Fail("fixture.restore", "Fixture restore", "SnapshotRestored", "Succeeded", "RestoreFailed", cleanupFailure);
            }

            return TestLabAssertions.Pass("fixture.restore", "Fixture restore", $"Isolation={IsolationMode} restored.");
        }

        public TestLabAutomationStepResult VerifyRestoredBaseline()
        {
            if (!string.IsNullOrWhiteSpace(cleanupFailure))
            {
                return TestLabAssertions.Fail("fixture.integrity", "Fixture baseline integrity", "CleanFixtureScope", "Succeeded", "CleanupFailed", cleanupFailure);
            }

            if (baselineFingerprint == null || Runtimes == null || IsolationMode != TestLabScenarioIsolationMode.SnapshotRestore)
            {
                return TestLabAssertions.Pass("fixture.integrity", "Fixture baseline integrity", $"Isolation={IsolationMode} no baseline restore comparison required.");
            }

            IReadOnlyList<TestLabRuntimeFingerprintDiff> diffs = baselineFingerprint.Compare(CreateCurrentFingerprint());
            if (diffs.Count > 0)
            {
                return TestLabAssertions.Fail("fixture.integrity", "Fixture baseline integrity", "BaselineRestored", "clean", "dirty", string.Join(" | ", diffs.Select(diff => diff.ToDiagnostic())));
            }

            return TestLabAssertions.Pass("fixture.integrity", "Fixture baseline integrity", $"Isolation={IsolationMode} baseline restored.");
        }

        public void Dispose()
        {
            RestoreIsolation();
            ownedFreshRuntime?.Dispose();
        }

        private static string Sanitize(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return "id";
            }

            char[] chars = value.Trim().ToLowerInvariant().ToCharArray();
            for (int i = 0; i < chars.Length; i++)
            {
                char c = chars[i];
                if (!char.IsLetterOrDigit(c) && c != '.' && c != '-')
                {
                    chars[i] = '-';
                }
            }

            return new string(chars).Trim('.', '-');
        }

        private const TestLabRuntimeArea RuntimeIsolationSupportedAreas = TestLabRuntimeArea.KnowledgeHistory | TestLabRuntimeArea.Items;

        private static bool CanIsolate(TestLabRuntimeArea supported, TestLabRuntimeArea required)
        {
            return (required & ~supported) == TestLabRuntimeArea.None;
        }

        private static bool IsCoreScopeRecord(TestLabFixtureMutationRecord record)
        {
            return record == null
                || string.Equals(record.FixtureId, RuntimeBaselineFixtureId, StringComparison.Ordinal)
                || string.Equals(record.FixtureId, MutableStateScopeFixtureId, StringComparison.Ordinal);
        }

        private TestLabRuntimeBundleFingerprint CreateCurrentFingerprint()
        {
            IEnumerable<TestLabRuntimeFingerprintSection> runtimeSections = Runtimes?.CreateFingerprint().Sections ?? Array.Empty<TestLabRuntimeFingerprintSection>();
            IEnumerable<TestLabRuntimeFingerprintSection> additionalSections = additionalFingerprintSections == null
                ? Array.Empty<TestLabRuntimeFingerprintSection>()
                : additionalFingerprintSections(RequiredRuntimeAreas) ?? Array.Empty<TestLabRuntimeFingerprintSection>();
            return new TestLabRuntimeBundleFingerprint(runtimeSections.Concat(additionalSections));
        }

        private void RegisterCoreFixtures()
        {
            TestLabCoreFixtureProviders.RegisterDefaults(this);
            TestLabHistoryFixtureProviders.RegisterDefaults(this);
        }
    }
}
#endif
