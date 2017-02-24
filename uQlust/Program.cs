using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Graph;
using WorkFlows;

namespace uQlustCore
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            
            //Application.Run(new Graph.StartForm());
            //Application.Run(new Graph.AdvancedVersion());
            Application.Run(new Rna_Protein_UserDef());
        }
    }
}

