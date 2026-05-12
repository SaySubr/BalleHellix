public static class FireballRoundState
{
    public static bool IsFinished { get; private set; }

    public static bool TryFinish()
    {
        if (IsFinished)
            return false;

        IsFinished = true;
        return true;
    }

    public static void Reset()
    {
        IsFinished = false;
    }
}
