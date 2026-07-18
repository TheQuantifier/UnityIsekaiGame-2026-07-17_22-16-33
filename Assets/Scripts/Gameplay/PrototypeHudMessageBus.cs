using System;

namespace UnityIsekaiGame.Gameplay
{
    public static class PrototypeHudMessageBus
    {
        public static event Action<string> MessageRequested;

        public static void Show(string message)
        {
            if (!string.IsNullOrWhiteSpace(message))
            {
                MessageRequested?.Invoke(message);
            }
        }
    }
}
