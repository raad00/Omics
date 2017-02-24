using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.IO;
using System.Diagnostics;
using System.Data;
//using System.Data;
using uQlustCore;
using uQlustCore.Interface;
using uQlustCore.dcd;
using uQlustCore.Distance;
using uQlustCore.Profiles;

namespace uQlustCore
{
    public delegate void UpdateJob(string item,bool errorFlag=false,bool finishAll=true);
    public delegate void StartJob(string name,string al,string dirName,string measure);
    public delegate void ErrorMessage(string item);
    public delegate void UpdateMessage(string m);
    public delegate void ErrorJob();

    class ThreadParam
    {
        public string name;
        public int num;
        public int start;
        public int stop;
        //public string dirName;
        //public DistanceMeasure distance;
    }
    public interface MessageUpdate
    {
        void UpdateMessage(string message);
        void ActivateUpdateting();
        void CloseUpdateting();
    }
    public class JobManager
    {
        Dictionary<string, Thread> runnigThreads = new Dictionary<string, Thread>();
        public Dictionary<string, ClusterOutput> clOutput = new Dictionary<string, ClusterOutput>();
        Dictionary<string, IProgressBar> progressDic = new Dictionary<string, IProgressBar>();
        public Options opt = new Options();
        string clType = "";
        public MessageUpdate mUpdate;        
        string currentProcessName="";
        Thread startProg;
       

        public UpdateJob updateJob;
        public StartJob beginJob;
        public ErrorMessage message;
        public UpdateMessage upMessage;
        public ErrorJob errorJob;
        private Object thisLock = new Object();


        public Dictionary<string,double> ProgressUpdate()
        {
            Dictionary<string, double> res = new Dictionary<string, double>();          

            if (progressDic.Count==0)
                return null;
            lock (thisLock)
            {
                foreach (var item in progressDic)
                {
                    res.Add(item.Key, item.Value.ProgressUpdate());
                }
                foreach (var item in res)
                    if (item.Value == 1)
                        progressDic.Remove(item.Key);

                return res;
            }            
            
        }
        public Exception GetException()
        {
            return null;
        }
        public List<KeyValuePair<string, DataTable>> GetResults()
        {
            return null;
        }


        private void RunHashCluster(string name, string dirName, string alignmentFile=null,DCDFile dcd = null)
        {
            DateTime cpuPart1 = DateTime.Now;
            HashCluster hk = null;

            if (dcd != null)
                hk = new HashCluster(dcd, opt.hash);                
            else
                if(alignmentFile!=null)
                    hk = new HashCluster("", alignmentFile, opt.hash);
                else
                    hk = new HashCluster(dirName, null, opt.hash);

            
            progressDic.Add(name, hk);
            if (beginJob != null)
                beginJob(currentProcessName, hk.ToString(), dirName, "HAMMING");
            hk.InitHashCluster();


            DateTime cpuPart2 = DateTime.Now;



            ClusterOutput output;
            output = hk.RunHashCluster();
            UpdateOutput(name, dirName,alignmentFile, output, "HAMMING", cpuPart1, cpuPart2, hk);

        }
        private void RunHashDendrogCombine(string name, string dirName, string alignmentFile = null, DCDFile dcd = null)
        {
            DateTime cpuPart1 = DateTime.Now;
            HashClusterDendrog hk = null;

            if (dcd != null)
                hk = new HashClusterDendrog(dcd, opt.hash, opt.hierarchical);
            else
                if (alignmentFile != null)
                    hk = new HashClusterDendrog(null, alignmentFile, opt.hash, opt.hierarchical);
                else
                    hk = new HashClusterDendrog(dirName, null, opt.hash, opt.hierarchical);
            

            ClusterOutput output;
            if (beginJob != null)
                beginJob(currentProcessName, hk.ToString(), dirName,"NONE");
            progressDic.Add(name, hk);
            hk.InitHashCluster();
            
            DateTime cpuPart2 = DateTime.Now;
            output = hk.RunHashDendrogCombine();

            UpdateOutput(name, dirName,alignmentFile, output, hk.UsedMeasure(), cpuPart1, cpuPart2, hk);

        }
        Dictionary<string,string> ReadLabels(string fileName)
        {
            StreamReader lFile = new StreamReader(fileName);
            Dictionary<string, string> classLabels;
            classLabels = new Dictionary<string, string>();
            string line = lFile.ReadLine();
            while (line != null)
            {
                string[] aux = line.Split(' ');
                if (aux.Length == 2)
                {
                    classLabels.Add(aux[0], aux[1]);
                }
                line = lFile.ReadLine();
            }

            lFile.Close();
            return classLabels;
        }

        private void RunHNN(string name, string dirName, string alignmentFile = null, DCDFile dcd = null)
        {
            DateTime cpuPart1 = DateTime.Now;
            HashCluster hCluster = null;
            OmicsProfile profOm = new OmicsProfile();
            if (alignmentFile != null)
                hCluster = new HashCluster(null, alignmentFile, opt.hash);
            else
                hCluster = new HashCluster(dirName, null, opt.hash);

            profOm.LoadOmicsSettings();
            string hClusterName = name + "upper";
            if (beginJob != null)
                beginJob(currentProcessName, hCluster.ToString(), dirName, "HAMMING");
            progressDic.Add(name, hCluster);



            hCluster.InitHashCluster();

            DateTime cpuPart2 = DateTime.Now;
            Dictionary<string, string> classLabels = ReadLabels(opt.hNNLabels);
            ClusterOutput aux = hCluster.RunHashCluster(new List<string>(classLabels.Keys));

            HNN knn = new HNN(hCluster,classLabels);

            Dictionary<string, string> resT = knn.HNNTest(new List<string>(hCluster.stateAlign.Keys));
            //double res = knn.HNNValidate(knn.validateList);
            ClusterOutput output = new ClusterOutput();
            output.hNNRes = resT;
            UpdateOutput(name, dirName, alignmentFile, output, "NONE", cpuPart1, cpuPart2, knn);

        }
        private void RunOmicsHeatmap(string name, string dirName, string alignmentFile = null, DCDFile dcd = null)
        {
            DateTime cpuPart1 = DateTime.Now;
            HashClusterDendrog upper = null;
            HashClusterDendrog left = null;
            OmicsProfile profOm = new OmicsProfile();

          //  opt.hierarchical.distance = DistanceMeasures.PEARSON;

           if (alignmentFile != null)
               upper = new HashClusterDendrog(null, alignmentFile, opt.hash, opt.hierarchical);
           else
               upper = new HashClusterDendrog(dirName, null, opt.hash, opt.hierarchical);

            profOm.LoadOmicsSettings();

            string nameUpper =name+"upper";
            if (beginJob != null)
                beginJob(currentProcessName, upper.ToString(), dirName, "HAMMING");
            try
            {


                progressDic.Add(name, upper);
                upper.InitHashCluster();
            }
            catch (Exception ex)
            {
                Console.Write("xxxx");
            }
                DateTime cpuPart2 = DateTime.Now;

                ClusterOutput output, aux;
                output = new ClusterOutput();
                aux = upper.RunHashDendrogCombine();
          
            output.nodes = new List<HClusterNode>();
            output.nodes.Add(aux.hNode);
          //  UpdateOutput(nameUpper, dirName, alignmentFile, output, "NONE", cpuPart1, cpuPart2, upper);
            opt.hash.relClusters = opt.hash.reqClusters;
            try
            {
                if (alignmentFile != null)
                    left = new HashClusterDendrog(null, alignmentFile, opt.hash, opt.hierarchical);
                else
                    left = new HashClusterDendrog(dirName, null, opt.hash, opt.hierarchical);
            }
            catch(Exception ex)
            {
                Console.Write("KKKK");
            }
            //profOm.transpose = true;
            profOm.heatmap = true;
            profOm.SaveOmicsSettings();
            string nameLeft = name + "left";
            //progressDic.Add(nameLeft, left);
            progressDic[name] = left;
            left.InitHashCluster();

            aux = left.RunHashDendrogCombine();
            output.nodes.Add(aux.hNode);
            UpdateOutput(name, dirName, alignmentFile, output, upper.UsedMeasure(), cpuPart1, cpuPart2, left);

        }

        private void RunHashDendrog(string name, string dirName, string alignmentFile = null, DCDFile dcd = null)
        {
            DateTime cpuPart1 = DateTime.Now;
            HashCluster hk = null;

            if (dcd != null)
                hk = new HashCluster(dcd, opt.hash);
            else
                if (alignmentFile != null)
                    hk = new HashCluster("", alignmentFile, opt.hash);
                else
                    hk = new HashCluster(dirName, null, opt.hash);

            progressDic.Add(name, hk);
            hk.InitHashCluster();

            DateTime cpuPart2 = DateTime.Now;

            ClusterOutput output;
            output = hk.RunHashDendrog();
            UpdateOutput(name, dirName,alignmentFile, output, "NONE", cpuPart1, cpuPart2, hk);

        }

        private void RunHierarchicalCluster(string name, string dirName,string alignFile=null, DCDFile dcd=null)
        {
            DateTime cpuPart1 = DateTime.Now;
            DistanceMeasure distance = null;
            //distance.CalcDistMatrix(distance.structNames);
           // opt.hierarchical.atoms = PDB.PDBMODE.ALL_ATOMS;
            if(dcd!=null)
                distance = CreateMeasureForDCD(dcd, opt.hierarchical.distance, opt.hierarchical.atoms, opt.hierarchical.reference1DjuryAglom,
                opt.hierarchical.alignmentFileName, opt.hierarchical.hammingProfile, opt.hierarchical.jury1DProfileAglom);
            else
                distance = CreateMeasure(name,dirName,opt.hierarchical.distance, opt.hierarchical.atoms, opt.hierarchical.reference1DjuryAglom,
                alignFile, opt.hierarchical.hammingProfile, opt.hierarchical.jury1DProfileAglom);

            DebugClass.WriteMessage("Measure Created");          
            hierarchicalCluster hk = new hierarchicalCluster(distance, opt.hierarchical,dirName);
            if (beginJob != null)
                beginJob(currentProcessName,hk.ToString(), dirName, distance.ToString());
            clType = hk.ToString();
            ClusterOutput output;
            progressDic.Add(name, hk);
            distance.InitMeasure();
            DateTime cpuPart2 = DateTime.Now;
            output = hk.HierarchicalClustering(new List<string>(distance.structNames.Keys));
            UpdateOutput(name, dirName, alignFile,output, distance.ToString(), cpuPart1, cpuPart2, hk);

        }
        private void RunHKMeans(string name, string dirName, string alignFile=null,DCDFile dcd = null)
        {
            DateTime cpuPart1 = DateTime.Now;
            ClusterOutput clustOut = null;
            DistanceMeasure distance = null;
            if(dcd==null)
                distance = CreateMeasure(name,dirName,opt.hierarchical.distance, opt.hierarchical.atoms, opt.hierarchical.reference1DjuryKmeans,
                                        alignFile, opt.hierarchical.hammingProfile, opt.hierarchical.jury1DProfileKmeans);
            else
                distance = CreateMeasureForDCD(dcd, opt.hierarchical.distance, opt.hierarchical.atoms, opt.hierarchical.reference1DjuryKmeans,
                                        opt.hierarchical.alignmentFileName, opt.hierarchical.hammingProfile, opt.hierarchical.jury1DProfileKmeans);
         
            kMeans km;

            km = new kMeans(distance,true);
            if (beginJob != null)
                beginJob(currentProcessName, km.ToString(), dirName, distance.ToString());

            progressDic.Add(name, km);
            DateTime cpuPart2 = DateTime.Now;
            distance.InitMeasure();

            

            clType = km.ToString();
            km.BMIndex = opt.hierarchical.indexDB;
            km.threshold = opt.hierarchical.numberOfStruct;
            km.maxRepeat = opt.hierarchical.repeatTime;
            km.maxK = opt.hierarchical.maxK;
            clustOut = km.HierarchicalKMeans();
            UpdateOutput(name, dirName,alignFile,clustOut, distance.ToString(), cpuPart1, cpuPart2, km);
        }
        private void RunFastHCluster(string name, string dirName, string alignFile=null, DCDFile dcd = null)
        {
            DateTime cpuPart1 = DateTime.Now;
            ClusterOutput clustOut = null;
            DistanceMeasure distance = null;

            if(dcd==null)
                distance = CreateMeasure(name,dirName,opt.hierarchical.distance, opt.hierarchical.atoms, opt.hierarchical.reference1DjuryFast,
                    alignFile, opt.hierarchical.hammingProfile, opt.hierarchical.jury1DProfileFast);
            else
                distance =CreateMeasureForDCD(dcd, opt.hierarchical.distance, opt.hierarchical.atoms, opt.hierarchical.reference1DjuryFast,
                    opt.hierarchical.alignmentFileName, opt.hierarchical.hammingProfile, opt.hierarchical.jury1DProfileFast);

            FastDendrog km;
            km = new FastDendrog(distance, opt.hierarchical,dirName);
            if (beginJob != null)
                beginJob(currentProcessName, km.ToString(), dirName, distance.ToString());

            progressDic.Add(name, km);
            distance.InitMeasure();
            DateTime cpuPart2 = DateTime.Now;
            clType = km.ToString();
            clustOut = km.Run(new List<string>(distance.structNames.Keys));
            UpdateOutput(name, dirName,alignFile, clustOut, distance.ToString(), cpuPart1, cpuPart2, km);

        }

        private void RunKMeans(string name, string dirName, string alignFile=null, DCDFile dcd = null)
        {
            DateTime cpuPart1 = DateTime.Now;
            ClusterOutput clustOut;
            DistanceMeasure distance = null;

            if(dcd==null)
                distance = CreateMeasure(name,dirName,opt.kmeans.kDistance, opt.kmeans.kAtoms, opt.kmeans.reference1Djury,
                    alignFile, opt.kmeans.hammingProfile, opt.kmeans.jury1DProfile);
            else
                distance =CreateMeasureForDCD(dcd, opt.kmeans.kDistance, opt.kmeans.kAtoms, opt.kmeans.reference1Djury,
                    opt.kmeans.alignmentFileName, opt.kmeans.hammingProfile, opt.kmeans.jury1DProfile);

            kMeans km;            
            km = new kMeans(distance, opt.kmeans.kMeans_init);
            if (beginJob != null)
                beginJob(currentProcessName, km.ToString(), dirName, distance.ToString());

            progressDic.Add(name, km);
            distance.InitMeasure();
            DateTime cpuPart2 = DateTime.Now;
            clType = km.ToString();
            if ((int)opt.kmeans.maxK <= 1)
                throw new Exception("k in k-Means must be bigger then 1, right now is: " + (int)opt.kmeans.maxK);
            if (distance.structNames.Count < 10)
                throw new Exception("Number of structures to cluster must be bigger then 10 right now is: " + distance.structNames.Count);

            clustOut = km.kMeansLevel((int)opt.kmeans.maxK, opt.kmeans.maxIter,new List <string>(distance.structNames.Keys));
            UpdateOutput(name, dirName,alignFile, clustOut, distance.ToString(), cpuPart1, cpuPart2,km);
            GC.SuppressFinalize(distance);                        
        }

        
        private void RunBakerCluster(string name, string dirName, string alignFile=null,DCDFile dcd = null)
        {
            DateTime cpuPart1 = DateTime.Now;
            ClusterOutput output = null;
            DistanceMeasure distance = null;
            if(dcd==null)
                distance = CreateMeasure(name,dirName,opt.threshold.hDistance, opt.threshold.hAtoms, opt.threshold.reference1Djury,
                alignFile, opt.threshold.hammingProfile, null);
            else
                distance = CreateMeasureForDCD(dcd, opt.threshold.hDistance, opt.threshold.hAtoms, opt.threshold.reference1Djury,
                opt.threshold.alignmentFileName, opt.threshold.hammingProfile, null);

            ThresholdCluster bk = new ThresholdCluster(distance, opt.threshold.distThresh, opt.threshold.bakerNumberofStruct);
            if (beginJob != null)
                beginJob(currentProcessName, bk.ToString(), dirName, distance.ToString());


            progressDic.Add(name, bk);
            distance.InitMeasure();
            DateTime cpuPart2 = DateTime.Now;
            clType = bk.ToString();
            output = bk.OrgClustering();
            UpdateOutput(name, dirName,alignFile, output, distance.ToString(), cpuPart1, cpuPart2,bk);
        }
        private void RunSift(string name, string dirName,DCDFile dcd = null)
        {
            DateTime cpuStart = DateTime.Now;
            ClusterOutput output = null;
            Sift s = new Sift();

            if (beginJob != null)
                beginJob(currentProcessName, s.ToString(), dirName, "NONE");
            progressDic.Add(name, s);            
            clType = s.ToString();
            output=s.RunSift(dirName);
            UpdateOutput(name, dirName, null,output, "Sift",cpuStart,DateTime.Now, s);
        }
        private void Run1DJury(string name, string dirName, string alignFile=null, DCDFile dcd = null)
        {
            DateTime cpuPart1 = DateTime.Now;
            ClusterOutput output;


            jury1D ju=new jury1D();
            if (beginJob != null)
                beginJob(currentProcessName, ju.ToString(), dirName, "NONE");

            progressDic.Add(name,ju);


            //DistanceMeasure distance = CreateMeasure();
                if (opt.other.alignGenerate)
                    opt.other.alignFileName = "";
                if (alignFile != null)
                    ju.PrepareJury(alignFile, opt.other.juryProfile);
                else
                    if (dcd != null)
                        ju.PrepareJury(dcd, alignFile, opt.other.juryProfile);
                    else
                        ju.PrepareJury(dirName, alignFile, opt.other.juryProfile);

                
            clType = ju.ToString();
            DateTime cpuPart2 = DateTime.Now;
            //jury1D ju = new jury1D(opt.weightHE,opt.weightC,(JuryDistance) distance);
            //output = ju.JuryOpt(new List<string>(ju.stateAlign.Keys));
            if (ju.alignKeys != null)
            {
              
                output = ju.JuryOptWeights(ju.alignKeys);
            }
            else
            {
                UpadateJobInfo(name, true, false);
                throw new Exception("Alignment is epmty! Check errors");
            }
            UpdateOutput(name, dirName,alignFile, output,ju.ToString(), cpuPart1,cpuPart2, ju);
        }
        private void Run3DJury(string name, string dirName, string alignFile=null, DCDFile dcd = null)
        {
            DateTime cpuStart = DateTime.Now;
            ClusterOutput output;
            DistanceMeasure distance = null;

            if(alignFile!=null)
                distance = CreateMeasure(name,null,opt.other.oDistance, opt.other.oAtoms, opt.other.reference1Djury,
                alignFile, opt.other.hammingProfile, opt.other.referenceProfile);
            else
                if(dirName!=null)
                    distance = CreateMeasure(name,dirName,opt.other.oDistance, opt.other.oAtoms, opt.other.reference1Djury,
                    alignFile, opt.other.hammingProfile, opt.other.referenceProfile);
                else
                    distance = CreateMeasureForDCD(dcd, opt.other.oDistance, opt.other.oAtoms, opt.other.reference1Djury,
                    opt.other.alignFileName, opt.other.hammingProfile, opt.other.referenceProfile);
            Jury3D ju = new Jury3D(distance);
            if (beginJob != null)
                beginJob(currentProcessName, ju.ToString(), dirName, distance.ToString());

            progressDic.Add(name, ju);
            distance.InitMeasure();            
            clType = ju.ToString();
            output = ju.Run3DJury();
            UpdateOutput(name, dirName,alignFile,output, distance.ToString(), cpuStart, DateTime.Now,ju);
            progressDic.Remove(name);
        }
        private void UpdateOutput(string name, string dirName, string alignFile, ClusterOutput output, string distStr, DateTime cpuPart1, DateTime cpuPart2, object obj)
        {           
            output.clusterType = obj.ToString();
            output.measure = distStr.ToString();

            DateTime cc = DateTime.Now;
            
            TimeSpan preprocess=new TimeSpan();
            TimeSpan cluster=new TimeSpan();
            if(cpuPart1!=null && cpuPart2!=null)
                preprocess = cpuPart2.Subtract(cpuPart1);
            if(cpuPart2!=null)
                cluster = cc.Subtract(cpuPart2);

            output.time = "Prep="+String.Format("{0:F2}", preprocess.TotalMinutes);
            if(cpuPart2!=null)
                output.time += " Clust=" + String.Format("{0:F2}", cluster.TotalMinutes);
            output.name = name;
            output.dirName = dirName;
            output.alignFile = alignFile;
            output.peekMemory = Process.GetCurrentProcess().PeakWorkingSet64;
            Process.GetCurrentProcess().Refresh();
            progressDic.Remove(name);

            //Process.GetCurrentProcess().
            clOutput.Add(output.name, output);
            UpadateJobInfo(name, false,false);
        }
        public void UpadateJobInfo(string processName, bool errorFlag,bool finishAll)
        {
            if (updateJob != null)
                updateJob(processName, errorFlag,finishAll);

        }
        public void FinishThread(string processName,bool errorFlag)
        {
            lock (runnigThreads)
            {
                UpadateJobInfo(currentProcessName, errorFlag,true);
                runnigThreads.Remove(processName);
                if (progressDic.ContainsKey(currentProcessName))
                    progressDic.Remove(currentProcessName);
            }
        }
        public void RemoveJob(string jobName)
        {
            if (runnigThreads.ContainsKey(jobName))
            {
                lock (runnigThreads)
                {
                    runnigThreads[jobName].Abort();
                    runnigThreads.Remove(jobName);
                }
            }
        }
        private DistanceMeasure CreateMeasureForDCD(DCDFile dcd, DistanceMeasures measure, PDB.PDBMODE atoms, bool jury1d, string alignFileName,
                                                      string profileName = null, string refJuryProfile = null)
        {
            DistanceMeasure dist=null;
            switch (measure)
            {
                case DistanceMeasures.HAMMING:
                    if (refJuryProfile == null || !jury1d)
                        throw new Exception("Sorry but for jury measure you have to define 1djury profile to find reference structure");
                    else
                        dist = new JuryDistance(dcd, alignFileName, true, profileName, refJuryProfile);
                    break;
                case DistanceMeasures.PEARSON:
                    dist = new Pearson(dcd, alignFileName, jury1d, refJuryProfile);
                    break;
                case DistanceMeasures.COSINE:
                    dist = new CosineDistance(dcd, alignFileName, jury1d, refJuryProfile);
                    break;

            }

            dist.InitMeasure();
            return dist;

        }
        private DistanceMeasure CreateMeasure(string processName, string dirName,DistanceMeasures measure,PDB.PDBMODE atoms,bool jury1d,string alignFileName,
                                              string profileName=null,string refJuryProfile=null)
        {
            DistanceMeasure dist=null;
            switch(measure)
            {
                case DistanceMeasures.HAMMING:
                    if (alignFileName != null)
                        dist = new JuryDistance(alignFileName, jury1d, profileName, refJuryProfile);
                    else
                        dist = new JuryDistance(dirName, alignFileName, jury1d, profileName, refJuryProfile);
                    break;
                case DistanceMeasures.COSINE:
                    if (alignFileName != null)
                        dist = new CosineDistance(alignFileName, jury1d, profileName, refJuryProfile);
                    else
                        dist = new CosineDistance(dirName, alignFileName, jury1d, profileName, refJuryProfile);
                    break;

                case DistanceMeasures.PEARSON:
                    if (dirName == null)
                        throw new Exception("RMSD and MAXSUB measures cannot be used for aligned profiles!");
                    dist = new Pearson(dirName, alignFileName, jury1d, refJuryProfile);
                    break;


            }
            return dist;
        }
        string MakeName(object processParams,ClusterAlgorithm alg,int counter)
        {
            string currentProcessName = "";
            if (((ThreadParam)processParams).name != null && ((ThreadParam)processParams).name.Length > 0)
                currentProcessName = ((ThreadParam)processParams).name + "_" + counter;
            else
                currentProcessName = alg.ToString() + "_" + counter;

            return currentProcessName;
        }
        public void StartAll(object processParams)
        {
            ErrorBase.ClearErrors();
            string orgProcessName = ((ThreadParam)processParams).name;
            currentProcessName = ((ThreadParam)processParams).name;
            int counter = 1;
            try
            {
                if (opt.profileFiles.Count == 0)
                {
                    foreach (var alg in opt.clusterAlgorithm)
                    {
                        
                        foreach (var item in opt.dataDir)
                        {
                         //   if (tTimer != null)
                         //       tTimer.Start();
                            currentProcessName = MakeName(processParams, alg, counter);
                            //if (beginJob != null)
                              //  beginJob(currentProcessName, alg.ToString("g"), item, opt.GetDistanceMeasure(alg));

                            switch (alg)
                            {
                                case ClusterAlgorithm.uQlustTree:
                                    RunHashDendrogCombine(currentProcessName, item);
                                    break;                                
                                case ClusterAlgorithm.HashCluster:
                                    RunHashCluster(currentProcessName, item);
                                    break;
                                case ClusterAlgorithm.HierarchicalCluster:
                                    RunHierarchicalCluster(currentProcessName, item);
                                    break;
                                case ClusterAlgorithm.HKmeans:
                                    RunHKMeans(currentProcessName, item);
                                    break;
                                case ClusterAlgorithm.FastHCluster:
                                    RunFastHCluster(currentProcessName, item);
                                    break;
                                case ClusterAlgorithm.Kmeans:
                                    RunKMeans(currentProcessName, item);
                                    break;
                                case ClusterAlgorithm.BakerCluster:
                                    RunBakerCluster(currentProcessName, item);
                                    break;
                                case ClusterAlgorithm.Jury1D:
                                    Run1DJury(currentProcessName, item);
                                    break;
                                case ClusterAlgorithm.Jury3D:
                                    Run3DJury(currentProcessName, item);
                                    break;
                                case ClusterAlgorithm.Sift:
                                    RunSift(currentProcessName, item);
                                    break;

                            }

                            counter++;
                        }
                        foreach (var item in opt.dcdFiles)
                        {
  //                          if (tTimer != null)
  //                              tTimer.Start();
                            currentProcessName = MakeName(processParams, alg, counter);
                            //if (beginJob != null)
                              //  beginJob(currentProcessName, opt.clusterAlgorithm.ToString(), item.dcdFile, opt.GetDistanceMeasure(alg));

                            switch (alg)
                            {
                                case ClusterAlgorithm.uQlustTree:
                                    RunHashDendrog(currentProcessName, null, null, item);
                                    break;

                                case ClusterAlgorithm.HashCluster:
                                    RunHashCluster(currentProcessName, null, null, item);
                                    break;
                                case ClusterAlgorithm.HierarchicalCluster:
                                    RunHierarchicalCluster(currentProcessName, null, null, item);
                                    break;
                                case ClusterAlgorithm.HKmeans:
                                    RunHKMeans(currentProcessName, null, null, item);
                                    break;
                                case ClusterAlgorithm.FastHCluster:
                                    RunFastHCluster(currentProcessName, null, null, item);
                                    break;
                                case ClusterAlgorithm.Kmeans:
                                    RunKMeans(currentProcessName, null, null, item);
                                    break;
                                case ClusterAlgorithm.BakerCluster:
                                    RunBakerCluster(currentProcessName, null, null, item);
                                    break;
                                case ClusterAlgorithm.Jury1D:
                                    Run1DJury(currentProcessName, null, null, item);
                                    break;
                                case ClusterAlgorithm.Jury3D:
                                    Run3DJury(currentProcessName, null, null, item);
                                    break;
                                case ClusterAlgorithm.Sift:
                                    RunSift(currentProcessName, null, item);
                                    break;

                            }

                            counter++;
                        }
                    }
                }
                else
                {
                    foreach(var alg in opt.clusterAlgorithm)
                    {
                    foreach (var item in opt.profileFiles)
                    {
//                        if (tTimer != null)
//                            tTimer.Start();
                        currentProcessName = MakeName(processParams, alg, counter);
                       // if (beginJob != null)
                         //   beginJob(currentProcessName, opt.clusterAlgorithm.ToString(), item, opt.GetDistanceMeasure(alg));

                        switch (alg)
                        {
                            case ClusterAlgorithm.uQlustTree:
                                RunHashDendrogCombine(currentProcessName, item,item);
                                //RunHashDendrog(currentProcessName, null, item);
                                break;
                            case ClusterAlgorithm.OmicsHeatMap:
                                RunOmicsHeatmap(currentProcessName, item, item);
                                break;
                            case ClusterAlgorithm.HashCluster:
                                RunHashCluster(currentProcessName, item, item);
                                break;
                            case ClusterAlgorithm.HierarchicalCluster:
                                RunHierarchicalCluster(currentProcessName, item, item);
                                break;
                            case ClusterAlgorithm.HKmeans:
                                RunHKMeans(currentProcessName, item, item);
                                break;
                            case ClusterAlgorithm.FastHCluster:
                                RunFastHCluster(currentProcessName, item, item);
                                break;
                            case ClusterAlgorithm.Kmeans:
                                RunKMeans(currentProcessName, item, item);
                                break;
                            case ClusterAlgorithm.BakerCluster:
                                RunBakerCluster(currentProcessName, item, item);
                                break;
                            case ClusterAlgorithm.Jury1D:
                                Run1DJury(currentProcessName, item, item);
                                break;
                            case ClusterAlgorithm.Jury3D:
                                Run3DJury(currentProcessName, item, item);
                                break;
                            case ClusterAlgorithm.HNN:
                                RunHNN(currentProcessName, item, item);
                                break;
                        }
                        counter++;
                    }

                }
                }
                FinishThread(orgProcessName, false);
            
            }
            catch (Exception ex)
            {
                FinishThread(orgProcessName, true);
                message(ex.Message);
            }
            
        }
        public void RunJob(string processName)
        {
            ThreadParam tparam=new ThreadParam();

             startProg = new Thread(StartAll);
             tparam.name = processName;
             startProg.Start(tparam);            
            lock (runnigThreads)
            {
                runnigThreads.Add(processName, startProg);
            }
        }
        public void WaitAllNotFinished()
        {
            while (runnigThreads.Count > 0)
            {
                Thread.Sleep(1000);
            }
        }
        public void SaveOutput(string fileName)
        {            
            StreamWriter w = new StreamWriter(fileName);
            int count=0;
            foreach (var item in clOutput.Keys)
            {
                string name=fileName+count++;
                w.WriteLine(name);
                ClusterOutput.Save(name, clOutput[item]);
            }
            w.Close();

        }
        public void LoadOutput(string fileName)
        {
            ClusterOutput outP;

            if (File.Exists(fileName))
            {
                clOutput.Clear();
                string line;
                StreamReader r = new StreamReader(fileName);
                while (!r.EndOfStream)
                {
                    line = r.ReadLine();
                    if (File.Exists(line))
                    {
                        outP = ClusterOutput.Load(line);
                        clOutput.Add(outP.name, outP);
                    }
                }
                r.Close();
            }
        }

        public void LoadExternal(string fileName, Func<string, string, ClusterOutput> Load)
        {
            ClusterOutput aux;
            if (File.Exists(fileName))
            {
                string line;
                StreamReader r = new StreamReader(fileName);
                line = r.ReadLine();
                string dirNameOrg = line;
                while (!r.EndOfStream)
                {
                    line = r.ReadLine();
                    if (File.Exists(line))
                    {
                        string nn=Path.GetFileName(line);
                        if(nn.Contains("list"))
                        {
                            string[] a = nn.Split('.');
                            nn = a[0];
                        }
                        if (nn.Contains("_"))
                        {
                            string[] aa = nn.Split('_');
                            nn = aa[0];
                        }

                        string dirName = dirNameOrg+Path.DirectorySeparatorChar+nn;
                        aux = Load(line,dirName);
                        clOutput.Add(aux.name, aux);
                    }
                }
                r.Close();
            }
        }

        public void LoadExternalF(string fileName)
        {
            LoadExternal(fileName, ClusterOutput.LoadExternal);
        }
        public void LoadExternalPleiades(string fileName)
        {
            LoadExternal(fileName,ClusterOutput.LoadExternalPleiades);
        }
        public void LoadExternalPconsD(string fileName)
        {
            LoadExternal(fileName, ClusterOutput.LoadExternalPconsD);
        }

    }

}
