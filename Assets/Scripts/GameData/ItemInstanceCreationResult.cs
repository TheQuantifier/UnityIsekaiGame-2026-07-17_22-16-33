namespace UnityIsekaiGame.GameData
{
    public sealed class ItemInstanceCreationResult
    {
        private ItemInstanceCreationResult(ItemInstanceCreationStatus status, ItemInstance itemInstance, string message)
        {
            Status = status;
            ItemInstance = itemInstance;
            Message = message;
        }

        public ItemInstanceCreationStatus Status { get; }
        public ItemInstance ItemInstance { get; }
        public string Message { get; }
        public bool Succeeded => Status == ItemInstanceCreationStatus.Success && ItemInstance != null;

        public static ItemInstanceCreationResult Success(ItemInstance itemInstance)
        {
            return new ItemInstanceCreationResult(ItemInstanceCreationStatus.Success, itemInstance, "Item instance created.");
        }

        public static ItemInstanceCreationResult Failure(ItemInstanceCreationStatus status, string message)
        {
            return new ItemInstanceCreationResult(status, null, message);
        }
    }
}
