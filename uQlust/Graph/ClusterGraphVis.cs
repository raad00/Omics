using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Graph;
using System.Drawing;
using uQlustCore;
using uQlustCore.Interface;
namespace Graph
{

    public class ClusterGraphVis : ClusterVis
    {
        string Name;
        Random r = new Random();
        int randomV;
        IVisual active = null;
        Dictionary<string,ClusterOutput> lOut;
        public static List<string> hNodeOptions = new List<string>{"Dendrogram", "Sunburst chart"};
        public static List<string> clusterOptions = new List<string> { "Text List",  "Order Visual"};
        public ClusterGraphVis() { randomV = r.Next(); }
        public ClusterGraphVis(ClusterOutput output, string name, Dictionary<string, ClusterOutput> lOut = null) : base(output) { this.lOut = lOut; this.Name = name; randomV = r.Next(); }
        public ClosingForm Closing=null;

        public static List<string> GetVisOptions(ClusterOutput output)
        {
            if (output.hNode != null)
                return hNodeOptions;
            if (output.clusters != null)
                return clusterOptions;
               
            return null;
        }
        public override string ToString()
        {
            if (active != null)
                return Name + " " +randomV;       

            return "";
        }
        public void ActivateWindow()
        {
            if(active!=null)
                active.ToFront();           

        }
        public void CloseWindow()
        {
            if(active!=null)
                active.Close();          

        }
        public void SClusters(string item,string measureName,string option)
        {
            Dictionary<string, string> dic = ClusterOutput.ReadLabelsFile(output.GetLabelFile());
            if (output.clusters != null)
            {
                
                switch(option)
                {
                    case "Order Visual":
                        if (active == null || !(active is VisOrder))
                        {
                            VisOrder visOrder;
                            visOrder = new VisOrder(output.clusters, item, null);
                            visOrder.closeForm = Closing;
                            active = visOrder;
                            visOrder.Show();
                        }
                        return;
                    case "Text List":
                    default:
                        if (active == null || !(active is ListVisual))
                        {
                            ListVisual visBaker;
                            visBaker = new ListVisual(output.clusters, item,dic);
                            visBaker.closeForm = Closing;
                            active = visBaker;
                            visBaker.Show();
                        }
                        return;
            }
             
            }
            if (output.hNode != null)
            {
               // win = new visHierar(output.hNode,item,measureName);
                if (option == null)
                    return;
                switch (option)
                {
                    case "Dendrogram":
                    default:
                        if (active == null || !(active is visHierar))
                        {
                            visHierar winH;
                            winH = new visHierar(output.hNode, item, measureName,dic);
                            winH.closeForm = Closing;
                            active = winH;
                            winH.Show();
                        }
                        return;                        
                    case "Sunburst chart":
                        if (active == null || !(active is VisHierarCircle))
                        {

                            VisHierarCircle winC;
                            winC = new VisHierarCircle(output.hNode, item, measureName);
                            winC.closeForm = Closing;
                            active = winC;
                            winC.Show();
                        }
                        return;                        
                }


            }
            if (output.juryLike != null || output.hNNRes!=null)
            {
                if (active == null || !(active is FormText))
                {

                    FormText showRes;
                    if(output.juryLike!=null)
                        showRes = new FormText(output.juryLike, item);
                    else
                        showRes = new FormText(output.hNNRes, item);
                    showRes.closeForm = Closing;
                    active = showRes;
                    showRes.Show();
                }
                return;
            }
            if(output.nodes!=null)
            {
                HeatMap heatRes = new HeatMap(output.nodes[1], output.nodes[0], null,output.measure, output.name);
                active = heatRes;
                heatRes.Show();

            }


        }

    }
}
