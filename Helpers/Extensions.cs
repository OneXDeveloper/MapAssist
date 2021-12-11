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
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using MapAssist.Types;
using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.Mathematics.Interop;
using Geometry = GameOverlay.Drawing.Geometry;
using Graphics = GameOverlay.Drawing.Graphics;
using Point = GameOverlay.Drawing.Point;
using Rectangle = GameOverlay.Drawing.Rectangle;
using Color = GameOverlay.Drawing.Color;
using GraphicsWindow = GameOverlay.Windows.GraphicsWindow;
using SystemBitmap = System.Drawing.Bitmap;
using SystemColor = System.Drawing.Color;
using SystemImaging = System.Drawing.Imaging;
using SystemRectangle = System.Drawing.Rectangle;

namespace MapAssist.Helpers
{
    public static class Extensions
    {
        public static bool IsWaypoint(this GameObject obj) => obj.ToString().Contains("Waypoint");

        // Math
        public static Point Subtract(this Point point, float offset) => point.Subtract(offset, offset);
        public static Point Subtract(this Point point, Point offset) => point.Subtract(offset.X, offset.Y);
        
        public static Point Subtract(this Point point, float x, float y)
        {
            return new Point(point.X - x, point.Y - y);
        }

        public static Point Add(this Point point, Point offset) => point.Add(offset.X, offset.Y);
        public static Point Add(this Point point, float x, float y)
        {
            return new Point(point.X + x, point.Y + y);
        }

        public static Point Multiply(this Point point, float factor) => point.Multiply(factor, factor);

        public static Point Multiply(this Point point, float x, float y)
        {
            return new Point(point.X * x, point.Y * y);
        }

        public static Point Rotate(this Point point, float angleRadians) => point.Rotate(angleRadians, new Point(0, 0));

        public static Point Rotate(this Point point, float angleRadians, Point centerPoint)
        {
            return new Point(
              (float)(centerPoint.X + Math.Cos(angleRadians) * (point.X - centerPoint.X) - Math.Sin(angleRadians) * (point.Y - centerPoint.Y)),
              (float)(centerPoint.Y + Math.Sin(angleRadians) * (point.X - centerPoint.X) + Math.Cos(angleRadians) * (point.Y - centerPoint.Y))
            );
        }

        public static float Angle(this Point point)
        {
            return (float)Math.Atan2(point.Y, point.X);
        }

        // System type conversions
        public static Vector2 ToVector(this Point point)
        {
            return new Vector2(point.X, point.Y);
        }

        public static Rectangle ToRectangle(this Point[] points)
        {
            var minX = points.Min(point => point.X);
            var maxX = points.Max(point => point.X);
            var minY = points.Min(point => point.Y);
            var maxY = points.Max(point => point.Y);

            return new Rectangle(minX, minY, maxX, maxY);
        }

        public static SystemColor SetOpacity(this SystemColor color, float opacity)
        {
            return SystemColor.FromArgb((int)(color.A * opacity), color.R, color.G, color.B);
        }

        // System to GameOverlay type conversions
        public static Geometry ToGeometry(this Point[] points, Graphics gfx, bool fill)
        {
            var geo = gfx.CreateGeometry();

            geo.BeginFigure(points[points.Length - 1], fill);

            for (var i = 0; i < points.Length; i++)
            {
                geo.AddPoint(points[i]);
            }

            geo.EndFigure(true);
            geo.Close();

            return geo;
        }

        public static Color ToGameOverlayColor(this SystemColor color)
        {
            return new Color(color.R, color.G, color.B, color.A);
        }

        // System to SharpDX type conversions
        public static Bitmap ToDXBitmap(this SystemBitmap bitmap, RenderTarget renderTarget)
        {
            var bmpData = bitmap.LockBits(new SystemRectangle(0, 0, bitmap.Width, bitmap.Height), SystemImaging.ImageLockMode.ReadOnly, bitmap.PixelFormat);
            var numBytes = bmpData.Stride * bitmap.Height;
            var byteData = new byte[numBytes];
            IntPtr ptr = bmpData.Scan0;
            Marshal.Copy(ptr, byteData, 0, numBytes);

            var newBmp = new Bitmap(renderTarget, new Size2(bitmap.Width, bitmap.Height), new BitmapProperties(renderTarget.PixelFormat));
            newBmp.CopyFromMemory(byteData, bmpData.Stride);

            bitmap.UnlockBits(bmpData);

            return newBmp;
        }

        public static RawMatrix3x2 ToDXMatrix(this Matrix3x2 matrix)
        {
            return new RawMatrix3x2(matrix.M11, matrix.M12, matrix.M21, matrix.M22, matrix.M31, matrix.M32);
        }
    }
}
