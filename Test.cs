using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace WebSurge
{
    public static class Test
    {
        private static string path = Path.Combine(Environment.CurrentDirectory, "check");
        private static List<string> sensitivityNames = new List<string>();
        private static List<string> paths = new List<string>();
        private static List<string> secretFileNames = new List<string>();
        private static List<string> secrets = new List<string>();
        private static Regex reg = new Regex("\".*?\"");
        private static Regex secretReg = new Regex("^[A-Za-z0-9]+$");
        private static int secretLength;
        private static string proxyIp;
        private static int proxyPort;

        public static void TestFile()
        {
            //var result = HttpRequestHelper.HttpPost("http://localhost:24271/Home/Test", new Dictionary<string, object>() { { "content", "19532506885" }, { "name", "112" } }, ContentType.StringContent);

            /*
            using (var inStrem = File.OpenRead(@"C:\Users\tianshuwang\Desktop\新建文本文档.txt"))
            {
                byte[] buffer = new byte[inStrem.Length];
                inStrem.Read(buffer, 0, (int)inStrem.Length);
                using (var memoryStream = new MemoryStream(buffer))
                {
                    memoryStream.Position = 0;
                    var result = HttpRequestHelper.HttpPost("http://localhost:24271/Home/FileTest", new Dictionary<string, object>() { { "files", memoryStream } }, ContentType.StreamContent);
                    Console.WriteLine(result);
                }
            }*/
            //var result = HttpRequestHelper.HttpPost("http://localhost:24271/Home/FileTest", new Dictionary<string, object>() { { "files", "19532506885" } }, ContentType.StreamContent);

            //Console.WriteLine(result);

            var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json").Build();
            sensitivityNames = config.GetSection("sensitivityNames").GetChildren().Select(x => x.Value).ToList();
            secretLength = Convert.ToInt32(config.GetSection("secretLength").Value);
            proxyIp = config.GetSection("proxyIp").Value;
            proxyPort = Convert.ToInt32(config.GetSection("proxyPort").Value);

            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            ReadFilePath(path);

            Console.WriteLine(paths.Count > 0 ? "存在未混淆的字段或加解密函数" : "不存在未混淆的字段或加解密函数");
            foreach (var path in paths)
            {
                Console.WriteLine(path);
            }

            Console.WriteLine(secrets.Count > 0 ? "存在未加密的明文密钥" : "不存在未加密的明文密钥");
            foreach (var secretFileName in secretFileNames)
            {
                Console.WriteLine(secretFileName);
            }

            Console.WriteLine(secrets.Count > 0 ? "密钥" : string.Empty);
            foreach (var secret in secrets)
            {
                Console.WriteLine(secret);
            }


            /*
            using (var inStream = File.OpenRead(@"D:\tianshuwang\wx_check_project\WebApplication2\ConsoleApp1\bin\Debug\netcoreapp3.1\check\WAGfxEmsc.wasm"))
            {
                byte[] buffer = new byte[inStream.Length];
                inStream.Read(buffer, 0, (int)inStream.Length);
                buffer[512 * 5] = 0x1A;
            }*/

            Console.ReadKey();
        }

        public static void ReadFilePath(string path)
        {
            string[] dirs = Directory.GetFileSystemEntries(path);//获取文件目录和文件名
            foreach (var dir in dirs)
            {
                if (Directory.Exists(dir))
                {
                    ReadFilePath(dir);
                }
                else
                {
                    using (var ouStream = File.OpenRead(dir))
                    using (var reader = new StreamReader(ouStream))
                    {
                        string str;
                        while ((str = reader.ReadLine()) != null)
                        {
                            str = str.Trim().ToLower();
                            //过滤是否有未混淆的加解密函数
                            if (sensitivityNames.Count(p => str.Contains(p)) > 0)
                            {
                                if (!paths.Contains(dir))
                                {
                                    paths.Add(dir);
                                }
                            }

                            //过滤是否有未加密的明文密钥
                            var matches = reg.Matches(str);
                            foreach (var item in matches)
                            {
                                str = item.ToString();
                                if (!string.IsNullOrEmpty(str) &&
                                    str.Length == secretLength)
                                {
                                    var secretArr = secretReg.Matches(str);

                                    if (secretArr != null && secretArr.Count > 0)
                                    {
                                        if (!secretFileNames.Contains(dir))
                                        {
                                            secretFileNames.Add(dir);
                                        }

                                        foreach (var secret in secretArr)
                                        {
                                            secrets.Add(secret.ToString());
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
