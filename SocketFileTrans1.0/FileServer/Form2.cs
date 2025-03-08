using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Net.Sockets;
using System.Net;
using System.Threading;
using System.IO;

namespace FileServer
{
    public partial class Form2 : Form
    {
        List<clientInfo> curclients = new List<clientInfo>();
        List<Socket> cursockets = new List<Socket>();

        List<string> filemd5s = new List<string>();

        string filePath;

        Socket Listensocket;
        Thread listenThread;
        bool stop = false;

        public Form2()
        {
            InitializeComponent();
        }

        //开始监听
        private void btnListen_Click(object sender, EventArgs e)
        {
            if (txtMins.Text.Trim() == "" || !int.TryParse(txtMins.Text,out swapData.fileFilterMin))
            {
                MessageBox.Show("时间限制设置错误！");
                return;
            }

            if (cboIP.Text == "" || txtPort.Text == "")
            {
                MessageBox.Show("开启失败，请检查IP及端口设置！");
                return;
            }

            if (!string.IsNullOrEmpty(txtPath.Text))
            {
                filePath = txtPath.Text;
                swapData.filePath = filePath;
                //服务端节点    
                IPEndPoint ipep = new IPEndPoint(IPAddress.Parse(cboIP.Text), int.Parse(txtPort.Text));

                //创建套接字         
                Listensocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

                //绑定IP地址和端口到套接字 
                try
                {
                    Listensocket.Bind(ipep);
                }
                catch
                {
                    MessageBox.Show("开启失败，请稍后再试！");
                    return;
                }

                stop = false;

                //在一个单独的线程中监听客户连接       
                listenThread = new Thread(startlistenClientConnnect);
                listenThread.Start();

                //监视文件变化
                MonitorDirectory(filePath, "*.*");
                AddMsg(string.Format("[{0}] [INFO]:文件夹监视启动", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")));

                btnListen.Enabled = false;
                btnStop.Enabled = true;
            }
            else
            {
                MessageBox.Show(this, "请选择分发文件夹后再开启监听！", "提示", MessageBoxButtons.OKCancel, MessageBoxIcon.Warning);
            }
        }

        private void startlistenClientConnnect()
        {
            try
            {
                Listensocket.Listen(10);
                //Console.WriteLine("TCP监听启动！");
                AddMsg(string.Format("[{0}] [INFO]:监听连接已启动", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")));
                while (!stop)
                {
                    try
                    {
                        Socket sokConnection = Listensocket.Accept();
                        if (sokConnection != null)
                        {
                            //获取一个连接，然后处理该连接
                            ParameterizedThreadStart threadStart = new ParameterizedThreadStart(clientThread);
                            Thread thr = new Thread(threadStart);
                            thr.Start(sokConnection);

                            cursockets.Add(sokConnection);
                        }
                    }
                    catch (Exception er)
                    {
                        Log.WriteLine("获取socket连接出错！-----" + er.Message);
                        break;
                    }
                }
                Log.WriteLine("监听停止！");
            }
            catch (Exception er)
            {
                Log.WriteLine("启动监听时报错----" + er.Message);
                AddMsg(string.Format("[{0}] [INFO]:监听已停止", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")));
            }
        }

        private byte[] ReceiveStrToByte(Socket s)
        {
            try
            {
                byte[] dataninfo = new byte[6000];
                int revByteNum = s.Receive(dataninfo, 0, dataninfo.Length, SocketFlags.None);
                byte[] retByte = new byte[revByteNum];
                for (int i = 0; i < revByteNum; i++)
                    retByte[i] = dataninfo[i];
                return retByte;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        private void clientThread(object socket)
        {
            byte[] bytes = new byte[10240];
            Socket CommunicationSocket = (Socket)socket;
            CommunicationSocket.ReceiveTimeout = 100000;
            clientInfo ci = new clientInfo();
            ci.ClientSocket = CommunicationSocket;
            ci.ConnectStatus = 1;

            bool issendfileing = false;
            bool clientstop = false;

            myFileInfo curfile = null;
            string filename = "";

            IPEndPoint clientIP = (IPEndPoint)CommunicationSocket.RemoteEndPoint;
            //Console.WriteLine("客户端" + clientIP + "已建立连接！");

            while (!clientstop)
            {
                try
                {
                    if (ci.FileList.Count > 0 && issendfileing == false)
                    {
                        curfile = ci.FileList.Dequeue();
                        filename = curfile.fileinfo.Name;
                    }
                    if (ci.ClientName != "" && ci.ConnectStatus == 1 && curfile != null && issendfileing == false)
                    {
                        byte[] datainfo = System.Text.Encoding.UTF8.GetBytes("*hq,data," + curfile.fileinfo.Name + "," + curfile.fileinfo.Length + "," + curfile.md5 + "#");
                        CommunicationSocket.Send(datainfo);
                        issendfileing = true;
                    }

                    string datacontent = System.Text.Encoding.UTF8.GetString(ReceiveStrToByte(CommunicationSocket)).Split('\0')[0];
                    if (datacontent == "")
                    {
                        break;
                    }
                    if (datacontent == "**##")
                    {
                        ci.ConnectStatus = 1;
                        ci.LastTime = DateTime.Now;
                    }

                    datacontent = datacontent.Replace("**##", "");
                    //注册包,*hq,zc,clientname,machineno#
                    //注册包,*hq,zc,clientname,machineno,ok#
                    //心跳包,*hq,xt,machineno#
                    //数据信息包,*hq,data,filename,filesize，md5#
                    //数据信息确认包,*hq,data,filename,filesize，md5,ok#
                    //数据包，字节流直发
                    //数据确认,*hq,datasure,md5#    *hq,datafail,md5#
                    //连接关闭,*hq,close,machineno# 

                    if (datacontent.StartsWith("*") && datacontent.EndsWith("#") && !datacontent.Contains("#*"))
                    {
                        string[] datas = datacontent.Substring(1).Substring(0, datacontent.Length - 2).Split(',');
                        switch (datas[1])
                        {
                            case "zc":
                                ci.ClientName = datas[2];
                                ci.MachineNo = datas[3];
                                ci.LastTime = DateTime.Now;
                                ci.ClientIP = clientIP;

                                AddMsg(string.Format("[{0}] [INFO]:客户端 {1}（" + clientIP + "） 已建立连接", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), ci.ClientName));

                                byte[] datainfo = System.Text.Encoding.UTF8.GetBytes("*hq,zc," + ci.ClientName + "," + ci.MachineNo + ",ok#");
                                CommunicationSocket.Send(datainfo);
                                foreach (clientInfo cc in curclients)
                                {
                                    if (cc.MachineNo == ci.MachineNo)
                                    {
                                        curclients.Remove(cc);
                                        break;
                                    }
                                }
                                ci.initFileList();
                                curclients.Add(ci);
                                Thread.Sleep(1000);
                                break;
                            case "datasure":
                                issendfileing = false;
                                ci.SendfileMD5.Add(datas[2]);
                                ci.LastTime = DateTime.Now;
                                AddMsg(string.Format("[{0}] [INFO]:向 {1}（" + clientIP + "） 发送 文件 {2} 已完成。", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), ci.ClientName, filename));
                                curfile = null;
                                break;
                            case "datafail":
                                issendfileing = false;
                                ci.FileList.Enqueue(curfile);
                                ci.LastTime = DateTime.Now;
                                AddMsg(string.Format("[{0}] [INFO]:向 {1}（" + clientIP + "） 发送 文件 {2} 失败，等待重试。", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), ci.ClientName, filename));
                                break;
                            case "close":
                                curclients.Remove(ci);
                                clientstop = true;
                                break;
                            case "data"://开始发送文件
                                ci.ConnectStatus = 1;
                                ci.LastTime = DateTime.Now;
                                {
                                    //创建一个文件对象     
                                    FileInfo fileinfo = curfile.fileinfo;

                                    //打开文件流            
                                    FileStream filestream = fileinfo.OpenRead();

                                    //文件分块传输，分块的大小,单位为字节         
                                    int PacketSize = 5000;
                                    //分块的数量               
                                    int PacketCount = (int)(fileinfo.Length / ((long)PacketSize));
                                    //最后一个分块的大小           
                                    int LastPacketSize = (int)(fileinfo.Length - ((long)(PacketSize * PacketCount)));
                                    //文件按数据包的形式发送，定义数据包的大小      
                                    byte[] data = new byte[PacketSize];
                                    //开始循环发送数据包           
                                    for (int i = 0; i < PacketCount; i++)
                                    {
                                        //从文件流读取数据并填充数据包  
                                        filestream.Read(data, 0, data.Length);
                                        //发送数据包                    
                                        CommunicationSocket.Send(data, 0, data.Length, SocketFlags.None);
                                        Thread.Sleep(100);
                                    }
                                    //发送最后一个数据包   
                                    if (LastPacketSize != 0)
                                    {
                                        data = new byte[LastPacketSize];
                                        filestream.Read(data, 0, data.Length);
                                        //发送数据包             
                                        CommunicationSocket.Send(data, 0, data.Length, SocketFlags.None);
                                        Thread.Sleep(100);
                                    }
                                    filestream.Close();
                                    curfile = null;
                                }
                                break;
                        }
                    }
                }
                catch
                {
                    clientstop = true;
                    //Console.WriteLine("客户端" + clientIP + "断开连接！");
                    AddMsg(string.Format("[{0}] [INFO]:客户端 {1}（" + clientIP + "） 断开连接", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), ci.ClientName));
                    break;
                }
                Thread.Sleep(1);
            }
            try
            {
                //Console.WriteLine("客户端" + clientIP + "断开连接！");
                AddMsg(string.Format("[{0}] [INFO]:客户端 {1}（" + clientIP + "） 断开连接", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), ci.ClientName));
                curclients.Remove(ci);
                cursockets.Remove(CommunicationSocket);
                CommunicationSocket.Shutdown(SocketShutdown.Both);
                CommunicationSocket.Close();
            }
            catch
            {

            }
        }

        private void MonitorDirectory(string path, string filter)
        {
            FileSystemWatcher fileSystemWatcher = new FileSystemWatcher();
            fileSystemWatcher.Path = path;
            fileSystemWatcher.NotifyFilter = NotifyFilters.LastAccess
                                           | NotifyFilters.LastWrite
                                           | NotifyFilters.FileName
                                           | NotifyFilters.DirectoryName;
            //文件类型，支持通配符，“*.txt”只监视文本文件
            fileSystemWatcher.Filter = filter;    // 监控的文件格式
            fileSystemWatcher.IncludeSubdirectories = true;  // 监控子目录
            fileSystemWatcher.Changed += new FileSystemEventHandler(OnProcess);
            fileSystemWatcher.Created += new FileSystemEventHandler(OnProcess);
            fileSystemWatcher.Renamed += new RenamedEventHandler(OnProcess);
            fileSystemWatcher.Deleted += new FileSystemEventHandler(OnProcess);
            //表示当前的路径正式开始被监控，一旦监控的路径出现变更，FileSystemWatcher 中的指定事件将会被触发。
            fileSystemWatcher.EnableRaisingEvents = true;
        }

        private void OnProcess(object source, FileSystemEventArgs e)
        {
            if (e.ChangeType == WatcherChangeTypes.Created)
            {
                OnCreated(source, e);
            }
            else if (e.ChangeType == WatcherChangeTypes.Changed)
            {
                OnChanged(source, e);
            }
            else if (e.ChangeType == WatcherChangeTypes.Deleted)
            {
                OnDeleted(source, e);
            }
            else if (e.ChangeType == WatcherChangeTypes.Renamed)
            {
                OnRenamed(source, e);
            }
        }

        private void OnCreated(object source, FileSystemEventArgs e)
        {
            Log.WriteLine(string.Format("File created: [{0}]  {1} {2}", e.ChangeType, e.FullPath, e.Name));
            //{
            //    myFileInfo mfi = new myFileInfo(e.FullPath);
            //    mfi.initMD5();
            //    if (mfi.md5 != "")
            //    {
            //        foreach (clientInfo ci in curclients)
            //        {
            //            if (!ci.FileMD5.Contains(mfi.md5) && mfi.fileinfo.CreationTime.AddMinutes(swapData.fileFilterMin) > System.DateTime.Now)
            //            {
            //                ci.FileList.Enqueue(mfi);
            //                ci.FileMD5.Add(mfi.md5);
            //            }
            //        }
            //    }

            //}
        }

        private void OnChanged(object source, FileSystemEventArgs e)
        {
            Log.WriteLine(string.Format("File changed: [{0}]  {1} {2}", e.ChangeType, e.FullPath, e.Name));
            {
                myFileInfo mfi = new myFileInfo(e.FullPath);
                mfi.initMD5();
                if (mfi.md5 != "")
                {
                    foreach (clientInfo ci in curclients)
                    {
                        if (!ci.FileMD5.Contains(mfi.md5) && mfi.fileinfo.CreationTime.AddMinutes(swapData.fileFilterMin) > System.DateTime.Now)
                        {
                            ci.FileList.Enqueue(mfi);
                            ci.FileMD5.Add(mfi.md5);
                            AddMsg(string.Format("[{0}] [INFO]:向 {1}（" + ci.ClientIP + "） 添加待发送 文件 {2} 。", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), ci.ClientName, mfi.fileinfo.Name));
                        }
                    }
                }

            }
        }

        private void OnDeleted(object source, FileSystemEventArgs e)
        {
            Console.WriteLine("File deleted: [{0}]  {1} {2}", e.ChangeType, e.FullPath, e.Name);
        }

        private void OnRenamed(object source, FileSystemEventArgs e)
        {
            Console.WriteLine("File renamed: [{0}]  {1} {2}", e.ChangeType, e.FullPath, e.Name);
        }

        //关闭
        private void Form2_FormClosed(object sender, FormClosedEventArgs e)
        {
            stop = true;
            dataGridView1.Rows.Clear();
            if (Listensocket != null)
            {
                foreach (Socket soc in cursockets)
                {
                    try
                    {
                        soc.Shutdown(SocketShutdown.Both);
                    }
                    catch
                    {

                    }
                }
                cursockets.Clear();
                Listensocket.Close();
                Listensocket.Dispose();
            }
            if (listenThread != null)
            {
                listenThread.Abort();
                listenThread = null;
            }
            btnListen.Enabled = true;
            btnStop.Enabled = false;
        }

        //停止监听
        private void btnStop_Click(object sender, EventArgs e)
        {
            stop = true;
            dataGridView1.Rows.Clear();
            if (Listensocket != null)
            {
                foreach (Socket soc in cursockets)
                {
                    try
                    {
                        soc.Shutdown(SocketShutdown.Both);
                    }
                    catch
                    {

                    }
                }
                cursockets.Clear();
                Listensocket.Close();
            }
            if (listenThread.IsAlive)
            {
                listenThread.Abort();
            }
            btnListen.Enabled = true;
            btnStop.Enabled = false;
        }

        //加载
        private void Form2_Load(object sender, EventArgs e)
        {
            btnStop.Enabled = false;
            btnListen.Enabled = true;

            IPAddress[] ip = Dns.GetHostAddresses(Dns.GetHostName());
            foreach (IPAddress address in ip)
            {
                if (address.AddressFamily == AddressFamily.InterNetwork)
                {
                    cboIP.Items.Add(address.ToString());
                }
            }
            cboIP.SelectedIndex = 0;
            //初始化
            txtPort.Text = (System.Configuration.ConfigurationManager.AppSettings["serverPort"].ToString());
            cboIP.Text = (System.Configuration.ConfigurationManager.AppSettings["serverIP"].ToString());
            txtMins.Text = (System.Configuration.ConfigurationManager.AppSettings["filterMin"].ToString());
            txtPath.Text = (System.Configuration.ConfigurationManager.AppSettings["filePath"].ToString());


            timer1.Start();
        }

        //停止监听
        private void stopListen()
        {
            if (btnStop.InvokeRequired)
            {
                Action stopAction = () => { stopListen(); };
                btnStop.Invoke(stopAction);
            }
            else
            {
                btnStop.Enabled = true;
            }
        }


        //选择文件夹
        private void llSelectPath_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            if (this.folderBrowserDialog1.ShowDialog() == DialogResult.OK)
            {
                filePath = folderBrowserDialog1.SelectedPath;
                txtPath.Text = filePath;
            }
        }

        List<object[]> objs = new List<object[]>();
        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            for (int i = 0; i < curclients.Count; i++)
            {
                clientInfo ci = curclients[i];
                if (ci.LastTime.AddMinutes(5) < DateTime.Now)
                {
                    curclients.Remove(ci);
                }
            }
            int count = 1;
            objs = new List<object[]>();
            foreach (clientInfo ci in curclients)
            {
                objs.Add(new object[] { 0, count, ci.ClientName, ci.ClientIP, ci.SendfileMD5.Count, ci.FileList.Count, ci.MachineNo });
                count++;
            }

            backgroundWorker1.ReportProgress(0);
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            if (!backgroundWorker1.IsBusy)
            {
                backgroundWorker1.RunWorkerAsync();
            }
            toolStripStatusLabel1.Text ="系统时间："+ System.DateTime.Now.ToString("G");
        }

        private void backgroundWorker1_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            dataGridView1.Rows.Clear();
            foreach (object[] obj in objs)
            {
                dataGridView1.Rows.Add(obj);
            }
        }

        //向listBox中增加信息
        private void AddMsg(string msgStr)
        {
            Log.WriteLine(msgStr);
            if (listBox1.InvokeRequired)
            {
                Action<string> myAction = (p) => { AddMsg(p); };
                this.listBox1.Invoke(myAction, msgStr);
            }
            else
            {
                if (checkBox1.Checked)
                {
                    while (listBox1.Items.Count > 100)
                    {
                        listBox1.Items.RemoveAt(0);	// 删除最早的数据
                    }
                }

                this.listBox1.Items.Add(msgStr);

                int visibleItems = listBox1.ClientSize.Height / listBox1.ItemHeight;
                listBox1.TopIndex = Math.Max(listBox1.Items.Count - visibleItems + 1, 0);

            }
        }

        private void linkLabel2_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            listBox1.Items.Clear();
        }

        private void label4_Click(object sender, EventArgs e)
        {

        }

        private void Form2_FormClosing(object sender, FormClosingEventArgs e)
        {
            //保存配置
            fileHelper.SetValue("serverIP", cboIP.Text);
            fileHelper.SetValue("serverPort", txtPort.Text);
            fileHelper.SetValue("filePath", txtPath.Text);
            fileHelper.SetValue("filterMin", txtMins.Text);
        }
    }
}
