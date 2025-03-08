using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Threading;
using System.Data;
using System.Windows.Forms;

namespace FileClient
{
    public class Log
    {
        public static bool _FILE_ = true;
        public static bool _STDOUT_ = false;
        public static string exeSql = "";
        public static string sqlFileName = "";
        public static string fileName = "FileClient";
        private static string rootPath;
        public const string ArrowUP = "▲";//△▲↑
        public const string ArrowDown = "▼";//▼↓
        public const int PageSize = 10;
        public const string DATAFORMAT = "yyyy-MM-dd HH:mm";
        public static string userName = "";

        public static void init(string filename)
        {
            fileName = filename;
        }

        public static int GetIndex(ComboBox ddlBox, object code)
        {
            int ret = 0;
            if (code == null)
                return ret;
            for (int i = 0; i < ddlBox.Items.Count; i++)
            {
                if (code.ToString() == ((DataRowView)ddlBox.Items[i]).Row[0].ToString())
                {
                    ret = i;
                    break;
                }
            }
            return ret;
        }

        public static string WriteLine(string log)
        {
            string str = "[" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "] [INFO] " + log;
            if (_FILE_)
            {
                WriteToFile(str);
            }
            if (_STDOUT_)
            {

                Console.WriteLine(str);
            }
            return str;
        }

        public static void WriteLine()
        {
            string str = "[" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "] [INFO] " + exeSql;
            if (_FILE_)
            {
                try
                {
                    bool createdNew;
                    Mutex m = new Mutex(false, "log_lock", out createdNew);
                    if (createdNew)
                    {
                        WriteToFile(str, sqlFileName);
                        m.ReleaseMutex();
                    }
                }
                catch (Exception er)
                {

                }
            }
            if (_STDOUT_)
            {

                Console.WriteLine(str);
            }
            return;
        }

        public static string WriteLine(string log, string file_name)
        {
            string str = "[" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "] [INFO] " + log;
            if (_FILE_)
            {

                WriteToFile(str, file_name);
            }
            if (_STDOUT_)
            {

                Console.WriteLine(str);
            }
            return str;
        }

        public static string WriteLine(Exception e)
        {
            string str = "[" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "] [ERROR] " + e.Message + ":\n" + e.StackTrace;
            if (_FILE_)
            {
                //WriteToFile(str);
                WriteToErrorFile(str);

            }
            if (_STDOUT_)
            {
                Console.WriteLine(str);
            }
            return str;
        }

        public static string WriteLine(string log, Exception e)
        {
            string str = "[" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "] [ERROR] " + log + ":" + e.Message + ":\n" + e.StackTrace;
            if (_FILE_)
            {
                //WriteToFile(str);
                WriteToErrorFile(str);

            }
            if (_STDOUT_)
            {
                Console.WriteLine(str);
            }
            return str;
        }


        private static void WriteToFile(string log)
        {
            try
            {
                //				bool createdNew;
                string fName = GetRootPath() + fileName + ".log";
                //				Mutex m = new Mutex(false,fName,out createdNew);
                //				if( createdNew )
                //				{
                StreamWriter writer = null;
                //if(!isServer)
                writer = File.AppendText(fName);
                //				else
                //					writer = File.AppendText(fileName+".log");
                //new StreamWriter("UpLoadDataServer.log", true,
                //System.Text.UnicodeEncoding.GetEncoding("GB2312"));
                writer.WriteLine(log);
                writer.Close();

                FileInfo fi = null;
                //if(!isServer)
                fi = new FileInfo(fName);
                //				else
                //					fi = new FileInfo(fileName+".log");

                if (fi.Length > 500000)
                {
                    BackupLog();
                }
                ////					m.ReleaseMutex();
                //				}
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        /// <summary>
        /// SQL语句写入文件
        /// </summary>
        /// <param name="log"></param>
        /// <param name="file_name"></param>
        private static void WriteToFile(string log, string file_name)
        {
            try
            {
                //				bool createNew;
                string fName = GetRootPath() + file_name + ".log";
                //
                //				Mutex m = new Mutex( false,fName,out createNew );
                //				if( createNew)
                //				{
                StreamWriter writer = null;

                writer = File.AppendText(fName);

                writer.WriteLine(log);
                writer.Close();

                FileInfo fi = null;
                //if(!isServer)
                fi = new FileInfo(fName);

                if (fi.Length > 500000)
                {
                    BackupLog(file_name);
                }
                //					m.ReleaseMutex();
                //				}
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }


        public static void BackupLog()
        {
            try
            {
                if (!System.IO.Directory.Exists(GetRootPath() + "logbak"))
                {
                    System.IO.Directory.CreateDirectory(GetRootPath() + "logbak");
                }
                System.IO.File.Move(GetRootPath() + fileName + ".log",
                    GetRootPath() + "logbak\\" + fileName + DateTime.Now.ToString("yyyyMMddHHmmss") + ".log");
            }
            catch
            {
                ;
            }
        }

        public static void BackupLog(string file_name)
        {
            try
            {
                if (!System.IO.Directory.Exists(GetRootPath() + "logbak"))
                {
                    System.IO.Directory.CreateDirectory(GetRootPath() + "logbak");
                }
                System.IO.File.Move(GetRootPath() + file_name + ".log",
                    GetRootPath() + "logbak\\" + file_name + DateTime.Now.ToString("yyyyMMddHHmmss") + ".log");
            }
            catch
            {
                ;
            }
        }

        private static void WriteToErrorFile(string log)
        {
            try
            {
                //				bool createdNew;
                string fName = GetRootPath() + fileName + "_error.log";
                //				Mutex m = new Mutex(false,fName,out createdNew );
                //				if( createdNew )
                //				{
                StreamWriter writer = null;
                writer = File.AppendText(fName);

                writer.WriteLine(log);
                writer.Close();

                FileInfo fi = null;

                fi = new FileInfo(fName);
                if (fi.Length > 500000)
                {
                    BackupErrorLog();
                }
                //					m.ReleaseMutex();
                //				}
            }
            catch
            {
            }
        }

        public static void BackupErrorLog()
        {
            try
            {
                if (!System.IO.Directory.Exists(GetRootPath() + "logbak"))
                {
                    System.IO.Directory.CreateDirectory(GetRootPath() + "logbak");
                }
                System.IO.File.Move(GetRootPath() + fileName + "_error.log",
                    GetRootPath() + "logbak\\" + fileName + DateTime.Now.ToString("yyyyMMddHHmmss") + "_error.log");
            }
            catch
            { }
        }

        public static string GetRootPath()
        {
            if (rootPath == null)
            {
                FileInfo fln = new FileInfo(@".");
                rootPath = fln.FullName;
                int index = rootPath.ToLower().IndexOf("bin\\debug");
                if (index != -1)
                    rootPath = rootPath.Substring(0, index);

                //int index = rootPath.ToLower().IndexOf("preventfloodapp");
                //rootPath = rootPath.Substring(0,index+16);
                rootPath += "\\";
            }
            return rootPath;

        }

        public static void SetRootPath(string path)
        {
            rootPath = path;
        }

    }

}
