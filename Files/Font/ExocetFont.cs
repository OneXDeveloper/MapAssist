using System;
using SharpDX.DirectWrite;

namespace MapAssist.Files.Font
{
    class ExocetFont
    {
        SharpDX.Direct2D1.Factory _factory2D;
        SharpDX.DirectWrite.Factory _factoryDWrite;
        ResourceFontLoader _resourceFontLoader;
        FontCollection _fontCollection { get; set; }

        public string FontFamilyName { get; set; }

        public ExocetFont()
        {
            try
            {
                InitDirect2DAndDirectWrite();
                InitCustomFont();
                FontFamilyName = "Exocet Blizzard Mixed Caps";
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed to load Exocet Font");
            }
        }

        /// <summary>
        /// Inits the direct2D and direct write.
        /// </summary>
        private void InitDirect2DAndDirectWrite()
        {
            _factory2D = new SharpDX.Direct2D1.Factory();
            _factoryDWrite = new SharpDX.DirectWrite.Factory();
        }

        /// <summary>
        /// Inits the custom font.
        /// </summary>
        private void InitCustomFont()
        {
            _resourceFontLoader = new ResourceFontLoader(_factoryDWrite);
            _fontCollection = new FontCollection(_factoryDWrite, _resourceFontLoader, _resourceFontLoader.Key);
        }

        public GameOverlay.Drawing.Font CreateFont(float size)
        {
            return new GameOverlay.Drawing.Font(
                new TextFormat(_factoryDWrite, FontFamilyName, size)
            );
        }
    }
}
