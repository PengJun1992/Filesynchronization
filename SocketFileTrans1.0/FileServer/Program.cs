using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace FileServer
{
    static class Program
    {//来自_5_1_a_s_p_x
        /// <summary>
        /// 应用程序的主入口点。
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form2());
        }
    }
}
