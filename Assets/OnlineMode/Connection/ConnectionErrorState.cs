namespace Assets.OnlineMode.Connection
{
    internal static class ConnectionErrorState
    {
        public static string LastErrorMessage { get; private set; }

        public static void Clear()
        {
            LastErrorMessage = null;
        }

        public static void Set(string message)
        {
            LastErrorMessage = message;
        }

        public static void SetIfEmpty(string message)
        {
            if (string.IsNullOrEmpty(LastErrorMessage))
            {
                LastErrorMessage = message;
            }
        }

        public static string GetOrDefault(string fallback)
        {
            return string.IsNullOrEmpty(LastErrorMessage) ? fallback : LastErrorMessage;
        }
    }
}