namespace UnityIsekaiGame.GameData
{
    public sealed class ItemInstanceRestoreResult
    {
        private ItemInstanceRestoreResult(ItemInstanceRestoreStatus status, ItemInstance itemInstance, string message)
        {
            Status = status;
            ItemInstance = itemInstance;
            Message = message;
        }

        public ItemInstanceRestoreStatus Status { get; }
        public ItemInstance ItemInstance { get; }
        public string Message { get; }
        public bool Succeeded => Status == ItemInstanceRestoreStatus.Success && ItemInstance != null;

        public static ItemInstanceRestoreResult Success(ItemInstance itemInstance)
        {
            return new ItemInstanceRestoreResult(ItemInstanceRestoreStatus.Success, itemInstance, "Item instance restored.");
        }

        public static ItemInstanceRestoreResult Failure(ItemInstanceRestoreStatus status, string message)
        {
            return new ItemInstanceRestoreResult(status, null, message);
        }
    }
}
