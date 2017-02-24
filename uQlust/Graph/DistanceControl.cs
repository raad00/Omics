using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using uQlustCore;
using uQlustCore.PDB;

namespace Graph
{
    public delegate void ReferenceBoxChanged(bool change);
    public partial class DistanceControl : UserControl
    {
        public ReferenceBoxChanged refChanged = null;
        public List<INPUTMODE> inputMode = null;

        public ProfileNameChanged changedProfile
        {
            set
            {
                jury1DSetup1.profileNameChanged = value;
                inputMode=jury1DSetup1.inputMode;
            }

        }
        private bool ProfileInfo=true;
        public bool profileInfo
        {
            set 
            {
                ProfileInfo = value;
                label2.Visible = value;
            }
            get
            {
                return ProfileInfo;
            }
        }

        private bool HideSetup;
        public bool hideSetup
        {
            set
            {
                HideSetup = value;
            }
            get
            {
                return HideSetup;
            }
        }
        private bool HideRef;
        public bool hideReference
        {
            set
            {
                HideRef = value;
                if (HideRef)
                    HideReference();
            }
            get
            {
                return HideRef;
            }

        }
        public bool reference
        {
            get
            {
                return referenceBox.Checked;                
            }
            set
            {
                jury1DSetup1.Visible = value;
                referenceBox.Checked = value;
            }
        }
        
        public string referenceProfile
        {
            get
            {
                return jury1DSetup1.profileName;
            }
            set
            {
                jury1DSetup1.profileName = value;
                inputMode = jury1DSetup1.inputMode;
            }
        }
        bool hideHamming;
        public bool HideHamming
        {
            set
            {
                hideHamming=value;
                radio1DJury.Enabled = !hideHamming;
            }
            get { return hideHamming; }
        }
        bool hideCosine;
        public bool HideCosine
        {
            set
            {
                hideCosine = value;
                radioEucl.Enabled = !hideCosine;
            }
            get
            {
                return hideCosine;
            }
        }
        public DistanceMeasures distDef 
        {
            get
            {
                if (radio1DJury.Checked)
                    return DistanceMeasures.HAMMING;
                else
                    if (radioEucl.Checked)
                        return DistanceMeasures.COSINE;
                    else
                        if (radioPearson.Checked)
                            return DistanceMeasures.PEARSON;

                return DistanceMeasures.HAMMING;
            }
            set
            {
                switch (value)
                {
                    case DistanceMeasures.HAMMING:
                    case DistanceMeasures.COSINE:
                        
                        if(value==DistanceMeasures.COSINE)
                            radioEucl.Checked = true;
                        else
                            radio1DJury.Checked = true;
                        if(value==DistanceMeasures.HAMMING)
                            referenceBox.Checked = true;
                        radioPearson.Checked = false;
                        label2.Visible = true;
                        break;

                }

            }
        }
        public void FreezDist()
        {
            radioPearson.Enabled = false;
        }
        public void UnfreezDist()
        {
            radioPearson.Enabled = true;
        }        
        private string profileFileName;
        public string profileName
        {
            get
            {
                return profileFileName;
            }
            set
            {
                if (value != null)
                {
                    this.profileFileName = value;
                    label2.Text = value;

                }

            }


        }
        public DistanceControl()
        {
            InitializeComponent();
        }
        public void HideReference()
        {
            this.Size = new Size(this.Size.Width, 123) ;
            referenceBox.Visible = false;
            jury1DSetup1.Visible = false;

        }
        public bool CheckIntegrity()
        {
            if(radio1DJury.Checked || radioEucl.Checked)
            {
                if (profileFileName==null || profileFileName.Length == 0)
                    return false;
            }
            if (reference)
                if (jury1DSetup1.profileName == null || jury1DSetup1.profileName.Length == 0)
                    return false;
            return true;
        }

        private void button5_Click(object sender, EventArgs e)
        {
            ProfileForm profDef;

            profDef = new ProfileForm(profileFileName, "",filterOPT.DISTANCE);


            DialogResult res = profDef.ShowDialog();
            if (res == DialogResult.OK)
            {
                try
                {
                    profileName = profDef.fileName;
                }
                catch
                {
                    MessageBox.Show("Profiles could not be saved!");
                }
            }
        }
        private string LabelActiveProfiles(string fileName)
        {
            string outStr = "";
            ProfileTree tree = new ProfileTree();
            if (fileName.Length > 0)
                try
                {
                    tree.LoadProfiles(fileName);
                }
                catch(Exception)
                {
                    label2.Text = "";
                    return null;
                }
            else
                return null;
            inputMode = tree.GetModes();
            List<profileNode> active = tree.GetActiveProfiles();
            outStr = "Active profiles: ";
            if(active!=null)
                for (int i = 0; i < active.Count; i++)
                {
                    outStr += active[i].profName;
                    if (i < active.Count - 1)
                        outStr += ", ";
                }

            return outStr;
        }

        private void radio1DJury_CheckedChanged(object sender, EventArgs e)
        {
            if (radio1DJury.Checked || radioEucl.Checked)
            {
                if (!hideSetup)
                {
                    label2.Visible = true;
                }

                if(radio1DJury.Checked)
                    referenceBox.Checked = true;
            }

        }

        private void radioRmsd_CheckedChanged(object sender, EventArgs e)
        {
            label2.Visible = false;

        }

        private void radioMaxSub_CheckedChanged(object sender, EventArgs e)
        {            
            label2.Visible = false;

        }

        private void referenceBox_CheckedChanged(object sender, EventArgs e)
        {
            if (refChanged != null)
                refChanged(referenceBox.Checked);

            jury1DSetup1.Visible = referenceBox.Checked;
                       

        }

        private void jury1DSetup1_Load(object sender, EventArgs e)
        {

        }


    
    }
}
