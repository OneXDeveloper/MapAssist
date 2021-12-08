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
using GameOverlay.Windows;
using MapAssist.Types;

namespace MapAssist.Helpers
{
    public static class Extensions
    {
        public static bool IsWaypoint(this GameObject obj) => obj.ToString().Contains("Waypoint");

        public static PointF OffsetFrom(this PointF point, float x, float y)
        {
            return new PointF(point.X - x, point.Y - y);
        }

        public static PointF OffsetFrom(this PointF point, PointF offset)
        {
            return point.OffsetFrom(offset.X, offset.Y);
        }

        public static PointF Multiply(this PointF point, float quantity)
        {
            return point.Multiply(quantity, quantity);
        }

        public static PointF Multiply(this PointF point, float x, float y)
        {
            return new PointF(point.X * x, point.Y * y);
        }

        public static PointF Rotate(this PointF point, float angleDegrees)
        {
            return point.Rotate(angleDegrees, new Point(0, 0));
        }

        public static PointF Rotate(this PointF point, float angleDegrees, PointF centerPoint)
        {
            var angleRadians = angleDegrees * Math.PI / 180d;

            return new PointF(
                (float)(centerPoint.X + Math.Cos(angleRadians) * (point.X - centerPoint.X) - Math.Sin(angleRadians) * (point.Y - centerPoint.Y)),
                (float)(centerPoint.Y + Math.Sin(angleRadians) * (point.X - centerPoint.X) + Math.Cos(angleRadians) * (point.Y - centerPoint.Y))
            );
        }

        public static Point ToPoint(this PointF point)
        {
            return new Point((int)Math.Round(point.X), (int)Math.Round(point.Y));
        }

        public static GameOverlay.Drawing.Point ToGameOverlayPoint(this PointF point)
        {
            return new GameOverlay.Drawing.Point((int)Math.Round(point.X), (int)Math.Round(point.Y));
        }

        public static PointF Center(this SizeF size)
        {
            return new PointF(size.Width / 2f, size.Height / 2f);
        }

        public static PointF Center(this Bitmap bitmap)
        {
            return new PointF(bitmap.Width / 2f, bitmap.Height / 2f);
        }

        public static PointF Center(this GraphicsWindow window)
        {
            return new PointF(window.Width / 2f, window.Height / 2f);
        }

        public static PointF[] MoveToOrigin(this PointF[] points, float padding = 0)
        {
            var minX = points.Min(point => point.X);
            var minY = points.Min(point => point.Y);

            return points.Select(point => new PointF(point.X - minX + padding, point.Y - minY + padding)).ToArray();
        }

        public static SizeF ToSizeF(this PointF[] points)
        {
            var minX = points.Min(point => point.X);
            var maxX = points.Max(point => point.X);
            var minY = points.Min(point => point.Y);
            var maxY = points.Max(point => point.Y);

            return new SizeF(maxX - minX, maxY - minY);
        }

        public static Bitmap ToBitmap(this PointF[] points, float padding = 0)
        {
            return points.ToSizeF().ToBitmap(padding);
        }

        public static Bitmap ToBitmap(this SizeF size, float padding = 0)
        {
            return new Bitmap((int)Math.Ceiling(size.Width + padding * 2), (int)Math.Ceiling(size.Height + padding * 2), PixelFormat.Format32bppArgb);
        }
    }
}
