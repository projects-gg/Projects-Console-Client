namespace MinecraftClient.Inventory
{
    /// <summary>
    /// The game's potion registry (<c>minecraft:potion</c>), which the <c>potion_contents</c> component (1.20.5+)
    /// names by number. Read from the registration order of the 1.21.8, 1.21.11 and 26.3 server jars (the same
    /// 46 potions in all three); 1.20.5 registered the last four too, behind its 1.21 experiment.
    /// </summary>
    public static class PotionRegistry
    {
        private static readonly string[] Names =
        [
            "water", "mundane", "thick", "awkward", "night_vision", "long_night_vision", "invisibility",
            "long_invisibility", "leaping", "long_leaping", "strong_leaping", "fire_resistance", "long_fire_resistance",
            "swiftness", "long_swiftness", "strong_swiftness", "slowness", "long_slowness", "strong_slowness",
            "turtle_master", "long_turtle_master", "strong_turtle_master", "water_breathing", "long_water_breathing",
            "healing", "strong_healing", "harming", "strong_harming", "poison", "long_poison", "strong_poison",
            "regeneration", "long_regeneration", "strong_regeneration", "strength", "long_strength", "strong_strength",
            "weakness", "long_weakness", "luck", "slow_falling", "long_slow_falling", "wind_charged", "weaving",
            "oozing", "infested",
        ];

        /// <summary>Number of potions the registry has.</summary>
        public static int Count => Names.Length;

        /// <summary>"minecraft:swiftness" for 13; null for a number the registry does not have.</summary>
        public static string? Name(int id) => id >= 0 && id < Names.Length ? "minecraft:" + Names[id] : null;
    }
}
