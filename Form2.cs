using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.WindowsAPICodePack.Dialogs;
using Microsoft.WindowsAPICodePack;
using System.IO;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using System.IO.Compression;
using DAE2GFX;
using DAE2BMF;
using TextConv;

namespace DAE2BMF
{

    public partial class Form2 : Form
    {
        public int[] xy = { 0 };
        TextureConvert texconv = new TextureConvert();
        public bool testthing = true;
        public Form2()
        {
            InitializeComponent();
            this.FormClosing += new FormClosingEventHandler(Form2_FormClosing);
        }

        private void Form2_FormClosing(Object sender, FormClosingEventArgs e)
        {
            if (!doneconverion)
            {
                DialogResult reconv = MessageBox.Show("The textures have not been converted.\nWould you like to convert them now?", "Warning", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                if (reconv == DialogResult.Yes)
                {
                    buttonSave.PerformClick();
                    MessageBox.Show("Textures converted succesfully.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    this.Close();
                }
                else
                {
                    MessageBox.Show("Operation aborted.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }

        private string ParseEnd(string line)
        {
            string endof = line.Substring(line.IndexOf("</") + "</".Length);
            endof = endof.Split('>')[0];
            return endof;
        }

        private string[] ParseValue(string line)
        {
            string[] result;
            string parsedelement = line.Substring(line.IndexOf("<") + "<".Length); 
            parsedelement = parsedelement.Split('>')[0];
            parsedelement = parsedelement.Split(' ')[0];
            string value = line.Substring(line.IndexOf(">") + ">".Length);
            if (!value.Contains("<"))
            {
                if (parsedelement.Contains("/"))
                {
                    parsedelement = "null";
                }
                result = new string[] { "null", parsedelement };
                return result;
            }
            value = value.Split('<')[0];
            result = new string[] { value, parsedelement };
            return result;
        }

        private float[] ParseFormats(string line, string vtxes)
        {
            string[] values = vtxes.Split();
            float[] elements = new float[values.Length];
            int haah = 0;

            foreach (string value in values)
            {
                elements[haah] = float.Parse(value.Replace('.', ','));
                haah++;
            }
            return elements;
        }

        private string ParseElement(string line, string element)
        {
            element = element + "=\"";
            string elementvalue = line.Substring(line.IndexOf(element) + element.Length);
            elementvalue = elementvalue.Split('\"')[0];
            return elementvalue;
        }
        string texpath = "";
        int items = 0;
        bool firsttimer = true;
        private void Form2_Load(object sender, EventArgs e)
        {
            texBytes.Parent = texturePreview;
            texSize.Parent = texturePreview;
            texBytes.BackColor = Color.Transparent;
            texSize.BackColor = Color.Transparent;
            texpath = Path.GetDirectoryName(File.ReadAllLines(Path.GetDirectoryName(Application.ExecutablePath) + "/settings.bmfo")[0]);
            if(File.ReadAllLines("settings.bmfo").Length > 2 && File.ReadAllLines("settings.bmfo")[2] == "TEXONLY")
            {
                comboTexture.Items.Add(Path.GetFileName(File.ReadAllLines("settings.bmfo")[0]));
                comboFormat.Text = "RGBA";
                comboBpp.Text = "16";
                comboTexture.SelectedIndex = 0;
                buttonSave.PerformClick();
                return;
            }
            string[] lines = File.ReadAllLines(File.ReadAllLines(Path.GetDirectoryName(Application.ExecutablePath) + "/settings.bmfo")[0]);
            bool image = false;
            File.AppendAllText("settings.bmfo", "\n");
            foreach (string line in lines)
            {
                if(ParseValue(line)[1] == "image")
                {
                    image = true;
                }

                if (ParseEnd(line) == "image")
                {
                    image = false;
                }

                if (ParseValue(line)[1] == "init_from" && image == true)
                {
                    if (firsttimer == true)
                    {
                        File.AppendAllText("settings.bmfo", ParseValue(line)[0]);
                        firsttimer = false;
                    }
                    else
                    {
                        File.AppendAllText("settings.bmfo", " " + ParseValue(line)[0]);
                    }
                    comboTexture.Items.Add(ParseValue(line)[0]);
                    materialToolStripMenuItem.Items.Add(ParseValue(line)[0]);
                    items++;
                }
            }
            firsttimer = true;
            File.AppendAllText("settings.bmfo", "\n");
            int repeats = 0;
            while (repeats < items)
            {
                if (firsttimer == true)
                {
                    File.AppendAllText("settings.bmfo", "RGBA16");
                    firsttimer = false;
                }
                else
                {
                    File.AppendAllText("settings.bmfo", " RGBA16");

                }
                repeats++;
            }
            File.AppendAllText("settings.bmfo", "\n32767\nNO");
            try
            {
                comboTexture.SelectedIndex = 0;
                materialToolStripMenuItem.SelectedIndex = 0;
            }
            catch
            {

            }
            int length = 0;
            foreach(string file in System.IO.Directory.GetFiles(Path.GetDirectoryName(File.ReadAllLines("settings.bmfo")[0]), Path.GetFileNameWithoutExtension(File.ReadAllLines("settings.bmfo")[0]) + "_*.dae"))
            {
                string meshname = Path.GetFileNameWithoutExtension(file).Substring(Path.GetFileNameWithoutExtension(file).IndexOf('_') + 1);
                meshMesh.Items.Add(meshname);
                meshCollision.Items.Add(meshname);
                meshShadow.Items.Add(meshname);
                length++;
            }
            string none = "(none)";
            meshMesh.Items.Add(none);
            meshCollision.Items.Add(none);
            meshShadow.Items.Add(none);
            meshMesh.SelectedIndex = length;
            meshCollision.SelectedIndex = length;
            meshShadow.SelectedIndex = length;
        }

        private void comboBox3_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (trigger == false)
            {
                return;
            }
            if (int.Parse(texSize.Text.Split('x')[0]) * int.Parse(texSize.Text.Split('x')[1]) * int.Parse(comboBpp.Text) > 32768)
            {
                texBytes.ForeColor = Color.Red;
            }
            else
            {
                texBytes.ForeColor = Color.Black;
            }
            texBytes.Text = ((int.Parse(texSize.Text.Split('x')[0]) * int.Parse(texSize.Text.Split('x')[1]) * int.Parse(comboBpp.Text)) / 8).ToString() + " Bytes";
            int x = (int.Parse(texSize.Text.Split('x')[0]));
            int y = (int.Parse(texSize.Text.Split('x')[1]));
            Bitmap imagething = new Bitmap(x, y);
            texconv.textureGraphics = Graphics.FromImage(imagething);
            Bitmap imagefile = new Bitmap(Path.GetDirectoryName(File.ReadAllLines("settings.bmfo")[0]) + "\\" + comboTexture.Text);
            Image<Bgra, byte> image = new Image<Bgra, byte>(Path.GetDirectoryName(File.ReadAllLines("settings.bmfo")[0]) + "\\" + comboTexture.Text);
            double[] b = new double[imagefile.Height * imagefile.Width];
            double[] g = new double[imagefile.Height * imagefile.Width];
            double[] r = new double[imagefile.Height * imagefile.Width];
            double[] a = new double[imagefile.Height * imagefile.Width];
            y = imagefile.Height - 1;
            if (flipUVsToolStripMenuItem.Checked)
            {
                y = 0;
            }
            x = 0;
            for (int xy = 0; xy < imagefile.Height * imagefile.Width; xy++)
            {
                if (x == imagefile.Width)
                {
                    if (flipUVsToolStripMenuItem.Checked)
                    {
                        y += 2;
                    }
                    y--;
                    x = 0;
                }
                Bgra pixel = image[y, x];
                b[xy] = pixel.Blue;
                g[xy] = pixel.Green;
                r[xy] = pixel.Red;
                a[xy] = pixel.Alpha;
                x++;
            }
            double[][] rgba = { r, g, b, a}; fixCI();
            for (int xy = 0; xy < imagefile.Height * imagefile.Width; xy++)
            {
                texconv.CalculateTex(rgba, comboFormat.Text, int.Parse(comboBpp.Text), xy, true, texSize.Text);
            }
            texturePreview.Image = imagething;
            if (comboFormat.Text == "YUV")
            {
                if(comboBpp.Text != "16")
                {
                    MessageBox.Show("Invalid BPP for the selected format!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    comboBpp.Text = "16";
                }
            }
            if (comboFormat.Text == "RGBA")
            {
                if (comboBpp.Text == "4" || comboBpp.Text == "8")
                {
                    MessageBox.Show("Invalid BPP for the selected format!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    comboBpp.Text = "16";
                }
            }
            if (comboFormat.Text == "I" )
            {
                if (comboBpp.Text == "32" || comboBpp.Text == "16")
                {
                    MessageBox.Show("Invalid BPP for the selected format!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    comboBpp.Text = "4";
                }
            }
            if (comboFormat.Text == "IA")
            {
                if (comboBpp.Text == "32")
                {
                    MessageBox.Show("Invalid BPP for the selected format!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    comboBpp.Text = "4";
                }
            }
            if (comboFormat.Text == "CI")
            {
                if (comboBpp.Text == "32" || comboBpp.Text == "16")
                {
                    MessageBox.Show("Invalid BPP for the selected format!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    comboBpp.Text = "4";
                }
            }
            string[] formats;
            try { formats = File.ReadAllLines("settings.bmfo")[3].Split(); } catch { return; }
            formats[comboTexture.SelectedIndex] = comboFormat.Text + comboBpp.Text;
            string[] lines = File.ReadAllLines("settings.bmfo");
            lines[3] = string.Join(" ", formats);
            File.WriteAllLines("settings.bmfo", lines);
        }

        //:*`*::image_part_001::image_part_002::image_part_003::image_part_004::image_part_005::image_part_006::*`*:
        //:*`*::image_part_007::image_part_008::image_part_009::image_part_010::image_part_011::image_part_012::*`*:
        //:*`*::image_part_013::image_part_014::image_part_015::image_part_016::image_part_017::image_part_018::*`*:
        //:*`*::image_part_019::image_part_020::image_part_021::image_part_022::image_part_023::image_part_024::*`*:
        //:*`*::image_part_025::image_part_026::image_part_027::image_part_028::image_part_029::image_part_030::*`*:
        //:*`*::image_part_031::image_part_032::image_part_033::image_part_034::image_part_035::image_part_036::*`*:

            private void fixCI()
        {

            if (comboFormat.Text == "CI")
            {
                int iterations = 0;
                foreach (Color color in texconv.CI4Palette)
                {
                    texconv.CI4Palette[iterations] = Color.HotPink;
                    iterations++;
                }
                iterations = 0;
                foreach (Color color in texconv.CI8Palette)
                {
                    texconv.CI8Palette[iterations] = Color.HotPink;
                    iterations++;
                }
            }
        }

        private void comboBox2_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboBpp.Text == "")
            {
                return;
            }
            if (trigger == false)
            {
                return;
            }
            int x = (int.Parse(texSize.Text.Split('x')[0]));
            int y = (int.Parse(texSize.Text.Split('x')[1]));
            Bitmap imagething = new Bitmap(x, y);
            texconv.textureGraphics = Graphics.FromImage(imagething);
            Bitmap imagefile = new Bitmap(Path.GetDirectoryName(File.ReadAllLines("settings.bmfo")[0]) + "\\" + comboTexture.Text);
            Image<Bgra, byte> image = new Image<Bgra, byte>(Path.GetDirectoryName(File.ReadAllLines("settings.bmfo")[0]) + "\\" + comboTexture.Text);
            double[] b = new double[imagefile.Height * imagefile.Width];
            double[] g = new double[imagefile.Height * imagefile.Width];
            double[] r = new double[imagefile.Height * imagefile.Width];
            double[] a = new double[imagefile.Height * imagefile.Width];
            y = imagefile.Height - 1;
            if (flipUVsToolStripMenuItem.Checked)
            {
                y = 0;
            }
            x = 0;
            for (int xy = 0; xy < imagefile.Height * imagefile.Width; xy++)
            {
                if (x == imagefile.Width)
                {
                    if (flipUVsToolStripMenuItem.Checked)
                    {
                        y += 2;
                    }
                    y--;
                    x = 0;
                }
                Bgra pixel = image[y, x];
                b[xy] = pixel.Blue;
                g[xy] = pixel.Green;
                r[xy] = pixel.Red;
                a[xy] = pixel.Alpha;
                x++;
            }
            double[][] rgba = { r, g, b, a}; fixCI();
            for (int xy = 0; xy < imagefile.Height * imagefile.Width; xy++)
            {
                texconv.CalculateTex(rgba, comboFormat.Text, int.Parse(comboBpp.Text), xy, true, texSize.Text);
            }
            texturePreview.Image = imagething;

            if (comboFormat.Text == "YUV")
            {
                if (comboBpp.Text != "16")
                {
                    MessageBox.Show("Invalid BPP for the selected format!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    comboBpp.Text = "16";
                }
            }
            if (comboFormat.Text == "RGBA")
            {
                if (comboBpp.Text == "4" || comboBpp.Text == "8")
                {
                    MessageBox.Show("Invalid BPP for the selected format!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    comboBpp.Text = "16";
                }
            }
            if (comboFormat.Text == "I")
            {
                if (comboBpp.Text == "32" || comboBpp.Text == "16")
                {
                    MessageBox.Show("Invalid BPP for the selected format!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    comboBpp.Text = "4";
                }
            }
            if (comboFormat.Text == "IA")
            {
                if (comboBpp.Text == "32")
                {
                    MessageBox.Show("Invalid BPP for the selected format!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    comboBpp.Text = "4";
                }
            }
            if (comboFormat.Text == "CI")
            {
                if (comboBpp.Text == "32" || comboBpp.Text == "16")
                {
                    MessageBox.Show("Invalid BPP for the selected format!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    comboBpp.Text = "4";
                }
            }
            string[] formats;
            try { formats = File.ReadAllLines("settings.bmfo")[3].Split(); } catch { return; }
            formats[comboTexture.SelectedIndex] = comboFormat.Text + comboBpp.Text;
            string[] lines = File.ReadAllLines("settings.bmfo");
            lines[3] = string.Join(" ", formats);
            File.WriteAllLines("settings.bmfo", lines);
        }

        private void label5_Click(object sender, EventArgs e)
        {

        }

        private void label4_Click(object sender, EventArgs e)
        {

        }

        private bool trigger = true;
        
        private void comboTexture_SelectedIndexChanged(object sender, EventArgs e)
        {
            trigger = false;
            string[] formats;
            try { formats = File.ReadAllLines("settings.bmfo")[3].Split(); } catch { formats = new string[]{ comboFormat.Text + comboBpp.Text }; }
            comboFormat.Text = System.Text.RegularExpressions.Regex.Replace(formats[comboTexture.SelectedIndex], "[^a-zA-Z]", "");
            comboBpp.Text = System.Text.RegularExpressions.Regex.Replace(formats[comboTexture.SelectedIndex], "[^0-9]", "");
            trigger = true;
            if (File.Exists(texpath + "/" + comboTexture.Text))
            {
                int x = Image.FromFile(texpath + "/" + comboTexture.Text).Width;
                int y = Image.FromFile(texpath + "/" + comboTexture.Text).Height;
                if (x * y * int.Parse(comboBpp.Text) > 32768)
                {
                    texBytes.ForeColor = Color.Red;
                }
                else
                {
                    texBytes.ForeColor = Color.Black;
                }
                texSize.Text = x.ToString() + "x" + y.ToString();
                texBytes.Text = ((int.Parse(texSize.Text.Split('x')[0]) * int.Parse(texSize.Text.Split('x')[1]) * int.Parse(comboBpp.Text)) / 8).ToString() + " Bytes";
                Bitmap imagething = new Bitmap(x,y);
                texconv.textureGraphics = Graphics.FromImage(imagething);
                Bitmap imagefile = new Bitmap(Path.GetDirectoryName(File.ReadAllLines("settings.bmfo")[0]) + "\\" + comboTexture.Text);
                Image<Bgra, byte> image = new Image<Bgra, byte>(Path.GetDirectoryName(File.ReadAllLines("settings.bmfo")[0]) + "\\" + comboTexture.Text);
                double[] b = new double[imagefile.Height * imagefile.Width];
                double[] g = new double[imagefile.Height * imagefile.Width];
                double[] r = new double[imagefile.Height * imagefile.Width];
                double[] a = new double[imagefile.Height * imagefile.Width];
                y = imagefile.Height - 1;
                if (flipUVsToolStripMenuItem.Checked)
                {
                    y = 0;
                }
                x = 0;
                for (int xy = 0; xy < imagefile.Height * imagefile.Width; xy++)
                {
                    if (x == imagefile.Width)
                    {
                        if (flipUVsToolStripMenuItem.Checked)
                        {
                            y+=2;
                        }
                        y--;
                        x = 0;
                    }
                    Bgra pixel = image[y, x];
                    b[xy] = pixel.Blue;
                    g[xy] = pixel.Green;
                    r[xy] = pixel.Red;
                    a[xy] = pixel.Alpha;
                    x++;
                }
                double[][] rgba = { r, g, b, a}; fixCI();
                for (int xy = 0; xy < imagefile.Height * imagefile.Width; xy++)
                {
                    texconv.CalculateTex(rgba, comboFormat.Text, int.Parse(comboBpp.Text), xy, true, texSize.Text);
                }
                texturePreview.Image = imagething;
            }
        }

        private void buttonSave_Click(object sender, EventArgs e)
        {
            xy = new int[comboTexture.Items.Count];
            doneconverion = true;
            string[] formats;
            try { formats = File.ReadAllLines("settings.bmfo")[3].Split(); } catch { formats = new string[] { comboFormat.Text + comboBpp.Text }; }
            int it = 0;
            texBytes.ForeColor = Color.Black;
            texpath = Path.GetDirectoryName(File.ReadAllLines(Path.GetDirectoryName(Application.ExecutablePath) + "/settings.bmfo")[0]);
            foreach (var items in comboTexture.Items)
            {
                if (texBytes.ForeColor == Color.Red) {
                    MessageBox.Show(comboTexture.Items[it-1] + "'s size (" + ((int.Parse(texSize.Text.Split('x')[0]) * int.Parse(texSize.Text.Split('x')[1]) * int.Parse(comboBpp.Text)) /8).ToString() + " Bytes) is too large for the N64's TMEM (4096 Bytes)", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                string format = System.Text.RegularExpressions.Regex.Replace(formats[it], "[^a-zA-Z]", "");
                int bpp = int.Parse(System.Text.RegularExpressions.Regex.Replace(formats[it], "[^0-9]", ""));
                comboTexture.SelectedItem = items;
                string texpathold = texpath;
                texpath = File.ReadAllLines("settings.bmfo")[1];
                File.WriteAllText(texpath + "\\" + Path.ChangeExtension(items.ToString(), "." + comboFormat.Text + comboBpp.Text), "");
                //MessageBox.Show("File: " + texpath + "\\" + Path.ChangeExtension(items.ToString(), "." + comboFormat.Text + comboBpp.Text) + "\n" + "Format: " + format + "\n" + "BPP: " + bpp.ToString(), "Texture Conversion Report", MessageBoxButtons.OK, MessageBoxIcon.Information);
                Bitmap imagefile = new Bitmap(texpathold + "\\" + items);
                Image<Bgra, byte> image = new Image<Bgra, byte>(texpathold + "\\" + items);
                double[] b = new double[imagefile.Height * imagefile.Width];
                double[] g = new double[imagefile.Height * imagefile.Width];
                double[] r = new double[imagefile.Height * imagefile.Width];
                double[] a = new double[imagefile.Height * imagefile.Width];
                xy[it] = imagefile.Width << 16 | imagefile.Height;
                int y = imagefile.Height - 1;
                if (flipUVsToolStripMenuItem.Checked)
                {
                    y = 0;
                }
                int x = 0;
                for (int xy = 0; xy < imagefile.Height * imagefile.Width; xy++)
                {
                    if (x == imagefile.Width)
                    {
                        if (flipUVsToolStripMenuItem.Checked)
                        {
                            y += 2;
                        }
                        y--;
                        x = 0;
                    }
                    Bgra pixel = image[y, x];
                    b[xy] = pixel.Blue;
                    g[xy] = pixel.Green;
                    r[xy] = pixel.Red;
                    a[xy] = pixel.Alpha;
                    x++;
                }
                double[][] rgba = { r, g, b, a}; fixCI();
                if (dontIncludeTextureHeaderToolStripMenuItem.CheckState == CheckState.Checked)
                {
                    byte[] toBytes = new byte[16];
                    byte[] toBytes3 = Encoding.ASCII.GetBytes(Path.GetFileNameWithoutExtension(comboTexture.Text));
                    int iter1 = 0;
                    foreach (byte bytee in toBytes3)
                    {
                        toBytes[iter1] = bytee;
                        iter1++;
                    }
                    texconv.texture.Write(new byte[]
                    {
                        0x00, 0x00,                                                                                                                                                                             //u16   extra flag 1
                        toBytes[0], toBytes[1], toBytes[2], toBytes[3], toBytes[4], toBytes[5], toBytes[6], toBytes[7], toBytes[8], toBytes[9], toBytes[0xa], toBytes[0xb], toBytes[0xc], toBytes[0xd],         //char[14]  name
                        0x00, 0x00, 0x00, (byte)(4 - comboFormat.SelectedIndex & 0xff),                                                                                                                         //u32   format
                        0x00, 0x00, 0x00, (byte)(comboBpp.SelectedIndex & 0xff),                                                                                                                                //u32   pixelsize
                        0x00, 0x00, (byte)((imagefile.Width >> 8) & 0xff), (byte)(imagefile.Width & 0xff),                                                                                                      //u32   width
                        0x00, 0x00, (byte)((imagefile.Height >> 8) & 0xff), (byte)(imagefile.Height & 0xff),                                                                                                    //u32   height
                        0x00, 0x00, 0x00, (byte)wraps,                                                                                                                                                          //u32   wraps
                        0x00, 0x00, 0x00, (byte)wrapt,                                                                                                                                                          //u32   wrapt
                        0x00, 0x00, 0x00, (byte)(9-Math.Log(Math.Max(imagefile.Width, imagefile.Height), 2)),                                                                                                   //u32   scale
                        0x00, 0x00, 0x00, 0x00                                                                                                                                                                  //u32   extra flag 2
                    }, 0, 48);

                }
                for (int xy = 0; xy < imagefile.Height * imagefile.Width; xy++)
                {
                    texconv.CalculateTex(rgba, format, bpp, xy, false, texSize.Text);
                }
                it++;
                FileStream texturefs = new FileStream(texpath + "\\" + Path.ChangeExtension(items.ToString(), "." + comboFormat.Text + comboBpp.Text), FileMode.Open);
                texconv.texture.WriteTo(texturefs);
                texturefs.Dispose();
                texconv.texture.Dispose();
                texconv.texture = new MemoryStream();
                texpath = texpathold;
            }
            string[] liness = File.ReadAllLines("settings.bmfo");
            liness[4] = toolStripTextBox1.Text;
            try { File.WriteAllLines("settings.bmfo", liness); } catch { return; }
        }

        private void buttonPreview(object sender, EventArgs e)
        {

            MessageBox.Show("Feature not implemented yet!", "Whoops!", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        private void toolTip1_Popup(object sender, PopupEventArgs e)
        {

        }

        private void texturePreview_Click(object sender, EventArgs e)
        {

        }

        private void nudScaling_ValueChanged(object sender, EventArgs e)
        {

        }

        private void materialToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void vertexAlphaToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void xToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void joinMaterialToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void toolStripTextBox1_Click(object sender, EventArgs e)
        {

        }

        private void flipUVsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (flipTextureToolStripMenuItem.Checked)
            {
                flipTextureToolStripMenuItem.Checked = false;
                flipUVsToolStripMenuItem.Checked = true;
            }
            int x = (int.Parse(texSize.Text.Split('x')[0]));
            int y = (int.Parse(texSize.Text.Split('x')[1]));
            Bitmap imagething = new Bitmap(x, y);
            texconv.textureGraphics = Graphics.FromImage(imagething);
            Bitmap imagefile = new Bitmap(Path.GetDirectoryName(File.ReadAllLines("settings.bmfo")[0]) + "\\" + comboTexture.Text);
            Image<Bgra, byte> image = new Image<Bgra, byte>(Path.GetDirectoryName(File.ReadAllLines("settings.bmfo")[0]) + "\\" + comboTexture.Text);
            double[] b = new double[imagefile.Height * imagefile.Width];
            double[] g = new double[imagefile.Height * imagefile.Width];
            double[] r = new double[imagefile.Height * imagefile.Width];
            double[] a = new double[imagefile.Height * imagefile.Width];
            y = imagefile.Height - 1;
            if (flipUVsToolStripMenuItem.Checked)
            {
                y = 0;
            }
            x = 0;
            for (int xy = 0; xy < imagefile.Height * imagefile.Width; xy++)
            {
                if (x == imagefile.Width)
                {
                    if (flipUVsToolStripMenuItem.Checked)
                    {
                        y += 2;
                    }
                    y--;
                    x = 0;
                }
                Bgra pixel = image[y, x];
                b[xy] = pixel.Blue;
                g[xy] = pixel.Green;
                r[xy] = pixel.Red;
                a[xy] = pixel.Alpha;
                x++;
            }
            double[][] rgba = { r, g, b, a}; fixCI();
            for (int xy = 0; xy < imagefile.Height * imagefile.Width; xy++)
            {
                texconv.CalculateTex(rgba, comboFormat.Text, int.Parse(comboBpp.Text), xy, true, texSize.Text);
            }
            texturePreview.Image = imagething;
        }
        bool doneconverion = false;
        private void changeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CommonOpenFileDialog dialog = new CommonOpenFileDialog();
            dialog.IsFolderPicker = true;
            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                foreach(var elem in comboTexture.Items)
                {
                    if(File.Exists(Path.ChangeExtension(elem.ToString(), "." + comboFormat.Text + comboBpp.Text)))
                    {
                        File.Delete(Path.ChangeExtension(elem.ToString(), "." + comboFormat.Text + comboBpp.Text));
                    }
                }
                string[] settings = File.ReadAllLines("settings.bmfo");
                settings[1] = dialog.FileName;
                File.WriteAllLines("settings.bmfo", settings);
            }
        }

        private void clearTexturesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            doneconverion = false;
            foreach (var item in comboTexture.Items)
            {
                string[] files = System.IO.Directory.GetFiles(File.ReadAllLines("settings.bmfo")[1], Path.GetFileNameWithoutExtension(item.ToString()) + ".*");

                foreach (string file in files)
                {
                    System.IO.File.Delete(file);
                }
            }
        }

        private void flipTextureToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (flipUVsToolStripMenuItem.Checked)
            {
                flipUVsToolStripMenuItem.Checked = false;
                flipTextureToolStripMenuItem.Checked = true;
            }
            int x = (int.Parse(texSize.Text.Split('x')[0]));
            int y = (int.Parse(texSize.Text.Split('x')[1]));
            Bitmap imagething = new Bitmap(x, y);
            texconv.textureGraphics = Graphics.FromImage(imagething);
            Bitmap imagefile = new Bitmap(Path.GetDirectoryName(File.ReadAllLines("settings.bmfo")[0]) + "\\" + comboTexture.Text);
            Image<Bgra, byte> image = new Image<Bgra, byte>(Path.GetDirectoryName(File.ReadAllLines("settings.bmfo")[0]) + "\\" + comboTexture.Text);
            double[] b = new double[imagefile.Height * imagefile.Width];
            double[] g = new double[imagefile.Height * imagefile.Width];
            double[] r = new double[imagefile.Height * imagefile.Width];
            double[] a = new double[imagefile.Height * imagefile.Width];
            y = imagefile.Height - 1;
            if (flipUVsToolStripMenuItem.Checked)
            {
                y = 0;
            }
            x = 0;
            for (int xy = 0; xy < imagefile.Height * imagefile.Width; xy++)
            {
                if (x == imagefile.Width)
                {
                    if (flipUVsToolStripMenuItem.Checked)
                    {
                        y += 2;
                    }
                    y--;
                    x = 0;
                }
                Bgra pixel = image[y, x];
                b[xy] = pixel.Blue;
                g[xy] = pixel.Green;
                r[xy] = pixel.Red;
                a[xy] = pixel.Alpha;
                x++;
            }
            double[][] rgba = { r, g, b, a}; fixCI();
            for (int xy = 0; xy < imagefile.Height * imagefile.Width; xy++)
            {
                texconv.CalculateTex(rgba, comboFormat.Text, int.Parse(comboBpp.Text), xy, true, texSize.Text);
            }
            texturePreview.Image = imagething;
        }

        private void wrapToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            wrapToolStripMenuItem1.CheckState = CheckState.Checked;
            mirrorToolStripMenuItem.CheckState = CheckState.Unchecked;
            clampToolStripMenuItem.CheckState = CheckState.Unchecked;
            wraps = 0;
        }

        private void mirrorToolStripMenuItem_Click(object sender, EventArgs e)
        {

            wrapToolStripMenuItem1.CheckState = CheckState.Unchecked;
            mirrorToolStripMenuItem.CheckState = CheckState.Checked;
            clampToolStripMenuItem.CheckState = CheckState.Unchecked;
            wraps = 1;
        }

        private void clampToolStripMenuItem_Click(object sender, EventArgs e)
        {

            wrapToolStripMenuItem1.CheckState = CheckState.Unchecked;
            mirrorToolStripMenuItem.CheckState = CheckState.Unchecked;
            clampToolStripMenuItem.CheckState = CheckState.Checked;
            wraps = 2;
        }

        private void wrapToolStripMenuItem2_Click(object sender, EventArgs e)
        {

            wrapToolStripMenuItem2.CheckState = CheckState.Checked;
            mirrorToolStripMenuItem1.CheckState = CheckState.Unchecked;
            clampToolStripMenuItem1.CheckState = CheckState.Unchecked;
            wrapt = 0;
        }

        private void mirrorToolStripMenuItem1_Click(object sender, EventArgs e)
        {


            wrapToolStripMenuItem2.CheckState = CheckState.Unchecked;
            mirrorToolStripMenuItem1.CheckState = CheckState.Checked;
            clampToolStripMenuItem1.CheckState = CheckState.Unchecked;
            wrapt = 1;
        }

        private void clampToolStripMenuItem1_Click(object sender, EventArgs e)
        {


            wrapToolStripMenuItem2.CheckState = CheckState.Unchecked;
            mirrorToolStripMenuItem1.CheckState = CheckState.Unchecked;
            clampToolStripMenuItem1.CheckState = CheckState.Checked;
            wrapt = 2;
        }
        int wraps = 0;
        int wrapt = 0;

        private void modelPreviewToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void axisToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void collisionToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }
    }
}
