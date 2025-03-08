using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Threading;
using System.Net.Sockets;
using System.Net;

namespace FileClient
{
    public partial class Form1 : Form
    {
        Socket clientsocket;
        bool stop = false;
        string machineNo = "";
        Thread xtthread;
        Thread mythread;
        bool autoConnect = false;
        string filePath = "";
        int recvFileCount = 0;
        IPEndPoint ipep;

        public Form1()
        {
            InitializeComponent();
        }

        //连接
        private void btnConnect_Click(object sender, EventArgs e)
        {
            recvFileCount = 0;
            if (txtServerIP.Text.Trim() == "" || txtServerPort.Text.Trim() == "")
            {
                ckbAutoConnect.Checked = false;
                MessageBox.Show(this, "开启失败，请检查IP及端口设置！", "提示", MessageBoxButtons.OKCancel, MessageBoxIcon.Warning);
                return;
            }
            filePath = txtPath.Text;
            if (filePath == "")
            {
                ckbAutoConnect.Checked = false;
                MessageBox.Show(this, "请选择保存路径再打开连接！", "提示", MessageBoxButtons.OKCancel, MessageBoxIcon.Warning);
                return;
            }
            try
            {
                ipep = new IPEndPoint(IPAddress.Parse(txtServerIP.Text), int.Parse(txtServerPort.Text));
                clientsocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                stop = false;
                clientsocket.Connect(ipep);
                btnConnect.Enabled = false;
                btnStop.Enabled = true;
                Thread.Sleep(1000);
                //提交注册
                string zc = "*hq,zc," + txtClientName.Text + "," + machineNo + "#";
                byte[] zcinfo = System.Text.Encoding.UTF8.GetBytes(zc);
                clientsocket.Send(zcinfo);
                Thread.Sleep(1000);

                mythread = new Thread(GetData);
                mythread.Start();
            }
            catch (Exception)
            {
                AddMsg(string.Format("[{0}] [INFO]:连接服务器 (" + ipep + ") 失败。", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")));
                stop = true;
                clientsocket = null;
            }
        }

        private void keepThread()
        {
            while (!stop)
            {
                Thread.Sleep(1000);
                string xt = "**##";
                byte[] xtinfo = System.Text.Encoding.UTF8.GetBytes(xt);
                try
                {
                    clientsocket.Send(xtinfo);
                }
                catch 
                {
                    stop = true;
                }
            }
        }

        //接受数据
        private void GetData()
        {
            string filename = "";
            string fullname = "";
            string md5 = "";
            int filesize = 0;
            while (!stop)
            {
                //接收 
                string datacontent = "";
                try
                {
                    datacontent = System.Text.Encoding.UTF8.GetString(ReceiveStrToByte(clientsocket)).Split('\0')[0];
                    if (datacontent == "")
                    {
                        stop = true;
                        AddMsg(string.Format("[{0}] [INFO]:连接被中断" + (autoConnect ? ",将立即重试连接服务器。 " : ""), DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")));
                        clientsocket.Shutdown(SocketShutdown.Send);
                        clientsocket.Close();
                        clientsocket = null;
                        break;
                    }
                }
                catch
                {
                    stop = true;
                    clientsocket = null;
                    break;
                }
                Thread.Sleep(10);
                if (datacontent.StartsWith("*") && datacontent.EndsWith("#") && !datacontent.Contains("#*"))
                {
                    string[] datas = datacontent.Split(',');

                    switch (datas[1])
                    {
                        case "zc":
                            AddMsg(string.Format("[{0}] [INFO]:连接服务器 (" + ipep + ") 成功。", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")));
                            xtthread = new Thread(keepThread);
                            xtthread.Start();
                            break;
                        case "data":
                            filename = datas[2];
                            filesize = int.Parse(datas[3]);
                            md5 = datas[4].Replace("#", "");
                            fullname = filePath + "\\" + filename;
                            if (filesize == 0)
                            {
                                FileStream MyFileStream = new FileStream(fullname, FileMode.Create);
                                MyFileStream.Close();

                                string infostr = "*hq,datasure," + md5 + "#";
                                byte[] info = System.Text.Encoding.UTF8.GetBytes(infostr);
                                clientsocket.Send(info);
                            }
                            else
                            {
                                clientsocket.Send(System.Text.Encoding.UTF8.GetBytes(datacontent.Replace("#", ",ok#")));
                                {
                                    //创建一个新文件        
                                    FileStream MyFileStream = new FileStream(fullname, FileMode.Create, FileAccess.Write);

                                    int PacketSize = 5000;
                                    //定义一个完整数据包缓存  
                                    byte[] data = new byte[PacketSize];
                                    while (true)
                                    {
                                        int revcount = clientsocket.Receive(data, 0, PacketSize, SocketFlags.None);
                                        if (revcount == 0)
                                        {
                                            data = null;
                                            break;
                                        }
                                        //处理最后一个数据包    
                                        if (revcount != PacketSize)
                                        {
                                            byte[] lastdata = new byte[revcount];
                                            for (int i = 0; i < revcount; i++)
                                                lastdata[i] = data[i];
                                            //将接收到的最后一个数据包写入到文件流对象      
                                            MyFileStream.Write(lastdata, 0, lastdata.Length);
                                            lastdata = null;
                                            break;
                                        }
                                        else
                                        {
                                            //将接收到的数据包写入到文件流对象      
                                            MyFileStream.Write(data, 0, data.Length);
                                        }

                                    }
                                    //关闭文件流       
                                    MyFileStream.Close();
                                    //校验MD5
                                    FileInfo fileinfo = new FileInfo(fullname);
                                    string localmd5 = fileHelper.GetMD5HashFromFile(fileinfo.OpenRead());

                                    if (localmd5 == md5)
                                    {
                                        string infostr = "*hq,datasure," + md5 + "#";
                                        byte[] info = System.Text.Encoding.UTF8.GetBytes(infostr);
                                        clientsocket.Send(info);
                                        AddMsg(string.Format("[{0}] [INFO]:接收到服务器 （{1}） 发送的 文件 {2} ,校验一致。", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), ipep, filename));
                                        recvFileCount++;
                                    }
                                    else
                                    {
                                        string infostr = "*hq,datafail," + md5 + "#";
                                        byte[] info = System.Text.Encoding.UTF8.GetBytes(infostr);
                                        clientsocket.Send(info);
                                        AddMsg(string.Format("[{0}] [INFO]:接收到服务器 （{1}） 发送的 文件 {2} ,校验不一致，等待重新发送。", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), ipep, filename));
                                    }
                                }
                            }

                            break;
                    }
                }
            }
            clientsocket = null;
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
                return null;
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            //初始化
            txtServerPort.Text = (System.Configuration.ConfigurationManager.AppSettings["serverPort"].ToString());
            txtServerIP.Text = (System.Configuration.ConfigurationManager.AppSettings["serverIP"].ToString());
            txtClientName.Text = (System.Configuration.ConfigurationManager.AppSettings["clientName"].ToString());
            txtPath.Text = (System.Configuration.ConfigurationManager.AppSettings["filePath"].ToString());
            ckbAutoConnect.Checked = autoConnect = (System.Configuration.ConfigurationManager.AppSettings["autoConnect"].ToString() == "1" ? true : false);

            SystemInfo si = new SystemInfo();
            machineNo = si.getMNum().ToUpper();
            if (txtClientName.Text == "")
            {
                txtClientName.Text = System.Environment.MachineName;
            }
            timer1.Start();
        }

        private void btnStop_Click(object sender, EventArgs e)
        {
            stop = true;
            Thread.Sleep(500);

            try
            {
                string infostr = "*hq,close," + machineNo + "#";
                byte[] info = System.Text.Encoding.UTF8.GetBytes(infostr);
                clientsocket.Send(info);

                clientsocket.Shutdown(SocketShutdown.Send);
                clientsocket.Close();
                clientsocket = null;

                AddMsg(string.Format("[{0}] [INFO]:主动中断连接 ", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")));
            }
            catch
            {
                clientsocket = null;
            }
            try
            {
                if (xtthread != null)
                {
                    xtthread.Abort();
                    xtthread = null;
                }
                if (mythread != null)
                {
                    mythread.Abort();
                    mythread = null;
                }
            }
            catch
            {

            }

            btnConnect.Enabled = true;
            btnStop.Enabled = false;
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

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            //保存配置
            fileHelper.SetValue("serverIP", txtServerIP.Text);
            fileHelper.SetValue("serverPort", txtServerPort.Text);
            fileHelper.SetValue("filePath", txtPath.Text);
            fileHelper.SetValue("clientName", txtClientName.Text);
            fileHelper.SetValue("autoConnect", autoConnect?"1":"0");

            if (!stop)
            {
                stop = true;
                Thread.Sleep(500);
                try
                {
                    string infostr = "*hq,close," + machineNo + "#";
                    byte[] info = System.Text.Encoding.UTF8.GetBytes(infostr);
                    clientsocket.Send(info);

                    Thread.Sleep(500);
                    clientsocket.Shutdown(SocketShutdown.Send);
                    clientsocket.Close();
                    clientsocket = null;
                }
                catch
                {
                    clientsocket = null;
                }
            }

            try
            {
                if (xtthread != null)
                {
                    xtthread.Abort();
                    xtthread = null;
                }
                if (mythread != null)
                {
                    mythread.Abort();
                    mythread = null;
                }
            }
            catch
            {

            }
        }

        private void llSelectPath_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            if (this.folderBrowserDialog1.ShowDialog() == DialogResult.OK)
            {
                filePath = folderBrowserDialog1.SelectedPath;
                txtPath.Text = filePath;
            }
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            lblRecvCount.Text = "接收文件总数：" + recvFileCount.ToString();
            toolStripStatusLabel1.Text = "系统时间：" + System.DateTime.Now.ToString("G");
            if (stop)
            {
                try
                {
                    if (xtthread != null)
                    {
                        xtthread.Abort();
                        xtthread = null;
                    }
                    if (mythread != null)
                    {
                        mythread.Abort();
                        mythread = null;
                    }
                }
                catch
                {

                }

                btnConnect.Enabled = true;
                btnStop.Enabled = false;
            }
            if (!backgroundWorker1.IsBusy)
            {
                backgroundWorker1.RunWorkerAsync();
            }
        }

        private void ckbAutoConnect_CheckedChanged(object sender, EventArgs e)
        {
            autoConnect = ckbAutoConnect.Checked;
        }

        private void linkLabel2_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            listBox1.Items.Clear();
        }

        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            backgroundWorker1.ReportProgress(0);
        }

        private void backgroundWorker1_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            if (autoConnect && clientsocket == null)
            {
                btnConnect_Click(null, null);
            }
        }
    }
}
