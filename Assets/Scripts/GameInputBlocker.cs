public static class GameInputBlocker
{
    public static bool IsInputBlocked { get; private set; }

    public static void SetBlocked(bool isBlocked)
    {
        IsInputBlocked = isBlocked;
    }
}
