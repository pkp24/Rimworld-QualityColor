using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using HarmonyLib;
using ModBase;
using RimWorld;
using UnityEngine;
using Verse;

namespace QualityColors
{
    public class QualityColorsMod : BaseMod<ColorSettings>
    {
        private static readonly Regex ColorMatcher = new Regex("<color=#([0-9A-F]{3,6})>(.*?)</color>",
            RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.RightToLeft);

        static QualityColorsMod()
        {
            SettingsRenderer.AddCustomDrawer(typeof(Dictionary<QualityCategory, Color>), typeof(ColorPicker));
        }

        public QualityColorsMod(ModContentPack content) : base("legodude17.QualityColors", null, content)
        {
            Harm.Patch(AccessTools.Method(typeof(TransferableUIUtility), "DrawTransferableInfo"),
                new HarmonyMethod(GetType(), "AddColors"));
            Harm.Patch(
                AccessTools.Method(typeof(CompQuality), "CompInspectStringExtra"),
                postfix: new HarmonyMethod(typeof(QualityColorsMod), "AddColor3"));
            Harm.Patch(AccessTools.Method(typeof(QualityUtility), "GetLabel"),
                postfix: new HarmonyMethod(typeof(QualityColorsMod), "AddColor2"));
            Harm.Patch(AccessTools.Method(typeof(QualityUtility), "GetLabelShort"),
                postfix: new HarmonyMethod(typeof(QualityColorsMod), "AddColor2"));
            Harm.Patch(AccessTools.Method(typeof(GenText), "Truncate",
                    new[] {typeof(string), typeof(float), typeof(Dictionary<string, string>)}),
                new HarmonyMethod(GetType(), "StripColor"), new HarmonyMethod(GetType(), "ReaddColor"));
            Harm.Patch(AccessTools.Method(typeof(GenText), "Truncate",
                    new[] {typeof(TaggedString), typeof(float), typeof(Dictionary<string, TaggedString>)}),
                new HarmonyMethod(GetType(), "StripColorTagged"), new HarmonyMethod(GetType(), "ReaddColorTagged"));
        }

        public static void StripColor(ref string str, out Dictionary<string, string> __state,
            Dictionary<string, string> cache = null)
        {
            __state = new Dictionary<string, string>();
            var replacedStr = ColorMatcher.Replace(str, match => match.Groups[2].Value, int.MaxValue);

            if (cache != null && cache.ContainsKey(replacedStr))
            {
                str = replacedStr;
                return;
            }

            foreach (Match match in ColorMatcher.Matches(str))
                __state.Add(match.Groups[2].Value, match.Groups[1].Value);

            str = replacedStr;
        }

        public static void ReaddColor(ref string __result, string str, Dictionary<string, string> __state,
            Dictionary<string, string> cache = null)
        {
            foreach (var key in __state.Keys)
            {
                var newRes = __result.Replace(key, $"<color=#{__state[key]}>{key}</color>");
                if (newRes == __result)
                    for (var i = key.Length - 1; i >= 1; i--)
                    {
                        newRes = __result.Replace(key.Substring(0, i) + "...",
                            $"<color=#{__state[key]}>{key.Substring(0, i)}</color>...");
                        if (newRes != __result) break;
                    }

                __result = newRes;
            }

            if (cache != null) cache[str] = __result;
        }

        public static void StripColorTagged(ref TaggedString str, out Dictionary<string, string> __state,
            Dictionary<string, TaggedString> cache = null)
        {
            var text = str.RawText;
            var temp = cache?.Select(kv => (kv.Key, kv.Value.RawText)).ToDictionary(val => val.Key, val => val.RawText);
            StripColor(ref text, out __state, temp);
            str = text;
        }

        public static void ReaddColorTagged(ref TaggedString __result, TaggedString str,
            Dictionary<string, string> __state,
            Dictionary<string, TaggedString> cache = null)
        {
            var text = __result.RawText;
            var temp = new Dictionary<string, string>();
            ReaddColor(ref text, str.RawText, __state, temp);
            if (cache != null)
                foreach (var kv in temp)
                    cache[kv.Key] = kv.Value;
            __result = text;
        }

        public static void AddColor(MainTabWindow_Inspect __instance)
        {
            if (Find.Selector.SingleSelectedThing != null && (Find.Selector.NumSelected == 1 || Find.Selector
                .SelectedObjects.OfType<Thing>().Select(t =>
                {
                    t.TryGetQuality(out var cat);
                    return cat;
                }).Distinct().Count() == 1) && Find.Selector.SingleSelectedThing.TryGetQuality(out var qual))
                GUI.color = Settings.Colors[qual];
        }

        public static void AddColor2(QualityCategory cat, ref string __result)
        {
            __result = ColorText(__result, Settings.Colors[cat]);
        }

        public static void AddColor3(CompQuality __instance, ref string __result)
        {
            __result = ColorText(__result, Settings.Colors[__instance.Quality]);
        }

        public static string ColorText(string text, Color color)
        {
            return
                $"<color=#{Mathf.RoundToInt(color.r * 255):X2}{Mathf.RoundToInt(color.g * 255):X2}{Mathf.RoundToInt(color.b * 255):X2}>{text}</color>";
        }

        public static void AddColors(Transferable trad, Rect idRect, ref Color labelColor)
        {
            if (labelColor == Color.white && trad.IsThing && trad.AnyThing.TryGetQuality(out var qual))
                labelColor = Settings.Colors[qual];
        }

        public override void ApplySettings()
        {
            base.ApplySettings();
            var dic = AccessTools.Field(typeof(GenLabel), "labelDictionary").GetValue(null) as IDictionary;
            dic?.Clear();
            Harm.Unpatch(AccessTools.Method(typeof(MainTabWindow_Inspect), "GetLabel"), HarmonyPatchType.Postfix,
                Harm.Id);
            if (Settings.FullLabel)
                Harm.Patch(AccessTools.Method(typeof(MainTabWindow_Inspect), "GetLabel"),
                    postfix: new HarmonyMethod(typeof(QualityColorsMod), "AddColor"));
        }
    }

    public class ColorSettings : BaseModSettings
    {
        public static Dictionary<string, Dictionary<QualityCategory, Color>> Presets =
            new Dictionary<string, Dictionary<QualityCategory, Color>>
            {
                {
                    "Legacy", new Dictionary<QualityCategory, Color>
                    {
                        {QualityCategory.Awful, Color.red},
                        {QualityCategory.Poor, Color.red},
                        {QualityCategory.Normal, Color.white},
                        {QualityCategory.Good, Color.cyan},
                        {QualityCategory.Excellent, Color.green},
                        {QualityCategory.Masterwork, Color.green},
                        {QualityCategory.Legendary, Color.yellow}
                    }
                },
                {
                    "WoW", new Dictionary<QualityCategory, Color>
                    {
                        {QualityCategory.Awful, new Color(0.41f, 0.41f, 0.41f)},
                        {QualityCategory.Poor, new Color(0.62f, 0.62f, 0.62f)},
                        {QualityCategory.Normal, Color.white},
                        {QualityCategory.Good, new Color(0.12f, 1f, 1f)},
                        {QualityCategory.Excellent, new Color(0, 0.44f, 0.87f)},
                        {QualityCategory.Masterwork, new Color(0.64f, 0.21f, 0.93f)},
                        {QualityCategory.Legendary, new Color(1f, 0.5f, 0)}
                    }
                },
                {
                    "Default", new Dictionary<QualityCategory, Color>
                    {
                        {QualityCategory.Awful, Color.red},
                        {QualityCategory.Poor, new Color(159 / 256f, 103 / 256f, 0)},
                        {QualityCategory.Normal, Color.white},
                        {QualityCategory.Good, Color.green},
                        {QualityCategory.Excellent, Color.blue},
                        {QualityCategory.Masterwork, new Color(178 / 256f, 132 / 256f, 190 / 256f)},
                        {QualityCategory.Legendary, Color.yellow}
                    }
                },
                {
                    "Colorblind", new Dictionary<QualityCategory, Color>
                    {
                        {QualityCategory.Awful, new Color(221 / 256f, 204 / 256f, 119 / 256f)},
                        {QualityCategory.Poor, new Color(136 / 256f, 34 / 256f, 85 / 256f)},
                        {QualityCategory.Normal, Color.white},
                        {QualityCategory.Good, new Color(136 / 256f, 204 / 256f, 238 / 256f)},
                        {QualityCategory.Excellent, new Color(51 / 256f, 34 / 256f, 136 / 256f)},
                        {QualityCategory.Masterwork, new Color(17 / 256f, 119 / 256f, 51 / 256f)},
                        {QualityCategory.Legendary, new Color(221 / 256f, 204 / 256f, 119 / 256f)}
                    }
                },
                {
                    "FFXIV", new Dictionary<QualityCategory, Color>
                    {
                        {QualityCategory.Awful, new Color(0.41f, 0.41f, 0.41f)},
                        {QualityCategory.Poor, new Color(0.62f, 0.62f, 0.62f)},
                        {QualityCategory.Normal, Color.white},
                        {QualityCategory.Good, new Color(1, 0.5898438f, 0.8632813f)},
                        {QualityCategory.Excellent, new Color(0.2416077f, 0.7929688f, 0.3406804f)},
                        {QualityCategory.Masterwork, new Color(0.1171875f, 0.3999634f, 1)},
                        {QualityCategory.Legendary, new Color(0.5544863f, 0.2177124f, 0.9609375f)}
                    }
                }
            };

        public Dictionary<QualityCategory, Color> Colors;

        [Default(false)] public bool FullLabel;

        public ColorSettings()
        {
            Colors = Presets["Default"];
        }

        public override void ExposeData()
        {
            Scribe_Collections.Look(ref Colors, "colors", LookMode.Value, LookMode.Value);
            if (Colors == null) Colors = Presets["Default"];
        }
    }
}