/*
Copyright (c) 2016 Stephen Stair (sgstair@akkit.org)

Permission is hereby granted, free of charge, to any person obtaining a copy
 of this software and associated documentation files (the "Software"), to deal
 in the Software without restriction, including without limitation the rights
 to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 copies of the Software, and to permit persons to whom the Software is
 furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
 all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
 THE SOFTWARE.
*/

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace imgscan
{
    public partial class Form1 : Form
    {
        BinaryReader br;
        Bitmap bmp;

        int startPos;

        public Form1()
        {
            InitializeComponent();
            hScrollBar1.Value = 0;
            hScrollBar2.Value = 1024;
            SetupComboBox();
            DoubleBuffered = true;
        }


        void SetupComboBox()
        {
            comboBox1.Items.Clear();
            comboBox1.Items.Add("1bpp Monochrome");
            comboBox1.Items.Add("8bpp Greyscale");
            comboBox1.Items.Add("24bpp RGB");
            comboBox1.Items.Add("32bpp RGB"); 
            comboBox1.Items.Add("24bpp BGR");
            comboBox1.Items.Add("32bpp BGR");

            comboBox1.SelectedIndex = 3;
        }


        private void Form1_Paint(object sender, PaintEventArgs e)
        {
            Bitmap b;
            b = bmp;
            if(b != null)
            {
                lock(b)
                {
                    int y = hScrollBar2.Location.Y + hScrollBar2.Height + 10;
                    e.Graphics.DrawImage(b, 10, y);

                }
            }
        }

        private void Form1_DragDrop(object sender, DragEventArgs e)
        {
            if(e.Data.GetDataPresent("FileDrop"))
            {
                string[] files = (string[])e.Data.GetData("FileDrop");
                LoadFile(files[0]);    
            }
        }

        void LoadFile(string filename)
        {
            lock (this)
            {
                br = new BinaryReader(File.OpenRead(filename));
                startPos = 0;

                hScrollBar1.Maximum = (int)br.BaseStream.Length-1;

            }
            hScrollBar1.Value = 0;
            UpdateText();
            ResizeImage(hScrollBar2.Value, true);
        }

        void UpdateImage()
        {
            Bitmap b = bmp;
            BinaryReader r = br;

            if (b != null && r != null)
            {
                lock (b)
                {
                    bool bgrSwap = false;
                    int bitsPerPixel;
                    PixelFormat pxFormat;
                    switch(comboBox1.SelectedIndex)
                    {
                        case 0: // 1bpp
                            pxFormat = PixelFormat.Format1bppIndexed;
                            bitsPerPixel = 1;
                            break;
                        case 1: // 8bpp
                            pxFormat = PixelFormat.Format8bppIndexed;
                            bitsPerPixel = 8;
                            break;
                        case 2: // 24bpp
                            pxFormat = PixelFormat.Format24bppRgb;
                            bitsPerPixel = 24;
                            break;
                        case 3: // 32bpp
                            pxFormat = PixelFormat.Format32bppRgb;
                            bitsPerPixel = 32;
                            break;
                        case 4: // 24bpp
                            pxFormat = PixelFormat.Format24bppRgb;
                            bitsPerPixel = 24;
                            bgrSwap = true;
                            break;
                        case 5: // 32bpp
                            pxFormat = PixelFormat.Format32bppRgb;
                            bitsPerPixel = 32;
                            bgrSwap = true;
                            break;
                        default:
                            return; // No clue what to do.
                    }

                    Rectangle rect = new Rectangle(0, 0, b.Width, b.Height);
                    BitmapData bd = b.LockBits(rect, ImageLockMode.WriteOnly, pxFormat);


                    int scanLength = b.Width * bitsPerPixel /8;
                    int length = b.Height * scanLength;
                    int available = (int)r.BaseStream.Length - startPos;
                    int pos = startPos;
                    r.BaseStream.Position = startPos;
                    for (int y = 0; y < b.Height; y++)
                    {
                        int sy = (b.Height - y - 1);
                        sy = y;

                        int copy = available;
                        if (copy >= scanLength)
                        {
                            copy = scanLength;
                        }

                        if (copy > 0)
                        {
                            byte[] data = r.ReadBytes(copy);
                            if(bgrSwap)
                            {
                                int step = 3;
                                if(bitsPerPixel == 32)
                                {
                                    step = 4;
                                }
                                int cursor = 0;
                                while((cursor + step - 1) < data.Length)
                                {
                                    byte temp = data[cursor];
                                    data[cursor] = data[cursor + 2];
                                    data[cursor + 2] = temp;
                                    cursor += step;
                                }
                            }
                            Marshal.Copy(data, 0, bd.Scan0 + sy * bd.Stride, copy);
                        }
                        if (copy < scanLength)
                        {
                            // Fill the rest with 0's
                            byte[] fill = new byte[scanLength - copy];
                            Marshal.Copy(fill, 0, bd.Scan0 + sy * bd.Stride + copy, fill.Length);
                        }

                        available -= copy;
                    }

                    b.UnlockBits(bd);

                }
            }


            Invalidate();
        }

        void ResizeImage(int newWidth, bool forceChange = false)
        {
            bool change = forceChange;
            lock(this)
            {
                if(bmp != null)
                {
                    if (bmp.Width != newWidth)
                        bmp = null;
                }

                if(bmp == null)
                {
                    bmp = new Bitmap(newWidth, 1024);
                    change = true;
                }
            }
            if(change)
                UpdateImage();
        }

        private void Form1_DragOver(object sender, DragEventArgs e)
        {
            
        }

        void UpdateText()
        {
            Text = string.Format("Offset {0} - Width {1}", hScrollBar1.Value, hScrollBar2.Value);
        }

        private void hScrollBar1_Scroll(object sender, ScrollEventArgs e)
        {
            startPos = hScrollBar1.Value;
            UpdateText();
            UpdateImage();
        }

        private void hScrollBar2_Scroll(object sender, ScrollEventArgs e)
        {
            UpdateText();
            ResizeImage(hScrollBar2.Value);
        }

        private void Form1_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Copy;
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateImage();
        }
    }
}
