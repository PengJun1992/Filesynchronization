using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace FileServer
{
    class myFileInfo
    {
        public FileInfo fileinfo;
        public string md5 = string.Empty;

        public myFileInfo(string fullpath)
        {
            fileinfo = new FileInfo(fullpath);
        }

        public void initMD5()
        {
            try
            {
                md5 = fileHelper.GetMD5HashFromFile(fileinfo.OpenRead());
            }
            catch
            {
                Log.WriteLine(fileinfo.FullName + " MD5 Read Fail!" );
            }
        }
    }
}
