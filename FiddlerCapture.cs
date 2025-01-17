﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Windows.Forms;
using Fiddler;

namespace WebSurge
{
    public partial class FiddlerCapture : Form
    {
        private const string Separator = "------------------------------------------------------------------";
        private UrlCaptureConfiguration CaptureConfiguration { get; set; }

        bool isFirstSslRequest;

        public FiddlerCapture()
        {
            //Test.TestFile();
            InitializeComponent();
            CaptureConfiguration = new UrlCaptureConfiguration();  
        }

        private void FiddlerCapture_Load(object sender, EventArgs e)
        {
            tbIgnoreResources.Checked = CaptureConfiguration.IgnoreResources;
            txtCaptureDomain.Text = CaptureConfiguration.CaptureDomain;

            UpdateButtonStatus();

            var processes = Process.GetProcesses().OrderBy(p => p.ProcessName);
            foreach (var process in processes)
            {
                txtProcessId.Items.Add(process.ProcessName + "  - " + process.Id);
            }
        }

        private void FiddlerApplication_AfterSessionComplete(Session sess)
        {
            // Ignore HTTPS connect requests
            if (sess.RequestMethod == "CONNECT")
                return;

            if (CaptureConfiguration.ProcessId > 0)
            {
                if (sess.LocalProcessID != 0 && sess.LocalProcessID != CaptureConfiguration.ProcessId)
                    return;
            }

            if (!string.IsNullOrEmpty(CaptureConfiguration.CaptureDomain))
            {
                if (sess.hostname.ToLower() != CaptureConfiguration.CaptureDomain.Trim().ToLower())
                    return;
            }

            if (CaptureConfiguration.IgnoreResources)   
            {
                var url = sess.fullUrl.ToLower();

                var extensions = CaptureConfiguration.ExtensionFilterExclusions;
                foreach (var ext in extensions)
                {
                    if (url.Contains(ext))
                        return;
                }

                var filters = CaptureConfiguration.UrlFilterExclusions;
                foreach (var urlFilter in filters)
                {
                    if (url.Contains(urlFilter))
                        return;
                }
            }

            if (sess == null || sess.oRequest == null || sess.oRequest.headers == null)
                return;

            var headers = sess.oRequest.headers.ToString();
            var reqBody = Encoding.UTF8.GetString(sess.RequestBody);


            //string respHeaders = session.oResponse.headers.ToString();
            //var respBody = Encoding.UTF8.GetString(session.ResponseBody);

            var firstLine = sess.RequestMethod + " " + sess.fullUrl + " " + sess.oRequest.headers.HTTPVersion;
            var at = headers.IndexOf("\r\n");
            if (at < 0)
                return;
            headers = firstLine + "\r\n" + headers.Substring(at + 1);

            var output = headers + "\r\n" +
                            (!string.IsNullOrEmpty(reqBody) ? reqBody + "\r\n" : string.Empty) +
                            Separator + "\r\n\r\n";

            BeginInvoke(new Action<string>((text) =>
            {
                txtCapture.AppendText(text);
                UpdateButtonStatus();
            }), output);
        }

        void Start()
        {
            if (tbIgnoreResources.Checked)
                CaptureConfiguration.IgnoreResources = true;
            else
                CaptureConfiguration.IgnoreResources = false;

            string strProcId = txtProcessId.Text;
            if (strProcId.Contains('-'))
                strProcId = strProcId.Substring(strProcId.IndexOf('-') + 1).Trim();

            strProcId = strProcId.Trim();

            int procId = 0;
            if (!string.IsNullOrEmpty(strProcId))
            {
                if (!int.TryParse(strProcId, out procId))
                    procId = 0;
            }

            CaptureConfiguration.ProcessId = procId;
            CaptureConfiguration.CaptureDomain = txtCaptureDomain.Text;


            FiddlerApplication.AfterSessionComplete += FiddlerApplication_AfterSessionComplete;
            FiddlerApplication.Startup(8888, true, true, true);
        }


        void Stop()
        {
            FiddlerApplication.AfterSessionComplete -= FiddlerApplication_AfterSessionComplete;

            if (FiddlerApplication.IsStarted())
                FiddlerApplication.Shutdown();
        }



        public static bool InstallCertificate()
        {
            if (!CertMaker.rootCertExists())
            {
                if (!CertMaker.createRootCert())
                    return false;

                if (!CertMaker.trustRootCert())
                    return false;
            }

            return true;
        }

        public static bool UninstallCertificate()
        {
            if (CertMaker.rootCertExists())
            {
                if (!CertMaker.removeFiddlerGeneratedCerts(true))
                    return false;
            }
            return true;
        }

        private void ButtonHandler(object sender, EventArgs e)
        {
            if (sender == tbCapture)
                Start();
            else if (sender == tbStop)
                Stop();
            else if (sender == tbSave)
            {
                var diag = new SaveFileDialog()
                {
                    AutoUpgradeEnabled = true,
                    CheckPathExists = true,
                    DefaultExt = "txt",
                    Filter = "Text files (*.txt)|*.txt|All Files (*.*)|*.*",
                    OverwritePrompt = false,
                    Title = "Save Fiddler Capture File",
                    RestoreDirectory = true
                };
                var res = diag.ShowDialog();

                if (res == DialogResult.OK)
                {
                    if (File.Exists(diag.FileName))
                        File.Delete(diag.FileName);

                    File.WriteAllText(diag.FileName, txtCapture.Text);


                }
            }
            else if (sender == tbClear)
            {
                txtCapture.Text = string.Empty;  
            }
            else if (sender == btnInstallSslCert)
            {
                Cursor = Cursors.WaitCursor;
                InstallCertificate();
                Cursor = Cursors.Default;
            }
            else if (sender == btnUninstallSslCert)
                UninstallCertificate();

            UpdateButtonStatus();
        }



        private void FiddlerCapture_FormClosing(object sender, FormClosingEventArgs e)
        {
            Stop();
        }


        public void UpdateButtonStatus()
        {
            tbCapture.Enabled = !FiddlerApplication.IsStarted();
            tbStop.Enabled = !tbCapture.Enabled;
            tbSave.Enabled = txtCapture.Text.Length > 0;
            tbClear.Enabled = tbSave.Enabled;

            btnInstallSslCert.Enabled = !CertMaker.rootCertExists();
            btnUninstallSslCert.Enabled = !btnInstallSslCert.Enabled;

            CaptureConfiguration.IgnoreResources = tbIgnoreResources.Checked;
        }

        public IEnumerable<string> GetLoacalIPMaybeVirtualNetwork()
        {
            var ipInfos = new List<string>()
            {
                $"Host：{Environment.MachineName.ToLower()}",
                $"Domain：{System.Net.NetworkInformation.IPGlobalProperties.GetIPGlobalProperties().DomainName.ToLower()}"
            };

            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
                ipInfos.Add(ip.ToString());

            return ipInfos;
        }

        private void label_OnLine_MouseEnter(object sender, EventArgs e)
        {
            var ipInfos = GetLoacalIPMaybeVirtualNetwork();
            var sb = new StringBuilder();
            foreach (var ipInfo in ipInfos)
            {
                if (ipInfo.Contains("Domain"))
                    sb.Append(ipInfo).Append(Environment.NewLine).Append(Environment.NewLine);
                else
                    sb.Append(ipInfo).Append(Environment.NewLine);
            }
            toolTip1.Show(sb.ToString(), label_OnLine);
        }

        private void label_OnLine_MouseLeave(object sender, EventArgs e)
        {
            toolTip1.Hide(label_OnLine);
        }

        public static byte[] FromHex(string hex)
        {
            hex = hex.Replace(" ", "");
            byte[] raw = new byte[hex.Length / 2];
            for (int i = 0; i < raw.Length; i++)
            {
                raw[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);
            }
            return raw;
        }
    }   
}
