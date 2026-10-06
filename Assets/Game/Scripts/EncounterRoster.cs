using System.Collections.Generic;

namespace MeadowQuest
{
    public enum EncounterHabitat
    {
        Meadow,
        Cave,
        Custom
    }

    public static class EncounterRoster
    {
        public static IReadOnlyList<string> Meadow { get; } = System.Array.AsReadOnly(new[] { "wild", "akabine", "mizuhane", "sunakuri", "haineko", "kororingo" });
        public static IReadOnlyList<string> Cave { get; } = System.Array.AsReadOnly(new[] { "sunakuri", "haineko", "yorumedama", "shizukukurage", "koganegumo" });
    }
}
