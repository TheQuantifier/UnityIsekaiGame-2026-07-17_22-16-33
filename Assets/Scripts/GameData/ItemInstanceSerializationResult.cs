namespace UnityIsekaiGame.GameData
{
    public sealed class ItemInstanceSerializationResult
    {
        private ItemInstanceSerializationResult(bool succeeded, ItemInstanceSaveData saveData, string message)
        {
            Succeeded = succeeded;
            SaveData = saveData;
            Message = message;
        }

        public bool Succeeded { get; }
        public ItemInstanceSaveData SaveData { get; }
        public string Message { get; }

        public static ItemInstanceSerializationResult Success(ItemInstanceSaveData saveData)
        {
            return new ItemInstanceSerializationResult(true, saveData, "Item instance serialized.");
        }

        public static ItemInstanceSerializationResult Failure(string message)
        {
            return new ItemInstanceSerializationResult(false, null, message);
        }
    }
}
