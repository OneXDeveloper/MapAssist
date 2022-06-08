namespace MapAssist.Helpers
{
    public static class D2Hash
    {
        private static uint divisor = 1 << 16;

        public static uint? Reverse(uint hash)
        {
            uint tryValue = 0;
            uint incr = 1;
            for (; tryValue < uint.MaxValue; tryValue += incr)
            {
                var seedResult = ((uint)tryValue * 0x6AC690C5 + 666) & 0xFFFFFFFF;

                if (seedResult == hash)
                {
                    return tryValue;
                }

                if (incr == 1 && (seedResult % divisor) == (hash % divisor))
                {
                    incr = divisor;
                }
            }

            return null;
        }
    }
}
