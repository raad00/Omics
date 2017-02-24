using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace uQlustCore.Distance
{
    class Pearson : JuryDistance
    {
        public Pearson(string dirName, string alignFile, bool flag, string profileName):
                base(dirName,alignFile,flag,profileName)
        {

        }
         public Pearson(DCDFile dcd, string alignFile, bool flag, string profileName, string refJuryProfile = null)        
            :base(dcd, alignFile, flag, profileName, refJuryProfile)        
        {
        }
        public Pearson(List<string> fileNames, string alignFile, bool flag, string profileName, string refJuryProfile = null) 
            :base(fileNames, alignFile, flag,  profileName,refJuryProfile)
        {

        }
        public Pearson(string dirName,string alignFile,bool flag,string profileName,string refJuryProfile=null) 
            :base(dirName,alignFile,flag,profileName,refJuryProfile) 
        {
          
        }
        public Pearson(string profilesFile, bool flag, string profileName, string refJuryProfile)
            :base(profilesFile,flag,profileName,refJuryProfile)
        {
        }
        public override List<KeyValuePair<string, double>> GetReferenceList(List<string> structures)
        {
          //  if (jury != null)
                //return jury.JuryOpt(structures).juryLike[0].Key;
            //    return jury.JuryOptWeights(structures).juryLike;
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
            double avr = 0; ;
            for (int j = 0; j < refPos.Length; j++)
                avr += refPos[j];
            avr /=refPos.Length;
            for (int i = 0; i < structures.Count; i++)
            {
                double dist = 0;
                List<byte> mod1 = stateAlign[structures[i]];
                double Sxx = 0;
                double Sxy = 0;
                double Syy = 0;

                double avrMod = 0;

                for (int j = 0; j < refPos.Length; j++)
                    avrMod += mod1[j];
                avrMod /= mod1.Count;

                for (int j = 0; j < mod1.Count; j++)
                {
                    Sxx+=mod1[j]*mod1[j];
                    Syy+=refPos[j]*refPos[j];
                    Sxy+=mod1[j]*refPos[j];
                }
             //   Sxx -= mod1.Count * avrMod * avrMod;
                //Syy-= mod1.Count * avr * avr;
                //Sxy-=mod1.Count*avr*avrMod;
                dist =(1- Sxy*Sxy/(Sxx*Syy))*100;

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
            double dist = 0;
            if (!stateAlign.ContainsKey(refStructure))
                throw new Exception("Structure: " + refStructure + " does not exists in the available list of structures");

            if (!stateAlign.ContainsKey(modelStructure))
                throw new Exception("Structure: " + modelStructure + " does not exists in the available list of structures");

            List<byte> mod1 = stateAlign[refStructure];
            List<byte> mod2 = stateAlign[modelStructure];
            double avrMod1=0,avrMod2=0;
            for(int j=0;j<mod1.Count;j++)
            {
                avrMod1 += mod1[j];
                avrMod2 += mod2[j];
            }

            avrMod1 /= mod1.Count;
            avrMod2 /= mod2.Count;


            double Sxx = 0;
            double Sxy = 0;
            double Syy = 0;
            for (int j = 0; j < mod1.Count; j++)
            {
                Sxx += mod1[j] * mod1[j];
                Syy += mod2[j] * mod2[j];
                Sxy += mod1[j] * mod2[j];
            }
            /*Sxx -= mod1.Count * avrMod1 * avrMod1;
            Syy -= mod2.Count * avrMod2 * avrMod2;
            Sxy -= mod1.Count * avrMod2 * avrMod1;*/
            double vv = Sxy * Sxy / (Sxx * Syy);
            vv = Math.Sqrt(vv);
            dist = (1.0-vv)*100;



            return (int)dist;
        }
        public override string ToString()
        {
            return "Pearson";
        }
    }
}
