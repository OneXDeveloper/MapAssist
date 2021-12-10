/**
 *   Copyright (C) 2021 okaygo, OneXDeveloper
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
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
using GameOverlay.Windows;
using MapAssist.Types;

namespace MapAssist.Helpers
{
    public static class Extensions
    {
        public static bool IsWaypoint(this GameObject obj) => obj.ToString().Contains("Waypoint");

        // Math
        public static PointF Subtract(this PointF point, float offset) => point.Subtract(offset, offset);
        public static PointF Subtract(this PointF point, PointF offset) => point.Subtract(offset.X, offset.Y);
        
        public static PointF Subtract(this PointF point, float x, float y)
        {
            return new PointF(point.X - x, point.Y - y);
        }

        public static PointF Add(this PointF point, PointF offset) => point.Add(offset.X, offset.Y);
        public static PointF Add(this PointF point, float x, float y)
        {
            return new PointF(point.X + x, point.Y + y);
        }

        public static PointF Multiply(this PointF point, float factor) => point.Multiply(factor, factor);

        public static PointF Multiply(this PointF point, float x, float y)
        {
            return new PointF(point.X * x, point.Y * y);
        }

        public static PointF Rotate(this PointF point, float angleRadians) => point.Rotate(angleRadians, new Point(0, 0));

        public static PointF Rotate(this PointF point, float angleRadians, PointF centerPoint)
        {
            return new PointF(
              (float)(centerPoint.X + Math.Cos(angleRadians) * (point.X - centerPoint.X) - Math.Sin(angleRadians) * (point.Y - centerPoint.Y)),
              (float)(centerPoint.Y + Math.Sin(angleRadians) * (point.X - centerPoint.X) + Math.Cos(angleRadians) * (point.Y - centerPoint.Y))
            );
        }

        public static float Angle(this PointF point)
        {
            return (float)Math.Atan2(point.Y, point.X);
        }

        // System.Drawing type conversions
        public static Point ToPoint(this PointF point)
        {
            return new Point((int)Math.Round(point.X), (int)Math.Round(point.Y));
        }

        public static Rectangle ToRectangle(this RectangleF rect)
        {
            return new Rectangle((int)Math.Round(rect.X), (int)Math.Round(rect.Y), (int)Math.Round(rect.Width), (int)Math.Round(rect.Height));
        }

        public static PointF Center(this SizeF size)
        {
            return new PointF(size.Width / 2f, size.Height / 2f);
        }

        public static SizeF ToSizeF(this PointF[] points)
        {
            var minX = points.Min(point => point.X);
            var maxX = points.Max(point => point.X);
            var minY = points.Min(point => point.Y);
            var maxY = points.Max(point => point.Y);

            return new SizeF(maxX - minX, maxY - minY);
        }

        public static PointF Center(this Bitmap bitmap)
        {
            return new PointF(bitmap.Width / 2f, bitmap.Height / 2f);
        }

        public static Bitmap ToBitmap(this SizeF size, float padding = 0)
        {
            return new Bitmap((int)Math.Ceiling(size.Width + padding * 2), (int)Math.Ceiling(size.Height + padding * 2), PixelFormat.Format32bppArgb);
        }

        public static Color SetOpacity(this Color color, float opacity)
        {
            return Color.FromArgb((int)(color.A * opacity), color.R, color.G, color.B);
        }

        // System.Drawing to GameOverlay type conversions
        public static GameOverlay.Drawing.Point ToGameOverlayPoint(this PointF point)
        {
            return new GameOverlay.Drawing.Point((int)Math.Round(point.X), (int)Math.Round(point.Y));
        }

        public static GameOverlay.Drawing.Geometry ToGeometry(this PointF[] points, GameOverlay.Drawing.Graphics gfx, bool fill)
        {
            var geo = gfx.CreateGeometry();

            geo.BeginFigure(points[points.Length - 1].ToGameOverlayPoint(), fill);

            for (var i = 0; i < points.Length; i++)
            {
                geo.AddPoint(points[i].ToGameOverlayPoint());
            }

            geo.EndFigure(true);
            geo.Close();

            return geo;
        }

        public static GameOverlay.Drawing.Color ToGameOverlayColor(this Color color)
        {
            return new GameOverlay.Drawing.Color(color.R, color.G, color.B, color.A);
        }

        // GameOverlay to System.Drawing type conversions
        public static PointF Center(this GraphicsWindow window)
        {
            return new PointF(window.Width / 2f, window.Height / 2f);
        }

        public static PointF Center(this GameOverlay.Drawing.Point point)
        {
            return new PointF(point.X / 2f, point.Y / 2f);
        }

        // System.Drawing to SharpDX type conversions
        public static SharpDX.Direct2D1.Bitmap ToDXBitmap(this Bitmap bitmap, SharpDX.Direct2D1.RenderTarget renderTarget)
        {
            var bmpData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly, bitmap.PixelFormat);
            var numBytes = bmpData.Stride * bitmap.Height;
            var byteData = new byte[numBytes];
            IntPtr ptr = bmpData.Scan0;
            Marshal.Copy(ptr, byteData, 0, numBytes);

            var newBmp = new SharpDX.Direct2D1.Bitmap(renderTarget, new SharpDX.Size2(bitmap.Width, bitmap.Height), new SharpDX.Direct2D1.BitmapProperties(renderTarget.PixelFormat));
            newBmp.CopyFromMemory(byteData, bmpData.Stride);

            bitmap.UnlockBits(bmpData);

            return newBmp;
        }
    }
}
