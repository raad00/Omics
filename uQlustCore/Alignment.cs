using System;
using System.IO;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Text;
using System.Data;
using uQlustCore.Interface;

namespace uQlustCore
{
    public class Alignment : IProgressBar
	{
        Settings dirSettings;        
		Dictionary <string,string> align=new Dictionary<string,string>();
        int gcCounter = 0;
        string refSeq = null;
        int maxV;
		public ProfileTree r;
        public Alignment()
        {
            r = new ProfileTree();
        }

        public double ProgressUpdate()
        {
            return r.ProgressUpdate();
        }

        public Exception GetException()
        {
            return null;
        }
        public List<KeyValuePair<string, DataTable>> GetResults()
        {
            return null;
        }


        public void Prepare(DCDFile dcd,Settings dirSettings, string profName)
        {
            StartAlignment(dirSettings, profName, dcd, null, null);
        }

		public void Prepare(string pathName,Settings dirSettings,string profName)
		{
            StartAlignment(dirSettings, profName, null, pathName, null);
        }
        public void Prepare(List<string>fileNames, Settings dirSettings, string profName)
        {
            StartAlignment(dirSettings, profName, null, null, fileNames);
        }
        public void Prepare(List<string> names,string alignFile,string profName)
        {

        }
        public void Clean()
       {
           align.Clear();
           r.protCombineStates.Clear();
           r.profiles.Clear();          
       }
        public void Prepare(List<KeyValuePair<string,string>> profilesStr,string profName,string profFile)
        {
            r = new ProfileTree();
            r.LoadProfiles(profFile);            
            
            foreach(var item in profilesStr)
            {
                List<string> aux = new List<string>(item.Value.Length);
                for (int i = 0; i < item.Value.Length; i++)
                    aux.Add(item.Value[i].ToString());

               r.AddItemsCombineStates(item.Key,aux);
                                
            }
        }
        public void Prepare(string profilesFile, string profName)
        {
            r.LoadProfiles(profName);
            r.listFile = profilesFile;
            DebugClass.WriteMessage("profiles gen started "+profName);
            try
            {
                r.MakeProfiles();
            }
            catch (Exception cdcdd)
                {
                    Console.Write("dddd");
                }
            DebugClass.WriteMessage("Prfofiles end");
        }
        private void StartAlignment(Settings dirSettings, string profName, DCDFile dcd, string dirName, List<string> fileNames )
        {

            DebugClass.WriteMessage("Start align");
            string refFile = null;
            this.dirSettings = dirSettings;            
           // r = new ProfileTree();
            r.LoadProfiles(profName);
            if (dcd != null)
            {
                DebugClass.WriteMessage("profiles gen started");
                r.PrepareProfiles(dcd);
            }
            else
                if (dirName != null)
                {
                    refFile = dirName + ".ref";
                    DebugClass.WriteMessage("profiles gen started");
                    r.PrepareProfiles(dirName);
                    DebugClass.WriteMessage("finished");
                    refSeq=ReadRefSeq(refFile);
                }
                else
                {
                    maxV = fileNames.Count;
                    string name = fileNames[0];
                    if(fileNames[0].Contains("|"))
                    {
                        string[] aux = fileNames[0].Split('|');
                        name = aux[0];
                    }
                    refFile = Directory.GetParent(name).ToString() + ".ref";
                    DebugClass.WriteMessage("profiles gen started");
                    r.PrepareProfiles(fileNames);
                    DebugClass.WriteMessage("finished");
                    refSeq = ReadRefSeq(refFile);
                }
            DebugClass.WriteMessage("Prfofiles end"); 


        }
		public Dictionary<string,string> GetAlignment()
		{
			return align;
		}
		public Dictionary<string, List<byte>> GetStateAlign()
		{
			return r.protCombineStates;
		}
        public static string ReadRefSeq(string fileName)
        {
            string rSeq;
            if (!File.Exists(fileName))
                return null;

            StreamReader stR = new StreamReader(fileName);
            rSeq = stR.ReadLine();
            stR.Close();
            return rSeq;
        }
        public static Dictionary<string, string> ReadAlignment(string fileName)
        {
            Dictionary<string, string> alignLoc = new Dictionary<string, string>();
            StreamReader file_in = null;
            string line, remName = "";

            try
            {
                file_in = new StreamReader(fileName);
                line = file_in.ReadLine();
                while (line != null)
                {
                    if (line.Contains(">"))
                    {
                        string name = line.Substring(1, line.Length-1);
                        string profile = "";
                        line = file_in.ReadLine();
                        while (line!=null && !(line.Contains(">")))
                        {
                            profile+=line;
                            line = file_in.ReadLine();                                                        
                        }
                        if (alignLoc.Count == 0)
                            remName = name;
                        else
                        {
                            profile.Replace("\n", "");
                            profile.Replace(" ", "");
                            if (profile.Length != alignLoc[remName].Length)
                                throw new Exception("Alignment incorrect for " + remName + " and "+ name + "\nDifferent number of symbols in the alignment!");
                        }
                        alignLoc.Add(name, profile);
                    }
                    else
                        line = file_in.ReadLine();
                }
            }
            finally
            {
                if (file_in != null)
                    file_in.Close();
            }

            return alignLoc;

        }

        public void AddStructureToAlignment(string protName,string profileName,ref protInfo profile)
        {
           
                MAlignment al = new MAlignment(refSeq.Length);
                string resSeq = (al.Align(refSeq, profile.sequence)).seq2;

                if (!align.ContainsKey(protName))
                    align.Add(protName, resSeq);
                if(!r.profiles[profileName].ContainsKey(protName))
                    r.profiles[profileName].Add(protName,profile);
                
            
            AlignProfile(protName, profileName, profile.profile);
            profile.alignment = r.profiles[profileName][protName].alignment;
          
        }
		public void MyAlign(string alignFile)
		{
            bool test=false;
            //Check if there is sequence that could be aligned            
            foreach (var item in r.profiles)
            {

                foreach (var pp in item.Value)
                {
                    if (pp.Value.profile == null)
                        throw new Exception("No profile has been found!");
                    if (pp.Value.sequence == null || pp.Value.sequence.Length != pp.Value.profile.Count)
                        test = true;
                    break;
                }
                break;
            }
            if ((alignFile!=null && alignFile.Length > 0) || test)
            {
               // align = ReadAlignment(alignFile);

               
                foreach (var item in r.profiles)
                {
                    
                    align = new Dictionary<string, string>(item.Value.Keys.Count);
                    foreach (var pp in item.Value)
                    {
                        string alignProf;
                        char []aux = new char[pp.Value.profile.Count];
                        for(int i=0;i<pp.Value.profile.Count;i++)
                        {                         
                                aux[i]= 'C';
                        }
                        alignProf = new string(aux);
                        align.Add(pp.Key, alignProf);
                    }
                    break;
                }

            }
            else
            {
                    foreach (var item in r.profiles)
                    {
                        foreach (var pp in item.Value)
                        {
                            if (refSeq==null || pp.Value.sequence.Length > refSeq.Length)
                                refSeq = pp.Value.sequence;
                        }
                        break;
                    }
                MAlignment al = new MAlignment(refSeq.Length);
                foreach (var item in r.profiles)
                {
                    foreach (var pp in item.Value)
                    {
                        if (refSeq.Length != pp.Value.sequence.Length)
                        {
                            //Console.WriteLine(pp);

                            string seq = (al.Align(refSeq, pp.Value.sequence)).seq2;
                            if (seq.Length > 0)
                                align[pp.Key] = seq;
                        }
                        else
                            align[pp.Key] = refSeq;
                    }
                    break;

                }
                if (align.Count == 0)
                    throw new Exception("There is no alignment");
            }
            try
            {
                AlignProfiles();
            }
            catch (Exception)
            {
                throw new Exception("Combining alignments went wrong!");
            }
			
		}
		public void NoBlast()
		{
 			foreach (var item in r.profiles)
            {
                    foreach (var pp in item.Value)
                    {
                        align[pp.Key]=pp.Value.sequence;
                    }
                    break;                
            }		
			
			try
			{
				AlignProfiles();
			}
			catch(Exception ex)
			{
				Console.WriteLine("Something went wrong :"+ex.Message);
			}
		}
        private void AlignProfile(string protName,string profileName,List<byte>prof)
        {
            int m = 0;
            string alignProfile = align[protName];
            List<byte> ll = new List<byte>(alignProfile.Length);
            for (int i = 0; i < alignProfile.Length; i++)
            {
                if (alignProfile[i] == '-')
                {
                    ll.Add(0);
                    continue;
                }
                if (m < prof.Count)
                    ll.Add(prof[m++]);
                else
                    ErrorBase.AddErrors("Profile " + profileName + " for " + protName + " seems to be incorect");

            }
            protInfo tmp = new protInfo();
            if (r.profiles[profileName].ContainsKey(protName))
            {
                tmp = r.profiles[profileName][protName];
                tmp.alignment = ll;
                r.profiles[profileName][protName] = tmp;
            }
           

        }
		public void AlignProfiles()
		{
            List <string> profName=new List<string>(r.profiles.Keys);
            List<string> alignKeys = new List<string>(align.Keys);
            foreach (string protName in alignKeys)
            {

                if (((string)align[protName]).Length < 5)
                    continue;
                bool test = true;
                foreach (var item in r.profiles)
                    if (!item.Value.ContainsKey(protName))
                        test = false;

                if (!test)
                    continue;


                foreach (var item in r.profiles)
                {
                    AlignProfile(protName, item.Key, item.Value[protName].profile);
                    align[item.Key]="";
                }
               
               // Console.WriteLine("Combine profiles " + Process.GetCurrentProcess().PeakWorkingSet64);
                 r.CombineProfiles(protName, r.profiles);
                 r.profiles.Remove(protName);
                //    protCombineStates.Add(protName, states);
               
               align.Remove(protName);
               gcCounter++;
               if (gcCounter > 5000)
               {
                   GC.Collect();
                   gcCounter = 0;
               }
            }
          
            GC.Collect();

           
		}
        public int ProfileLength()
        {
            return r.GetProfileLength();
        }
        public Dictionary<byte, double> StatesStat()
        {
            return r.StatesStat();
        }
        public void CombineAll()
        {
            List<string> prots=new List<string>(r.profiles.Keys);
            foreach (var item in r.profiles[prots[0]].Keys)
                r.CombineProfiles(item, r.profiles);
        }
        public Dictionary<string,List<byte>> CombineProfiles(string protName,Dictionary<string,Dictionary<string,protInfo>> pr)
        {
            return r.CombineProfiles(protName,pr);
        }
		public void PrintAlign()
		{
			StreamWriter file=null;
			try
			{
				file=new StreamWriter("aaa");
			
			
				file.WriteLine("Alignment:");
				foreach(string i in align.Keys)
				{
				//	file.WriteLine(i);
					file.WriteLine(align[i]);
				}
			}
			finally
			{
				if(file!=null)
					file.Close();
				
			}
				
		}
	}
}

