using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace QualityColors
{
    public class QualityColorsMod : Mod
    {
        private static readonly Regex ColorMatcher = new Regex("<color=#([0-9A-F]{3,6})>(.*?)</color>",
            RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.RightToLeft);

        private static readonly Dictionary<Thing, QualityCategory> qualCache = new Dictionary<Thing, QualityCategory>();
        private static readonly HashSet<ThingDef> qualityLess = new HashSet<ThingDef>();

        public static Harmony Harm;
        public static ColorSettings Settings;

        public QualityColorsMod(ModContentPack content) : base(content)
        {
            Harm = new Harmony("legodude17.QualityColors");
            Settings = GetSettings<ColorSettings>();
            Harm.Patch(AccessTools.Method(typeof(TransferableUIUtility), "DrawTransferableInfo"),
                new HarmonyMethod(GetType(), nameof(AddColors)));
            Harm.Patch(
                AccessTools.Method(typeof(CompQuality), "CompInspectStringExtra"),
                postfix: new HarmonyMethod(typeof(QualityColorsMod), nameof(AddColor3)));
            Harm.Patch(AccessTools.Method(typeof(QualityUtility), "GetLabel"),
                postfix: new HarmonyMethod(typeof(QualityColorsMod), nameof(AddColor2)));
            Harm.Patch(AccessTools.Method(typeof(QualityUtility), "GetLabelShort"),
                postfix: new HarmonyMethod(typeof(QualityColorsMod), nameof(AddColor2)));
            Harm.Patch(AccessTools.Method(typeof(GenText), "Truncate",
                    new[] { typeof(string), typeof(float), typeof(Dictionary<string, string>) }),
                new HarmonyMethod(GetType(), nameof(StripColor)), new HarmonyMethod(GetType(), nameof(ReaddColor)));
            Harm.Patch(AccessTools.Method(typeof(GenText), "Truncate",
                    new[] { typeof(TaggedString), typeof(float), typeof(Dictionary<string, TaggedString>) }),
                new HarmonyMethod(GetType(), nameof(StripColorTagged)), new HarmonyMethod(GetType(), nameof(ReaddColorTagged)));
            ApplySettings();
        }

        public override string SettingsCategory() => "QualityColors".Translate();

        public override void DoSettingsWindowContents(Rect inRect)
        {
            base.DoSettingsWindowContents(inRect);
            var listing = new Listing_Standard();
            listing.Begin(inRect);
            listing.CheckboxLabeled("QualityColors.FullLabel.Label".Translate(), ref Settings.FullLabel, "QualityColors.FullLabel.Tooltip".Translate());
            listing.Label("QualityColors.Colors.Label".Translate());
            if (listing.ButtonText("QualityColors.Presets".Translate()))
                Find.WindowStack.Add(new FloatMenu(ColorSettings.Presets.Keys.Select(key =>
                        new FloatMenuOption(("QualityColors.Presets." + key).Translate(),
                            () => { Settings.Colors = ColorSettings.Presets[key].ToDictionary(kv => kv.Key, kv => kv.Value); }))
                   .ToList()));
            foreach (var cat in QualityUtility.AllQualityCategories)
                if (Widgets.ButtonText(listing.GetRect(30f), "QualityColors.Change".Translate(cat.GetLabel()), false, true, Settings.Colors[cat]))
                {
                    var glower = new FakeGlower(ColorInt.FromHdrColor(Settings.Colors[cat]), color => Settings.Colors[cat] = color.ToColor);
                    var colorPicker = new Dialog_GlowerColorPicker(glower, new List<CompGlower>(), Widgets.ColorComponents.All, Widgets.ColorComponents.All)
                    {
                        ShowDarklight = false
                    };
                    Find.WindowStack.Add(colorPicker);
                }

            listing.End();
        }

        public static void StripColor(ref string str, out Dictionary<string, string> __state,
            Dictionary<string, string> cache = null)
        {
            __state = new Dictionary<string, string>();
            if (str.NullOrEmpty()) return;
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

        private static bool TryGetQuality(Thing t, out QualityCategory cat)
        {
            if (t == null || qualityLess.Contains(t.def))
            {
                cat = QualityCategory.Normal;
                return false;
            }

            if (qualCache.TryGetValue(t, out cat)) return true;
            if (t.TryGetQuality(out cat))
            {
                qualCache.Add(t, cat);
                return true;
            }

            qualityLess.Add(t.def);
            return false;
        }

        public static void AddColor(MainTabWindow_Inspect __instance)
        {
            if (Find.Selector.SingleSelectedThing != null && (Find.Selector.NumSelected == 1 || Find.Selector
                   .SelectedObjects.OfType<Thing>()
                   .Select(t =>
                    {
                        TryGetQuality(t, out var cat);
                        return cat;
                    })
                   .Distinct()
                   .Count() == 1) && TryGetQuality(Find.Selector.SingleSelectedThing, out var qual))
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

        public static string ColorText(string text, Color color) =>
            $"<color=#{Mathf.RoundToInt(color.r * 255):X2}{Mathf.RoundToInt(color.g * 255):X2}{Mathf.RoundToInt(color.b * 255):X2}>{text}</color>";

        public static void AddColors(Transferable trad, Rect idRect, ref Color labelColor)
        {
            if (labelColor == Color.white && trad.IsThing && TryGetQuality(trad.AnyThing, out var qual))
                labelColor = Settings.Colors[qual];
        }

        public override void WriteSettings()
        {
            base.WriteSettings();
            ApplySettings();
        }

        public void ApplySettings()
        {
            (AccessTools.Field(typeof(GenLabel), "labelDictionary").GetValue(null) as IDictionary)?.Clear();
            (AccessTools.Field(typeof(InspectPaneUtility), "truncatedLabelsCached").GetValue(null) as IDictionary)?.Clear();
            Harm.Unpatch(AccessTools.Method(typeof(MainTabWindow_Inspect), "GetLabel"), HarmonyPatchType.Postfix, Harm.Id);
            if (Settings.FullLabel)
                Harm.Patch(AccessTools.Method(typeof(MainTabWindow_Inspect), "GetLabel"), postfix: new HarmonyMethod(typeof(QualityColorsMod), "AddColor"));
        }
    }
}
