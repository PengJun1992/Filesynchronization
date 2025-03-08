using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Collections;
using System.Net;
using System.IO;

namespace FileServer
{
    //注册包,*hq,zc,clientname,machineno#
    //心跳包,*hq,xt,machineno,#
    //数据信息包,*hq,data,filename,filesize，md5#
    //数据包，字节流直发
    //数据确认,*hq,datasure,md5,ok#    *hq,datasure,md5,fail#

    class clientInfo
    {
        private string _clientName;

        public string ClientName
        {
            get { return _clientName; }
            set { _clientName = value; }
        }

        private string _machineNo;

        public string MachineNo
        {
            get { return _machineNo; }
            set { _machineNo = value; }
        }

        private Socket _clientSocket;

        public Socket ClientSocket
        {
            get { return _clientSocket; }
            set { _clientSocket = value; }
        }

        private int _sendFileCount;

        public int SendFileCount
        {
            get { return _sendFileCount; }
            set { _sendFileCount = value; }
        }

        private int _sendFileSucessCount;

        public int SendFileSucessCount
        {
            get { return _sendFileSucessCount; }
            set { _sendFileSucessCount = value; }
        }

        private int _sendFileFailCount;

        public int SendFileFailCount
        {
            get { return _sendFileFailCount; }
            set { _sendFileFailCount = value; }
        }

        private int _waitSendFileCount;

        public int WaitSendFileCount
        {
            get { return _waitSendFileCount; }
            set { _waitSendFileCount = value; }
        }

        private int _connectStatus;
        /// <summary>
        /// 连接状态（0：断开连接，1：连接正常）
        /// </summary>
        public int ConnectStatus
        {
            get { return _connectStatus; }
            set { _connectStatus = value; }
        }

        private DateTime _lastTime;

        public DateTime LastTime
        {
            get { return _lastTime; }
            set { _lastTime = value; }
        }

        private Queue<myFileInfo> _fileList = new Queue<myFileInfo>();

        public Queue<myFileInfo> FileList
        {
            get { return _fileList; }
            set { _fileList = value; }
        }

        private IPEndPoint _clientIP;

        public IPEndPoint ClientIP
        {
            get { return _clientIP; }
            set { _clientIP = value; }
        }

        private List<string> _fileMD5 = new List<string>();

        public List<string> FileMD5
        {
            get { return _fileMD5; }
            set { _fileMD5 = value; }
        }

        private List<string> _sendfileMD5 = new List<string>();

        public List<string> SendfileMD5
        {
            get { return _sendfileMD5; }
            set { _sendfileMD5 = value; }
        }

        public void initFileList()
        {
            try
            {
                DirectoryInfo root = new DirectoryInfo(swapData.filePath);
                FileInfo[] files = root.GetFiles();
                foreach (FileInfo fi in files)
                {
                    myFileInfo mfi = new myFileInfo(fi.FullName);
                    mfi.initMD5();
                    if (!_fileMD5.Contains(mfi.md5) && mfi.fileinfo.CreationTime.AddMinutes(swapData.fileFilterMin) > System.DateTime.Now)
                    {
                        _fileList.Enqueue(mfi);
                        _fileMD5.Add(mfi.md5);
                    }
                }
                Log.WriteLine(FileList.Count.ToString());
            }
            catch (System.Exception ex)
            {
                Log.WriteLine(ex.Message);
            }
            
        }
    }
}
