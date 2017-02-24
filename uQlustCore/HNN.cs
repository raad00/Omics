using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace uQlustCore
{
    class HNN
    {
        List<List<string>> clusters;
        Dictionary<string, string> classLabels;
        Dictionary<string, string> caseBase = new Dictionary<string,string>();
        Dictionary<string, string> labelToBaseKey = new Dictionary<string, string>();
        public List<string> validateList = new List<string>();
        public List<string> testList = new List<string>();
        HashCluster hk=null;
        double acc = 0;
        public HNN(HashCluster hk,Dictionary<string,string> classLabels)        
        {
            this.hk=hk;
            this.classLabels = classLabels;
            if (classLabels == null || classLabels.Count == 0)
                throw new Exception("No lables! Nothing to be done");
            Dictionary<string, int> classDic = new Dictionary<string, int>();
            foreach (var item in hk.dicFinal)
            {
                if (item.Value.Count > 0)
                {
                    classDic.Clear();
                    foreach (var index in item.Value)
                    {

                        string structV = hk.structNames[index];
                        if (classLabels.ContainsKey(structV))
                        {
                            if (classDic.ContainsKey(classLabels[structV]))
                                classDic[classLabels[structV]]++;
                            else
                                classDic.Add(classLabels[structV], 1);
                        }
                    }
                    foreach (var index in item.Value)
                        labelToBaseKey.Add(hk.structNames[index], item.Key);
                    if (classDic.Count == 0)
                        continue;
                    List<string> classLab = new List<string>(classDic.Keys);

                    classLab.Sort((x, y) => classDic[x].CompareTo(classDic[y]));
                    caseBase.Add(item.Key, classLab[0]);
                   
                }
                else
                    labelToBaseKey.Add(hk.structNames[item.Value[0]], item.Key);
            }


        }
        public double HNNValidate(List<string> validList)
        {
            int good = 0;
            int all = 0;
            double acc=0;
            Dictionary<string, int> classDic = new Dictionary<string, int>();
            foreach (var vItem in validList)
            {
                if(labelToBaseKey.ContainsKey(vItem) && classLabels.ContainsKey(vItem))
                {
                    if (classLabels[vItem] == caseBase[labelToBaseKey[vItem]])
                        good++;
                    all++;
                }
                
            }
            acc = (double)good / all;
            return acc;
        }
        public Dictionary<string, string> HNNTest(List<string> testList)
        {
            Dictionary<string, string> aux = new Dictionary<string, string>();
            Dictionary<string, string> res = new Dictionary<string, string>();
            Dictionary<string,List<int>> kk=hk.PrepareKeys(testList, true, false);
            List<string> caseKeys = new List<string>(kk.Keys);

            foreach (var item in caseKeys)
            {
                if (caseBase.ContainsKey(item))
                {
                    foreach (var v in kk[item])
                        if (!res.ContainsKey(testList[v]))
                            res.Add(testList[v], caseBase[item]);
                        else
                            Console.Write("kjsdksd");
                }
                else
                {
                    foreach (var v in kk[item])
                        if (!aux.ContainsKey(item))
                            aux.Add(item, testList[v]);
                       // else
                         //   Console.Write("kksk");
                }
            }
            Dictionary<string, List<string>> keys = hk.AddToClusters(new List<string>(caseBase.Keys), new List<string>(aux.Keys));
            foreach (var item in keys.Keys)
            {
                string final = "";
                if (keys[item].Count > 0)
                {
                    foreach (var it in keys[item])
                        for (int i = 0; i < keys[item].Count - 1; i++)
                            final += caseBase[keys[item][i]] + ":";
                    final += caseBase[keys[item][keys[item].Count - 1]];
                }
                if (final.Length == 0)
                    res.Add(aux[item], "NOT CLASSIFIED");
                else
                    res.Add(aux[item], final);
            }

            return res;
        }

       // public HNNTest(List)
    }
}
