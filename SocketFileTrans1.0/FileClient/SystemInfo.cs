using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Management;
using System.Security.Cryptography;

namespace FileClient
{
    public class SystemInfo
    {

        public int[] intCode = new int[127];//存储密钥    
        public int[] intNumber = new int[25];//存机器码的Ascii值  
        public char[] Charcode = new char[25];//存储机器码字      

        /// <summary>  
        /// 取得设备硬盘的卷标号      
        /// </summary>    
        /// <returns></returns>     
        public string GetDiskVolumeSerialNumber()
        {
            ManagementClass mc = new ManagementClass("Win32_NetworkAdapterConfiguration");
            ManagementObject disk = new ManagementObject("win32_logicaldisk.deviceid=\"c:\"");
            disk.Get();
            return disk.GetPropertyValue("VolumeSerialNumber").ToString();
        }

        /// <summary>     
        /// 获得CPU的序列号   
        /// </summary>    
        /// <returns></returns>    
        public string getCpu()
        {
            string strCpu = null;
            ManagementClass myCpu = new ManagementClass("win32_Processor");
            ManagementObjectCollection myCpuConnection = myCpu.GetInstances();
            foreach (ManagementObject myObject in myCpuConnection)
            {
                strCpu = myObject.Properties["Processorid"].Value.ToString();
                break;
            }
            return strCpu;

        }
        /// <summary>
        /// 把字符转换成MD5
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static string GetMD5Hash(String str)
        {
            //把字符串转换成字节数组
            byte[] buffer = Encoding.Default.GetBytes(str);

            MD5CryptoServiceProvider md5 = new MD5CryptoServiceProvider();
            //md5加密
            byte[] cryptBuffer = md5.ComputeHash(buffer);
            string s = "";
            //把每一个字节 0-255，转换成两位16进制数     
            for (int i = 0; i < cryptBuffer.Length; i++)
            {
                //大X转黄的是大写字母，小X转换的是小写字母
                s += cryptBuffer[i].ToString("x2");
            }
            return s;
        }

        /// <summary>     
        /// 生成机器码  
        /// </summary>   
        /// <returns></returns>  
        public string getMNum()
        {
            try
            {
                string strNum = getCpu() + GetDiskVolumeSerialNumber();//获得24位Cpu和硬盘序列号     
                string strMNum = GetMD5Hash(strNum).Substring(0, 24);//从生成的字符串中取出前24个字符做为机器码     
                return strMNum;
            }
            catch
            {
                return GetMD5Hash("phper.in").Substring(0, 24);
            }
        }

        public void setIntCode()//给数组赋值小于10的数    
        {
            for (int i = 1; i < intCode.Length; i++)
            {
                intCode[i] = i % 9;
            }
        }

        /// <summary>    
        /// 生成注册码     
        /// </summary>    
        /// <returns></returns>  
        public string getRNum()
        {
            setIntCode();//初始化127位数组       
            string MNum = this.getMNum();//获取注册码    
            for (int i = 1; i < Charcode.Length; i++)//把机器码存入数组中   
            {
                Charcode[i] = Convert.ToChar(MNum.Substring(i - 1, 1));
            }

            for (int j = 1; j < intNumber.Length; j++)//把字符的ASCII值存入一个整数组中。  
            {
                intNumber[j] = intCode[Convert.ToInt32(Charcode[j])] + Convert.ToInt32(Charcode[j]);
            }

            string strAsciiName = "";//用于存储注册码      
            for (int j = 1; j < intNumber.Length; j++)
            {
                if (intNumber[j] >= 48 && intNumber[j] <= 57)//判断字符ASCII值是否0－9之间   
                {
                    strAsciiName += Convert.ToChar(intNumber[j]).ToString();
                }
                else if (intNumber[j] >= 65 && intNumber[j] <= 90)//判断字符ASCII值是否A－Z之间    
                {
                    strAsciiName += Convert.ToChar(intNumber[j]).ToString();
                }
                else if (intNumber[j] >= 97 && intNumber[j] <= 122)//判断字符ASCII值是否a－z之间    
                {
                    strAsciiName += Convert.ToChar(intNumber[j]).ToString();
                }
                else//判断字符ASCII值不在以上范围内         
                {
                    if (intNumber[j] > 122)//判断字符ASCII值是否大于z          
                    {
                        strAsciiName += Convert.ToChar(intNumber[j] - 10).ToString();
                    }
                    else
                    {
                        strAsciiName += Convert.ToChar(intNumber[j] - 9).ToString();
                    }
                }
            }
            return strAsciiName;//返回注册码    

        }
    }
}
