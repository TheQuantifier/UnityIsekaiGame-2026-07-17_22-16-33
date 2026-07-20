using System;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityIsekaiGame.GameData.Persistence;

namespace UnityIsekaiGame.Tests
{
    public sealed class SaveSlotsAutosaveTests
    {
        private string testRoot;

        [SetUp]
        public void SetUp()
        {
            testRoot = Path.Combine(Path.GetTempPath(), "UnityIsekaiGameSaveSlotsAutosaveTests", Guid.NewGuid().ToString("N"));
        }

        [TearDown]
        public void TearDown()
        {
            if (!string.IsNullOrWhiteSpace(testRoot) && Directory.Exists(testRoot))
            {
                Directory.Delete(testRoot, true);
            }
        }

        [Test]
        public void CatalogCreatesFiveManualAndThreeAutosaveDescriptors()
        {
            PersistenceService service = CreateService(new TestState());

            var descriptors = PrototypeSaveSlotCatalog.BuildDescriptors(service, 5, 3);

            Assert.That(descriptors, Has.Count.EqualTo(8));
            Assert.That(descriptors[0].slotId, Is.EqualTo("manual-1"));
            Assert.That(descriptors[4].slotId, Is.EqualTo("manual-5"));
            Assert.That(descriptors[5].slotId, Is.EqualTo("autosave-0"));
            Assert.That(descriptors[7].slotId, Is.EqualTo("autosave-2"));
            Assert.That(descriptors[0].CanSave, Is.True);
            Assert.That(descriptors[5].CanSave, Is.False);
            Assert.That(descriptors[5].isNewestAutosave, Is.True);
            Assert.That(descriptors[7].compatibilityStatus, Is.EqualTo(SaveCompatibilityStatus.Empty));
        }

        [Test]
        public void ManualSaveDescriptorIncludesMetadataAndPlaytime()
        {
            TestState state = new TestState { Value = 7, Note = "manual" };
            PersistenceService service = CreateService(state);
            service.PlaytimeSecondsProvider = () => 125d;

            PersistenceSaveResult result = service.Save(PrototypeSaveSlotCatalog.ManualSlotId(0), PrototypeSaveSlotCatalog.ManualDisplayName(0));
            Assert.That(result.Succeeded, Is.True, result.Message);

            var descriptors = PrototypeSaveSlotCatalog.BuildDescriptors(service, 5, 3);
            SaveSlotDescriptor manualOne = descriptors[0];
            Assert.That(manualOne.exists, Is.True);
            Assert.That(manualOne.isValid, Is.True);
            Assert.That(manualOne.CanLoad, Is.True);
            Assert.That(manualOne.compatibilityStatus, Is.EqualTo(SaveCompatibilityStatus.Compatible));
            Assert.That(manualOne.playTimeSeconds, Is.EqualTo(125d));
            Assert.That(manualOne.displayName, Is.EqualTo("Manual Save 1"));
        }

        [Test]
        public void AutosaveRotationMaintainsStableGenerationSlotIds()
        {
            TestState state = new TestState();
            PersistenceService service = CreateService(state);

            SaveStagingAndRotate(service, state, 1);
            SaveStagingAndRotate(service, state, 2);
            SaveStagingAndRotate(service, state, 3);

            AssertGeneration(service, 0, expectedValue: 3);
            AssertGeneration(service, 1, expectedValue: 2);
            AssertGeneration(service, 2, expectedValue: 1);
            Assert.That(File.Exists(Paths(PrototypeSaveSlotCatalog.AutosaveStagingSlotId).PrimaryPath), Is.False);
        }

        [Test]
        public void AutosaveRotationRejectsMissingStagingWithoutDeletingGenerations()
        {
            TestState state = new TestState();
            PersistenceService service = CreateService(state);
            SaveStagingAndRotate(service, state, 1);

            string newestPath = Paths(PrototypeSaveSlotCatalog.AutosaveSlotId(0)).PrimaryPath;
            string previousJson = File.ReadAllText(newestPath);

            PersistenceSaveResult result = service.RotateAutosaveSlots(
                PrototypeSaveSlotCatalog.AutosaveStagingSlotId,
                PrototypeSaveSlotCatalog.BuildAutosaveSlotIds(3));

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Status, Is.EqualTo(PersistenceSaveStatus.TemporaryWriteFailed));
            Assert.That(File.ReadAllText(newestPath), Is.EqualTo(previousJson));
        }

        [Test]
        public void DirtyTrackerRaisesEventsAndStoresLastReason()
        {
            GameObject root = new GameObject("Dirty Tracker Test");
            try
            {
                Type trackerType = TestTypeResolver.RequiredType("UnityIsekaiGame.Persistence.GameSaveDirtyTracker");
                Assert.That(trackerType, Is.Not.Null);

                Component tracker = root.AddComponent(trackerType);
                int eventCount = 0;
                bool lastDirty = false;
                string lastReason = string.Empty;
                Delegate handler = Delegate.CreateDelegate(
                    trackerType.GetEvent("DirtyStateChanged").EventHandlerType,
                    new DirtyEventSink((dirty, reason) =>
                    {
                        eventCount++;
                        lastDirty = dirty;
                        lastReason = reason;
                    }),
                    nameof(DirtyEventSink.Handle));
                trackerType.GetEvent("DirtyStateChanged").AddEventHandler(tracker, handler);

                trackerType.GetMethod("MarkDirty").Invoke(tracker, new object[] { "Inventory changed." });
                Assert.That((bool)trackerType.GetProperty("IsDirty").GetValue(tracker), Is.True);
                Assert.That((string)trackerType.GetProperty("LastReason").GetValue(tracker), Is.EqualTo("Inventory changed."));
                Assert.That(eventCount, Is.EqualTo(1));
                Assert.That(lastDirty, Is.True);
                Assert.That(lastReason, Is.EqualTo("Inventory changed."));

                trackerType.GetMethod("MarkClean").Invoke(tracker, new object[] { "Manual save complete." });
                Assert.That((bool)trackerType.GetProperty("IsDirty").GetValue(tracker), Is.False);
                Assert.That((string)trackerType.GetProperty("LastReason").GetValue(tracker), Is.EqualTo("Manual save complete."));
                Assert.That(eventCount, Is.EqualTo(2));
                Assert.That(lastDirty, Is.False);
                trackerType.GetEvent("DirtyStateChanged").RemoveEventHandler(tracker, handler);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private sealed class DirtyEventSink
        {
            private readonly Action<bool, string> handler;

            public DirtyEventSink(Action<bool, string> handler)
            {
                this.handler = handler;
            }

            public void Handle(bool dirty, string reason)
            {
                handler?.Invoke(dirty, reason);
            }
        }

        private PersistenceService CreateService(TestState state)
        {
            PersistenceService service = new PersistenceService(new PersistencePathProvider(testRoot));
            Assert.That(service.RegisterParticipant(new TestParticipant(state), out string failure), Is.True, failure);
            return service;
        }

        private void SaveStagingAndRotate(PersistenceService service, TestState state, int value)
        {
            state.Value = value;
            state.Note = $"autosave-{value}";
            PersistenceSaveResult save = service.Save(PrototypeSaveSlotCatalog.AutosaveStagingSlotId, $"Autosave {value}");
            Assert.That(save.Succeeded, Is.True, save.Message);

            PersistenceSaveResult rotate = service.RotateAutosaveSlots(
                PrototypeSaveSlotCatalog.AutosaveStagingSlotId,
                PrototypeSaveSlotCatalog.BuildAutosaveSlotIds(3));
            Assert.That(rotate.Succeeded, Is.True, rotate.Message);
        }

        private void AssertGeneration(PersistenceService service, int generation, int expectedValue)
        {
            string slotId = PrototypeSaveSlotCatalog.AutosaveSlotId(generation);
            Assert.That(service.ValidateSlot(slotId).Succeeded, Is.True);

            GameSaveEnvelope envelope = JsonUtility.FromJson<GameSaveEnvelope>(File.ReadAllText(Paths(slotId).PrimaryPath));
            Assert.That(envelope.slotId, Is.EqualTo(slotId));
            PrototypePersistenceStateSaveData payload = JsonUtility.FromJson<PrototypePersistenceStateSaveData>(envelope.participants[0].payloadJson);
            Assert.That(payload.testValue, Is.EqualTo(expectedValue));
        }

        private SaveSlotPaths Paths(string slotId)
        {
            PersistencePathProvider provider = new PersistencePathProvider(testRoot);
            Assert.That(provider.TryGetPaths(slotId, out SaveSlotPaths paths, out string failure), Is.True, failure);
            return paths;
        }

        private sealed class TestState : IPrototypePersistenceState
        {
            public int Value;
            public string Note;

            public PrototypePersistenceStateSaveData CreateSaveData()
            {
                return new PrototypePersistenceStateSaveData
                {
                    testValue = Value,
                    note = Note,
                    flag = true
                };
            }

            public void RestoreFromSaveData(PrototypePersistenceStateSaveData saveData)
            {
                Value = saveData.testValue;
                Note = saveData.note;
            }
        }

        private sealed class TestParticipant : IPersistenceParticipant
        {
            private readonly PrototypePersistenceStateParticipant inner;

            public TestParticipant(TestState state)
            {
                inner = new PrototypePersistenceStateParticipant(state);
            }

            public string ParticipantKey => PrototypePersistenceStateParticipant.Key;
            public int ParticipantSchemaVersion => PrototypePersistenceStateParticipant.CurrentParticipantSchemaVersion;
            public bool IsRequired => true;
            public PersistenceScope Scope => PersistenceScope.Player;
            public string OwnerId => PersistenceService.LocalPlayerId;
            public PersistenceLoadPhase LoadPhase => PersistenceLoadPhase.Prototype;
            public int LoadPriority => 0;

            public PersistenceParticipantSaveResult CapturePayload()
            {
                return inner.CapturePayload();
            }

            public PersistenceParticipantPrepareResult PreparePayload(string payloadJson, int payloadSchemaVersion)
            {
                return inner.PreparePayload(payloadJson, payloadSchemaVersion);
            }

            public PersistenceParticipantCommitResult CommitPreparedPayload(object preparedPayload)
            {
                return inner.CommitPreparedPayload(preparedPayload);
            }

            public void DiscardPreparedPayload(object preparedPayload)
            {
                inner.DiscardPreparedPayload(preparedPayload);
            }
        }
    }
}
