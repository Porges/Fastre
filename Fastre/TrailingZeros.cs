namespace Fastre
{
    internal static class TrailingZeros
    {
        private static readonly int[] Mod37BitPosition =
            {
              32, 0, 1, 26, 2, 23, 27, 0, 3, 16, 24, 30, 28, 11, 0, 13, 4,
              7, 17, 0, 25, 22, 31, 15, 29, 10, 12, 6, 0, 21, 14, 9, 5,
              20, 8, 19, 18
            };

        public static int TrailingZeroes(uint input)
            => Mod37BitPosition[(-input & input) % 37];
    }
}
