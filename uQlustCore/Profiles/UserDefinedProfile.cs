﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Text.RegularExpressions;
namespace uQlustCore.Profiles
{
    
    public class UserDefinedProfile:InternalProfileBase        
    {
        //string profileName;

//        InternalProfilesManager manager = new InternalProfilesManager();
        public static string ProfileName = "User defined profile";


        public UserDefinedProfile()
        {           
          
            AddInternalProfiles();
            destination = new List<INPUTMODE>();
            destination.Add(INPUTMODE.PROTEIN);
            destination.Add(INPUTMODE.RNA);
            destination.Add(INPUTMODE.USER_DEFINED);
        }

        public override int Run(object processParams)
        {
            return 0;
        }
        public override void RunThreads(string fileName)
        {

        }
        public override void Run(DCDFile dcd)
        {
            throw new NotImplementedException();
        }        
        public override Dictionary<string, protInfo> GetProfile(profileNode node, string fileName, DCDFile dcd)
        {
            Dictionary<string, protInfo> dic = new Dictionary<string, protInfo>();
            StreamReader wr;
            DebugClass.WriteMessage("profile" + fileName);
            wr = new StreamReader(fileName);
            
            protInfo info;
            string line = wr.ReadLine();
            string name = "";
            string seq = "";
            List<string> profile = new List<string>();
            List<byte> newProfile = new List<byte>();
            while (line != null)
            {
                if (line.Contains(">"))
                {
                    if (name.Length > 0)
                    {
                        info = new protInfo();
                        info.sequence = null;
                        info.profile = newProfile;
                        dic.Add(name, info);
                    }

                    name = line.Replace(">", "");
                    line = wr.ReadLine();
                }
                if (line.Contains(node.profName+" "))
                {

                    string cLine=line.Remove(0, (node.profName+" profile ").Length);
                    cLine = Regex.Replace(cLine, @"\s+", " ");
                    cLine = cLine.Trim();
                    string[] aux;
                    if (cLine.Contains(' '))
                        aux = cLine.Split(' ');
                    else
                    {
                        char[] charArray = cLine.ToCharArray();
                        aux = cLine.Select(x => x.ToString()).ToArray();                        
                    }

                    profile.Clear();

                    foreach (var item in aux)
                        profile.Add(item);

                    
                    newProfile = new List<byte>();
                    for (int i = 0; i < profile.Count; i++)
                        if (node.ContainsState(profile[i].ToString()))
                            newProfile.Add(node.GetCodedState(node.states[profile[i].ToString()]));
                        else
                            if(profile[i].ToString()!="-" && profile[i].ToString()!="X")
                                throw new Exception("Unknow state " + profile[i].ToString() + " in "+ node.profName + " profile!");
                            else
                                newProfile.Add(0);


                }
                line = wr.ReadLine();
            }
            info = new protInfo();
            info.sequence = seq;
            info.profile = newProfile;
            dic.Add(name, info);
            DebugClass.WriteMessage("number of profiles " + dic.Keys.Count);

            wr.Close();            

            return dic;
        }
        public override void AddInternalProfiles()
        {
            profileNode node = new profileNode();

            node.profName = ProfileName;
            node.internalName = ProfileName;           
            InternalProfilesManager.AddNodeToList(node, this.GetType().FullName);
        }
        public override void RemoveInternalProfiles()
        {            
            InternalProfilesManager.RemoveNodeFromList(ProfileName);
        }
        public override List<INPUTMODE> GetMode()
        {
            List<INPUTMODE> oList = new List<INPUTMODE>();
            oList.Add(INPUTMODE.PROTEIN);
            oList.Add(INPUTMODE.RNA);

            return oList;
        }
    }
}
