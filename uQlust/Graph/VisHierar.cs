using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using uQlustCore;
using uQlustCore.Interface;

namespace Graph
{
    interface SavePic
    {
        void SavePicture(string fileName,Bitmap bmp);
    }
    public partial class visHierar : Form,IVisual,SavePic
    {        
        public Dictionary< HClusterNode,Color> listNodes = null;
//        Dictionary<string, Color> classColor = null;
        Dictionary<string, string> vecColor = null;
        List <Color> colorMap=new List<Color>();
        Bitmap buffer;
        bool linemode = false;
        bool markflag = false;
        bool viewType = true;
        public ClosingForm closeForm;
        bool clearAll = false;
        string winName;
        int mposX, mposY;
        DrawHierarchical drawH;
        public override string ToString()
        {
            return "Dendrogram";
        }
        public void ToFront()
        {
            this.BringToFront();
        }
        public visHierar(HClusterNode hnode,string name,string measureName,Dictionary<string,string> labels)
        {
            InitializeComponent();
            buffer = new Bitmap(pictureBox1.Width, pictureBox1.Height);
            drawH = new DrawHierarchical(hnode, measureName, labels,buffer,true);
            drawH.horizontalView = true;
            winName = name;
            this.Text = name;
            int[] tab = new int[4];

            tab[0] = 0; tab[1] = 85; tab[2] = 170; tab[3] = 255;

            for (int i = 0; i < tab.Length; i++)
                for (int j = 0; j < tab.Length; j++)
                    for (int n = 0; n < tab.Length; n++)
                        colorMap.Add(Color.FromArgb(tab[i], tab[j], tab[n]));

            vecColor = labels;
            drawH.SetColors();

        }
        private void ClearBuffer()
        {
            Graphics g = Graphics.FromImage(buffer);
            g.Clear(pictureBox1.BackColor);
        }
        private void Form2_MouseClick(object sender, MouseEventArgs e)
        {
        
        }
        private void Form2_ResizeEnd(object sender, EventArgs e)
        {
            clearAll = true;
            buffer = new Bitmap(pictureBox1.Width, pictureBox1.Height);
            drawH.PrepareGraphNodes(buffer);
            drawH.DrawOnBuffer(buffer,true,1,Color.Empty);
            pictureBox1.Refresh();
            this.Invalidate();
            drawH.maxGraphicsY = pictureBox1.Height-drawH.posStart-30;
            drawH.maxGraphicsX = pictureBox1.Width - drawH.posStart - 30;
        }

        private void toolStripButton1_Click(object sender, EventArgs e)
        {
            ClusterOutput output = new ClusterOutput();
            output.hNode = drawH.hnode;
            ClusterVis wrCluster=new ClusterVis(output);
            saveFileDialog1.DefaultExt = "png";
            saveFileDialog1.Filter = "Png files|*.png";
            DialogResult res=saveFileDialog1.ShowDialog();
            if (res == DialogResult.OK && saveFileDialog1.FileName.Length > 0)
            {
                Resolution resForm = new Resolution(buffer.Width, buffer.Height,Color.Black);
                res=resForm.ShowDialog();
                if (res == DialogResult.OK)
                {
                    Bitmap bmp = new Bitmap(resForm.WidthR, resForm.HeightR);
                    drawH.PrepareGraphNodes(bmp);

                    drawH.DrawOnBuffer(bmp,resForm.ShowLegend,resForm.LineThickness,resForm.LinesColor);
                    SavePicture(saveFileDialog1.FileName, bmp);
                    drawH.PrepareGraphNodes(buffer);
                }
                //       this.SavePicture(saveFileDialog1.FileName);          
            }
        }

        private void toolStripButton2_Click(object sender, EventArgs e)
        {
            if (drawH.hnode != drawH.rootNode)
            {
                drawH.rootNode = drawH.hnode;
                drawH.PrepareGraphNodes(buffer);
                ClearBuffer();
                pictureBox1.Invalidate();
            }
        }

        private void Form2_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (closeForm != null)
                closeForm(winName);
        }
        public void SavePicture(string fileName,Bitmap bmp)
        {
            if (!fileName.Contains(".png"))
                fileName += ".png";
            bmp.Save(fileName, ImageFormat.Png);
        }
        private void pictureBox1_Paint(object sender, PaintEventArgs e)
        {

            if (buffer != null && drawH.currentRootNode == drawH.rootNode && buffer.Height == pictureBox1.Height && buffer.Width == pictureBox1.Width)
            {
                e.Graphics.Clear(this.BackColor);
                e.Graphics.DrawImage(buffer, 0, 0);
                if (linemode)
                {

                    Pen p = new Pen(Color.Brown);
                    System.Drawing.Font drawFont = new System.Drawing.Font("Arial", 10);
                    System.Drawing.SolidBrush drawBrush = new System.Drawing.SolidBrush(System.Drawing.Color.Black);
                    System.Drawing.StringFormat drawFormat = new System.Drawing.StringFormat();


                    if (listNodes != null)
                        e.Graphics.DrawString(listNodes.Count.ToString(), drawFont, drawBrush, 50, mposY);
                    e.Graphics.DrawLine(p, 0, mposY, buffer.Width, mposY);
                    return;
                }
                if (listNodes != null)
                {
                    SolidBrush brush;
                    foreach (var item in listNodes)
                    {
                        brush = new SolidBrush(item.Value);
                        if (drawH.rootNode.IsVisible(item.Key))
                            e.Graphics.FillEllipse(brush, item.Key.gNode.x, item.Key.gNode.y - 7, 7, 7);
                    }
                }

            }
            else
            {
              
                if (drawH.hnode != null)
                {
                    if (clearAll)
                    {
                        //pictureBox1.Image = null;
                    }
/*                    e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                    e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;*/
                    if (buffer == null || buffer.Width != pictureBox1.Width || buffer.Height != pictureBox1.Height)
                    {
                        buffer = new Bitmap(pictureBox1.Width, pictureBox1.Height);
                    }
                    drawH.currentRootNode = drawH.rootNode;

                    drawH.DrawOnBuffer(buffer,true,1,Color.Empty);
                    e.Graphics.DrawImage(buffer, 0, 0);
                   // buffer.Save("proba.png", ImageFormat.Png);
                    //maxHDist = minHDist = rootNode;
                    //DrawGraph(rootNode, e.Graphics);
                    //DrawDistanceAx(rootNode, e.Graphics);

                    if (listNodes != null)
                    {
                        SolidBrush brush;
                        foreach (var item in listNodes)
                        {
                            brush= new SolidBrush(item.Value);
                            if (drawH.rootNode.IsVisible(item.Key))
                                e.Graphics.FillEllipse(brush, item.Key.gNode.x, item.Key.gNode.y - 7, 7, 7);
                        }
                    }

                }
            }

        }

        private void pictureBox1_MouseClick(object sender, MouseEventArgs e)
        {
            HClusterNode clickNode;

            if (linemode)
            {
                linemode = false;
                if (listNodes.Count > 0)
                {
                    SaveMarkedClusters.Enabled = true;
                    orderVis.Enabled = true;
                }
                pictureBox1.Invalidate();
                return;
            }
            if (markflag)
            {
                clickNode = drawH.CheckClick(drawH.rootNode, e.X, e.Y);
                if (clickNode != null)
                {
                    if (e.Button == MouseButtons.Right)
                    {
                        if (listNodes.ContainsKey(clickNode))
                            listNodes.Remove(clickNode);
                    }
                    else
                    {
                        if (listNodes == null)
                            listNodes = new Dictionary<HClusterNode, Color>();
                        if (!listNodes.ContainsKey(clickNode))
                            listNodes.Add(clickNode,Color.Red);
                    }
                    if (listNodes.Count > 0)
                    {
                        SaveMarkedClusters.Enabled = true;
                        orderVis.Enabled = true;
                    }
                    pictureBox1.Invalidate();
                }


                return;
            }
            clickNode = drawH.CheckClick(drawH.rootNode, e.X, e.Y);

            if (clickNode != null)
            {
                if (e.Button == MouseButtons.Right)
                {
                    drawH.rootNode = clickNode;
                    drawH.PrepareGraphNodes(buffer);
                    ClearBuffer();
                    pictureBox1.Invalidate();

                    //this.Invalidate();
                }
                else
                {
                    FormText info = new FormText(clickNode);
                    info.Show();
                }
            }
            string lab = drawH.CheckColorRegion(e.X, e.Y);
            if (lab != null)
            {
                DialogResult res;
                colorDialog1 = new ColorDialog();
                int []colorTab=new int [drawH.classColor.Count];
                int i=0;
                foreach (var item in drawH.classColor)
                    colorTab[i++] = ColorTranslator.ToOle(item.Value);
                colorDialog1.CustomColors = colorTab;
   
                colorDialog1.Color = drawH.classColor[lab];
                
                res = colorDialog1.ShowDialog();
                if (res == DialogResult.OK)
                {
                    drawH.classColor[lab] = Color.FromArgb(colorDialog1.Color.R, colorDialog1.Color.G, colorDialog1.Color.B);
                    buffer = null;
                    pictureBox1.Invalidate();
                    pictureBox1.Refresh();
                }
            }

        }

        private void toolStripButton3_Click(object sender, EventArgs e)
        {
            if (!linemode)
            {
                linemode = true;
                /*buffer = new Bitmap(pictureBox1.Width, pictureBox1.Height);

                Graphics g = Graphics.FromImage(buffer); ;

                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;

                DrawGraph(rootNode, g);
                DrawDistanceAx(rootNode, g);*/
            }
        }

        private void pictureBox1_MouseMove(object sender, MouseEventArgs e)
        {
            if (linemode)
            {
                mposX = mposY;
                mposY = e.Y;
                listNodes=drawH.rootNode.CutDendrog(-(int)((e.Y-drawH.maxGraphicsY - drawH.posStart)/drawH.distanceStepY));
                pictureBox1.Invalidate();
            }
        }

        private void toolStripButton4_Click(object sender, EventArgs e)
        {
            markflag = !markflag;
            
            if(markflag)
                toolStripButton4.Image = ((System.Drawing.Image)uQlustCore.Properties.Resources.Flag2);
            else
                toolStripButton4.Image = ((System.Drawing.Image)uQlustCore.Properties.Resources.Flag);
        }

        private void Form2_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)27)
            {
                markflag = false ;
                linemode = false;
                if(listNodes!=null)
                    listNodes.Clear();
                SaveMarkedClusters.Enabled = false;
                pictureBox1.Invalidate();
            }
            
        }

        private void toolStripButton6_Click(object sender, EventArgs e)
        {
            if (listNodes != null)
            {
                listNodes.Clear();
                SaveMarkedClusters.Enabled = false;
                pictureBox1.Invalidate();
            }
        }

        private void toolStripButton5_Click(object sender, EventArgs e)
        {
            saveFileDialog1.Filter = "txt files|*.txt";
            saveFileDialog1.DefaultExt = "txt";
            if (saveFileDialog1.ShowDialog() == DialogResult.OK)
            {
                if (saveFileDialog1.FileName.Length > 0)
                {
                    string name = saveFileDialog1.FileName;
                    if (!name.Contains(".txt"))
                        name += ".txt";
                    StreamWriter file = new StreamWriter(name);

                    int i = 0;
                    foreach (var item in listNodes)
                    {
                        item.Key.SaveNode(file, i++);
                    }

                    file.Close();
                }
            }

        }
        public void ShowCloseButton()
        {
            toolStripButton5.Visible = true;
        }

        private void toolStripButton5_Click_1(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.OK;
            this.Close();  
        }


        private void toolStripButton8_Click(object sender, EventArgs e)
        {
            DialogResult res;
            SelectStruct s = new SelectStruct(drawH.hnode.setStruct);

            res=s.ShowDialog();
            if(res==DialogResult.OK)
            {
                drawH.hnode.ColorNode(s.selectedStruct, 2);
                buffer = null;
                pictureBox1.Invalidate();
                pictureBox1.Refresh(); 
            }
        }

        private void orderVis_Click(object sender, EventArgs e)
        {
            List<List<string>> clusterList=new List<List<string>>();
            List<int[]> colorMap = new List<int[]>();
            int[] tab = new int[4];
            tab[0] = 0; tab[1] = 65; tab[2] = 150; tab[3] = 235;

            for (int i = 0; i < tab.Length; i++)
                for (int j = 0; j < tab.Length; j++)
                    for (int n = 0; n < tab.Length; n++)
                    {
                        int[] aux = new int[3];
                        aux[0] = tab[i];
                        aux[1] = tab[j];
                        aux[2] = tab[n];

                        colorMap.Add(aux);
                    }
            int counter=0;
            List<Color> clusterColor = new List<Color>();
            List<HClusterNode> hk=new List<HClusterNode>(listNodes.Keys);
             VisOrder visFrame =null;
             if (hk.Count < colorMap.Count)
             {
                 for (int i = 0; i < hk.Count; i++)
                 {
                     clusterList.Add(hk[i].setStruct);
                     if (listNodes.Count < colorMap.Count)
                     {
                         Color r = Color.FromArgb(colorMap[counter][0], colorMap[counter][1], colorMap[counter][2]);
                         listNodes[hk[i]] = r;
                         clusterColor.Add(r);
                     }
                     counter += colorMap.Count / (hk.Count+10);
                 }
                 visFrame = new VisOrder(clusterList, null, clusterColor);
             }
             else
             {
                 for (int i = 0; i < hk.Count; i++)
                     clusterList.Add(hk[i].setStruct);
                 visFrame = new VisOrder(clusterList, null, null);
             }
            visFrame.Show();
            this.Invalidate();
            this.Refresh();
        }
/*        private void SetColors()
        {            
                if (vecColor == null)
                    return;
                classColor = new Dictionary<string, Color>();
                foreach (var item in vecColor)
                {
                    if (!classColor.ContainsKey(item.Value))
                        classColor.Add(item.Value, Color.Azure);
                }

                double step = (colorMap.Count - 2) / classColor.Keys.Count;

                List<string> ll = new List<string>(classColor.Keys);

                int count = 2;
                for (int i = 0; i < ll.Count; i++)
                {
                    classColor[ll[i]] = colorMap[count];
                    count = (int)((i + 1) * step);
                }
                buffer = null;
                pictureBox1.Invalidate();
                pictureBox1.Refresh();            

            
        }*/
        private void toolStripButton9_Click(object sender, EventArgs e)
        {
            DialogResult res= openFileDialog1.ShowDialog();

            if(res==DialogResult.OK)
            {
                drawH.ColorAndLabels(openFileDialog1.FileName);
                buffer = null;
                pictureBox1.Invalidate();
                pictureBox1.Refresh(); 
            }

        }

        private void toolStripButton10_Click(object sender, EventArgs e)
        {
            viewType = !viewType;
            drawH.PrepareGraphNodes(buffer);
            //ClearBuffer();
            buffer = null;
            pictureBox1.Invalidate();
           
        }



    }
}
