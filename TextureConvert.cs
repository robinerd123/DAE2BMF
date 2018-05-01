using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.IO;

namespace TextConv
{ 
    class TextureConvert
    {
        bool bytefirst = true;
        byte bytefirst1 = 0;
        public MemoryStream texture = new MemoryStream();
        public SolidBrush brush = new SolidBrush(Color.HotPink);
        public Color[] CI4Palette = new Color[15];
        public Color[] CI8Palette = new Color[255];

        public Bitmap ExtractTex(byte[] filebytes, string format, int bpp, int x, int y)
        {
            Bitmap result = new Bitmap(x, y);
            Graphics textureGraphics = Graphics.FromImage(result);
            if (format == "RGBA")
            {
                if (bpp == 16)
                {
                    bool first = true;
                    int pixel = 0;
                    int it = 0;
                    foreach (byte filebyte in filebytes)
                    {
                        if (!first)
                        {
                            pixel = pixel | (int)filebyte;
                            //i originally was using binary but it looked messy so i converted the binary to hex
                            brush = new SolidBrush(Color.FromArgb((pixel & 0x01) * 255, ((pixel & 0x7C0) >> 6) * 8, ((pixel & 0xF800) >> 11) * 8, ((pixel & 0x3E) >> 1) * 8));
                            textureGraphics.FillRectangle(brush, it % x, (it - (it % x)) / x, 1, 1);
                            first = true;
                        }
                        else
                        {
                            pixel = (int)filebyte << 8;
                            first = false;
                        }
                        it++;
                    }
                }
                else if (bpp == 32)
                {
                    int first = 0;
                    long pixel = 0;
                    foreach (byte filebyte in filebytes)
                    {
                        if (first == 3)
                        {
                            pixel = pixel | (long)filebyte;
                            brush = new SolidBrush(Color.FromArgb((int)(pixel & 0xFF), (int)(pixel & 0xFF00 >> 8), (int)(pixel & 0xFF0000 >> 16), (int)(pixel & 0xFF000000 >> 24)));
                            textureGraphics.FillRectangle(brush, x, y, 1, 1);
                            first = 0;
                        }
                        else
                        {
                            pixel = (long)filebyte << (first + 1) * 8;
                            first++;
                        }
                    }
                }
            }
            else if (format == "I")
            {
                if (bpp == 4)
                {
                    foreach (byte filebyte in filebytes)
                    {
                        brush = new SolidBrush(Color.FromArgb(255, (int)(filebyte & 0xF0), (int)(filebyte & 0xF0), (int)(filebyte & 0xF0)));
                        textureGraphics.FillRectangle(brush, x, y, 1, 1);
                        brush = new SolidBrush(Color.FromArgb(255, (int)(filebyte & 0x0F << 4), (int)(filebyte & 0x0F << 4), (int)(filebyte & 0x0F << 4)));
                        textureGraphics.FillRectangle(brush, x, y, 1, 1);
                    }
                }
                else if (bpp == 8)
                {
                    foreach (byte filebyte in filebytes)
                    {
                        brush = new SolidBrush(Color.FromArgb(255, (int)(filebyte), (int)(filebyte), (int)(filebyte)));
                        textureGraphics.FillRectangle(brush, x, y, 1, 1);
                    }
                }
            }
            else if (format == "IA")
            {
                if (bpp == 4)
                {
                    foreach (byte filebyte in filebytes)
                    {
                        brush = new SolidBrush(Color.FromArgb((int)(filebyte << 2 & 0xC0), (int)(filebyte & 0xC0), (int)(filebyte & 0xC0), (int)(filebyte & 0xC0)));
                        textureGraphics.FillRectangle(brush, x, y, 1, 1);
                        brush = new SolidBrush(Color.FromArgb((int)(filebyte << 2 & 0xC << 4), (int)(filebyte & 0xC << 4), (int)(filebyte & 0xC << 4), (int)(filebyte & 0xC << 4)));
                        textureGraphics.FillRectangle(brush, x, y, 1, 1);
                    }
                }
                else if (bpp == 8)
                {
                    foreach (byte filebyte in filebytes)
                    {
                        brush = new SolidBrush(Color.FromArgb((int)(filebyte & 0xF << 4), (int)(filebyte & 0xF0), (int)(filebyte & 0xF0), (int)(filebyte & 0xF0)));
                        textureGraphics.FillRectangle(brush, x, y, 1, 1);
                    }
                }
                else if (bpp == 16)
                {
                    bool first = true;
                    int pixel = 0;
                    foreach (byte filebyte in filebytes)
                    {
                        if (!first)
                        {
                            pixel = pixel | (int)filebyte;
                            brush = new SolidBrush(Color.FromArgb(pixel & 0xFF00 >> 8, pixel & 0xFF, pixel & 0x7C0 >> 6 * 8, pixel & 0xF800 >> 11 * 8));
                            textureGraphics.FillRectangle(brush, x, y, 1, 1);
                        }
                        else
                        {
                            pixel = (int)filebyte << 8;
                            first = !first;
                        }
                    }
                }
            }

            return result;
        }

        public void CalculateTex(double[][] rgba, string format, int bpp, int xy, bool loadtopb, string texsize)
        {
            if (format == "RGBA")
            {
                if (bpp == 16)
                {
                    if (loadtopb) { brush = new SolidBrush(Color.FromArgb((((int)rgba[3][xy] + 1) / 255) * 255, (((int)rgba[0][xy]) / 8) * 8, (((int)rgba[1][xy]) / 8) * 8, (((int)rgba[2][xy]) / 8) * 8)); }
                    else
                    {
                        int line1 = ((int)rgba[0][xy]) / 8 << 11 | ((int)rgba[1][xy]) / 8 << 6 | ((int)rgba[2][xy]) / 8 << 1 | ((int)rgba[3][xy] + 1) / 256;
                        texture.Write(new byte[] {
                            (byte) ((line1 >> 8) & 0xff),
                            (byte) (line1 & 0xff)
                        }, 0, 2);
                    }
                }
                else if (bpp == 32)
                {
                    byte red = (byte)rgba[0][xy];
                    byte green = (byte)rgba[1][xy];
                    byte blue = (byte)rgba[2][xy];
                    byte alpha = (byte)rgba[3][xy];
                    if (loadtopb) { brush = new SolidBrush(Color.FromArgb(alpha, red, green, blue)); }
                    else
                    {
                        texture.Write(new byte[] {
                            red,
                            green,
                            blue,
                            alpha
                        }, 0, 4);
                    }
                }
            }
            else if (format == "I")
            {
                if (bpp == 4)
                {
                    byte red = (byte)rgba[0][xy];
                    byte green = (byte)rgba[1][xy];
                    byte blue = (byte)rgba[2][xy];
                    byte grey = (byte)((((int)red + (int)green + (int)blue) / 3) & 0xF0);
                    if (bytefirst == true)
                    {
                        bytefirst = false;
                        bytefirst1 = (byte)(grey);
                    }
                    else
                    {
                        texture.Write(new byte[] {
                            (byte)((int)bytefirst1 | (int)grey >> 4)
                        }, 0, 1);
                        bytefirst = true;
                    }
                    if (loadtopb) { brush = new SolidBrush(Color.FromArgb(255, grey, grey, grey)); }
                }
                else if (bpp == 8)
                {
                    byte red = (byte)rgba[0][xy];
                    byte green = (byte)rgba[1][xy];
                    byte blue = (byte)rgba[2][xy];
                    byte grey = (byte)(((int)red + (int)green + (int)blue) / 3);
                    texture.Write(new byte[] {
                        grey
                    }, 0, 1);
                    if (loadtopb) { brush = new SolidBrush(Color.FromArgb(255, grey, grey, grey)); }
                }
            }
            else if (format == "IA")
            {
                if (bpp == 4)
                {
                    byte grey1 = (byte)((byte)((((int)rgba[0][xy] + (int)rgba[1][xy] + (int)rgba[2][xy]) / 3) >> 6) << 2);
                    byte grey2 = (byte)((byte)(rgba[3][xy]) >> 6);
                    byte grey = (byte)((int)grey1 | (int)grey2);
                    if (bytefirst == true)
                    {
                        bytefirst = false;
                        bytefirst1 = (byte)(grey);
                    }
                    else
                    {
                        texture.Write(new byte[] {
                            (byte)((int)bytefirst1 | (int)grey >> 4)
                        }, 0, 1);
                        bytefirst = true;
                    }
                    if (loadtopb) { brush = new SolidBrush(Color.FromArgb(grey2 << 6, grey1 << 4, grey1 << 4, grey1 << 4)); }
                }
                else if (bpp == 8)
                {
                    byte red = (byte)rgba[0][xy];
                    byte green = (byte)rgba[1][xy];
                    byte blue = (byte)rgba[2][xy];
                    byte grey = (byte)((((int)red + (int)green + (int)blue) / 3) & 0xF0);
                    byte alpha = (byte)(((int)rgba[3][xy] & 0xF0) >> 4);
                    texture.Write(new byte[] {
                        (byte)(grey|alpha)
                    }, 0, 1);
                    if (loadtopb) { brush = new SolidBrush(Color.FromArgb(alpha << 4, grey >> 4 * 64, grey >> 4 * 64, grey >> 4 * 64)); }
                }
                else if (bpp == 16)
                {
                    byte red = (byte)rgba[0][xy];
                    byte green = (byte)rgba[1][xy];
                    byte blue = (byte)rgba[2][xy];
                    byte alpha = (byte)rgba[3][xy];
                    byte grey = (byte)(((int)red + (int)green + (int)blue) / 3);
                    texture.Write(new byte[] {
                        grey,
                        alpha
                    }, 0, 2);
                    if (loadtopb) { brush = new SolidBrush(Color.FromArgb(alpha, grey, grey, grey)); }
                }
            }
            else if (format == "CI")
            {

                if (bpp == 4)
                {
                    byte red = (byte)rgba[0][xy];
                    byte green = (byte)rgba[1][xy];
                    byte blue = (byte)rgba[2][xy];
                    byte alpha = (byte)rgba[3][xy];
                    byte grey = (byte)((((int)red + (int)green + (int)blue) / 3) & 0xF0);
                    if (CI4Palette[grey >> 4] == Color.HotPink)
                    {
                        CI4Palette[grey >> 4] = Color.FromArgb(alpha, red, green, blue);
                    }
                    if (bytefirst == true)
                    {
                        bytefirst = false;
                        bytefirst1 = (byte)(grey);
                    }
                    else
                    {
                        texture.Write(new byte[] {
                            (byte)((int)bytefirst1 | (int)grey >> 4)
                        }, 0, 1);
                        bytefirst = true;
                    }
                    if (loadtopb) { brush = new SolidBrush(CI4Palette[grey >> 4]); }
                }
                else if (bpp == 8)
                {
                    //still gotta add this shit
                    //if (loadtopb) { brush = new SolidBrush(Color.FromArgb(alpha << 4, grey >> 4 * 64, grey >> 4 * 64, grey >> 4 * 64)); }
                }
            }
            if (loadtopb)
            {
                textureGraphics.FillRectangle(brush, xy % (int.Parse(texsize.Split('x')[0])), ((xy - (xy % (int.Parse(texsize.Split('x')[0])))) / ((int.Parse(texsize.Split('x')[0])))), 1, 1);
                //MessageBox.Show(brush.Color + "\n" + (xy % (int.Parse(texSize.Text.Split('x')[0]))).ToString() + "x" + ((xy - (xy % (int.Parse(texSize.Text.Split('x')[0])))) / ((int.Parse(texSize.Text.Split('x')[0])))).ToString());
            }
        }
        public Graphics textureGraphics = Graphics.FromImage(new Bitmap(1, 1));
    }
}
