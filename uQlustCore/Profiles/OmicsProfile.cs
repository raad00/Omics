using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Reflection;
using System.Globalization;
using uQlustCore;

namespace uQlustCore.Profiles
{
    public enum CodingAlg
    {
        PERCENTILE,
        Z_SCORE,
        EQUAL_DIST
    };
    public class OmicsProfile : UserDefinedProfile
    {
        public static string omicsSettings = "omicsSettings.dat";
        public int numCol=10;
        public int numRow=18;
        public bool genePosition;
        public bool zScore;
        public bool quantile;
        public int numStates = 6;
        public int labelGeneStart = 17;
        public int labelSampleStart = 1;
        public bool uLabelGene = false;
        public bool uLabelSample = false;
        public int labelNumRows = 1;
        string processName;
        public bool heatmap = false;

        profileNode localNode = null;
        profileNode localNodeDist = null;
        List<string> labels = new List<string>();
        List<string> labelGenes = new List<string>();
        List<string> labelSamples = new List<string>();
        List<string> labelsData=new List<string>();
        public bool transpose = false;
        public CodingAlg coding = CodingAlg.EQUAL_DIST;
        static int profSize = 0;

        double []dev = null;
       
        double []avr=null;


        Settings set=new Settings();
        public OmicsProfile()
        {
            try
            {
                ProfileName = "Omics profile";
                profileName = ProfileName;
                AddInternalProfiles();
                destination = new List<INPUTMODE>();
                destination.Add(INPUTMODE.OMICS);
            }
            catch(Exception ffff)
            {
                Console.Write("SJSKSK");
            }
            try
            {
                set.Load();
                LoadOmicsSettings();
            }
            catch(Exception eee)
            {
                Console.Write("SJSKSK");
            }
        }
        public void SaveOmicsSettings()
        {
            StreamWriter wr = new StreamWriter(omicsSettings);

            wr.WriteLine("Column " + numCol);
            wr.WriteLine("Rows " + numRow);
            wr.WriteLine("Use gene labels " + uLabelGene);
            wr.WriteLine("Use sample labels " + uLabelSample);
            wr.WriteLine("Label Genes " + labelGeneStart);
            wr.WriteLine("Label Samples " +labelSampleStart);
            wr.WriteLine("Label Number of rows " + labelNumRows);
            wr.WriteLine("States " + numStates);
            wr.WriteLine("transposition " + transpose);
            wr.WriteLine("Coding Algorithm " + coding);
            wr.WriteLine("Heatmap " + heatmap);
            if(processName.Length>0)
                wr.WriteLine("OutputName " + processName);
            wr.WriteLine("Gene Position Rows " + genePosition);
            wr.WriteLine("Z-score " + zScore);
            wr.WriteLine("Quantile " + quantile);
            wr.Close();
        }
        public void LoadOmicsSettings()
        {
            try
            {
                if (!File.Exists(omicsSettings))
                    return;

                StreamReader r = new StreamReader(omicsSettings);
                string line = r.ReadLine();

                while (line != null)
                {
                    string[] aux = line.Split(' ');
                    if (line.Contains("Column "))
                        numCol = Convert.ToInt32(aux[aux.Length - 1], CultureInfo.InvariantCulture);
                    if (aux[0].Equals("Rows"))
                        numRow = Convert.ToInt32(aux[aux.Length - 1], CultureInfo.InvariantCulture);
                    if (line.Contains("Label"))
                        if(line.Contains("Genes"))
                            labelGeneStart = Convert.ToInt32(aux[aux.Length - 1], CultureInfo.InvariantCulture);
                        else
                            if(line.Contains("Samples"))
                                labelSampleStart = Convert.ToInt32(aux[aux.Length - 1], CultureInfo.InvariantCulture);
                            else
                                if (line.Contains("Number"))
                                    labelNumRows = Convert.ToInt32(aux[aux.Length - 1], CultureInfo.InvariantCulture);
                    if(line.Contains("Use"))
                        if(line.Contains("gene"))
                            uLabelGene = Convert.ToBoolean(aux[aux.Length - 1], CultureInfo.InvariantCulture);
                        else
                            if(line.Contains("sample"))
                                uLabelSample = Convert.ToBoolean(aux[aux.Length - 1], CultureInfo.InvariantCulture);


                    if (line.Contains("States"))
                        numStates = Convert.ToInt32(aux[aux.Length - 1], CultureInfo.InvariantCulture);
                    if (line.Contains("trans"))
                        transpose = Convert.ToBoolean(aux[aux.Length - 1], CultureInfo.InvariantCulture);
                    if (line.Contains("Algo"))
                        coding = (CodingAlg)Enum.Parse(typeof(CodingAlg), aux[aux.Length - 1]);
                    if (line.Contains("Heatmap"))
                        heatmap = Convert.ToBoolean(aux[aux.Length - 1], CultureInfo.InvariantCulture);
                    if (line.Contains("OutputName"))
                        processName = aux[aux.Length - 1];
                    if (line.Contains("Gene Position Rows"))
                        genePosition = Convert.ToBoolean(aux[aux.Length - 1], CultureInfo.InvariantCulture);
                    if (line.Contains("Z-score"))
                        zScore = Convert.ToBoolean(aux[aux.Length - 1], CultureInfo.InvariantCulture);
                    if (line.Contains("Quantile"))
                        quantile = Convert.ToBoolean(aux[aux.Length - 1], CultureInfo.InvariantCulture);
                    line = r.ReadLine();
                }
                r.Close();
            }
            catch(Exception ex)
            {
                Console.Write("");
            }

        }
        public static List<KeyValuePair< string,List<int>>> ReadOmicsProfile(string fileName)
        {
            List<KeyValuePair<string, List<int>>> dic = new List<KeyValuePair< string,List<int>>>();
            string key="";
            string value;
            StreamReader r = new StreamReader("generatedProfiles"+Path.DirectorySeparatorChar+fileName);

            string line = r.ReadLine();
            while(line!=null)
            {
                if(line.Contains(">"))                
                    key = line.Remove(0, 1);
                else
                {
                    if(line.Contains(ProfileName))
                    {
                        value = line.Remove(0, ProfileName.Length+1);
                        string[] aux = value.Split(' ');
                        List<int> v = new List<int>(aux.Length);
                        foreach (var item in aux)
                            v.Add(Convert.ToInt32(item, CultureInfo.InvariantCulture));
                        KeyValuePair<string, List<int>> xx = new KeyValuePair<string, List<int>>(key, v);
                        dic.Add(xx);
                    }
                }
                line = r.ReadLine();
            }
            r.Close();
            return dic;
        }
        public override void RunThreads(string fileName)
        {
            ThreadFiles ff = new ThreadFiles();

            //StreamReader str = new StreamReader(fileName);
            ff.fileName = fileName ;     
            Run((object)ff);
        }
        string GetProfileName(string fileName)
        {
       //     string name =/* Path.GetFileNameWithoutExtension(fileName) + "_" +*/ profileName;
       //     name = name.Replace(" ", "_");
         //   return set.profilesDir + Path.DirectorySeparatorChar + name +"_"+ processName;
            return set.profilesDir + Path.DirectorySeparatorChar +  processName;
        }
        public double [,] QuantileNorm(double[,] data)
        {
            int[][] copyData;
            double[,] rankData;
            Dictionary<double, int> dic = new Dictionary<double, int>();
            double[] avr;

            avr = new double[data.GetLength(0)];
            rankData = new double[data.GetLength(0), data.GetLength(1)];
            copyData = new int[data.GetLength(1)][];
            for (int i = 0; i < data.GetLength(1); i++)
                copyData[i] = new int[data.GetLength(0)];

            for (int i = 0; i < data.GetLength(1); i++)
                for (int j = 0; j < data.GetLength(0); j++)
                    copyData[i][j] = j;

            for (int i = 0; i < data.GetLength(1); i++)
            {
                Array.Sort<int>(copyData[i], (a, b) => data[a, i].CompareTo(data[b, i]));
                dic.Clear();
                for (int j = 0; j < data.GetLength(0); j++)
                {
                    if (data[copyData[i][j], i] != double.NaN)
                    {
                        if (!dic.ContainsKey(data[copyData[i][j], i]))
                            dic.Add(data[copyData[i][j], i], j + 1);
                        rankData[copyData[i][j], i] = dic[data[copyData[i][j], i]];
                    }
                    else
                        rankData[copyData[i][j], i] = double.NaN;
                }
            }


            for (int i = 0; i < avr.Length; i++)
            {
                double sum = 0;
                for (int j = 0; j < data.GetLength(1); j++)
                    if (data[copyData[j][i], j]!=double.NaN)
                        sum += data[copyData[j][i], j];
                avr[i] = sum / data.GetLength(1);
            }

            for (int i = 0; i < data.GetLength(0); i++)
                for (int j = 0; j < data.GetLength(1); j++)
                    if (rankData[i, j]!=double.NaN)
                        rankData[i, j] = avr[(int)rankData[i, j] - 1];


            return rankData;
        }
        int ReadLabels(StreamReader r,string line)
        {
            int i;
            int rowPosition;
            List<string> tmpAux;
           // line = r.ReadLine();

            if (!genePosition)
            {
                tmpAux = labelGenes;
                rowPosition = labelGeneStart;
            }
            else
            {
                tmpAux = labelSamples;
                rowPosition = labelSampleStart;
            }

            for (i = 1; i < rowPosition; i++)
                line = r.ReadLine();

            line = line.Replace('\t', ' ');
            string[] aux = line.Split(' ');            
            for (int j = numCol, n = 0; j < aux.Length; j++, n++)
                tmpAux.Add(aux[j]);

            return i;
        }
        List<List<double>> ReadOmicsFile(string fileName)
        {
            List<List<double>> localData = new List<List<double>>();
            StreamReader r = new StreamReader(fileName);

            if (r == null)
                throw new Exception("Cannot open file: " + fileName);
            int i=0;
            
            string line= r.ReadLine();
            char tab = '\t';
            List<List<double>> data = new List<List<double>>();
            if(genePosition && uLabelSample || !genePosition && uLabelGene)
                i = ReadLabels(r,line);

            for (; i < numRow; i++)
                line = r.ReadLine();
            int counter=1;

            while (line != null)
            {

                line = line.Replace(tab.ToString(), " ");

                string[] aux = line.Split(' ');
                List<double> rowFile = new List<double>(aux.Length - numCol);

                if (genePosition && uLabelGene)
                    labelGenes.Add(aux[labelGeneStart-1]+"_"+counter++);
                else
                    if(!genePosition && uLabelSample)
                        labelSamples.Add(aux[labelSampleStart-1]+"_"+counter++);

                for (i = numCol; i < aux.Length; i++)
                {
                    double tmp=0;
                    try
                    {
                        tmp = Convert.ToDouble(aux[i], CultureInfo.InvariantCulture);
                    }
                    catch(Exception ex)
                    {
                        tmp = double.NaN;
                    }
                    rowFile.Add(tmp);
                }
                localData.Add(rowFile);
                line = r.ReadLine();
            }
            r.Close();

            if (!genePosition)
            {
                if (labelSamples != null || labelSamples.Count == 0)
                    for (i = 0; i < localData.Count; i++)
                        labelSamples.Add("Sample_" + (i + 1));
                if (labelGenes == null || labelGenes.Count == 0)
                    for (i = 0; i < localData[0].Count; i++)
                        labelGenes.Add("Gene_" + (i + 1));
            }
            else
            {
                if (labelGenes == null || labelGenes.Count == 0)
                    for (i = 0; i < localData.Count; i++)
                        labelGenes.Add("Gene_" + (i + 1));

                if (labelSamples != null || labelSamples.Count == 0)
                    for (i = 0; i < localData[0].Count; i++)
                        labelSamples.Add("Sample_" + (i + 1));


            }
            return localData;
        }
        double [,] TransposeData(double [,] data)
        {
            double [,]dataFinal = new double[data.GetLength(1), data.GetLength(0)];

            for (int i = 0; i < data.GetLength(0); i++)
                for (int j = 0; j < data.GetLength(1); j++)
                    dataFinal[j, i] = data[i, j];


                    return dataFinal;
        }
        void Check(double [,] data)
        {
            /*Console.WriteLine("Wiersze");
            for(int i=0;i<data.GetLength(0);i++)
            {
                double sum=0;                
                for (int j = 0; j < data.GetLength(1); j++)
                    sum += data[i, j];

                Console.WriteLine("sum="+sum);
            }*/
            Console.WriteLine("Columny");
            List<double> median = new List<double>();
            for (int j = 0; j < data.GetLength(1); j++)
            {
                double sum = 0;
                median.Clear();
                for (int i = 0; i < data.GetLength(0); i++)
                    median.Add(data[i, j]);
                    //sum += data[i, j];
                median.Sort((x, y) => x.CompareTo(y));
                sum = median[median.Count / 2];
                Console.WriteLine("sum=" + sum);
            }
        }
        public override int Run(object processParams)
        {
            string fileName = ((ThreadFiles)(processParams)).fileName;
            StreamReader r = new StreamReader(fileName);
            int i;

            if (heatmap)
                return 0;

            List<List<double>> data = ReadOmicsFile(fileName);

            dev = new double[data.Count];
            avr = new double[data.Count];


            double [,]dataFinal;
            dataFinal=new double [data.Count,data[0].Count];
            for (i = 0; i < data.Count; i++)
                for (int j = 0; j < data[i].Count; j++)
                        dataFinal[i, j] = data[i][j];

            if (genePosition)
            {
                if (labelGenes.Count > 0)
                    labelsData = labelGenes;
            }
            else
                labelsData = labelSamples;

            List<string> transposeLabelsData;


            if (!genePosition)
                dataFinal = TransposeData(dataFinal);

            if (zScore)
                StandardData(dataFinal);

            if (quantile)
            {
                // dataFinal = TransposeData(dataFinal);
                dataFinal = QuantileNorm(dataFinal);
            //    dataFinal = TransposeData(dataFinal);
            }
//            Check(dataFinal);
            //if (zScore)
              //  StandardData(dataFinal);

            

            //if (!transpose)
           //     dataFinal = TransposeData(dataFinal);
            
            StreamWriter wr;
            string profFile = GetProfileName(fileName);

            wr = new StreamWriter(profFile);
           // dataFinal = TransposeData(dataFinal);
            double [,] outData=IntervalCoding(dataFinal);
            if (!genePosition)
                outData = TransposeData(outData);
            
            int k=0;
            for (i = 0; i < outData.GetLength(0); i++)
            {
                wr.WriteLine(">" + labelsData[i]);
                wr.Write(ProfileName + " ");
                for (k = 0; k < outData.GetLength(1) - 1; k++)
                    wr.Write((int)outData[i, k] + " ");
                wr.WriteLine((int)outData[i, k]);
            }
            wr.Close();

            if (!genePosition)
                    labelsData = labelGenes;
            else
                    labelsData = labelSamples;

            wr.Close();
            string profFileTransp = profFile + "_transpose";
            wr = new StreamWriter(profFileTransp);
            for (i = 0; i < outData.GetLength(1); i++)
            {
                wr.WriteLine(">" + labelsData[i]);
                wr.Write(ProfileName + " ");
                for (k = 0; k < outData.GetLength(0) - 1; k++)
                    wr.Write((int)outData[k,i] + " ");
                wr.WriteLine((int)outData[k,i]);
            }
            wr.Close();

            ProfileTree td=ProfileAutomatic.AnalyseProfileFile(profFile, SIMDIST.DISTANCE, ProfileName);
            List<string> keys=new List<string>(td.masterNode.Keys);

            ProfileTree ts = ProfileAutomatic.AnalyseProfileFile(profFile, SIMDIST.SIMILARITY, ProfileName);
                string locPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + Path.DirectorySeparatorChar + "profiles" + Path.DirectorySeparatorChar;
                td.SaveProfiles(locPath + Path.GetFileNameWithoutExtension(fileName) + "_distance.profile");
                ts.SaveProfiles(locPath + Path.GetFileNameWithoutExtension(fileName) + ".profiles");
                profSize = td.masterNode[keys[0]].codingToByte.Count;

                List<string> m = new List<string>(ts.masterNode.Keys);
                localNode = ts.masterNode[m[0]];
                localNodeDist = td.masterNode[m[0]];
            return 0;

        }
        void StandardData(double [,]data)
        {
            double sumX = 0;
            double sumX2 = 0;

            dev = new double[data.GetLength(0)];
            avr = new double[data.GetLength(0)];


            for (int i = 0; i < data.GetLength(0); i++)
            {
                sumX = 0;
                sumX2 = 0;
                for (int j = 0; j < data.GetLength(1); j++)
                    if (data[i, j] != double.NaN)
                    {
                        sumX += data[i, j];
                        sumX2 += data[i, j] * data[i, j];
                    }

                sumX /= data.GetLength(1);
                sumX2 /= data.GetLength(1);
                avr[i] = sumX;
                dev[i] = Math.Sqrt(sumX2 - avr[i] * avr[i]);

                for (int j = 0; j < data.GetLength(1); j++)
                    if (data[i,j] != double.NaN && dev[i]>0)
                        data[i,j] = (data[i,j] - avr[i]) / dev[i];
            }
        }




        double [,] IntervalCoding(double [,] data)
        {
            double[,] outData = new double [data.GetLength(0), data.GetLength(1)];
            double[,] newData = data;
            HashSet<double> hashValues = new HashSet<double>();
            double []colValues = new double [data.GetLength(1)];
            double[][,] intervals = new double[data.GetLength(0)][,];
            double[,] intervalsNew;
            if (coding == CodingAlg.Z_SCORE)
                newData=ZscoreCoding(data);

            Console.WriteLine("Start interval");
            
            for (int j = 0; j < newData.GetLength(0); j++)
            {
                for (int i = 0; i < newData.GetLength(1); i++)
                {
                    colValues[i] = data[j, i];

                    if (!hashValues.Contains(data[j, i]))
                        hashValues.Add(data[j,i]);
                }

                Array.Sort(colValues);
                intervals[j] = SetupIntervals(colValues);
            }

            int nn=0;
            colValues = new double[hashValues.Count];
            foreach (var item in hashValues)
                colValues[nn++] = item;
            Array.Sort(colValues);
            intervalsNew = SetupIntervals(colValues);

            Console.WriteLine("Start interval 2");
            int[] codedRow = new int[newData.GetLength(1)];
            for (int i = 0; i < newData.GetLength(0); i++)
            {
                for (int j = 0; j < newData.GetLength(1); j++)
                {
                    int code = intervals[0].GetLength(0);
                    for (int k = 0; k < intervalsNew.GetLength(0); k++)
                    {
                        if(newData[i,j]==double.NaN)
                        {
                            code = 0;
                            break;
                        }
/*                        if (newData[i, j] >= intervals[i][k, 0] && newData[i, j] < intervals[i][k, 1])
                        {
                            code = k+1;
                            break;
                        }*/
                        if (newData[i, j] >= intervalsNew[k, 0] && newData[i, j] < intervalsNew[k, 1])
                        {
                            code = k + 1;
                            break;
                        }
                    }
                    codedRow[j] = code;
                }
                Console.WriteLine("Start interval 3");
                //string rowLine = String.Join(" ", codedRow);
                for (int n = 0; n < outData.GetLength(1); n++)
                    outData[i, n] = codedRow[n];
                   // wr.WriteLine(">" + labelsData[i]);
                //wr.WriteLine(profileName + " " + rowLine);

            }
            StreamWriter ir;
            if(processName.Length>0)
                ir = new StreamWriter("generatedProfiles/OmicsIntervals_"+processName+".dat");
            else
                ir = new StreamWriter("generatedProfiles/OmicsIntervals_.dat");
            for (int i = 0; i < intervalsNew.GetLength(0); i++)
                ir.WriteLine("Code " + i + " " + intervalsNew[i,0] + " " + intervalsNew[i,1]);
            ir.Close();


            return outData;
        }
        double[, ] SetupIntervals(double []colValues)
        {
            double [,] intervals=new double [numStates,2];
            double max, min;
            max = double.MinValue;
            min = double.MaxValue;
            for (int i = 0; i < colValues.Length; i++)
            {
                if (max < colValues[i])
                    max = colValues[i];
                if (min > colValues[i])
                    min = colValues[i];
            }

            switch(coding)
            {
                case CodingAlg.EQUAL_DIST:
                case CodingAlg.Z_SCORE:
                    double step = (max - min) / numStates;
                    for (int i = 0; i < numStates; i++)
                    {
                        intervals[i, 0] = colValues[0] + i * step;
                        intervals[i, 1] = colValues[0] + (i + 1) * step;
                    }
                    break;
                case CodingAlg.PERCENTILE:                
                    double size = (double)colValues.Length / numStates;
                    int num=0;
                    int rem = 0;
                    int n = 0;
                    for(n=0;n<colValues.Length;n++)
                    {
                        if(n>=size*(num+1))
                        {
                            while (n + 1 < colValues.Length && colValues[n] == colValues[n+1] || colValues[n]==double.NaN) n++;
                            
                            if (n < colValues.Length)
                            {
                                intervals[num, 1] = colValues[n];
                                intervals[num, 0] = colValues[rem];
                            }
                      
                            rem = n;
                            num++;
                        }
                    }
                    if (num < numStates)
                    {
                        intervals[num, 0] = colValues[rem];
                        intervals[num, 1] = max;
                    }

                    break;
            }
            return intervals;

        }

        double [,] ZscoreCoding(double [,] data)
        {
            double[,] newData = new double[data.GetLength(0), data.GetLength(1)];
            int binData = data.GetLength(0) / numStates;
            double []colValues = new double [data.GetLength(0)];
            double[,] zScoreData = new double[data.GetLength(0), data.GetLength(1)];
            double[] stdDev = new double[data.GetLength(1)];
            double[] avr = new double[data.GetLength(1)];
            for (int j = 0; j < data.GetLength(1); j++)
            {
                for (int i = 0; i < data.GetLength(0); i++)                
                    colValues[i]=data[i, j];                

                double sumX2=0, sumX=0;

                foreach(var item in colValues)
                {
                    if (item != double.NaN)
                    {
                        sumX2 += item * item;
                        sumX += item;
                    }
                }
                avr[j] = sumX / colValues.Length;

                stdDev[j] = Math.Sqrt(sumX2 / colValues.Length - avr[j]  * avr[j]);
                
                for(int i=0;i<data.GetLength(0);i++)                
                    if(data[i,j]!=double.NaN)
                        newData[i,j]= (data[i, j] - avr[j]) / stdDev[j];
                               
            }
            return newData;
        }
        public override Dictionary<string, protInfo> GetProfile(profileNode node, string fileNameProf, DCDFile dcd)
        {
            Dictionary<string, protInfo> data;
            string fileName = GetProfileName(fileNameProf);

            if (heatmap || transpose)
                fileName = fileName + "_transpose";

            ProfileTree ts = new ProfileTree();
            string locPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + Path.DirectorySeparatorChar + "profiles" + Path.DirectorySeparatorChar;
            ts.LoadProfiles(locPath + Path.GetFileNameWithoutExtension(fileNameProf) + ".profiles");
            List<string> k = new List<string>(ts.masterNode.Keys);
            localNode = ts.masterNode[k[0]];
            if (node.profWeights.Count == localNode.profWeights.Count)
            {
                data = base.GetProfile(localNode, fileName, dcd);
            }
            else
            {
                ts.LoadProfiles(locPath + Path.GetFileNameWithoutExtension(fileNameProf) + "_distance.profile");
                k = new List<string>(ts.masterNode.Keys);
                localNode = ts.masterNode[k[0]];
                data = base.GetProfile(localNode, fileName, dcd);
            }
         
            return data;
        }
    }
}
