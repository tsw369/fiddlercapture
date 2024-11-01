using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;


namespace WebSurge
{
    public static class Program
    {
        [STAThread]
        static void Main()
        {

            //https://weblog.west-wind.com/posts/2014/jul/29/using-fiddlercore-to-capture-http-requests-with-net
            var limit = ServicePointManager.DefaultConnectionLimit;
            if (ServicePointManager.DefaultConnectionLimit < 10)
                ServicePointManager.DefaultConnectionLimit = 200;

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            var mainForm = new FiddlerCapture();
            

            Application.ThreadException += Application_ThreadException;
            try
            {
                Application.Run(mainForm);
            }
            catch (Exception ex)
            {
                Application_ThreadException(null, new ThreadExceptionEventArgs(ex));
            } 
        }
        
        static void Application_ThreadException(object sender, ThreadExceptionEventArgs e)
        {
            var ex = e.Exception;
            //App.Log(ex);

            var msg = string.Format("异常：{0}",ex.Message);

            DialogResult res = MessageBox.Show(msg," 错误",
                                                MessageBoxButtons.YesNo,MessageBoxIcon.Error);
            if (res == DialogResult.No)
                Application.Exit();
        } 
        
    }
}
