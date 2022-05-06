using System;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using MapAssist.Types;
using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.Mathematics.Interop;
using System.Text.RegularExpressions;
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

        public static string ToProperCase(this string text)
        {
            return Regex.Replace(text, "(\\B[A-Z])", " $1");
        }

        public static string ToPascalCase(this string text)
        {
            return text.Replace(" ", "");
        }

        public static float Length(this Point point)
        {
            return point.ToVector().Length();
        }

        // System type conversions
        public static Vector2 ToVector(this Point point)
        {
            return new Vector2(point.X, point.Y);
        }

        public static Point ToPoint(this Vector2 vector)
        {
            return new Point(vector.X, vector.Y);
        }

        public static Rectangle ToRectangle(this Point[] points, float padding = 0)
        {
            var minX = points.Min(point => point.X);
            var maxX = points.Max(point => point.X);
            var minY = points.Min(point => point.Y);
            var maxY = points.Max(point => point.Y);

            return new Rectangle(minX - padding, minY - padding, maxX + padding, maxY + padding);
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

        // SharpDX to System type conversions
        public static Matrix3x2 ToMatrix(this RawMatrix3x2 matrix)
        {
            return new Matrix3x2(matrix.M11, matrix.M12, matrix.M21, matrix.M22, matrix.M31, matrix.M32);
        }

        public static float ScaleFontSize(this Graphics gfx, float fontSize)
        {
            return gfx.Height / (1080 / fontSize);
        }

        public static float LineHeight(this Graphics gfx, float fontSize)
        {
            return fontSize + (gfx.Height / 280f);
        }
    }
}
