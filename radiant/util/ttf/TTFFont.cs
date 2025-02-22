﻿using Cosmos.Debug.Kernel;
using Cosmos.System.Graphics;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using Font = LunarLabs.Fonts.Font;

namespace radiant.util.CosmosTTF
{
    public class TTFFont
    {
        private Font font;
        private Debugger debugger = new("TTFFont");

        // Dictionary<color, Dictionary<scale, Dictionary<glyph, GlyphResult>>>
        private Dictionary<int,
                    Dictionary<int,
                        Dictionary<int, GlyphResult>>> cache = new Dictionary<int, Dictionary<int, Dictionary<int, GlyphResult>>>();

        public TTFFont(byte[] data)
        {
            font = new Font(data);
        }

        /// <summary>
        /// Render a glyph
        /// </summary>
        /// <param name="font">Registered font name</param>
        /// <param name="glyph">The character to render</param>
        /// <param name="color">The color to make the text (ignores alpha)</param>
        /// <param name="scalePx">The scale in pixels</param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public GlyphResult? RenderGlyphAsBitmap(Rune glyph, Color color, int scalePx)
        {
            int argbColor = color.ToArgb();
            int runeValue = glyph.Value;

            Dictionary<int, Dictionary<int, GlyphResult>> colorCache;
            Dictionary<int, GlyphResult> scaleCache;

            if (cache.TryGetValue(argbColor, out colorCache))
            {
                if (colorCache.TryGetValue(scalePx, out scaleCache))
                {
                    if (scaleCache.TryGetValue(runeValue, out var foundGlyph))
                    {
                        return foundGlyph;
                    }
                }
                else
                {
                    colorCache[scalePx] = new Dictionary<int, GlyphResult>();
                    scaleCache = colorCache[scalePx];
                }
            }
            else
            {
                cache[argbColor] = new Dictionary<int, Dictionary<int, GlyphResult>>();
                colorCache = cache[argbColor];

                cache[argbColor][scalePx] = new Dictionary<int, GlyphResult>();
                scaleCache = cache[argbColor][scalePx];
            }

            var rgbOffset = ((color.R & 0xFF) << 16) + ((color.G & 0xFF) << 8) + color.B;

            float scale = font.ScaleInPixels(scalePx);
            var glyphRendered = font.RenderGlyph(glyph, scale, rgbOffset);

            if (glyphRendered == null)
            {
                return null;
            }

            var image = glyphRendered.Image;
            var result = new GlyphResult(image, glyphRendered.xAdvance, glyphRendered.yOfs);

            scaleCache[runeValue] = result;
            return result;
        }

        /// <summary>
        /// Gets a glyphs horizontal metrics
        /// </summary>
        public bool GetGlyphHMetrics(Rune c, int px, out int advWidth, out int lsb)
        {
            advWidth = 0;
            lsb = 0;

            int idx = font.FindGlyphIndex(c);

            if (idx == 0)
            {
                return false;
            }

            float scale = font.ScaleInPixels(px);

            font.GetGlyphHMetrics(idx, out advWidth, out lsb);
            advWidth = (int)(advWidth * scale);
            lsb = (int)(lsb * scale);

            return true;
        }

        /// <summary>
        /// Gets the kerning between two characters
        /// </summary>
        public bool GetKerning(Rune left, Rune right, int px, out int kerning)
        {
            kerning = 0;

            float scale = font.ScaleInPixels(px);
            kerning = font.GetKerning(left, right, scale);
            return true;
        }

        public int CalculateWidth(string text, float px)
        {
            float scale = font.ScaleInPixels(px);
            int totalWidth = 0;

            foreach (Rune c in text.EnumerateRunes())
            {
                font.GetCodepointHMetrics(c, out int advWidth, out int lsb);
                totalWidth += advWidth;
            }

            return (int)(totalWidth * scale);
        }

        public void DrawToSurface(ITTFSurface surface, int px, int x, int y, string text, Color color)
        {
            int offX = 0;
            GlyphResult g;

            Rune prevRune = new Rune('\0');

            foreach (Rune c in text.EnumerateRunes())
            {
                var gMaybe = RenderGlyphAsBitmap(c, color, px);
                if (!gMaybe.HasValue) continue;

                g = gMaybe.Value;

                GetGlyphHMetrics(c, px, out int advWidth, out int lsb);
                GetKerning(prevRune, c, px, out int kerning);

                offX += lsb + kerning;
                surface.DrawBitmap(g.bmp, x + offX, y + g.offY);

                if (kerning > 0)
                    offX -= lsb;
                else
                    offX += advWidth - lsb;

                prevRune = c;
            }
        }
    }

    public struct GlyphResult
    {
        public Bitmap bmp;
        public int offY = 0;
        public int offX = 0;

        public int hashCode = 0;

        public GlyphResult(Bitmap bmp, int offX, int offY)
        {
            this.bmp = bmp;
            this.offX = offX;
            this.offY = offY;
        }
    }

    public interface ITTFSurface
    {
        void DrawBitmap(Bitmap bmp, int x, int y);
    }

    public class CGSSurface : ITTFSurface
    {
        private Canvas canvas;

        public CGSSurface(Canvas canvas)
        {
            this.canvas = canvas;
        }

        public void DrawBitmap(Bitmap bmp, int x, int y)
        {
            canvas.DrawImageAlpha(bmp, x, y);
        }
    }
}