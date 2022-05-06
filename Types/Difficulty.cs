namespace MapAssist.Types
{
    public enum Difficulty : ushort
    {
        Normal = 0,
        Nightmare = 1,
        Hell = 2
    }

    public static class DifficultyExtension
    {
        public static bool IsValid(this Difficulty difficulty)
        {
            return (ushort)difficulty >= 0 && (ushort)difficulty <= 2;
        }
    }
}
