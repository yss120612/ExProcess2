using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExProcess
{
    class Tools
    {
        public static Dictionary<string,int> users;
        public static void prepareUsers()
        {
            users = new Dictionary<string,int>();
            string usersfile = AppDomain.CurrentDomain.BaseDirectory + "\\users.list";
            if (!File.Exists(usersfile))
            {
                return;
            }
            StreamReader US = new StreamReader(usersfile, Encoding.UTF8);
            string [] data;
            char[] charSeparators = new char[] { ' ','\t' };
            int rn;
            while (!US.EndOfStream)
            {
                data = US.ReadLine().Split(charSeparators, StringSplitOptions.RemoveEmptyEntries);
                if (int.TryParse(data[1], out rn))
                {
                    users.Add(data[0],rn);
                }
            }
            US.Close();
            foreach (KeyValuePair<string,int> kv in users)
            {
                Console.WriteLine("User=" + kv.Key + " Raion=" + kv.Value);
            }
        }

        public static int NR(string s)
        {
            int pos=s.LastIndexOf('\\');
            if (pos < 0) return -1;
            string sresult = s.Substring(pos + 1, s.Length - pos-1);
            if (users.ContainsKey(sresult))
            {
                return users[sresult];
            }
                        //if (sresult.Contains("048BodyaginaTA")|| sresult.Contains("048GusevaOA") || sresult.Contains("048AsaulenkoVA"))
            //{
            //    return 1;
            //}
            //else if (sresult.Contains("048LitvinovaAN"))
            //{
            //    return 4;
            //}
            //else if (sresult.Contains("048SenkinaTA"))
            //{
            //    return 6;
            //}
            //else if (sresult.Contains("048AslamovaNA") || sresult.Contains("048PankratovaES") || sresult.Contains("048TSvetkovaOA"))
            //{
            //    return 7;
            //}
            //else if (sresult.Contains("048AkentievaAN") || sresult.Contains("048BokovaSA") || sresult.Contains("048KonovalovaNV") || sresult.Contains("048LyzhinaMA") || sresult.Contains("048SkorokhodovaVYU") || sresult.Contains("048KHaryanovaOV"))
            //{
            //    return 9;
            //}
            //else if (sresult.Contains("048MoiseenkoNN") || sresult.Contains("048PetrovaSA1"))
            //{
            //    return 10;
            //}
            //else if (sresult.Contains("048KotovaNA"))
            //{
            //    return 28;
            //}
            //else if (sresult.Contains("048KudryavtsevaTN"))
            //{
            //    return 29;
            //}
            //else if (sresult.Contains("048BatagaevVA") || sresult.Contains("048MikhalevaNV") || sresult.Contains("048KHankhasaevaIS") || sresult.Contains("048ButuevaLS") || sresult.Contains("048KhunkhinovaVA"))
            //{
            //    return 106;
            //}

            if (sresult.Length < 7) return -1;
            bool BAO = sresult[4] != '0' && sresult[2] != '0';
            sresult = sresult.Substring(2, sresult.Length - 2);
            sresult = sresult.Substring(0, sresult.Length - (BAO?3:4));
            if (int.TryParse(sresult, out pos)) return pos;
            return -1;
        }

        public static bool CompareExists(FTP f, string ftp_path_name, string os_path_name)
        {
            if (!File.Exists(os_path_name)) return false;
            
            FileInfo fi = new FileInfo(os_path_name);
            long fs = fi.Length;
            return fs == f.FileSize(ftp_path_name);
        }
    }
}
