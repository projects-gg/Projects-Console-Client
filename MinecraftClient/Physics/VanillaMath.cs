using System;

namespace MinecraftClient.Physics
{
    /// <summary>
    /// The game's sine and cosine (net.minecraft.util.Mth): a table of 65536 floats, not the exact functions. The game
    /// turns a walk's input and a sprint jump's push by the look with them (Entity.getInputVector, LivingEntity
    /// .jumpFromGround), and an anticheat replays the move with the same table, so the client's physics uses it too.
    /// </summary>
    public static class VanillaMath
    {
        private const double SinScale = 10430.378350470453;
        private static readonly float[] SinTable = BuildSinTable();

        private static float[] BuildSinTable()
        {
            var table = new float[65536];
            for (int i = 0; i < table.Length; i++)
                table[i] = (float)Math.Sin(i / SinScale);
            return table;
        }

        /// <summary>Mth.sin: the table's sine of an angle in radians.</summary>
        public static float Sin(double radians) => SinTable[(int)((long)(radians * SinScale) & 65535L)];

        /// <summary>Mth.cos: the table's sine a quarter turn on.</summary>
        public static float Cos(double radians) => SinTable[(int)((long)(radians * SinScale + 16384.0) & 65535L)];

        /// <summary>A rotation in degrees to radians as the game works it out for these: a float product.</summary>
        public static float ToRadians(float degrees) => degrees * (float)(Math.PI / 180.0);
    }
}
