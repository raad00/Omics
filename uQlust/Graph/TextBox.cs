using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Graph
{
    public partial class TextBoxView : Form
    {
        TextInput input=null;        
        public TextBoxView(List <string> data)
        {
            InitializeComponent();
            if (data != null)
            {
                int start = 0;
                StringBuilder textAux = new StringBuilder(); ;
                if (data.Count > 1000)
                {
                    textAux.AppendLine("Number of Errors is bigger than 1000, only last 1000 is shown!");
                    start = data.Count - 1000;
                }
                for(int i=start;i<data.Count;i++)
                    textAux.AppendLine(data[i]);

                richTextBox1.Text = textAux.ToString();
            }
        }

        public void AddText(string text)
        {
            richTextBox1.Text += text + "\n";
        }
        private void button1_Click(object sender, EventArgs e)
        {
            if (input!=null)
                input.Close();
            this.Close();
        }
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            switch (keyData)
            {
                case Keys.Control | Keys.F:
                {
                    if (input == null)
                    {
                        input = new TextInput("NEXT");
                        input.textBox=richTextBox1;
                        input.Show();
                    }
                    else
                        input.BringToFront();

                    
                    return true;
                }
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }
        void FindAndHighlight(string str)
        {
            int index = richTextBox1.Find(str);

            if (index >= 0)
            {
                richTextBox1.Select(index, index + str.Length);
            }
        }

    }
}
