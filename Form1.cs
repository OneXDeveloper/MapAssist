/**
 *   Copyright (C) 2021 okaygo
 *   
 *   https://github.com/misterokaygo/D2RAssist/
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
using System.Linq;
using System.Windows.Forms;
using Newtonsoft.Json;
using System.Drawing.Imaging;
using System.Drawing.Drawing2D;
using System.Net.Http;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace D2RAssist
{
    public partial class Form1 : Form
    {

        private static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        private const UInt32 SWP_NOSIZE = 0x0001;
        private const UInt32 SWP_NOMOVE = 0x0002;
        private const UInt32 TOPMOST_FLAGS = SWP_NOMOVE | SWP_NOSIZE;

        private static readonly HttpClient client = new HttpClient();
        private Font fnt = new Font("Arial", 10);
        private GameData lastGameData = null;
        private GameData currentGameData = null;
        private SessionData mapApiSession;
        private MapData mapData;
        private Bitmap background;

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            // AllocConsole();
            // AttachConsole(-1);
            //Console.WriteLine("D2R Assist");
            //MapSeedReader.GetMapSeed();

            // StartPosition was set to FormStartPosition.Manual in the properties window.
            Rectangle screen = Screen.PrimaryScreen.WorkingArea;
            int w = Width >= screen.Width ? screen.Width : (screen.Width + Width) / 2;
            int h = Height >= screen.Height ? screen.Height : (screen.Height + Height) / 2;
            this.Location = new Point((screen.Width - w) / 2, (screen.Height - h) / 2);
            this.Size = new Size(w, h);

            Timer GameDataTimer = new Timer();
            GameDataTimer.Interval = 1000;
            GameDataTimer.Tick += new EventHandler(GameDataTimer_Tick);
            GameDataTimer.Start();



            uint initialStyle = (uint)GetWindowLongPtr(this.Handle, -20);
            SetWindowLong(this.Handle, -20, initialStyle | 0x80000 | 0x20);

            SetWindowPos(this.Handle, HWND_TOPMOST, 0, 0, 0, 0, TOPMOST_FLAGS);

            pictureBox1.Location = new Point(0, 0);
            pictureBox1.Width = this.Width;
            pictureBox1.Height = this.Height;
            pictureBox1.BackColor = Color.Transparent;
            // pictureBox1.SizeMode = System.Windows.Forms.PictureBoxSizeMode.CenterImage;

        }

        // This helper static method is required because the 32-bit version of user32.dll does not contain this API
        // (on any versions of Windows), so linking the method will fail at run-time. The bridge dispatches the request
        // to the correct function (GetWindowLong in 32-bit mode and GetWindowLongPtr in 64-bit mode)
        public static IntPtr SetWindowLongPtr(HandleRef hWnd, int nIndex, IntPtr dwNewLong)
        {
            if (IntPtr.Size == 8)
                return SetWindowLongPtr64(hWnd, nIndex, dwNewLong);
            else
                return new IntPtr(SetWindowLong32(hWnd, nIndex, dwNewLong.ToInt32()));
        }

        [DllImport("user32.dll", EntryPoint = "SetWindowLong")]
        private static extern int SetWindowLong32(HandleRef hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr")]
        private static extern IntPtr SetWindowLongPtr64(HandleRef hWnd, int nIndex, IntPtr dwNewLong);

        // If that doesn't work, the following signature can be used alternatively.
        [DllImport("user32.dll")]
        static extern int SetWindowLong(IntPtr hWnd, int nIndex, uint dwNewLong);

        [DllImport("user32.dll", EntryPoint = "GetWindowLong")]
        private static extern IntPtr GetWindowLongPtr32(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", EntryPoint = "GetWindowLongPtr")]
        private static extern IntPtr GetWindowLongPtr64(IntPtr hWnd, int nIndex);

        // This static method is required because Win32 does not support
        // GetWindowLongPtr directly
        public static IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex)
        {
            if (IntPtr.Size == 8)
                return GetWindowLongPtr64(hWnd, nIndex);
            else
                return GetWindowLongPtr32(hWnd, nIndex);
        }

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool AllocConsole();

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool AttachConsole(int dwProcessId);

        private async void GameDataTimer_Tick(object sender, EventArgs e)
        {
            Timer timer = sender as Timer;
            timer.Stop();
            
            currentGameData = MapSeedReader.GetMapSeed();

            if (currentGameData != null)
            {

                if (lastGameData?.MapSeed != currentGameData.MapSeed && currentGameData.HasMapSeed())
                {
                    if (mapApiSession != null)
                    {
                        using (HttpClient client = new HttpClient())
                        {
                            HttpResponseMessage response = await client.DeleteAsync("http://localhost:8080/sessions/" + mapApiSession.id);
                            mapApiSession = null;
                            background = null;
                            mapData = null;
                        }
                    }

                    var values = new Dictionary<string, uint>
                    {
                        // { "id", "1" },
                        {"difficulty", currentGameData.Difficulty},
                        {"mapid", currentGameData.MapSeed}
                    };

                    using (HttpClient client = new HttpClient())
                    {
                        var json = JsonConvert.SerializeObject(values);
                        var content = new StringContent(json, Encoding.UTF8, "application/json");
                        Console.WriteLine(json);
                        Console.WriteLine(content);
                        HttpResponseMessage response = await client.PostAsync("http://localhost:8080/sessions/", content);
                        this.mapApiSession = JsonConvert.DeserializeObject<SessionData>(await response.Content.ReadAsStringAsync());
                        this.background = null;
                        mapData = null;
                    }
                 
                }

                if (currentGameData.HasMapSeed() && this.mapData == null || currentGameData.AreaId != lastGameData?.AreaId && currentGameData.AreaId != 0)
                {
                    await GetMapData();
                }

                if (currentGameData.HasMapSeed() && currentGameData.IsOutsideTown())
                {
                    pictureBox1.Show();
                } else
                {
                    pictureBox1.Hide();
                }



                lastGameData = currentGameData;

                pictureBox1.Refresh();

                timer.Start();
            }
        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {

        }

        private void pictureBox1_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            Graphics graphics = null;
            Graphics backgroundGraphics;
            Bitmap updatedMap;

            int transparency = 0;

            if (this.mapData == null || this.mapData?.mapRows.Length == 0)
            {
                return;
            }

            if (this.background == null)
            {

                MapData mapData = this.mapData;

                var uncroppedBackground = new Bitmap(mapData.mapRows[0].Length, mapData.mapRows.Length, PixelFormat.Format32bppArgb);
                backgroundGraphics = Graphics.FromImage(uncroppedBackground);
                //backgroundGraphics.CompositingMode = CompositingMode.SourceCopy;
                //backgroundGraphics.CompositingQuality = CompositingQuality.HighQuality;
                //backgroundGraphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                //backgroundGraphics.SmoothingMode = SmoothingMode.HighQuality;
                //backgroundGraphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
                // backgroundGraphics.FillRectangle(new SolidBrush(Color.FromArgb(0, 0, 0)), 0, 0, mapData.mapRows[0].Length, mapData.mapRows.Length);

                var doorImgNext = new Bitmap(10, 10, PixelFormat.Format32bppArgb);
                graphics = Graphics.FromImage(doorImgNext);
                graphics.SmoothingMode = SmoothingMode.AntiAlias;
                graphics.FillRectangle(new SolidBrush(Color.FromArgb(237, 107, 0)), 0, 0, 10, 10);

                var doorImgPrev = new Bitmap(10, 10, PixelFormat.Format32bppArgb);
                graphics = Graphics.FromImage(doorImgPrev);
                graphics.SmoothingMode = SmoothingMode.AntiAlias;
                graphics.FillRectangle(new SolidBrush(Color.FromArgb(255, 0, 149)), 0, 0, 10, 10);

                var waypointImg = new Bitmap(10, 10, PixelFormat.Format32bppArgb);
                graphics = Graphics.FromImage(waypointImg);
                graphics.SmoothingMode = SmoothingMode.AntiAlias;
                graphics.FillRectangle(new SolidBrush(Color.FromArgb(16, 140, 235)), 0, 0, 10, 10);


                for (int x = 0; x < mapData.mapRows.Length; x++)
                {
                    for (int y = 0; y < mapData.mapRows[x].Length; y++)
                    {
                        var type = mapData.mapRows[x][y];
                        if (type == 1)
                        {
                            uncroppedBackground.SetPixel(y, x, Color.FromArgb(70, 51, 41));
                        }
                        else if (type == -1)
                        {
                            // uncroppedBackground.SetPixel(y, x, Color.FromArgb(255, 255, 255));
                        }
                        else if (type == 0)
                        {
                            uncroppedBackground.SetPixel(y, x, Color.FromArgb(0, 0, 0));
                        }
                        else if (type == 16)
                        {
                            uncroppedBackground.SetPixel(y, x, Color.FromArgb(168, 56, 50));
                        }
                        else if (type == 7)
                        {
                            uncroppedBackground.SetPixel(y, x, Color.FromArgb(255, 255, 255));
                        }
                        else if (type == 5)
                        {
                            // uncroppedBackground.SetPixel(y, x, Color.FromArgb(0, 0, 0));
                        }
                        else if (type == 33)
                        {
                            uncroppedBackground.SetPixel(y, x, Color.FromArgb(0, 0, 255));
                        }
                        else if (type == 23)
                        {
                            uncroppedBackground.SetPixel(y, x, Color.FromArgb(0, 0, 255));
                        }
                        else if (type == 4)
                        {
                            uncroppedBackground.SetPixel(y, x, Color.FromArgb(0, 255, 255));
                        }
                        else if (type == 21)
                        {
                            uncroppedBackground.SetPixel(y, x, Color.FromArgb(255, 0, 255));
                        }
                        else if (type == 20)
                        {
                            uncroppedBackground.SetPixel(y, x, Color.FromArgb(70, 51, 41));
                        }
                        else if (type == 17)
                        {
                            uncroppedBackground.SetPixel(y, x, Color.FromArgb(255, 51, 255));
                        }
                        else if (type == 3)
                        {
                            uncroppedBackground.SetPixel(y, x, Color.FromArgb(255, 0, 255));
                        }
                        else if (type == 19)
                        {
                            uncroppedBackground.SetPixel(y, x, Color.FromArgb(0, 51, 255));
                        }
                        else if (type == 2)
                        {
                            uncroppedBackground.SetPixel(y, x, Color.FromArgb(10, 51, 23));
                        }
                        else if (type == 37)
                        {
                            uncroppedBackground.SetPixel(y, x, Color.FromArgb(50, 51, 23));
                        }
                        else if (type == 6)
                        {
                            uncroppedBackground.SetPixel(y, x, Color.FromArgb(80, 51, 33));
                        }
                        else if (type == 39)
                        {
                            uncroppedBackground.SetPixel(y, x, Color.FromArgb(20, 11, 33));
                        }
                        else if (type == 53)
                        {
                            uncroppedBackground.SetPixel(y, x, Color.FromArgb(10, 11, 43));
                        }
                        else
                        {
                            uncroppedBackground.SetPixel(y, x, Color.FromArgb(255, 255, 255));
                        }
                    }
                }

                int counter = 0;
                int originX = mapData.levelOrigin.x;
                int originY = mapData.levelOrigin.y;

                foreach (KeyValuePair<string, AdjacentLevel> i in mapData.adjacentLevels)
                {
                    if (mapData.adjacentLevels[i.Key].exits.Length == 0)
                    {
                        continue;
                    }

                    int xnew = mapData.adjacentLevels[i.Key].exits[0].x;
                    int ynew = mapData.adjacentLevels[i.Key].exits[0].y;

                    int xcoord = xnew - originX;
                    int ycoord = ynew - originY;
                    if (counter == 0)
                    {
                        backgroundGraphics.DrawImage(doorImgPrev, new Point(xcoord, ycoord));
                    }
                    else
                    {
                        backgroundGraphics.DrawImage(doorImgNext, new Point(xcoord, ycoord));
                    }
                    counter++;

                }

                foreach (KeyValuePair<string, XY[]> mapObject in mapData.objects)
                {
                    if (mapData.objects[mapObject.Key].Length == 1)
                    {
                        // backgroundGraphics.DrawImage(waypointImg, new Point(mapData.objects[mapObject.Key][0].x - originX, mapData.objects[mapObject.Key][0].y - originY));
                    }
                }

                //var rect = GetAutoCropBounds(uncroppedBackground);
                // background = uncroppedBackground;
                background = CropBitmap(uncroppedBackground);
            }

            double biggestDimension = background.Width > background.Height ? background.Width : background.Height;

            double multiplier = 500 / biggestDimension;

            if (multiplier == 0)
            {
                multiplier = 1;
            }

            updatedMap = ResizeImage((Bitmap)background.Clone(), (int)(background.Width * multiplier), (int)(background.Height * multiplier));
            var playerLoc = new Bitmap(5, 5, PixelFormat.Format32bppArgb);
            graphics = Graphics.FromImage(playerLoc);
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            graphics.FillRectangle(new SolidBrush(Color.FromArgb(255, 0, 0)), 0, 0, 5, 5);
            backgroundGraphics = Graphics.FromImage(updatedMap);
            backgroundGraphics.DrawImage(playerLoc, new Point((int)((this.currentGameData.PlayerX - mapData.levelOrigin.x) * multiplier), (int)((this.currentGameData.PlayerY - mapData.levelOrigin.y) * multiplier)));


            // var backgroundCropped = new Bitmap(maxX, maxY, PixelFormat.Format32bppArgb);
            // graphics = Graphics.FromImage(backgroundCropped);
            //graphics.PixelOffsetMode = PixelOffsetMode.Half;
            //graphics.TranslateTransform(100, 100);
            // g.RotateTransform(53);
            // graphics.DrawImage(background, new Point(0, 0));

            //g.RotateTransform(45);
            // g.ScaleTransform(0.5F, 0.5F);

            updatedMap = RotateImage(updatedMap, 53, true, false, Color.Transparent);

            g.DrawImage(updatedMap, new Point(pictureBox1.Width - updatedMap.Width, 0));
            // g.DrawImage(updatedMap, new Point(pictureBox1.Width / 2, -1 * pictureBox1.Height / 2
            // g.DrawImage(updatedMap, new Point(0, 0));

            // pictureBox1.SizeMode = PictureBoxSizeMode.Zoom;
        }

        public static Bitmap RotateImage(Image inputImage, float angleDegrees, bool upsizeOk,
                                   bool clipOk, Color backgroundColor)
        {
            // Test for zero rotation and return a clone of the input image
            if (angleDegrees == 0f)
                return (Bitmap)inputImage.Clone();

            // Set up old and new image dimensions, assuming upsizing not wanted and clipping OK
            int oldWidth = inputImage.Width;
            int oldHeight = inputImage.Height;
            int newWidth = oldWidth;
            int newHeight = oldHeight;
            float scaleFactor = 1f;

            // If upsizing wanted or clipping not OK calculate the size of the resulting bitmap
            if (upsizeOk || !clipOk)
            {
                double angleRadians = angleDegrees * Math.PI / 180d;

                double cos = Math.Abs(Math.Cos(angleRadians));
                double sin = Math.Abs(Math.Sin(angleRadians));
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
            Bitmap newBitmap = new Bitmap(newWidth, newHeight, backgroundColor == Color.Transparent ?
                                             PixelFormat.Format32bppArgb : PixelFormat.Format24bppRgb);
            newBitmap.SetResolution(inputImage.HorizontalResolution, inputImage.VerticalResolution);

            // Create the Graphics object that does the work
            using (Graphics graphicsObject = Graphics.FromImage(newBitmap))
            {
                graphicsObject.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphicsObject.PixelOffsetMode = PixelOffsetMode.HighQuality;
                graphicsObject.SmoothingMode = SmoothingMode.HighQuality;

                // Fill in the specified background color if necessary
                if (backgroundColor != Color.Transparent)
                    graphicsObject.Clear(backgroundColor);

                // Set up the built-in transformation matrix to do the rotation and maybe scaling
                graphicsObject.TranslateTransform(newWidth / 2f, newHeight / 2f);

                if (scaleFactor != 1f)
                    graphicsObject.ScaleTransform(scaleFactor, scaleFactor);

                graphicsObject.RotateTransform(angleDegrees);
                graphicsObject.TranslateTransform(-oldWidth / 2f, -oldHeight / 2f);

                // Draw the result 
                graphicsObject.DrawImage(inputImage, 0, 0);
            }

            return newBitmap;
        }

        public Bitmap CropBitmap(Bitmap originalBitmap)
        {

            // Find the min/max non-white/transparent pixels
            Point min = new Point(int.MaxValue, int.MaxValue);
            Point max = new Point(int.MinValue, int.MinValue);

            for (int x = 0; x < originalBitmap.Width; ++x)
            {
                for (int y = 0; y < originalBitmap.Height; ++y)
                {
                    Color pixelColor = originalBitmap.GetPixel(x, y);
                    if (pixelColor.A == 255)
                    {
                        if (x < min.X) min.X = x;
                        if (y < min.Y) min.Y = y;

                        if (x > max.X) max.X = x;
                        if (y > max.Y) max.Y = y;
                    }
                }
            }

            // Create a new bitmap from the crop rectangle
            Rectangle cropRectangle = new Rectangle(min.X, min.Y, max.X - min.X, max.Y - min.Y);
            Bitmap newBitmap = new Bitmap(cropRectangle.Width, cropRectangle.Height);
            using (Graphics g = Graphics.FromImage(newBitmap))
            {
                g.DrawImage(originalBitmap, 0, 0, cropRectangle, GraphicsUnit.Pixel);
            }

            return newBitmap;
        }

        public static Rectangle GetAutoCropBounds(Bitmap bitmap)
        {
            int maxX = 0;
            int maxY = 0;

            int minX = bitmap.Width;
            int minY = bitmap.Height;

            for (int x = 0; x < bitmap.Width; x++)
            {
                for (int y = 0; y < bitmap.Height; y++)
                {
                    var c = bitmap.GetPixel(x, y);
                    var w = Color.White;
                    if (c == Color.Transparent)
                    {
                        if (x > maxX)
                            maxX = x;
                        if (x < minX)
                            minX = x;
                        if (y > maxY)
                            maxY = y;
                        if (y < minY)
                            minY = y;
                    }
                }
            }

            maxX += 2;

            return new Rectangle(minX, minY, maxX - minX, maxY - minY);
        }

        private async Task GetMapData()
        {
            if (mapApiSession == null)
            {
                return;
            }

            this.background = null;

            // Get the currently selected item in the ListBox.

            using (HttpClient client = new HttpClient())
            {
                HttpResponseMessage response = await client.GetAsync("http://localhost:8080/sessions/" + mapApiSession.id + "/areas/" + currentGameData.AreaId);
                this.mapData = JsonConvert.DeserializeObject<MapData>(await response.Content.ReadAsStringAsync());
            }
        }

        /// <summary>
        /// Resize the image to the specified width and height.
        /// </summary>
        /// <param name="image">The image to resize.</param>
        /// <param name="width">The width to resize to.</param>
        /// <param name="height">The height to resize to.</param>
        /// <returns>The resized image.</returns>
        public static Bitmap ResizeImage(Image image, int width, int height)
        {
            var destRect = new Rectangle(0, 0, width, height);
            var destImage = new Bitmap(width, height);

            destImage.SetResolution(image.HorizontalResolution, image.VerticalResolution);

            using (var graphics = Graphics.FromImage(destImage))
            {
                graphics.CompositingMode = CompositingMode.SourceCopy;
                graphics.CompositingQuality = CompositingQuality.HighQuality;
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.SmoothingMode = SmoothingMode.HighQuality;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

                using (var wrapMode = new ImageAttributes())
                {
                    wrapMode.SetWrapMode(WrapMode.TileFlipXY);
                    graphics.DrawImage(image, destRect, 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, wrapMode);
                }
            }

            return destImage;
        }
    }
}
