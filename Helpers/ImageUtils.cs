/**
 *   Copyright (C) 2021 okaygo
 *
 *   https://github.com/misterokaygo/MapAssist/
 *
 *  This program is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 *
 *  This program is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 *
 *  You should have received a copy of the GNU General Public License
 *  along with this program.  If not, see <https://www.gnu.org/licenses/>.
 **/

using System;
using System.Diagnostics;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing;

namespace MapAssist.Helpers
{
    public static class ImageUtils
    {
        public static Bitmap RotateImage(Image inputImage, float angleRadians, bool upsizeOk, bool clipOk,
          Color backgroundColor)
        {
            // Test for zero rotation and return a clone of the input image
            if (angleRadians == 0f)
                return (Bitmap)inputImage.Clone();

            // Set up old and new image dimensions, assuming upsizing not wanted and clipping OK
            var oldWidth = inputImage.Width;
            var oldHeight = inputImage.Height;
            var newWidth = oldWidth;
            var newHeight = oldHeight;
            var scaleFactor = 1f;

            // If upsizing wanted or clipping not OK calculate the size of the resulting bitmap
            if (upsizeOk || !clipOk)
            {
                var cos = Math.Abs(Math.Cos(angleRadians));
                var sin = Math.Abs(Math.Sin(angleRadians));
                newWidth = (int)Math.Round(oldWidth * cos + oldHeight * sin);
                newHeight = (int)Math.Round(oldWidth * sin + oldHeight * cos);
            }

            // If upsizing not wanted and clipping not OK need a scaling factor
            if (!upsizeOk && !clipOk)
            {
                scaleFactor = Math.Min((float)oldWidth / newWidth, (float)oldHeight / newHeight);
                newWidth = oldWidth;
                newHeight = oldHeight;
            }

            // Create the new bitmap object. If background color is transparent it must be 32-bit, 
            //  otherwise 24-bit is good enough.
            var newBitmap = new Bitmap(newWidth, newHeight,
              backgroundColor == Color.Transparent ? PixelFormat.Format32bppArgb : PixelFormat.Format24bppRgb);
            newBitmap.SetResolution(inputImage.HorizontalResolution, inputImage.VerticalResolution);

            // Create the Graphics object that does the work
            using (var graphicsObject = Graphics.FromImage(newBitmap))
            {
                graphicsObject.InterpolationMode = InterpolationMode.Bicubic;
                graphicsObject.PixelOffsetMode = PixelOffsetMode.HighSpeed;
                graphicsObject.SmoothingMode = SmoothingMode.HighSpeed;

                // Fill in the specified background color if necessary
                if (backgroundColor != Color.Transparent)
                    graphicsObject.Clear(backgroundColor);

                // Set up the built-in transformation matrix to do the rotation and maybe scaling
                graphicsObject.TranslateTransform(newWidth / 2f, newHeight / 2f);

                if (scaleFactor != 1f)
                    graphicsObject.ScaleTransform(scaleFactor, scaleFactor);

                graphicsObject.RotateTransform((float)(angleRadians * 180f / Math.PI));
                graphicsObject.TranslateTransform(-oldWidth / 2f, -oldHeight / 2f);

                // Draw the result
                graphicsObject.DrawImage(inputImage, 0, 0);
            }

            return newBitmap;
        }

        public static (Bitmap, PointF) CropBitmap(Bitmap originalBitmap)
        {
            // Find the min/max non-white/transparent pixels
            var min = new PointF(int.MaxValue, int.MaxValue);
            var max = new PointF(int.MinValue, int.MinValue);

            unsafe
            {
                var bData = originalBitmap.LockBits(new Rectangle(0, 0, originalBitmap.Width, originalBitmap.Height),
                    ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
                byte bitsPerPixel = 32;
                var scan0 = (byte*)bData.Scan0.ToPointer();

                for (var y = 0; y < bData.Height; ++y)
                {
                    for (var x = 0; x < bData.Width; ++x)
                    {
                        var data = scan0 + y * bData.Stride + x * bitsPerPixel / 8;
                        // data[0 = blue, 1 = green, 2 = red, 3 = alpha]
                        if (data[3] > 0)
                        {
                            if (x < min.X) min.X = x;
                            if (y < min.Y) min.Y = y;

                            if (x > max.X) max.X = x;
                            if (y > max.Y) max.Y = y;
                        }
                    }
                }

                originalBitmap.UnlockBits(bData);
            }

            // Create a new bitmap from the crop rectangle
            var cropRectangle = new RectangleF(min.X, min.Y, max.X - min.X, max.Y - min.Y);
            var newBitmap =  new Bitmap((int)cropRectangle.Width, (int)cropRectangle.Height);
            using (var g = Graphics.FromImage(newBitmap))
            {
                g.DrawImage(originalBitmap, 0, 0, cropRectangle, GraphicsUnit.Pixel);
            }

            return (newBitmap, min);
        }

        /// <summary>
        /// Resize the image to the specified width and height.
        /// </summary>
        /// <param name="image">The image to resize.</param>
        /// <param name="width">The width to resize to.</param>
        /// <param name="height">The height to resize to.</param>
        /// <returns>The resized image.</returns>
        public static Bitmap ResizeImage(Bitmap image, SizeF size)
        {
            var destRect = new RectangleF(0, 0, size.Width, size.Height);
            var destImage = size.ToBitmap();

            destImage.SetResolution(image.HorizontalResolution, image.VerticalResolution);

            using (var graphics = Graphics.FromImage(destImage))
            {
                graphics.CompositingMode = CompositingMode.SourceCopy;
                graphics.CompositingQuality = CompositingQuality.HighSpeed;
                graphics.InterpolationMode = InterpolationMode.Bicubic;
                graphics.SmoothingMode = SmoothingMode.HighSpeed;
                graphics.PixelOffsetMode = PixelOffsetMode.HighSpeed;

                using (var wrapMode = new ImageAttributes())
                {
                    wrapMode.SetWrapMode(WrapMode.TileFlipXY);
                    graphics.DrawImage(image, destRect.ToRectangle(), 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, wrapMode);
                }
            }

            return destImage;
        }
    }
}
