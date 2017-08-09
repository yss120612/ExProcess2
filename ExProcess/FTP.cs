using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace ExProcess
{
    class FTP
    {
        public string user;
        public string password;
        public string host;

        public FTP(string u, string p,string h)
        { 
            user = u;
            password = p;
            host = h;
        }

        public void CreateDirectory(string path, string folderName)
        {
            FtpWebRequest ftpRequest = (FtpWebRequest)WebRequest.Create("ftp://" + host +"/"+ path +"/"+ folderName);

            ftpRequest.Credentials = new NetworkCredential(user, password);
            ftpRequest.EnableSsl = false;
            ftpRequest.Method = WebRequestMethods.Ftp.MakeDirectory;

            FtpWebResponse ftpResponse = (FtpWebResponse)ftpRequest.GetResponse();
            ftpResponse.Close();
        }

        public void DownloadFile(string path_ftp, string path_to, string fileName)
        {

            FtpWebRequest ftpRequest = (FtpWebRequest)WebRequest.Create("ftp://" + host +"/"+path_ftp+"/" + fileName);
            //Console.WriteLine(ftpRequest.RequestUri);
            ftpRequest.Credentials = new NetworkCredential(user, password);
            //команда фтп RETR
            ftpRequest.Method = WebRequestMethods.Ftp.DownloadFile;

            ftpRequest.EnableSsl = false;
            
            FileStream downloadedFile = new FileStream(path_to+"\\"+fileName, FileMode.Create, FileAccess.ReadWrite);
            
            FtpWebResponse ftpResponse = (FtpWebResponse)ftpRequest.GetResponse();
            //Получаем входящий поток
            Stream responseStream = ftpResponse.GetResponseStream();

            //Буфер для считываемых данных
            byte[] buffer = new byte[1024];
            int size = 0;

            while ((size = responseStream.Read(buffer, 0, 1024)) > 0)
            {
                downloadedFile.Write(buffer, 0, size);

            }
            ftpResponse.Close();
            downloadedFile.Close();
            responseStream.Close();
        }
        
        public void UploadFile(string path_ftp, string path_from,  string shortName)
        {
            //для имени файла
            //string shortName = fileName.Remove(0, fileName.LastIndexOf("\\") + 1);
            string fileName = path_from + "\\"+shortName;
            FileStream uploadedFile = new FileStream(fileName, FileMode.Open, FileAccess.Read);

            FtpWebRequest ftpRequest = (FtpWebRequest)WebRequest.Create("ftp://" + host +"/"+ path_ftp +"/"+ shortName);
            ftpRequest.Credentials = new NetworkCredential(user, password);
            ftpRequest.EnableSsl = false;
            ftpRequest.Method = WebRequestMethods.Ftp.UploadFile;

            //Буфер для загружаемых данных
            byte[] file_to_bytes = new byte[uploadedFile.Length];
            //Считываем данные в буфер
            uploadedFile.Read(file_to_bytes, 0, file_to_bytes.Length);

            uploadedFile.Close();

            //Поток для загрузки файла
            Stream writer = ftpRequest.GetRequestStream();

            writer.Write(file_to_bytes, 0, file_to_bytes.Length);
            writer.Close();
        }


        private List<string> GetList(string path, bool is_dir)
        {
            if (path == null || path == "")
            {
                path = "/";
            }
            //Создаем объект запроса
            FtpWebRequest ftpRequest = (FtpWebRequest)WebRequest.Create("ftp://" + host+ "/" + path);
            //LOG.WriteLine(DateTime.Now + " request=" + ftpRequest.RequestUri);
            //логин и пароль
            ftpRequest.Credentials = new NetworkCredential(user, password);
            //команда фтп LIST
            ftpRequest.Method = WebRequestMethods.Ftp.ListDirectoryDetails;
            

            ftpRequest.EnableSsl = false;
            //Получаем входящий поток
            FtpWebResponse ftpResponse = (FtpWebResponse)ftpRequest.GetResponse();

            //переменная для хранения всей полученной информации
            string content = "";

            StreamReader sr = new StreamReader(ftpResponse.GetResponseStream(), System.Text.Encoding.ASCII);
            content = sr.ReadToEnd();
            sr.Close();
            ftpResponse.Close();
            //content = "Directory of ftp://" + host + "/" + path + '\n' + content;
            List<string> res = new List<string>();
            //return res;
            string s1,s0;
            int ii;
            //Regex Rx = new Regex(@"(?<=\d\d:\d\d\s+).+$");
            Regex Rx = new Regex(@"\s+(\S+)\s?$");
            Match M;
            foreach (string st in content.Split('\n'))
            {
                s1 = st.Trim();
                ii=s1.LastIndexOf(' ');
                //res.Add(st);
                if (s1.Length > 0 && (is_dir && (s1.Trim()[0] == 'd' || s1.Contains("<DIR>")) || !is_dir && s1.Trim()[0] != 'd' && !s1.Contains("<DIR>")))
                {
                    //s0 = st.Substring(ii).Trim();
           //         res.Add(st);
                    M=Rx.Match(st);
                    if (M.Success)
                    {
                        if (M.Value.Trim() != "." && M.Value.Trim() != "..") res.Add(M.Value.Trim());
                    }
                }
            }
            //DirectoryListParser parser = new DirectoryListParser(content);
            return res;
                //parser.FullListing;
        }


        public List<string> ListDirectory(string path)
        {
            return GetList(path, true);
        }

        public List<string> ListFiles(string path)
        {
            return GetList(path, false);
        }


        public long FileSize(string full_file_path)
        {

            try
            {
                //Создаем объект запроса
                FtpWebRequest ftpRequest = (FtpWebRequest)WebRequest.Create("ftp://" + host + "/" + full_file_path);

                //логин и пароль
                ftpRequest.Credentials = new NetworkCredential(user, password);
                //команда фтп LIST
                //ftpRequest.Method = WebRequestMethods.Ftp.ListDirectoryDetails;
                ftpRequest.Method = WebRequestMethods.Ftp.GetFileSize;

                ftpRequest.EnableSsl = false;
                //Получаем входящий поток
                FtpWebResponse ftpResponse = (FtpWebResponse)ftpRequest.GetResponse();

                //переменная для хранения всей полученной информации
                //string content = "";

                //StreamReader sr = new StreamReader(ftpResponse.GetResponseStream(), System.Text.Encoding.ASCII);
                //content = sr.ReadToEnd();
                //content = sr.ReadToEnd();

                //sr.Close();
                //Console.WriteLine("ftp://" + host + "/" + full_file_path);
                //Console.WriteLine(ftpResponse.StatusCode);
                long res = ftpResponse.ContentLength;
                ftpResponse.Close();
                return res;
            }
            catch(Exception E)
            {
                Console.WriteLine(E.Message);
                return -1;
            }
            //if (int.TryParse(content, out res))
            //    return res;
            //return -1;
            
        }

        public void DeleteFile(string path)
        {
            FtpWebRequest ftpRequest = (FtpWebRequest)WebRequest.Create("ftp://" + host +"/"+ path);
            ftpRequest.Credentials = new NetworkCredential(user, password);
            ftpRequest.EnableSsl = false;
            ftpRequest.Method = WebRequestMethods.Ftp.DeleteFile;

            FtpWebResponse ftpResponse = (FtpWebResponse)ftpRequest.GetResponse();
            ftpResponse.Close();
        }


    }
}
