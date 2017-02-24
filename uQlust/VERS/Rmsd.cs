using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using ClusterV;
using PDB;
using ClusterV.dcd;
using System.Threading;

namespace Distance
{
    class Rmsd : DistanceMeasure
    {
        struct paramsThread
        {
            public int k;
           
        };
        Settings dirSettings = new Settings();
        //Dictionary<string, float[,]> aRotMol = new Dictionary<string, float[,]>();

        Optimization[] optList = null;
        float[][,] pdbPos = null;        

        List<string> structures;
        public Rmsd(DCDFile dcd, string alignFile, bool flag, PDBMODE allAtoms, string refJuryProfile = null)
            : base(dcd, alignFile, flag, refJuryProfile)
        {
            DCDReader readDCD = new DCDReader(dcd);
                dirSettings.Load();
            
            pdbs = new PDBFiles();
            readDCD.DCDPrepareReading(dcd.dcdFile, dcd.pdbFile);
            int counter = 0;
            bool cont = true;
            do
            {
                MemoryStream mStream = new MemoryStream();
                StreamWriter wStream = new StreamWriter(mStream);
                cont=readDCD.ReadAndSavePDB(wStream);
                string name = "Model" + counter++;
                mStream.Position = 0;
                pdbs.AddPDB(mStream, allAtoms, name);
            }
            while(cont);

            readDCD.FinishDCDReading();

            pdbs.FindReferenceSeq();
            pdbs.MakeAlignment(alignFile);
            if(pdbs is ErrorBase)
                AddErrors(pdbs.errors);
            opt = new Optimization();

            structNames = CheckAvailableStructures();
            order = true;
        }

        private Dictionary<string,int> CheckAvailableStructures()
        {

            Dictionary<string,int> strNames = new Dictionary<string,int>();

            if (jury == null)
            {
                foreach (var item in pdbs.molDic.Keys) 
                    strNames.Add(item,1);
                return strNames;
            }
            else
            {
                Dictionary<string, int> dic = new Dictionary<string, int>();

                foreach (var item in jury.alignKeys)
                {
                            dic.Add(item, 1);
                }
                foreach (var str in pdbs.molDic.Keys)
                    if (dic.ContainsKey(str))
                        dic[str]++;
                    else
                        dic.Add(str, 1);

                int max = -1;
                foreach (var item in dic)
                    if (max < item.Value)
                        max = item.Value;

                foreach (var item in dic)
                    if (item.Value == max)
                        strNames.Add(item.Key,1);
            }


            return strNames;
        }

        public Rmsd(string dirName,string alignFile,bool flag,PDBMODE allAtoms,string refJuryProfile=null):base(dirName,alignFile,flag,refJuryProfile)
        {

            dirSettings.Load();
            pdbs = new PDBFiles();
            string[] files;
            if(dirSettings.extension==null)
                files = Directory.GetFiles(dirName);
            else
                files = Directory.GetFiles(dirName, dirSettings.extension);

            if (files.Length == 0)
                throw new Exception("In selected directory " + dirName + " there are no files with extesion " + dirSettings.extension);

            string refSeqFile = dirName + ".ref";
            pdbs.ReadRefSeq(refSeqFile);


            pdbs.AddPDB(files, allAtoms);

            if (pdbs.molDic.Keys.Count == 0)
                throw new Exception("Non pdb files correctly read");
            pdbs.FindReferenceSeq();
            pdbs.MakeAlignment(alignFile);
			opt=new Optimization();
            structNames = CheckAvailableStructures();
            order = true;
         }
        public Rmsd(List <string> fileNames, string alignFile, bool flag, PDBMODE allAtoms,string refJuryProfile=null)
            : base(fileNames, alignFile, flag,refJuryProfile)
        {

            dirSettings.Load();
            pdbs = new PDBFiles();

            pdbs.AddPDB(fileNames, allAtoms);
            pdbs.FindReferenceSeq();
            pdbs.MakeAlignment(null);
            opt = new Optimization();
            structNames = CheckAvailableStructures();
            order = true;
        }
               
        public override string ToString()
        {
            return "Rmsd";
        }
        
        public override bool SimilarityThreshold(float threshold,float dist)
        {
            if (dist < threshold)
                return true;

            return false;
        }
        public override List<KeyValuePair<string, double>> GetReferenceList(List<string> structures)
        {
            float[,,] refPosition = null;

            if (jury != null)
                //return jury.JuryOpt(structures).juryLike[0].Key;
                return jury.JuryOptWeights(structures).juryLike;
                //return jury.ConsensusJury(structures).juryLike;

            Dictionary<string, float[,,]> aRotMol = new Dictionary<string, float[,,]>(structures.Count);
            refPosition = CalcRefPosition(structures,aRotMol);
            List<KeyValuePair<string, double>> refList = new List<KeyValuePair<string, double>>();
            foreach (var item in aRotMol.Keys)
            {
                double sum = 0;
                int count = 0;
                for (int i = 0; i < pdbs.molDic[item].indexMol.Length; i++)
                    for (int j = 0; j < aRotMol[item].GetLength(1); j++)
                        for (int n = 0; n < aRotMol[item].GetLength(2); n++)
                            if (pdbs.molDic[item].indexMol[i] != -1)
                            {
                                sum += (refPosition[i,j, n] - aRotMol[item][i,j, n]) *
                                    (refPosition[i,j, n] - aRotMol[item][i,j, n]);
                                count++;
                            }

                if (count > 0)
                {
                    KeyValuePair<string, double> aux = new KeyValuePair<string, double>(item, Math.Sqrt(sum) / count);
                    refList.Add(aux);
                }
            }
            refList.Sort((nextPair,firstPair) =>
            {
                return nextPair.Value.CompareTo(nextPair.Value);
            });

            return refList;


        }
        private float[,,] CalcRefPosition(List<string> structures,Dictionary<string, float[,,]> aRotMol)
        {
            //float[,] refPosition = null;
            int[] counts;
            aRotMol.Clear();

            int maxV=0;
            if (pdbs.molDic.Keys.Count == 1)
                pdbs.EmptyAlignment(structures[0]);
            
            foreach (var item in pdbs.molDic.Keys)
            {

                for (int i = 0; i < pdbs.molDic[item].mol.Residues.Count; i++)
                    if (pdbs.molDic[item].mol.Residues[i].Atoms.Count > maxV)
                        maxV = pdbs.molDic[item].mol.Residues[i].Atoms.Count;
            }
            float[, ,] refPosition = new float[pdbs.molDic[structures[0]].indexMol.Length, maxV, 3];
            //refPosition = new float[maxV, 3];
            counts = new int[pdbs.molDic[structures[0]].indexMol.Length];

            foreach (var item in structures)
            {
                    float [,]aux=GetStructAfterRotation(structures[0], item);
                    if(aux==null)
                        continue;
                    float[, ,] auxK = new float[pdbs.molDic[structures[0]].indexMol.Length, maxV, 3];
                    
                    
                    for (int i = 0,m=0; i < pdbs.molDic[item].indexMol.Length; i++)
                    {
                        if (pdbs.molDic[item].indexMol[i] != -1 && pdbs.molDic[structures[0]].indexMol[i]!=-1)
                        {
                            for (int j = 0; j < pdbs.molDic[item].mol.Residues[pdbs.molDic[item].indexMol[i]].Atoms.Count; j++,m++)
                                for (int n = 0; n < 3; n++)
                                {
                                    refPosition[i, j, n] += aux[m, n];
                                    auxK[i, j, n] = aux[m, n];
                                }
                            counts[i]++;
                        }  

                    }
                    aRotMol.Add(item, auxK);
            }
            for (int i = 0; i < refPosition.GetLength(0); i++)
                if (counts[i] > 0)
                    for (int j = 0; j < refPosition.GetLength(1); j++)
                        for (int n= 0; n < refPosition.GetLength(2); n++)
                        refPosition[i, j,n] /= counts[i];


                    return refPosition;
        }

        public override string GetReferenceStructure(List<string> structures)
        {
            List<KeyValuePair<string, double>> refList=null;
                refList = GetReferenceList(structures);

            return refList[0].Key;
        }
        private float [,] GetStructAfterRotation(string refStructure, string modelStructure)
        {
            float[,] transMatrix=null;

            if (!pdbs.molDic.ContainsKey(refStructure) || !pdbs.molDic.ContainsKey(modelStructure))
                return null;
            posMOL locPosMol=opt.PrepareData(pdbs.molDic[refStructure], pdbs.molDic[modelStructure]);

            transMatrix = opt.TransMatrix(locPosMol.posmol2, locPosMol.posmol1);
            if (transMatrix == null)
                return null;
            return Optimization.MultMatrixTrans(locPosMol.posmol2, transMatrix);
            //return opt.TransMatrix(opt.posMol1, opt.posMol2);

        }

        public override int GetDistance(string refStructure, string modelStructure)
        {
            if (!pdbs.molDic.ContainsKey(refStructure) || !pdbs.molDic.ContainsKey(modelStructure))
                return int.MaxValue;
            posMOL locPosMol=opt.PrepareData(pdbs.molDic[refStructure], pdbs.molDic[modelStructure]);
            return (int)(opt.Rmsd(locPosMol.posmol1, locPosMol.posmol2, false) * 100);
        }

        private int GetDistance(string refStructure, string modelStructure,float []cent1,float []cent2)
        {
            if (!pdbs.molDic.ContainsKey(refStructure) || !pdbs.molDic.ContainsKey(modelStructure))
                return int.MaxValue;
            posMOL locPosMol = opt.PrepareData(pdbs.molDic[refStructure], pdbs.molDic[modelStructure]);
            return (int)(opt.Rmsd(locPosMol.posmol1, locPosMol.posmol2, cent1, cent2,false) * 100);
        }
        private int ThreadingGetDistance(string refStructure, string modelStructure, float[] cent1, float[] cent2,int threadNum)
        {
            if (!pdbs.molDic.ContainsKey(refStructure) || !pdbs.molDic.ContainsKey(modelStructure))
                return int.MaxValue;
            if (pdbPos == null)
            {
                posMOL locPosMol = optList[threadNum].PrepareData(pdbs.molDic[refStructure], pdbs.molDic[modelStructure]);
                return (int)(optList[threadNum].Rmsd(locPosMol.posmol1, locPosMol.posmol2, cent1, cent2, false) * 100);
            }
            else
            {
                return (int)(optList[threadNum].Rmsd(pdbPos[hashIndex[refStructure]], pdbPos[hashIndex[modelStructure]], null, null, false) * 100);
            }
        }

        private void CalcMatrixList(object o)
        {
            int num = (int)o;
            float[] center1 = new float[3];
            float[] center2 = new float[3];

            for (int i = 0; i < indexList[num].Count; i++)
                distanceMatrix[indexList[num][i].Key] = ThreadingGetDistance(structures[structures.Count - 1], structures[indexList[num][i].Key], center1, center2,num);

            resetEvents[num].Set();
        }
        protected override void CalcMatrix(object o)
        {
            //StreamWriter oo = new StreamWriter("dist.dat");
            float[] center1=new float[3];
            float[] center2 = new float[3];
            paramsThread p = (paramsThread)o;

            int maxV = -1;
            foreach (var item in indexList[p.k])
            {
                int dd = FindIndex(item.Key);

                int distC = ThreadingGetDistance(structures[item.Key], structures[item.Value],center1,center2,p.k);
                if (distC < 0 || distC==int.MaxValue)
                    distC = -1;

                distanceMatrix[dd + item.Value - item.Key] = distC;
                if (distC > maxV)
                    maxV = distC;
              //  oo.WriteLine(p.structures[item.Key] + " " + p.structures[item.Value] + " " + distC);
            }
            resetEvents[p.k].Set();
          //  oo.Close();
        }
        public override int[] GetDistance(string refStructure, List<string> structures)
        {
            int[] dist = new int[structures.Count];
            int i = 0;
            distanceMatrix = new int[structures.Count];
            Settings set = new Settings();
            set.Load();
            int threadNumbers = set.numberOfCores;
            this.structures = new List<string>(structures);
            int part = this.structNames.Count / threadNumbers;
            indexList = new List<KeyValuePair<int, int>>[threadNumbers];
            optList = new Optimization[threadNumbers];
            for (int j = 0; j < indexList.Length; j++)
            {
                indexList[j] = new List<KeyValuePair<int, int>>();
                optList[j] = new Optimization();
            }
            int count = 0;
            for (int j = 0; j < this.structNames.Count; j++)
            {
                if (indexList[count].Count < part)
                {
                    KeyValuePair<int, int> aux = new KeyValuePair<int, int>(j, 0);
                    indexList[count].Add(aux);
                }
                else
                    count++;
            }
            this.structures.Add(refStructure);
            resetEvents = new ManualResetEvent[threadNumbers];
            for (int n = 0; n < threadNumbers; n++)
            {
                int k = n;
                ThreadPool.QueueUserWorkItem(new WaitCallback(CalcMatrixList), (object)k);
                resetEvents[n] = new ManualResetEvent(false);

            }
            for (int j = 0; j < threadNumbers; j++)
                resetEvents[j].WaitOne();

            for (int j = 0; j < this.structures.Count - 1; j++)
                dist[j] = distanceMatrix[j];

            return dist;

        }
        private void PreparePDBPos()
        {
            //Check if all alignm have the same length
            foreach(var item in pdbs.molDic)
            {
                foreach (var itemIndex in item.Value.indexMol)
                    if (itemIndex == -1)
                        return;
            }
            pdbPos = new float[structures.Count][,];
            for (int i = 0; i < structures.Count;i++)
                pdbPos[i] = new float[pdbs.molDic[structures[0]].indexMol.Length, 3];
            float []center=new float [3]; 
            for (int i = 0; i < structures.Count;i++ )
            {
                int count=0;

                for (int j = 0; j < pdbs.molDic[structures[i]].mol.Residues.Count; j++)
                    for (int n = 0; n < pdbs.molDic[structures[i]].mol.Residues[j].Atoms.Count; n++)
                    {
                        pdbPos[i][count, 0] = pdbs.molDic[structures[i]].mol.Residues[j].Atoms[n].Position.X;
                        pdbPos[i][count, 1] = pdbs.molDic[structures[i]].mol.Residues[j].Atoms[n].Position.Y;
                        pdbPos[i][count++,2] = pdbs.molDic[structures[i]].mol.Residues[j].Atoms[n].Position.Z;
                    }

                Optimization.CenterMol(pdbPos[i],center);
            }

        }
        public override void CalcDistMatrix(List <string> structures)
        {
            hashIndex = new Dictionary<string, int>(structures.Count);
            for(int i=0;i<structures.Count;i++)
            {
                if (!pdbs.molDic.ContainsKey(structures[i]))
                    throw new Exception("There is no pdb file or file is incorrect for the structure: " + structures[i]);
                hashIndex.Add(structures[i], i);
            }
            Settings set = new Settings();
            set.Load();
            int threadNumbers =  set.numberOfCores;
            this.structures = new List<string>(structures);

            int part = structures.Count * (structures.Count+1) / (2 * threadNumbers)+1;
            distanceMatrix = new int[structures.Count * (structures.Count + 1) / 2];
            indexList = new List<KeyValuePair<int, int>>[threadNumbers];
            optList = new Optimization[threadNumbers];
            for (int n= 0; n < threadNumbers;n++)
            {
                indexList[n] = new List<KeyValuePair<int, int>>(part);
                optList[n] = new Optimization();
            }
            
            int count = 0;
                foreach (var dim1 in structures)
                {
                    foreach (var dim2 in structures)
                    {
                        if (hashIndex[dim1] <= hashIndex[dim2])
                        {
                            indexList[count].Add(new KeyValuePair<int, int>(hashIndex[dim1], hashIndex[dim2]));
                            if (part <= indexList[count].Count)
                                count++;
                        }
                    }
                }
                PreparePDBPos();
            resetEvents = new ManualResetEvent[threadNumbers];
            for(int n=0;n<threadNumbers;n++)
            {
                paramsThread p=new paramsThread();
                p.k=n;
                ThreadPool.QueueUserWorkItem(new WaitCallback(CalcMatrix),(object) p);
                resetEvents[n] = new ManualResetEvent(false);

            }
            for (int j = 0; j < threadNumbers; j++)
                resetEvents[j].WaitOne();

        }
    /*    public override void CalcDistMatrix(List <string> structures)
        {
            int i = 0;
            hashIndex = new Dictionary<string, int>(structures.Count);

            foreach (var item in structures)
            {
                if (!pdbs.molDic.ContainsKey(item))
                    throw new Exception("There is no pdb file or file is incorrect for the structure: " + item);
                hashIndex.Add(item, i++);
            }
            int maxV = -1;
            distanceMatrix = new int[structures.Count * (structures.Count + 1) / 2];
            //StreamWriter wr = new System.IO.StreamWriter("maxsub.txt");
            foreach(var dim1 in structures)
            {
                foreach (var dim2 in structures)
                {
                    if(hashIndex[dim1]<=hashIndex[dim2])
                    {
                                int dd=FindIndex(hashIndex[dim1]);
                                int distC = GetDistance(dim1, dim2);
                                if (distC < 0 || distC > 10000000)                                
                                    distC = -1;                                   
                                
                                distanceMatrix[dd + hashIndex[dim2] - hashIndex[dim1]] = distC;
                                if (distC > maxV)
                                    maxV = distC;
                       //         wr.WriteLine(dim1 + " " + dim2 + " " + distanceMatrix[dd + hashIndex[dim2] - hashIndex[dim1]]);
                     }
                }
                    
            }
            for (int n = 0; n < distanceMatrix.Length; n++)
                if (distanceMatrix[n] == -1)
                    distanceMatrix[n] = maxV;
                
            //wr.Close();
        }*/

    }
}
