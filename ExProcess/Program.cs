using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

//using System.Net;

namespace ExProcess
{
    class Program
    {


        protected static FTP ftp;
        protected static StreamWriter LOG;


        static void Main(string[] args)
        {
            
#region Get vars
            NameValueCollection appSettings = ConfigurationManager.AppSettings;
            
            string SourcePathA = appSettings.Get("SourcePathA");
            string TargetPathA = appSettings.Get("TargetPathA");
            string SourcePathR = appSettings.Get("SourcePathR");
            string TargetPathR = appSettings.Get("TargetPathR");
            string ClearPath = appSettings.Get("ClearPath");

            string SourcePathA2 = appSettings.Get("SourcePathA2");
            string TargetPathA2 = appSettings.Get("TargetPathA2");
            string SourcePathR2 = appSettings.Get("SourcePathR2");
            string TargetPathR2 = appSettings.Get("TargetPathR2");
            string ClearPath2 = appSettings.Get("ClearPath2");

            string SourcePathAREP = appSettings.Get("SourcePathREP");//c:\spures\result\success\048
            string TargetPathAREP = appSettings.Get("TargetPathREP");//REPORT
            //string ClearPathREP   = appSettings.Get("ClearPathREP");
            //string SourcePathRREP = "X";
            //string TargetPathRREP = "X";
            


            string _Host = appSettings.Get("FTP_Host");
            string _User = appSettings.Get("FTP_User");
            string _Password = appSettings.Get("FTP_Password");
            string _LogRotateMB = appSettings.Get("LOG_Rotate_MB");

            #endregion

            Tools.prepareUsers();
            //if (1 == 1) return;

            long LOGMB;

            if (_LogRotateMB==null || !long.TryParse(_LogRotateMB, out LOGMB)|| LOGMB>100) LOGMB=1;
            string logfile = AppDomain.CurrentDomain.BaseDirectory + "\\ExProcess.log";
            FileInfo FI = new FileInfo(logfile);
            if (FI.Length > 1024*1024*LOGMB) FI.MoveTo(AppDomain.CurrentDomain.BaseDirectory + "\\" + DateTime.Now.ToShortDateString() +"_ExProcess.log");

            FI = new FileInfo(logfile);
            if (!FI.Exists) FI.Create();


            LOG=new StreamWriter(logfile, true , Encoding.UTF8);

            if (_Host == null || _Host == "" )
            {
                LOG.WriteLine(DateTime.Now + " Host не задан (FTP_Host)");
                Console.WriteLine("Host не задан (FTP_Host)");
                goto end;
            }

            if (_User == null || _User == "")
            {
                LOG.WriteLine(DateTime.Now + " User не задан (FTP_User)");
                Console.WriteLine("User не задан (FTP_User)");
                goto end;
            }

            if (_Password == null || _Password == "")
            {
                LOG.WriteLine(DateTime.Now + " User не задан (FTP_Password)");
                Console.WriteLine("User не задан (FTP_Password)");
                goto end;
            }

            ftp = Connect2FTP(_User, _Password, _Host);
            if (ftp==null)
            {
                goto end;
            }

           if (!checkPhase(SourcePathA, TargetPathA, SourcePathR, TargetPathR, ClearPath, 1))
            {
                goto end;
            }
            processPhase(SourcePathA, TargetPathA, SourcePathR, TargetPathR,1);
            clearPhase(ClearPath+ "\\SUCCESS\\48", 1);
            clearPhase(ClearPath + "\\FAILURE\\48", 1);

            //работаем с C:\SPURES\RESULT\SUCCESS\48\%NR%\REPORT
            if (!checkPhase(SourcePathAREP, TargetPathAREP, "X", "X", ClearPath, 3))
            {
                goto end;
            }
            processPhase(SourcePathAREP, TargetPathAREP, "X", "X", 3);
            //clearPhase(ClearPath + "\\SUCCESS\\48", 3);


            //работаем с C:\SPUSOCD\RESULT\SUCCESS\48\% NR %
            
            if (!checkPhase(SourcePathA2, TargetPathA2, SourcePathR2, TargetPathR2, ClearPath2, 2))
            {
                goto end;
            }
            processPhase(SourcePathA2, TargetPathA2, SourcePathR2, TargetPathR2,1);
            clearPhase(ClearPath2 + "\\SUCCESS\\48", 2);

            //работаем с C:\SPUSOCD\RESULT\FAILURE\48\% NR %
            clearPhase(ClearPath2 + "\\FAILURE\\48", 2);

            
        end:

            LOG.Close();
        }

        private static void processPhase(string As, string At, string Rs, string Rt, int phase )
        {
            List<string> flist, list;
            string[] fstr;
            string[] dstr;
            int raion=0;
            string filename;
            string dir;
            int pos;
            try
            {
                list = ftp.ListDirectory("/");
                bool ok;
                
                //from c:\SPURES\RESULT\SUCCESS\48\%N_RAION%\REPORT to ftp://%N_RAION%/REPORT
                if (phase==3)
                {
                    foreach (string d in list)
                    {
                        if (!int.TryParse(d, out raion)) continue;


                        dir = As + "\\" + raion.ToString() + "\\REPORT";

                        if (!Directory.Exists(dir))
                        {
                            LOG.WriteLine(DateTime.Now + " Phase " + phase.ToString() + " directory " + dir + " not exists");
                            continue;
                        }

                        fstr = Directory.GetFiles(dir);

                        foreach (string fl in fstr)
                        {

                            pos = fl.LastIndexOf('\\');
                            if (pos < 0 || !fl.Contains(".csv")) continue;
                            filename = fl.Substring(pos + 1, fl.Length - pos - 1);

                            ftp.UploadFile(raion.ToString() + "/" + At, fl.Substring(0,pos),filename);
                           
                            if (Tools.CompareExists(ftp, raion.ToString() + "/" + At + "/" + filename, fl))
                            {
                                File.Delete(fl);
                                LOG.WriteLine(DateTime.Now + " Phase " + phase.ToString() + " R=" + raion.ToString() + " File=" + filename + " отдан в обработку");
                            }
                            else
                            {
                                ok = false;
                                LOG.WriteLine(DateTime.Now + " Phase " + phase.ToString() + " R=" + raion.ToString() + " File=" + filename + " ошибка копирования на FTP");
                            }
                        }
                    }
                    return;
                }


                //from c:\SPUROOT\%USERNAME%\*.* to ftp://%N_RAION%/SPUROOT/
                foreach (string d in Directory.GetDirectories(As))
                {
                    raion = Tools.NR(d);
                    if (raion < 0) continue;
                    if (!list.Contains(raion.ToString()))
                    {
                        LOG.WriteLine(DateTime.Now  + " Phase " + phase.ToString() + " " + raion.ToString() + "/" + At + " не существует на ФТП");
                        continue;
                    }
                    dstr = Directory.GetDirectories(d);
                    foreach (string d1 in dstr)
                    {
                        ok = true;
                        fstr = Directory.GetFiles(d1);
                        foreach (string fl in fstr)
                        {
                            pos = fl.LastIndexOf('\\');
                            if (pos < 0) continue;
                            filename = fl.Substring(pos + 1, fl.Length - pos - 1);
                            ftp.UploadFile(raion.ToString() + "/" + At, d1, filename);
                            if (Tools.CompareExists(ftp, raion.ToString() + "/" + At + "/" + filename, fl))
                            {
                                File.Delete(fl);
                                LOG.WriteLine(DateTime.Now + " Phase "+phase.ToString()+" R=" + raion.ToString() + " File=" + filename + " отдан в обработку");
                            }
                            else
                            {
                                ok = false;
                                LOG.WriteLine(DateTime.Now + " Phase " + phase.ToString() + " R=" + raion.ToString() + " File=" + filename + " ошибка копирования на FTP");
                            }
                        }
                        if (ok) Directory.Delete(d1);
                    }
                }

                //Console.WriteLine("Stage 1 ended");

                //list = ftp.ListDirectory("/");
                //from ftp://%N_RAION%/SPURES
                
                foreach (string d2 in list)
                {
                    if (!int.TryParse(d2, out raion)) continue;
                    flist = ftp.ListFiles(raion.ToString() + "/" + Rs);
                    foreach (string f5 in flist)
                    {
                        if (!Directory.Exists(Rt + "\\" + raion.ToString()))
                            Directory.CreateDirectory(Rt + "\\" + raion.ToString());
                        ftp.DownloadFile(raion.ToString() + "/" + Rs, Rt + "\\" + raion.ToString(), f5);
                        if (Tools.CompareExists(ftp, raion.ToString() + "/" + Rs + "/" + f5, Rt + "\\" + raion.ToString() + "\\" + f5))
                        {
                            ftp.DeleteFile(raion.ToString() + "/" + Rs + "/" + f5);
                            LOG.WriteLine(DateTime.Now + " Phase " + phase.ToString() + " R=" + raion.ToString() + " file=" + f5 + " возвращен с обработки");
                        }
                        else
                        {
                            LOG.WriteLine(DateTime.Now + " Phase " + phase.ToString() + " R=" + raion.ToString() + " file=" + f5 + " ошибка копирования c FTP");
                        }
                    }
                }

                //DateTime CriticalDate = DateTime.Now.AddDays(-30);
                //int i1 = 0, i2 = 0;
                //astr = Directory.GetDirectories(Cp + "\\FAILURE\\48");
                //foreach (string d3 in astr)
                //{
                //    pos = d3.LastIndexOf('\\');
                //    if (pos < 0 || !int.TryParse(d3.Substring(pos + 1, d3.Length - pos - 1), out raion)) continue;
                //    fstr = Directory.GetFiles(d3);
                //    foreach (string f3 in fstr)
                //    {
                //        if (File.GetLastWriteTime(f3) < CriticalDate)
                //        {
                //            File.Delete(f3);
                //            i1++;
                //        }
                //    }
                //}


                //if (i1 > 0) LOG.WriteLine(DateTime.Now + " Phase " + phase.ToString() + " " + i1.ToString() + " устаревших файлов удалено в FAILURE");

                //astr = Directory.GetDirectories(Cp + "\\SUCCESS\\48");
                //foreach (string d4 in astr)
                //{
                //    pos = d4.LastIndexOf('\\');
                //    if (pos < 0 || !int.TryParse(d4.Substring(pos + 1, d4.Length - pos - 1), out raion)) continue;

                //    fstr = Directory.GetFiles(d4);
                //    foreach (string f4 in fstr)
                //    {
                //        if (File.GetLastWriteTime(f4) < CriticalDate)
                //        {
                //            File.Delete(f4);
                //            i2++;
                //        }
                //    }
                //}

                //if (i2 > 0) LOG.WriteLine(DateTime.Now + " Phase " + phase.ToString() + " " + i2.ToString() + " устаревших файлов удалено в SUCCESS");
                //Console.WriteLine("Stage 4 ended");
            }
            catch (Exception E)
            {
                LOG.WriteLine(DateTime.Now + " Phase " + phase.ToString() + " " + "ОШИБКА обработки " + E.Message);
            }
        }

        private static bool clearPhase(string Cp,int phase)
        {
            DateTime CriticalDate = DateTime.Now.AddDays(-30);
            int i1 = 0, pos;
            int raion;
            string[] fstr;
            string cd;
            string[] astr = Directory.GetDirectories(Cp);
            try
            {
                foreach (string d4 in astr)
                {
                    pos = d4.LastIndexOf('\\');
                    if (pos < 0 || !int.TryParse(d4.Substring(pos + 1, d4.Length - pos - 1), out raion)) continue;
                    if (phase==3)
                    {
                        cd=d4 +"\\REPORT";
                    }
                    else
                    {
                        cd = d4;
                    }
                    fstr = Directory.GetFiles(cd);
                    foreach (string f4 in fstr)
                    {
                        if (File.GetLastWriteTime(f4) < CriticalDate)
                        {
                            File.Delete(f4);
                            i1++;
                        }
                    }
                }
            }
            catch(Exception Ex)
            {
                LOG.WriteLine(DateTime.Now + " Phase " + phase.ToString() + " " + "ОШИБКА обработки при очистке " + Cp + " " + Ex.Message);
            }
            if (i1 > 0) LOG.WriteLine(DateTime.Now + " Phase " + phase.ToString() + " " + i1.ToString() + " устаревших файлов удалено в "+ Cp);
            return true;

        }

        private static bool checkPhase(string As, string At, string Rs, string Rt, string Cp, int phase)
        {
            if (As == null || As == "" || !Directory.Exists(As))//DRIVE C
            {
                LOG.WriteLine(DateTime.Now + " Phase="+phase.ToString()+" " + As + " не существует (исходящий источник)");
                Console.WriteLine(As + " Phase=" + phase.ToString() + " не существует (исходящий источник)");
                return false;
            }

            if (At == null || At == "")//FTP
            {
                LOG.WriteLine(DateTime.Now + " Phase=" + phase.ToString() + " " + At + " не существует (исходящий цель)");
                Console.WriteLine(At + " Phase=" + phase.ToString() + " не существует (исходящий цель)");
                return false;
            }

            if (!Rs.Equals("X") && (Rs == null || Rs == ""))//FTP
            {
                LOG.WriteLine(DateTime.Now + " Phase=" + phase.ToString() + " " + Rs + " не существует (входящий источник)");
                Console.WriteLine(Rs + " Phase=" + phase.ToString() + " не существует (входящий источник)");
                return false;
            }

            if (!Rt.Equals("X") && (Rt == null || Rt == "" || !Directory.Exists(Rt)))//DRIVE C
            {
                LOG.WriteLine(DateTime.Now + " Phase=" + phase.ToString() + " " + Rt + " не существует (входящий цель)");
                Console.WriteLine(Rt + " Phase=" + phase.ToString() + " не существует (входящий цель)");
                return false;
            }
            return true;
        }

        private static FTP Connect2FTP(string user, string pass, string host)
        {
            FTP _ftp = null;
            try
            {
                _ftp = new FTP(user,pass,host);
            }
            catch (Exception E)
            {
                LOG.WriteLine(DateTime.Now + " FTP Error " + E.Message);
                Console.WriteLine(E.Message);
                return ftp;
            }
            return _ftp;
        }
    }

    
}
