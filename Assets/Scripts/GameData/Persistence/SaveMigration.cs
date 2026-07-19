using System.Collections.Generic;

namespace UnityIsekaiGame.GameData.Persistence
{
    public interface ISaveMigration
    {
        int FromSchemaVersion { get; }
        int ToSchemaVersion { get; }
        bool TryMigrate(GameSaveEnvelope envelope, out GameSaveEnvelope migratedEnvelope, out string failureReason);
    }

    public sealed class SaveMigrationRegistry
    {
        private readonly List<ISaveMigration> migrations = new List<ISaveMigration>();

        public int Count => migrations.Count;

        public bool Register(ISaveMigration migration, out string failureReason)
        {
            failureReason = string.Empty;
            if (migration == null)
            {
                failureReason = "Cannot register a null save migration.";
                return false;
            }

            if (migration.FromSchemaVersion < 1 || migration.ToSchemaVersion <= migration.FromSchemaVersion)
            {
                failureReason = "Save migration versions must move forward from a valid source version.";
                return false;
            }

            for (int i = 0; i < migrations.Count; i++)
            {
                if (migrations[i].FromSchemaVersion == migration.FromSchemaVersion
                    && migrations[i].ToSchemaVersion == migration.ToSchemaVersion)
                {
                    failureReason = $"A migration from {migration.FromSchemaVersion} to {migration.ToSchemaVersion} is already registered.";
                    return false;
                }
            }

            migrations.Add(migration);
            migrations.Sort((a, b) => a.FromSchemaVersion.CompareTo(b.FromSchemaVersion));
            return true;
        }
    }
}
