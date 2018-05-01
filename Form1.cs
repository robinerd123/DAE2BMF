using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using DAE2GFX;
using DAE2BMF;
using System.IO.Compression;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using Collada141;
using Microsoft.VisualBasic;
using TextConv;

namespace DAE2GFX
{
    public partial class Form1 : Form
    {
        TextureConvert texconv = new TextureConvert();

        int[] xy = { 0 };


        public Form1()
        {
            InitializeComponent();
            this.FormClosing += new FormClosingEventHandler(Form1_FormClosing);
        }

        string sFileName = "";
        private void buttonBrowse_Click(object sender, EventArgs e)
        {
            OpenFileDialog loadobj = new OpenFileDialog();
            loadobj.Filter = "COLLADA Files (*.dae)|*.dae|TEXTURE Files|*.*";
            loadobj.FilterIndex = 1;

            if (loadobj.ShowDialog() == DialogResult.OK)
            {
                textPath.Text = loadobj.FileName;
                if (Path.GetExtension(loadobj.FileName) == ".dae")
                {
                    //sFileName = loadobj.FileName;
                    if (Regex.Matches(File.ReadAllText(sFileName), "<geometry").Count!=1)
                    {
                        if (Regex.Matches(File.ReadAllText(sFileName), "<geometry").Count == 0)
                        {
                            MessageBox.Show("This collada file doesn't contain any geometry!", "Warning!", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        }

                        for (int i = 0; i < Regex.Matches(File.ReadAllText(sFileName), "<geometry").Count; i++)
                        {

                            XmlDocument doc = new XmlDocument();
                            doc.Load(sFileName);
                            XmlNamespaceManager mgr = new XmlNamespaceManager(doc.NameTable);
                            mgr.AddNamespace("df", doc.DocumentElement.NamespaceURI);
                            XmlNodeList geometrytags = doc.SelectNodes("//df:COLLADA/df:library_geometries/df:geometry", mgr);
                            string oh = "";
                            foreach(XmlNode geometrytag in geometrytags)
                            {
                                if (geometrytag == geometrytags[i])
                                {
                                    oh = geometrytag.Attributes["name"].Value;
                                }
                                else
                                {
                                    geometrytag.ParentNode.RemoveChild(geometrytag);
                                }
                            }
                            doc.SelectSingleNode("//df:COLLADA/df:asset/df:contributor/df:author", mgr).InnerText = "DAE2BMF";
                            doc.SelectSingleNode("//df:COLLADA/df:asset/df:contributor/df:authoring_tool", mgr).InnerText = "DAE2BMF for ONE64 Engine (https://discord.gg/9Ej79C3)";
                            doc.SelectSingleNode("//df:COLLADA/df:asset/df:modified", mgr).InnerText = DateTime.Now.ToString(@"yyyy-MM-ddTHH\:mm\:ss\:ff");
                            doc.Save(Path.GetDirectoryName(sFileName) + "\\" + Path.GetFileNameWithoutExtension(sFileName) + "_" + oh + Path.GetExtension(sFileName));
                        }
                    }
                    //textPath.Text = sFileName;
                    buttonSaveBmf.Enabled = true;
                    sFileName = textPath.Text;
                    File.WriteAllText(Path.GetDirectoryName(Application.ExecutablePath) + "/settings.bmfo", sFileName + "\n" + Directory.GetCurrentDirectory());
                }
                else
                {
                    if (Path.GetExtension(loadobj.FileName).Contains("RGBA") || Path.GetExtension(loadobj.FileName).Contains("IA") || Path.GetExtension(loadobj.FileName).Contains("I") || Path.GetExtension(loadobj.FileName).Contains("CI"))
                    {
                        var x = Interaction.InputBox("Image width (X):", "Extra params", "");
                        var y = Interaction.InputBox("Image height (Y):", "Extra params", "");
                        Bitmap converted = texconv.ExtractTex(File.ReadAllBytes(loadobj.FileName), Regex.Replace(Path.GetExtension(loadobj.FileName), "[^A-Z _]", ""), int.Parse(Regex.Replace(Path.GetExtension(loadobj.FileName), "[^1-9 _]", "")), int.Parse(x), int.Parse(y));
                        MessageBox.Show(converted.GetPixel(0, 0).ToString());
                        converted.Save(Path.ChangeExtension(loadobj.FileName, "bmp"));
                    }
                    File.WriteAllText(Path.GetDirectoryName(Application.ExecutablePath) + "/settings.bmfo", sFileName + "\n" + Directory.GetCurrentDirectory() + "\nTEXONLY");
                    buttonSaveBmf.Enabled = false;
                }
            }
            button4.PerformClick();
            button1.Enabled = false;

            /*FolderBrowserDialog texdir = new FolderBrowserDialog();

            if (texdir.ShowDialog() == DialogResult.OK)
            {
                tFileName = texdir.SelectedPath;
            }*/

        }
        
        SolidBrush brush = new SolidBrush(Color.HotPink);


        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            if (textPath.Text == "")
            {

                //button2.Enabled = false;
                button4.Enabled = false;
            }
            else
            {
                sFileName = textPath.Text;
                //button2.Enabled = false;
                button4.Enabled = true;
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
        }

        private void button4_Click(object sender, EventArgs e)
        {
            if (!File.Exists(sFileName))
            {
                MessageBox.Show("Unable to access " + sFileName + "!", "Error!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            Form2 frm2 = new Form2();
            frm2.ShowDialog();
            xy = frm2.xy;
            //button2.Enabled = true;

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

        private float[] ParsePolyData(string line)
        {

            string vtxes = ParseValue(line)[0];
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

        private void button3_Click(object sender, EventArgs e)
        {
            if (!File.Exists(sFileName))
            {
                MessageBox.Show("Unable to access " + sFileName + "!", "Error!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            bool rep = false;
            int offsets = 0;
            string trisall = "";
            string vertstring = "";
            string[] vertexids = { };
            string normalsid = "";
            string normalsstring = "";
            string texcoordid = "";
            string texcoordstring = "";
            int textriarraycount = 0;
            int textricount = 0;
            int[] textriarray = new int[File.ReadAllLines("settings.bmfo")[2].Split().Length];
            int[,] tris = new int[0, 0];
            int vertexoffset = 0;
            int texcoordoffset = 0;
            int normaloffset = 0;
            int coloroffset = 0;
            float[,] bind_float_matrix = new float[0,0];
            byte[,] output = new byte[0, 0];
            COLLADA model = COLLADA.Load(sFileName);

            foreach (var item in model.Items)
            {
                var geometries = item as library_geometries;
                if (geometries == null)
                    continue;
                foreach (var geom in geometries.geometry)
                {
                    var mesh = geom.Item as mesh;
                    if (mesh == null)
                        continue;
                    if (1 != 0)
                    {
                        var meshItem = mesh.vertices;
                        var vertices = meshItem as vertices;
                        var inputs = vertices.input;
                        int vertexidnumbers = inputs.Length;
                        vertexids = new string[vertexidnumbers];
                        vertexidnumbers = 0;
                        foreach (var input in inputs)
                            vertexids[vertexidnumbers] = input.source;
                        vertexidnumbers++;
                    }
                    foreach (var meshItem in mesh.Items)
                    {
                        try
                        {
                            textriarray[textriarraycount] = textricount / 3;
                        }
                        catch
                        {
                            break;
                        }
                        textriarraycount++;
                        var triangles = meshItem as polylist;
                        var inputs = triangles.input;
                        offsets = 0;
                        foreach (var input in inputs)
                        {
                            if (input.semantic == "VERTEX")
                            {
                                vertexoffset = offsets;
                            }
                            else if (input.semantic == "TEXCOORD")
                            {
                                texcoordid = input.source;
                                texcoordoffset = offsets;
                            }
                            else if (input.semantic == "NORMAL")
                            {
                                normalsid = input.source;
                                normaloffset = offsets;
                            }

                            offsets++;
                        }
                        trisall = trisall + triangles.p + " ";
                        foreach(var hmmm in triangles.p.Split())
                        {
                            textricount++;
                        }
                    }
                    foreach (var source in mesh.source)
                    {
                        foreach (var vertexid in vertexids)
                        {
                            if (vertexid == "#" + source.id)
                            {
                                var verts = source.Item as float_array;
                                foreach (var vert in verts.Values)
                                    vertstring = vertstring + vert + " ";
                            }
                        }
                        if (normalsid == "#" + source.id)
                        {
                            var normalsarray = source.Item as float_array;
                            foreach (var normal in normalsarray.Values)
                                normalsstring = normalsstring + normal + " ";
                        }
                        else if (texcoordid == "#" + source.id)
                        {
                            var texcoordsarray = source.Item as float_array;
                            foreach (var texcoord in texcoordsarray.Values)
                                texcoordstring = texcoordstring + texcoord + " ";
                        }
                    }
                    foreach (var meshItem in mesh.Items)
                    {
                        if (meshItem is polylist && rep == false)
                        {
                            string trisold2 = trisall;
                            string[] trisold = trisold2.Split();
                            tris = new int[offsets, (trisold.Length + 1 / (offsets + 1))];
                            int currenttri = 0;
                            int currentelem = 0;
                            int currentelem2 = 0;
                            foreach (string elem in trisold)
                            {
                                if (currentelem == offsets - 1)
                                {
                                    currentelem2++;
                                }
                                if (int.TryParse(elem, out int result))
                                {
                                    tris[currenttri % offsets, currentelem2] = result;
                                    //MessageBox.Show("tris[" + (currenttri%offsets).ToString() + ", " + currentelem2 + "] = " + result.ToString());
                                }
                                currentelem = currenttri % offsets;
                                currenttri++;
                            }
                            rep = true;
                        }
                    }
                }

                var controllers = item as library_controllers;
                if (controllers == null)
                    continue;
                foreach (var control in controllers.controller)
                {
                    string bind_name = "";
                    var skins = item as skin;
                    if (skins == null)
                        continue;
                    var joints = item as skinJoints;
                    foreach (var joint in joints.input)
                    {
                        if(joint.semantic == "INV_BIND_MATRIX")
                        {
                            bind_name = joint.source;
                        }
                    }
                    foreach (var source in skins.source)
                    {
                        if(bind_name == "#" + source.id)
                        {
                            var bind_poses = source.Item as float_array;
                            bind_float_matrix = new float[16, bind_poses.count / 16];
                            int iter = 0;
                            int iter2 = 0;
                            foreach (var poses in bind_poses.Values)
                            {
                                bind_float_matrix[iter, iter2] = (float)poses;
                                iter++;
                                if (iter == 16)
                                {
                                    iter2++;
                                    iter = 0;
                                }
                            }
                        }
                    }
                }

                output = new byte[16,bind_float_matrix.GetLength(1)]; 
                var visual_scenes = item as library_visual_scenes;
                if (visual_scenes == null)
                    continue;
                foreach (var visual_scene in visual_scenes.visual_scene)
                {
                    var nodes = visual_scene.node as node[];
                    if (nodes == null)
                        continue;
                    foreach(var node in nodes)
                    {
                        if (node.id == "Armature")
                        {
                            XmlDocument doc = new XmlDocument();
                            doc.Load(sFileName);
                            XmlNodeList list = doc.SelectNodes("library_visual_scenes/visual_scene/node[@id=\"Armature\"]");
                            string nodething = "library_visual_scenes/visual_scene/node";
                            int nodecount = list.Count;
                            int iterthing = 0;
                            for (int j = 0; j < nodecount; j++){
                                XmlNode elem = list[j];
                                if (1 > 0)
                                {
                                    string childid = elem.Name;
                                    int countchild = doc.SelectNodes(nodething + "[@name=\"" + childid + "\"]").Count;
                                    byte[] toBytes = new byte[16];
                                    byte[] toBytes3 = Encoding.ASCII.GetBytes(childid);
                                    int iter1 = 0;
                                    foreach (byte bytee in toBytes3)
                                    {
                                        toBytes[iter1] = bytee;
                                        iter1++;
                                    }
                                    output[0, iterthing] = (byte)((countchild >> 8) & 0xff);
                                    output[1, iterthing] = (byte)(countchild & 0xff);
                                    output[2, iterthing] = toBytes[0];
                                    output[3, iterthing] = toBytes[1];
                                    output[4, iterthing] = toBytes[2];
                                    output[5, iterthing] = toBytes[3];
                                    output[6, iterthing] = toBytes[4];
                                    output[7, iterthing] = toBytes[5];
                                    output[8, iterthing] = toBytes[6];
                                    output[9, iterthing] = toBytes[7];
                                    output[10, iterthing] = toBytes[8];
                                    output[11, iterthing] = toBytes[9];
                                    output[12, iterthing] = toBytes[10];
                                    output[13, iterthing] = toBytes[11];
                                    output[14, iterthing] = toBytes[12];
                                    output[15, iterthing] = toBytes[13];
                                    iterthing++;
                                }
                                while (1 > 0)
                                {
                                    nodething = nodething + "/node";
                                    if (doc.SelectNodes(nodething).Count != 0)
                                    {
                                        string childid = doc.SelectSingleNode(nodething).Name;
                                        int countchild = doc.SelectNodes(nodething + "[@name=\"" + childid + "\"]").Count;
                                        byte[] toBytes = new byte[16];
                                        byte[] toBytes3 = Encoding.ASCII.GetBytes(childid);
                                        int iter1 = 0;
                                        foreach (byte bytee in toBytes3)
                                        {
                                            toBytes[iter1] = bytee;
                                            iter1++;
                                        }
                                        output[0, iterthing] = (byte)((countchild >> 8) & 0xff);
                                        output[1, iterthing] = (byte)(countchild & 0xff);
                                        output[2, iterthing] = toBytes[0];
                                        output[3, iterthing] = toBytes[1];
                                        output[4, iterthing] = toBytes[2];
                                        output[5, iterthing] = toBytes[3];
                                        output[6, iterthing] = toBytes[4];
                                        output[7, iterthing] = toBytes[5];
                                        output[8, iterthing] = toBytes[6];
                                        output[9, iterthing] = toBytes[7];
                                        output[10, iterthing] = toBytes[8];
                                        output[11, iterthing] = toBytes[9];
                                        output[12, iterthing] = toBytes[10];
                                        output[13, iterthing] = toBytes[11];
                                        output[14, iterthing] = toBytes[12];
                                        output[15, iterthing] = toBytes[13];
                                        iterthing++;
                                    }
                                    else
                                    {
                                        nodething = "library_visual_scenes/visual_scene/node";
                                        string childid = doc.SelectSingleNode(nodething).Name;
                                        byte[] toBytes = new byte[16];
                                        byte[] toBytes3 = Encoding.ASCII.GetBytes(childid);
                                        int iter1 = 0;
                                        foreach (byte bytee in toBytes3)
                                        {
                                            toBytes[iter1] = bytee;
                                            iter1++;
                                        }
                                        output[0, iterthing] = 0;
                                        output[1, iterthing] = 0;
                                        output[2, iterthing] = toBytes[0];
                                        output[3, iterthing] = toBytes[1];
                                        output[4, iterthing] = toBytes[2];
                                        output[5, iterthing] = toBytes[3];
                                        output[6, iterthing] = toBytes[4];
                                        output[7, iterthing] = toBytes[5];
                                        output[8, iterthing] = toBytes[6];
                                        output[9, iterthing] = toBytes[7];
                                        output[10, iterthing] = toBytes[8];
                                        output[11, iterthing] = toBytes[9];
                                        output[12, iterthing] = toBytes[10];
                                        output[13, iterthing] = toBytes[11];
                                        output[14, iterthing] = toBytes[12];
                                        output[15, iterthing] = toBytes[13];
                                        break;
                                    }
                                }
                                iterthing++;
                            }
                        }
                    }
                }
            }

            int scaling = int.Parse(File.ReadAllLines("settings.bmfo")[4]);
            string[] vertarray = vertstring.Split();
            string[] normalarray = normalsstring.Split();
            string[] texcoordarray = texcoordstring.Split();
            MemoryStream BMF = new MemoryStream();
            int vertheader = 0;
            string FileName = File.ReadAllLines("settings.bmfo")[1] + "/" + Path.GetFileNameWithoutExtension(sFileName).Replace(' ', '_') + ".bmf";
            using (FileStream fs = File.Create(FileName))
            {
            }
            BMF.Write(new byte[] {
                        0x00,
                        0x00,
                        0x00,
                        0x28,
                        //adding this if i ever want to change this part
                        0x00,
                        0x00,
                        0x00,
                        0x00,
                        //(byte)((tricount >> 8) & 0xff),
                        //(byte)(tricount & 0xff),
                        //(byte)((trioffset >> 8) & 0xff),
                        //(byte)(trioffset & 0xff),
                        0x00,
                        0x00,
                        0xFF,
                        0xFF,
                        0x00,
                        0x00,
                        0xFF,
                        0xFF
                    }, 0, 16);
            BMF.Write(new byte[] {
                        0x00,
                        0x00,
                        0xFF,
                        0xFF,
                        0x00,
                        0x00,
                        0xFF,
                        0xFF,
                        0x00,
                        0x00,
                        0xFF,
                        0xFF,
                        0x00,
                        0x00,
                        0xFF,
                        0xFF
                    }, 0, 16);
            BMF.Write(new byte[] {
                        0x00,
                        0x00,
                        0xFF,
                        0xFF,
                        0x00,
                        0x00,
                        0xFF,
                        0xFF,
                        0x00,
                        0x00,
                        0xFF,
                        0xFF,
                        0x00,
                        0x00,
                        0xFF,
                        0xFF
                    }, 0, 16);
            BMF.Write(new byte[] {
                        0x47,
                        0x45,
                        0x4E,
                        0x45,
                        0x52,
                        0x41,
                        0x54,
                        0x45,
                        0x44,
                        0x00,
                        0x42,
                        0x59,
                        0x00,
                        0x00,
                        0x00,
                        0x00
                    }, 0, 16);
            BMF.Write(new byte[] {
                        0x44,
                        0x41,
                        0x45,
                        0x32,
                        0x42,
                        0x4D,
                        0x46,
                        0x00,
                        0x00,
                        0x00,
                        0x56,
                        0x45,
                        0x52,
                        0x54,
                        0x53,
                        0x3A
                    }, 0, 16);
            int i = 0;
            int iterb = 0;
            int iterrerew = 0;
            while (1 < 2)
            {
                //cheap texcord fix
                if (iterrerew < textriarray.Length && iterb == textriarray[iterrerew]/3)
                {
                    iterrerew++;
                }
                iterb++;

                //vertex1
                int vtxx1, vtxy1, vtxz1;
                double dvtxx1 = Math.Round(float.Parse(vertarray[tris[vertexoffset, i] * 3]) * scaling);
                if (dvtxx1 > -1) { vtxx1 = (int)dvtxx1; } else { vtxx1 = (int)(65535 + dvtxx1); }
                double dvtxy1 = Math.Round(float.Parse(vertarray[tris[vertexoffset, i] * 3 + 1]) * scaling);
                if (dvtxy1 > -1) { vtxy1 = (int)dvtxy1; } else { vtxy1 = (int)(65535 + dvtxy1); }
                double dvtxz1 = Math.Round(float.Parse(vertarray[tris[vertexoffset, i] * 3 + 2]) * scaling);
                if (dvtxz1 > -1) { vtxz1 = (int)dvtxz1; } else { vtxz1 = (int)(65535 + dvtxz1); }
                //texcoord1
                float tcu11 = float.Parse(texcoordarray[tris[texcoordoffset, i] * 2]);
                short tcu1 = Convert.ToInt16(tcu11 * (xy[iterrerew - 1] >> 16) * 32);
                float tcv11 = float.Parse(texcoordarray[tris[texcoordoffset, i] * 2 + 1]);
                short tcv1 = Convert.ToInt16(tcv11 * (xy[iterrerew - 1] & 0xFFFF) * 32);
                //normals1
                double dn11 = Math.Round(float.Parse(normalarray[tris[normaloffset, i] * 3]) * 127);
                double dn21 = Math.Round(float.Parse(normalarray[tris[normaloffset, i] * 3 + 1]) * 127);
                double dn31 = Math.Round(float.Parse(normalarray[tris[normaloffset, i] * 3 + 2]) * 127);


                //vertex2
                int vtxx2, vtxy2, vtxz2;
                double dvtxx2 = Math.Round(float.Parse(vertarray[tris[vertexoffset, i + 1] * 3]) * scaling);
                if (dvtxx2 > -1) { vtxx2 = (int)dvtxx2; } else { vtxx2 = (int)(65535 + dvtxx2); }
                double dvtxy2 = Math.Round(float.Parse(vertarray[tris[vertexoffset, i + 1] * 3 + 1]) * scaling);
                if (dvtxy2 > -1) { vtxy2 = (int)dvtxy2; } else { vtxy2 = (int)(65535 + dvtxy2); }
                double dvtxz2 = Math.Round(float.Parse(vertarray[tris[vertexoffset, i + 1] * 3 + 2]) * scaling);
                if (dvtxz2 > -1) { vtxz2 = (int)dvtxz2; } else { vtxz2 = (int)(65535 + dvtxz2); }
                //texcoord2
                float tcu12 = float.Parse(texcoordarray[tris[texcoordoffset, i + 1] * 2]);
                short tcu2 = Convert.ToInt16(tcu12 * (xy[iterrerew - 1] >> 16) * 32);
                float tcv12 = float.Parse(texcoordarray[tris[texcoordoffset, i + 1] * 2 + 1]);
                short tcv2 = Convert.ToInt16(tcv12 * (xy[iterrerew - 1] & 0xFFFF) * 32);
                //normals2
                double dn12 = Math.Round(float.Parse(normalarray[tris[normaloffset, i + 1] * 3]) * 127);
                double dn22 = Math.Round(float.Parse(normalarray[tris[normaloffset, i + 1] * 3 + 1]) * 127);
                double dn32 = Math.Round(float.Parse(normalarray[tris[normaloffset, i + 1] * 3 + 2]) * 127);


                //vertex3
                int vtxx3, vtxy3, vtxz3;
                double dvtxx3 = Math.Round(float.Parse(vertarray[tris[vertexoffset, i + 2] * 3]) * scaling);
                if (dvtxx3 > -1) { vtxx3 = (int)dvtxx3; } else { vtxx3 = (int)(65535 + dvtxx3); }
                double dvtxy3 = Math.Round(float.Parse(vertarray[tris[vertexoffset, i + 2] * 3 + 1]) * scaling);
                if (dvtxy3 > -1) { vtxy3 = (int)dvtxy3; } else { vtxy3 = (int)(65535 + dvtxy3); }
                double dvtxz3 = Math.Round(float.Parse(vertarray[tris[vertexoffset, i + 2] * 3 + 2]) * scaling);
                if (dvtxz2 > -1) { vtxz3 = (int)dvtxz3; } else { vtxz3 = (int)(65535 + dvtxz3); }
                //texcoord2
                float tcu13 = float.Parse(texcoordarray[tris[texcoordoffset, i + 2] * 2]);
                short tcu3 = Convert.ToInt16(tcu13 * (xy[iterrerew - 1] >> 16) * 32);
                float tcv13 = float.Parse(texcoordarray[tris[texcoordoffset, i + 2] * 2 + 1]);
                short tcv3 = Convert.ToInt16(tcv13 * (xy[iterrerew - 1] & 0xFFFF) * 32);
                //normals2
                double dn13 = Math.Round(float.Parse(normalarray[tris[normaloffset, i + 2] * 3]) * 127);
                double dn23 = Math.Round(float.Parse(normalarray[tris[normaloffset, i + 2] * 3 + 1]) * 127);
                double dn33 = Math.Round(float.Parse(normalarray[tris[normaloffset, i + 2] * 3 + 2]) * 127);



                byte[] vert1 = {
                                    (byte)((vtxx1 >> 8) & 0xff),
                                    (byte)(vtxx1 & 0xff),
                                    (byte)((vtxy1 >> 8) & 0xff),
                                    (byte)(vtxy1 & 0xff),
                                    (byte)((vtxz1 >> 8) & 0xff),
                                    (byte)(vtxz1 & 0xff),
                                    0x00,
                                    0x00,
                                    (byte)((tcu1 >> 8) & 0xff),
                                    (byte)(tcu1 & 0xff),
                                    (byte)((tcv1 >> 8) & 0xff),
                                    (byte)(tcv1 & 0xff),
                                    (byte)dn11,
                                    (byte)dn21,
                                    (byte)dn31,
                                    0xFF };

                byte[] vert2 = {
                                    (byte)((vtxx2 >> 8) & 0xff),
                                    (byte)(vtxx2 & 0xff),
                                    (byte)((vtxy2 >> 8) & 0xff),
                                    (byte)(vtxy2 & 0xff),
                                    (byte)((vtxz2 >> 8) & 0xff),
                                    (byte)(vtxz2 & 0xff),
                                    0x00,
                                    0x00,
                                    (byte)((tcu2 >> 8) & 0xff),
                                    (byte)(tcu2 & 0xff),
                                    (byte)((tcv2 >> 8) & 0xff),
                                    (byte)(tcv2 & 0xff),
                                    (byte)dn12,
                                    (byte)dn22,
                                    (byte)dn32,
                                    0xFF };

                byte[] vert3 = {
                                    (byte)((vtxx3 >> 8) & 0xff),
                                    (byte)(vtxx3 & 0xff),
                                    (byte)((vtxy3 >> 8) & 0xff),
                                    (byte)(vtxy3 & 0xff),
                                    (byte)((vtxz3 >> 8) & 0xff),
                                    (byte)(vtxz3 & 0xff),
                                    0x00,
                                    0x00,
                                    (byte)((tcu3 >> 8) & 0xff),
                                    (byte)(tcu3 & 0xff),
                                    (byte)((tcv3 >> 8) & 0xff),
                                    (byte)(tcv3 & 0xff),
                                    (byte)dn13,
                                    (byte)dn23,
                                    (byte)dn33,
                                    0xFF };
                if (Enumerable.SequenceEqual(vert1, vert2) && Enumerable.SequenceEqual(vert2, vert3))
                {

                }
                else
                {
                    BMF.Write(new byte[] {
                                    //vert1
                                    (byte)((vtxx1 >> 8) & 0xff),
                                    (byte)(vtxx1 & 0xff),
                                    (byte)((vtxy1 >> 8) & 0xff),
                                    (byte)(vtxy1 & 0xff),
                                    (byte)((vtxz1 >> 8) & 0xff),
                                    (byte)(vtxz1 & 0xff),
                                    0x00,
                                    0x00,
                                    (byte)((tcu1 >> 8) & 0xff),
                                    (byte)(tcu1 & 0xff),
                                    (byte)((tcv1 >> 8) & 0xff),
                                    (byte)(tcv1 & 0xff),
                                    (byte)dn11,
                                    (byte)dn21,
                                    (byte)dn31,
                                    0xFF,

                                    //vert2
                                    (byte)((vtxx2 >> 8) & 0xff),
                                    (byte)(vtxx2 & 0xff),
                                    (byte)((vtxy2 >> 8) & 0xff),
                                    (byte)(vtxy2 & 0xff),
                                    (byte)((vtxz2 >> 8) & 0xff),
                                    (byte)(vtxz2 & 0xff),
                                    0x00,
                                    0x00,
                                    (byte)((tcu2 >> 8) & 0xff),
                                    (byte)(tcu2 & 0xff),
                                    (byte)((tcv2 >> 8) & 0xff),
                                    (byte)(tcv2 & 0xff),
                                    (byte)dn12,
                                    (byte)dn22,
                                    (byte)dn32,
                                    0xFF,

                                    //vert3
                                    (byte)((vtxx3 >> 8) & 0xff),
                                    (byte)(vtxx3 & 0xff),
                                    (byte)((vtxy3 >> 8) & 0xff),
                                    (byte)(vtxy3 & 0xff),
                                    (byte)((vtxz3 >> 8) & 0xff),
                                    (byte)(vtxz3 & 0xff),
                                    0x00,
                                    0x00,
                                    (byte)((tcu3 >> 8) & 0xff),
                                    (byte)(tcu3 & 0xff),
                                    (byte)((tcv3 >> 8) & 0xff),
                                    (byte)(tcv3 & 0xff),
                                    (byte)dn13,
                                    (byte)dn23,
                                    (byte)dn33,
                                    0xFF
                    }, 0, 48);
                    vertheader++;
                }
                i+=3;
                if (i >= tris.GetLength(1) - 2)
                {
                    break;
                }
            }
            if (File.ReadAllLines("settings.bmfo")[5] == "YES")
            {

            }
            if (File.ReadAllLines("settings.bmfo")[2] != "")
            {
                string[] textures = File.ReadAllLines("settings.bmfo")[2].Split();
                int iterationals = 0;
                foreach (string texture in textures)
                {
                    byte[] toBytes = new byte[16];
                    byte[] toBytes3 = Encoding.ASCII.GetBytes(Path.GetFileNameWithoutExtension(texture));
                    int iter1 = 0;
                    foreach (byte bytee in toBytes3)
                    {
                        toBytes[iter1] = bytee;
                        iter1++;
                    }
                    BMF.Write(new byte[] {
                    (byte)((textriarray[iterationals]/3 >> 8) & 0xff),
                    (byte)(textriarray[iterationals]/3 & 0xff),
                    toBytes[0],
                    toBytes[1],
                    toBytes[2],
                    toBytes[3],
                    toBytes[4],
                    toBytes[5],
                    toBytes[6],
                    toBytes[7],
                    toBytes[8],
                    toBytes[9],
                    toBytes[0xa],
                    toBytes[0xb],
                    toBytes[0xc],
                    toBytes[0xd]
                }, 0, 16);
                    iterationals++;
                }
                byte[] toBytes1 = new byte[16];
                byte[] toBytes4 = Encoding.ASCII.GetBytes("texturedUnlit");
                int iter2 = 0;
                foreach (byte bytee in toBytes4)
                {
                    toBytes1[iter2] = bytee;
                    iter2++;
                }
                BMF.Write(new byte[] {
                    0x00,
                    0x00,
                    toBytes1[0],
                    toBytes1[1],
                    toBytes1[2],
                    toBytes1[3],
                    toBytes1[4],
                    toBytes1[5],
                    toBytes1[6],
                    toBytes1[7],
                    toBytes1[8],
                    toBytes1[9],
                    toBytes1[0xa],
                    toBytes1[0xb],
                    toBytes1[0xc],
                    0x00
                }, 0, 16); 
            }
            //write triangles
            /*

            gotta remember re adding this code generator as an option later on

            BMF.Write(new byte[] {
                0x00,
                0x00,
                0x00,
                0x00,
                0x00,
                0x00,
                0x00,
                0x00,
                0x00,
                0x00,
                0x00,
                0x54,
                0x52,
                0x49,
                0x53,
                0x3A
            }, 0, 16);
            if (texcoords == true)
            {
                float[] tridata = new float[tricount/2];
                int valz = 0;
                foreach(float td in tridata)
                {
                    BMF.Write(new byte[] {
                            (byte)((valz >> 8) & 0xff),
                            (byte)(valz & 0xff),
                            (byte)((valz + 1 >> 8) & 0xff),
                            (byte)(valz + 1 & 0xff),
                            (byte)((valz + 2 >> 8) & 0xff),
                            (byte)(valz + 2 & 0xff),
                            0x00,
                            0x00,
                            (byte)((valz + 3 >> 8) & 0xff),
                            (byte)(valz + 3 & 0xff),
                            (byte)((valz + 4 >> 8) & 0xff),
                            (byte)(valz + 4 & 0xff),
                            (byte)((valz + 5 >> 8) & 0xff),
                            (byte)(valz + 5 & 0xff),
                            0x00,
                            0x00
            }, 0, 16);
                    valz += 6;
                }
            }*/
            for(int j = 0; j < output.GetLength(1); j++)
            {
                BMF.Write(new byte[] {
                    output[0, j],
                    output[1, j],
                    output[2, j],
                    output[3, j],
                    output[4, j],
                    output[5, j],
                    output[6, j],
                    output[7, j],
                    output[8, j],
                    output[9, j],
                    output[10, j],
                    output[11, j],
                    output[12, j],
                    output[13, j],
                    output[14, j],
                    output[15, j],
                }, 0, 16);
            }

            FileStream bmffile = new FileStream(FileName, FileMode.Open);
            BMF.WriteTo(bmffile);
            bmffile.Dispose();
            //fix verts (temp)
            vertheader= vertheader*3;
            byte[] array = File.ReadAllBytes(FileName);
            array[0] = (byte)((vertheader >> 8) & 0xff);
            array[1] = (byte)(vertheader & 0xff);
            File.WriteAllBytes(FileName, array);
        }

        public static void CopyStream(Stream input, Stream output)
        {
            byte[] buffer = new byte[32768];
            int read;
            while ((read = input.Read(buffer, 0, buffer.Length)) > 0)
            {
                output.Write(buffer, 0, read);
            }
        }

        protected virtual bool IsFileLocked(FileInfo file)
        {
            FileStream stream = null;

            try
            {
                stream = file.Open(FileMode.Open, FileAccess.Read, FileShare.None);
            }
            catch (IOException)
            {
                return true;
            }
            finally
            {
                if (stream != null)
                    stream.Close();
            }
            
            return false;
        }

        private void Form1_FormClosing(Object sender, FormClosingEventArgs e)
        {
            if (File.Exists("settings.bmfo")) {
                File.Delete("settings.bmfo");
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            if (File.Exists(Directory.GetCurrentDirectory() + "\\update.exe"))
            {
                File.Delete(Directory.GetCurrentDirectory() + "\\update.exe");
            }
            if (!File.Exists(Directory.GetCurrentDirectory() + "\\Emgu.CV.World.dll"))
            {
                try
                {
                    using (var client = new System.Net.WebClient())
                    {
                        client.DownloadFile("http://n64hbc.000webhostapp.com/Emgu.CV.World.zip", Directory.GetCurrentDirectory() + "\\Emgu.CV.World.dll");
                    }
                    Directory.CreateDirectory("x86");
                    using (var client = new System.Net.WebClient())
                    {
                        client.DownloadFile("http://n64hbc.000webhostapp.com/cvextern.zip", Directory.GetCurrentDirectory() + "\\x86\\cvextern.dll");
                    }
                }
                catch
                {
                    MessageBox.Show("Unable to download some DLL files!\nThey are needed for texture conversion.\nFor help please refer to Robin#7080 or Kokiri#1499.", "Error!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            //lazy frm2 = new lazy();
            //frm2.ShowDialog();
        }

        private void labelUpdate(object sender, EventArgs e)
        {
            try
            {
                using (var client = new System.Net.WebClient())
                {
                    client.DownloadFile("http://n64hbc.000webhostapp.com/update", Directory.GetCurrentDirectory() + "\\update.exe");
                }
                using (var client = new System.Net.WebClient())
                {
                    client.DownloadFile("http://n64hbc.000webhostapp.com/changes.txt", Directory.GetCurrentDirectory() + "\\changes.txt");
                }
                File.WriteAllText(Directory.GetCurrentDirectory() + "\\path.txt", System.AppDomain.CurrentDomain.FriendlyName);
                System.Diagnostics.Process.Start(Directory.GetCurrentDirectory() + "\\update.exe");
                System.Diagnostics.Process.GetCurrentProcess().Kill();
            }
            catch
            {
                MessageBox.Show("Unable to check for updates!", "Whoops!", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void labelOptions_Click(object sender, EventArgs e)
        {
            var x = MessageBox.Show("Are you sure you want to close the currently open file?", "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (x == DialogResult.Yes)
            {
                if (sFileName != "")
                {
                    Application.Restart();
                }
                else
                {
                    MessageBox.Show("No file is currently open.", "Cancelled", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            else
            {
                MessageBox.Show("Phew.", "Cancelled", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
    }
}
