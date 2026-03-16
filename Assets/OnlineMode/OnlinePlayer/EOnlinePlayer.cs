namespace Assets.OnlineMode.OnlinePlayer
{
    using Assets.OnlineMode.Connection;
    using Assets.OnlineMode.GameMatch;

    internal static class EOnlinePlayer
    {
        public static void LeaveMatch ()
        {
            EConnection.Disconnect();
        }

        public static void MarkCell ((int rowIndex, int columnIndex) position)
        {
            EGameMatch.Singleton.MarkCell_ServerRPC(rowIndex: position.rowIndex, columnIndex: position.columnIndex);
        }
    }
}
