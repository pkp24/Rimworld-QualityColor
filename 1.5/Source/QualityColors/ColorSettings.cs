using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace QualityColors
{
    public class ColorSettings : ModSettings
    {
        public static Dictionary<string, Dictionary<QualityCategory, Color>> Presets =
            new Dictionary<string, Dictionary<QualityCategory, Color>>
            {
                {
                    "Legacy", new Dictionary<QualityCategory, Color>
                    {
                        { QualityCategory.Awful, Color.red },
                        { QualityCategory.Poor, Color.red },
                        { QualityCategory.Normal, Color.white },
                        { QualityCategory.Good, Color.cyan },
                        { QualityCategory.Excellent, Color.green },
                        { QualityCategory.Masterwork, Color.green },
                        { QualityCategory.Legendary, Color.yellow }
                    }
                },
                {
                    "WoW", new Dictionary<QualityCategory, Color>
                    {
                        { QualityCategory.Awful, new Color(0.41f, 0.41f, 0.41f) },
                        { QualityCategory.Poor, new Color(0.62f, 0.62f, 0.62f) },
                        { QualityCategory.Normal, Color.white },
                        { QualityCategory.Good, new Color(0.12f, 1f, 1f) },
                        { QualityCategory.Excellent, new Color(0, 0.44f, 0.87f) },
                        { QualityCategory.Masterwork, new Color(0.64f, 0.21f, 0.93f) },
                        { QualityCategory.Legendary, new Color(1f, 0.5f, 0) }
                    }
                },
                {
                    "Default", new Dictionary<QualityCategory, Color>
                    {
                        { QualityCategory.Awful, Color.red },
                        { QualityCategory.Poor, new Color(159 / 256f, 103 / 256f, 0) },
                        { QualityCategory.Normal, Color.white },
                        { QualityCategory.Good, Color.green },
                        { QualityCategory.Excellent, Color.blue },
                        { QualityCategory.Masterwork, new Color(178 / 256f, 132 / 256f, 190 / 256f) },
                        { QualityCategory.Legendary, Color.yellow }
                    }
                },
                {
                    "Colorblind", new Dictionary<QualityCategory, Color>
                    {
                        { QualityCategory.Awful, new Color(221 / 256f, 204 / 256f, 119 / 256f) },
                        { QualityCategory.Poor, new Color(136 / 256f, 34 / 256f, 85 / 256f) },
                        { QualityCategory.Normal, Color.white },
                        { QualityCategory.Good, new Color(136 / 256f, 204 / 256f, 238 / 256f) },
                        { QualityCategory.Excellent, new Color(51 / 256f, 34 / 256f, 136 / 256f) },
                        { QualityCategory.Masterwork, new Color(17 / 256f, 119 / 256f, 51 / 256f) },
                        { QualityCategory.Legendary, new Color(221 / 256f, 204 / 256f, 119 / 256f) }
                    }
                },
                {
                    "FFXIV", new Dictionary<QualityCategory, Color>
                    {
                        { QualityCategory.Awful, new Color(0.41f, 0.41f, 0.41f) },
                        { QualityCategory.Poor, new Color(0.62f, 0.62f, 0.62f) },
                        { QualityCategory.Normal, Color.white },
                        { QualityCategory.Good, new Color(1, 0.5898438f, 0.8632813f) },
                        { QualityCategory.Excellent, new Color(0.2416077f, 0.7929688f, 0.3406804f) },
                        { QualityCategory.Masterwork, new Color(0.1171875f, 0.3999634f, 1) },
                        { QualityCategory.Legendary, new Color(0.5544863f, 0.2177124f, 0.9609375f) }
                    }
                }
            };

        public Dictionary<QualityCategory, Color> Colors;

        public bool FullLabel;

        public ColorSettings() => Colors = Presets["Default"];

        public override void ExposeData()
        {
            Scribe_Collections.Look(ref Colors, "colors", LookMode.Value, LookMode.Value);
            Scribe_Values.Look(ref FullLabel, "fullLabel");
            if (Colors == null) Colors = Presets["Default"];
        }
    }
}
