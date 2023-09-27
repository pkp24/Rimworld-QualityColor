using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ModBase;
using RimWorld;
using UnityEngine;
using Verse;

namespace QualityColors
{
    public class ColorPicker : ICustomRenderer
    {
        public float Height => QualityUtility.AllQualityCategories.Count * 30f + 100f;

        public void Render(string label, string tooltip, FieldInfo info, object obj, Listing_Standard listing,
            SettingsRenderer renderer)
        {
            listing.Label(label);
            if (listing.ButtonText("QualityColors.Presets".Translate()))
                Find.WindowStack.Add(new FloatMenu(ColorSettings.Presets.Keys.Select(key =>
                    new FloatMenuOption(("QualityColors.Presets." + key).Translate(),
                        () => { info.SetValue(obj, ColorSettings.Presets[key]); })).ToList()));
            var value = (Dictionary<QualityCategory, Color>) info.GetValue(obj);
            foreach (var cat in QualityUtility.AllQualityCategories)
            {
                var rect = listing.GetRect(30f);
                if (Widgets.ButtonText(rect, "QualityColors.Change".Translate(cat.GetLabel()), false, true, value[cat]))
                    Find.WindowStack.Add(new Dialog_ColorPicker(value[cat], color => value[cat] = color, cat));
            }
        }
    }

    public class Dialog_ColorPicker : Window
    {
        private readonly QualityCategory qual;
        private readonly Texture2D[] textures = new Texture2D[6];
        private Color color;
        private float hue;
        private float sat;
        public Action<Color> SetColor;
        private string tempStr;
        private float value;

        public Dialog_ColorPicker(Color initColor, Action<Color> setColor, QualityCategory cat)
        {
            color = initColor;
            doCloseButton = true;
            SetColor = setColor;
            qual = cat;
            SyncColor();
        }

        public override Vector2 InitialSize => new Vector2(350f, 590f);

        public override void PostClose()
        {
            SetColor(color);
            base.PostClose();
        }

        public override void Close(bool doCloseSound = true)
        {
            SetColor(color);
            base.Close(doCloseSound);
        }

        public void GenerateTextures()
        {
            tempStr = $"{color.r},{color.g},{color.b}";

            var texture = new Texture2D(256, 256);
            for (var i = 0; i <= 256; i++)
            for (var j = 0; j <= 256; j++)
                texture.SetPixel(i, j, Color.HSVToRGB(i / 256f, j / 256f, value));
            texture.Apply();
            textures[0] = texture;

            texture = new Texture2D(15, 256);
            for (var i = 0; i <= 15; i++)
            for (var j = 0; j <= 256; j++)
                texture.SetPixel(i, j, Color.HSVToRGB(hue, sat, j / 256f));
            texture.Apply();
            textures[1] = texture;

            texture = new Texture2D(256, 16);
            for (var i = 0; i <= 15; i++)
            for (var j = 0; j <= 256; j++)
                texture.SetPixel(j, i, new Color(j / 256f, color.g, color.b));
            texture.Apply();
            textures[2] = texture;

            texture = new Texture2D(256, 16);
            for (var i = 0; i <= 15; i++)
            for (var j = 0; j <= 256; j++)
                texture.SetPixel(j, i, new Color(color.r, j / 256f, color.b));
            texture.Apply();
            textures[3] = texture;

            texture = new Texture2D(256, 16);
            for (var i = 0; i <= 15; i++)
            for (var j = 0; j <= 256; j++)
                texture.SetPixel(j, i, new Color(color.r, color.g, j / 256f));
            texture.Apply();
            textures[4] = texture;

            texture = new Texture2D(256, 16);
            for (var i = 0; i <= 15; i++)
            for (var j = 0; j <= 256; j++)
                texture.SetPixel(j, i, color);
            texture.Apply();
            textures[5] = texture;
        }

        public override void DoWindowContents(Rect inRect)
        {
            var rect = inRect.ContractedBy(10f);
            GUI.color = color;
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(new Rect(rect.x, rect.y, 256f, 30f), "QualityColors.Changing".Translate(qual.ToString()));
            Text.Anchor = TextAnchor.UpperLeft;
            GUI.color = Color.white;
            rect.y += 35f;
            var rect2 = new Rect(rect.x, rect.y, 256f, 256f);
            GUI.DrawTexture(rect2, textures[0], ScaleMode.StretchToFill);
            if (Mouse.IsOver(rect2) && Event.current.isMouse && Event.current.button == 0)
            {
                var pos = Event.current.mousePosition;
                hue = (pos.x - rect2.x) / 256f;
                sat = (rect2.yMax - pos.y) / 256f;
                color = Color.HSVToRGB(hue, sat, value);
                GenerateTextures();
            }

            var rect3 = new Rect(rect2.xMax + 5f, rect.y, 15f, 256f);
            GUI.DrawTexture(rect3, textures[1], ScaleMode.StretchToFill);
            if (Mouse.IsOver(rect3) && Event.current.isMouse && Event.current.button == 0)
            {
                value = (rect3.yMax - Event.current.mousePosition.y) / 256f;
                color = Color.HSVToRGB(hue, sat, value);
                GenerateTextures();
            }

            rect.y += 266f;

            rect3 = rect.TopPartPixels(30f);
            rect3.width /= 2;
            tempStr = Widgets.TextField(rect3, tempStr);
            rect3.x = rect3.xMax;
            if (Widgets.ButtonText(rect3, "QualityColors.Apply".Translate()))
                try
                {
                    color = ParseHelper.ParseColor(tempStr);
                    SyncColor();
                }
                catch (FormatException)
                {
                }

            rect.y += 40f;

            rect3 = new Rect(rect.x + 55f, rect.y, 256f, 15f);
            Widgets.Label(new Rect(rect.x, rect.y, 55f, 25f), "QualityColors.Red".Translate());
            GUI.DrawTexture(rect3, textures[2], ScaleMode.StretchToFill);
            if (Mouse.IsOver(rect3) && Event.current.isMouse && Event.current.button == 0)
            {
                Log.Message(
                    $"Mouse: {Event.current.mousePosition.x}, Rect: {rect3.x}, Sub: {Event.current.mousePosition.x - rect3.x}, r will be {(Event.current.mousePosition.x - rect3.x) / 256f}");
                color.r = (Event.current.mousePosition.x - rect3.x) / 256f;
                SyncColor();
            }

            rect.y += 40f;

            rect3 = new Rect(rect.x + 55f, rect.y, 256f, 15f);
            Widgets.Label(new Rect(rect.x, rect.y, 55f, 25f), "QualityColors.Green".Translate());
            GUI.DrawTexture(rect3, textures[3], ScaleMode.StretchToFill);
            if (Mouse.IsOver(rect3) && Event.current.isMouse && Event.current.button == 0)
            {
                color.g = (Event.current.mousePosition.x - rect3.x) / 256f;
                SyncColor();
            }

            rect.y += 40f;

            rect3 = new Rect(rect.x + 55f, rect.y, 256f, 15f);
            Widgets.Label(new Rect(rect.x, rect.y, 55f, 25f), "QualityColors.Blue".Translate());
            GUI.DrawTexture(rect3, textures[4], ScaleMode.StretchToFill);
            if (Mouse.IsOver(rect3) && Event.current.isMouse && Event.current.button == 0)
            {
                color.b = (Event.current.mousePosition.x - rect3.x) / 256f;
                SyncColor();
            }

            rect.y += 40f;

            rect3 = new Rect(rect.x + 55f, rect.y, 256f, 15f);
            Widgets.Label(new Rect(rect.x, rect.y, 55f, 25f), "QualityColors.Preview".Translate());
            GUI.DrawTexture(rect3, textures[5], ScaleMode.StretchToFill);
        }

        private void SyncColor()
        {
            Color.RGBToHSV(color, out hue, out sat, out value);
            GenerateTextures();
        }
    }
}