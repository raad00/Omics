using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace uQlustCore.Distance
{
    class Euclidian: JuryDistance

    {
    
        public Euclidian(string dirName, string alignFile, bool flag, string profileName):
                base(dirName,alignFile,flag,profileName)
        {

        }
         public Euclidian(DCDFile dcd, string alignFile, bool flag, string profileName, string refJuryProfile = null)        
            :base(dcd, alignFile, flag, profileName, refJuryProfile)        
        {
        }
        public Euclidian(List<string> fileNames, string alignFile, bool flag, string profileName, string refJuryProfile = null) 
            :base(fileNames, alignFile, flag,  profileName,refJuryProfile)
        {

        }
        public Euclidian(string dirName,string alignFile,bool flag,string profileName,string refJuryProfile=null) 
            :base(dirName,alignFile,flag,profileName,refJuryProfile) 
        {
          
        }
        public Euclidian(string profilesFile, bool flag, string profileName, string refJuryProfile)
            :base(profilesFile,flag,profileName,refJuryProfile)
        {
        }
        public override string ToString()
        {
            return "Cosine";
        }
        public override List<KeyValuePair<string, double>> GetReferenceList(List<string> structures)
        {
            if (jury != null)
                //return jury.JuryOpt(structures).juryLike[0].Key;
                return jury.JuryOptWeights(structures).juryLike;
            //return jury.ConsensusJury(structures).juryLike;

            List<KeyValuePair<string, double>> refList = new List<KeyValuePair<string, double>>();
            int[] refPos = new int[stateAlign[structures[0]].Count];
            for (int i = 0; i < structures.Count; i++)
            {
                List<byte> mod1 = stateAlign[structures[i]];
                for (int j = 0; j < mod1.Count; j++)
                    refPos[j] += mod1[j];
            }
            for (int j = 0; j < refPos.Length; j++)
                refPos[j] /= structures.Count;

            for (int i = 0; i < structures.Count; i++)
            {
                double dist = 0;
                List<byte> mod1 = stateAlign[structures[i]];
                //for (int j = 0; j < mod1.Count; j++)
                //  dist += (mod1[j] - refPos[j]) * (mod1[j] - refPos[j]);
                for (int j = 0; j < mod1.Count; j++)
                {
                    // dist += (mod1[j] - mod2[j]) * (mod1[j] - mod2[j]);
                    dist += (mod1[j] - refPos[j]) * (mod1[j] - refPos[j]);

                }

                KeyValuePair<string, double> aux = new KeyValuePair<string, double>(structures[i], dist);
                refList.Add(aux);
            }
            refList.Sort((nextPair, firstPair) =>
            {
                return nextPair.Value.CompareTo(firstPair.Value);
            });
            return refList;

        }
        public override string GetReferenceStructure(List<string> structures)
        {
            List<KeyValuePair<string, double>> refList = null;
            refList = GetReferenceList(structures);

            return refList[0].Key;
        }
        public override int GetDistance(string refStructure, string modelStructure)
        {
            int dist = 0;
            if (!stateAlign.ContainsKey(refStructure))
                throw new Exception("Structure: " + refStructure + " does not exists in the available list of structures");

            if (!stateAlign.ContainsKey(modelStructure))
                throw new Exception("Structure: " + modelStructure + " does not exists in the available list of structures");

            List<byte> mod1 = stateAlign[refStructure];
            List<byte> mod2 = stateAlign[modelStructure];
            for (int j = 0; j < stateAlign[refStructure].Count; j++)
            {
                 dist += (mod1[j] - mod2[j]) * (mod1[j] - mod2[j]);
            }
            return dist;
        }

    }
}
