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
using uQlustCore;
using uQlustCore.Profiles;
using uQlustCore.Interface;

namespace Graph
{
    public partial class HeatMap : Form,IVisual
    {
        List<Color> colorMap;
        DrawHierarchical upper;
        DrawHierarchical left;

        Bitmap upperBitMap;
        Bitmap leftBitMap;
        bool showLabels = false;
        HClusterNode colorNodeUpper = null;
        HClusterNode colorNodeLeft = null;
        HClusterNode upperNode,auxUpper;
        HClusterNode leftNode,auxLeft;
        List<KeyValuePair<string, List<int>>> rowOmicsProfiles;
        Dictionary<string, Dictionary<string,int>> omicsProfiles;
        Dictionary<int, int> distV = new Dictionary<int, int>();
        Dictionary<int, double []> codingInterv = null;
        public void ToFront()
        {
            this.BringToFront();
        }

        public HeatMap(HClusterNode upperNode,HClusterNode leftNode,Dictionary<string,string> labels,string measure,string intervalsFile)
        {
            upperNode.ClearColors(Color.Black);
            leftNode.ClearColors(Color.Black);
            this.upperNode = auxUpper=upperNode;
            this.leftNode = auxLeft=leftNode;
            List<KeyValuePair<string, List<int>>> colOmicsProfiles;
            rowOmicsProfiles = OmicsProfile.ReadOmicsProfile(/*"omics_Omics_profile"+"_"+*/intervalsFile);
            colOmicsProfiles = OmicsProfile.ReadOmicsProfile(/*"omics_Omics_profile"+"_"+*/intervalsFile+"_transpose");
            omicsProfiles = new Dictionary<string, Dictionary<string, int>>();
            for (int i = 0; i < rowOmicsProfiles.Count; i++)
            {
                if (!omicsProfiles.ContainsKey(rowOmicsProfiles[i].Key))
                    omicsProfiles.Add(rowOmicsProfiles[i].Key, new Dictionary<string, int>());
                for (int j = 0; j < colOmicsProfiles.Count; j++)
                {
                    if (!omicsProfiles[rowOmicsProfiles[i].Key].ContainsKey(colOmicsProfiles[j].Key))
                        omicsProfiles[rowOmicsProfiles[i].Key].Add(colOmicsProfiles[j].Key, rowOmicsProfiles[i].Value[j]);

                }
            }
            colorMap = DrawHierarchical.PrepareColorMap();
            ReadOmicsIntervals(intervalsFile);
            InitializeComponent();
            upperBitMap = new Bitmap(pictureBox2.Width, pictureBox2.Height);
            leftBitMap = new Bitmap(pictureBox3.Width, pictureBox3.Height);
            upper = new DrawHierarchical(upperNode, measure, labels, upperBitMap, true);
            //upper.viewType = true;
            left = new DrawHierarchical(leftNode, measure, labels, leftBitMap, false);
            //left.viewType = true;
            foreach (var item in rowOmicsProfiles)
                foreach (var v in item.Value)
                    if (!distV.ContainsKey(v))
                        distV.Add(v,0);

            pictureBox2.Refresh();
        }
        void ReadOmicsIntervals(string intervalsFile)
        {
            string fileName = "generatedProfiles/OmicsIntervals_" + intervalsFile + ".dat";
            if (!File.Exists(fileName))
                return;

            StreamReader wr = new StreamReader(fileName);

            codingInterv = new Dictionary<int, double[]>();
            string line=wr.ReadLine();
            while(line!=null)
            {
                string[] aux = line.Split(' ');
                if(aux.Length==4)
                {
                    double[] tab = new double[2];
                    tab[0] = Convert.ToDouble(aux[2]);
                    tab[1] = Convert.ToDouble(aux[3]);
                    codingInterv.Add(Convert.ToInt32(aux[1]), tab);
                }
                line = wr.ReadLine();
            }
            wr.Close();

            colorMap = new List<Color>(codingInterv.Count);
            for (int i = 0; i < codingInterv.Count; i++)
                colorMap.Add(Color.Black);
            List<int> hot = new List<int>();
            List<int> cool = new List<int>();
            foreach(var item in codingInterv.Keys)
            {
                if (codingInterv[item][0] < 0 && codingInterv[item][1] < 0)                
                    cool.Add(item);                
                else
                    if (codingInterv[item][0] > 0 && codingInterv[item][1] > 0)
                        hot.Add(item);
            }

            hot.Sort((x, y) => x.CompareTo(y));
            cool.Sort((x, y) => y.CompareTo(x));
            if (hot.Count > 1)
            {
                
                Color stepper = Color.FromArgb(
                                       (byte)((255 - 0) / (hot.Count)),
                                       (byte)((255 - 0) / (hot.Count)),
                                       (byte)((0 - 0) / (hot.Count)));
                for (int i = 0; i < hot.Count; i++)
                {
                    colorMap[hot[i]]=Color.FromArgb(
                                                0 + (stepper.R * (i+1)),
                                                0 + (stepper.G * (i+1)),
                                                0 + (stepper.B * (i+1)));
                }
            }
            else
                if (hot.Count == 1)
                    colorMap[hot[0]]=Color.FromArgb(255, 255, 0);

            if (cool.Count > 1)
            {
                Color stepper = Color.FromArgb(
                                       (byte)((20 - 0) / (cool.Count)),
                                       (byte)((0 - 0) / (cool.Count)),
                                       (byte)((255 - 0) / (cool.Count)));
                for (int i = 0; i < cool.Count; i++)
                {
                    colorMap[cool[i]]=Color.FromArgb(
                                                0 + (stepper.R * (i+1)),
                                                0 + (stepper.G * (i+1)),
                                                0 + (stepper.B * (i+1)));
                }
            }
            else
                if (cool.Count == 1)
                    colorMap[cool[0]]=Color.FromArgb(20, 0, 255);



            
        }
        void DrawHeatMap(Graphics g)
        {
            List<HClusterNode> upperLeaves =auxUpper.GetLeaves();
            List<HClusterNode> leftLeaves = auxLeft.GetLeaves();
            
            upperLeaves=upperLeaves.OrderByDescending(o => o.gNode.x).Reverse().ToList();
            leftLeaves=leftLeaves.OrderByDescending(o => o.gNode.y).Reverse().ToList();
            SolidBrush b = new SolidBrush(Color.Black);
            double yPos1, yPos2;
            for(int i=0;i<leftLeaves.Count;i++)
            {

                int y = leftLeaves[i].gNode.y;
                if (i == 0)
                {
                    yPos1 = y - (leftLeaves[i + 1].gNode.y - y) / 2.0;
                    yPos2 = y + (leftLeaves[i + 1].gNode.y - y) / 2.0;

                }
                else
                    if (i + 1 < leftLeaves.Count)
                    {
                        yPos1 = y - (y - leftLeaves[i - 1].gNode.y) / 2.0;
                        yPos2 = y + (leftLeaves[i + 1].gNode.y - y) / 2.0;

                    }
                    else
                    {
                        yPos1 = y - (y - leftLeaves[i - 1].gNode.y) / 2.0;
                        yPos2 = y + (y - leftLeaves[i - 1].gNode.y) / 2.0;

                    }


                double xPos1, xPos2;
                for(int j=0;j<upperLeaves.Count;j++)
                {
                    int x = upperLeaves[j].gNode.x;
                    double vv = 0;

                    if (j + 1 < upperLeaves.Count)
                        vv = (upperLeaves[j + 1].gNode.x - x) / 2.0;
                    if (j == 0)
                    {
                        xPos1 = x - vv;
                        xPos2 = x + vv;

                    }
                    else
                        if (j + 1 < upperLeaves.Count)
                        {
                            xPos1 = x - (x - upperLeaves[j - 1].gNode.x) / 2.0;
                            xPos2 = x + vv;
                        }
                        else
                        {
                            xPos1 = x - (x - upperLeaves[j - 1].gNode.x) / 2.0;
                            xPos2 = x + (x - upperLeaves[j - 1].gNode.x) / 2.0;
                        }
                    if ((xPos2 - xPos1) == 0)
                        continue;
                    if (!omicsProfiles.ContainsKey(leftLeaves[i].refStructure))
                        throw new Exception("Omics profile does not contain " + leftLeaves[i].refStructure);
                    if(!omicsProfiles[leftLeaves[i].refStructure].ContainsKey(upperLeaves[j].refStructure))
                        throw new Exception("Omics profile does not contain " + upperLeaves[j].refStructure);
                    int ind = omicsProfiles[leftLeaves[i].refStructure][upperLeaves[j].refStructure];
                    ind--;
                    if (colorMap.Count <= ind)
                        throw new Exception("Color map is to small");
                    Color c = colorMap[ind];
                    b.Color = c;
                    if(yPos2-yPos1>0)
                        g.FillRectangle(b, (float)xPos1,(float) yPos1, (float)(xPos2-xPos1), (float)(yPos2-yPos1));                    
                }
            }

        }

        private void pictureBox2_Paint(object sender, PaintEventArgs e)
        {
            e.Graphics.Clear(this.BackColor);
            upper.DrawOnBuffer(upperBitMap, false, 1, Color.Empty);
            e.Graphics.DrawImage(upperBitMap, 0, 0);
        }

        private void pictureBox3_Paint(object sender, PaintEventArgs e)
        {
            e.Graphics.Clear(this.BackColor);
            left.posStart = 5;
            left.DrawOnBuffer(leftBitMap, false, 1, Color.Empty);
            e.Graphics.DrawImage(leftBitMap, 0, 0);
        }


        private void pictureBox1_Paint(object sender, PaintEventArgs e)
        {
            e.Graphics.Clear(this.BackColor);
            DrawHeatMap(e.Graphics);
        }

        private void HeatMap_ResizeEnd(object sender, EventArgs e)
        {
            upperBitMap = new Bitmap(pictureBox2.Width, pictureBox2.Height);
            leftBitMap = new Bitmap(pictureBox3.Width, pictureBox3.Height);
            left.PrepareGraphNodes(leftBitMap);
            upper.PrepareGraphNodes(upperBitMap);
           /* pictureBox1.Refresh();
            pictureBox2.Refresh();
            pictureBox3.Refresh();*/
            this.Invalidate();
        }

        private void pictureBox4_Paint(object sender, PaintEventArgs e)
        {
            SolidBrush b=new SolidBrush(Color.Black);
            int xPos,yPos;
            xPos=5;yPos=5;
            System.Drawing.Font drawFont = new System.Drawing.Font("Arial", 10);
            System.Drawing.SolidBrush drawBrush = new System.Drawing.SolidBrush(System.Drawing.Color.Black);
            System.Drawing.StringFormat drawFormat = new System.Drawing.StringFormat();

            List<int> ordered = distV.Keys.ToList();
            ordered.Sort();
                foreach(var item in ordered)
                {
                    b.Color=colorMap[item-1];
                    e.Graphics.FillRectangle(b,xPos,yPos,15,10);
                    e.Graphics.DrawString(item.ToString(), drawFont, drawBrush, xPos+25,yPos-3);
                    yPos += 25;
                    if(yPos>pictureBox4.Height)
                    {
                        yPos = 5;
                        xPos += 40;
                    }
                }
             
        }

        private void tableLayoutPanel1_Paint_1(object sender, PaintEventArgs e)
        {

        }

        private void pictureBox2_MouseMove(object sender, MouseEventArgs e)
        {
            bool test = false;
            if (colorNodeUpper != null)
            {
                upper.ChangeColors(colorNodeUpper, Color.Black);
                test = true;
            }

            colorNodeUpper = upper.FindClosestNode(e.X, e.Y);

            if(colorNodeUpper!=null)
            {
                upper.ChangeColors(colorNodeUpper, Color.Red);
                test = true;
            }
            if (test)
            {
                Graphics g = Graphics.FromImage(upperBitMap);
                g.Clear(pictureBox2.BackColor);
                pictureBox2.Refresh();
            }
        }
        private void pictureBox2_MouseClick(object sender, MouseEventArgs e)
        {

            
            switch(e.Button)
            {

                case MouseButtons.Left:
                    HClusterNode nodeC = upper.CheckClick(upper.rootNode,e.X,e.Y);
                    if (nodeC != null)
                    {
                        TextBoxView rr = new TextBoxView(nodeC.setStruct);
                        rr.Show();                        
                    }
                    if (colorNodeUpper != null && colorNodeUpper.joined!=null)
                        auxUpper = colorNodeUpper;
                    break;      
                case MouseButtons.Right:
                        auxUpper = upperNode;
                        break;
            }
            if (auxUpper != null)
            {
                if(colorNodeUpper!=null)
                    upper.ChangeColors(colorNodeUpper, Color.Black);
                colorNodeUpper = null;
                upper.rootNode = auxUpper;
                upper.PrepareGraphNodes(upperBitMap);
                Graphics g = Graphics.FromImage(upperBitMap);
                g.Clear(pictureBox2.BackColor);
                pictureBox2.Refresh();
                pictureBox1.Refresh();
            }
        }

        private void pictureBox3_MouseMove(object sender, MouseEventArgs e)
        {
            bool test = false;
            if (colorNodeLeft != null)
            {
                test = true;
                left.ChangeColors(colorNodeLeft, Color.Black);
            }

            colorNodeLeft = left.FindClosestNode(e.X, e.Y);

            if (colorNodeLeft != null)
            {
                left.ChangeColors(colorNodeLeft, Color.Red);
                test = true;
            }
            if (test)
            {
                Graphics g = Graphics.FromImage(leftBitMap);
                g.Clear(pictureBox3.BackColor);

                pictureBox3.Refresh();
            }
        }

        private void pictureBox3_MouseClick(object sender, MouseEventArgs e)
        {
            switch (e.Button)
            {

                case MouseButtons.Left:
                    HClusterNode nodeC = left.CheckClick(left.rootNode,e.X,e.Y);
                    if (nodeC != null)
                    {
                        TextBoxView rr = new TextBoxView(nodeC.setStruct);
                        rr.Show();                        
                    }
                    if (colorNodeLeft != null && colorNodeLeft.joined!=null)
                        auxLeft = colorNodeLeft;
                    break;
                case MouseButtons.Right:
                    auxLeft = leftNode;
                    break;
            }
            if (auxLeft != null)
            {
                if (colorNodeLeft != null)
                    left.ChangeColors(colorNodeLeft, Color.Black);
                colorNodeLeft = null;
                left.rootNode = auxLeft;
                left.PrepareGraphNodes(leftBitMap);
                Graphics g = Graphics.FromImage(leftBitMap);
                g.Clear(pictureBox3.BackColor);
                pictureBox3.Refresh();
                pictureBox1.Refresh();
            }
        }

        private void labels_Click(object sender, EventArgs e)
        {
           

        }

        private void toolStripLabel1_Click(object sender, EventArgs e)
        {
            showLabels = !showLabels;
            upper.showLabels = showLabels;
            numericUpDown1.Visible = showLabels;
            numericUpDown1.Value = upper.labelSize;
            left.showLabels = showLabels;
            Graphics g = Graphics.FromImage(upperBitMap);
            g.Clear(pictureBox2.BackColor);
            g = Graphics.FromImage(leftBitMap);
            g.Clear(pictureBox3.BackColor);

            pictureBox3.Refresh();
            pictureBox2.Refresh();
        }

        private void numericUpDown1_ValueChanged(object sender, EventArgs e)
        {
            left.labelSize = (int)numericUpDown1.Value;
            upper.labelSize = (int)numericUpDown1.Value;

            Graphics g = Graphics.FromImage(upperBitMap);
            g.Clear(pictureBox2.BackColor);
            g = Graphics.FromImage(leftBitMap);
            g.Clear(pictureBox3.BackColor);

            pictureBox3.Refresh();
            pictureBox2.Refresh();

        }

      
    }
}
