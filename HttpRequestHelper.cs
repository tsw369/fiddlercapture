using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace WebSurge
{
    public static class HttpRequestHelper
    {
        public static string HttpGet(string url, string proxyIp, int proxyPort)
        {
            using (var handler = new HttpClientHandler())
            {
                //绕过SSL
                handler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => { return true; };
                //设置代理
                handler.Proxy = new WebProxy(proxyIp, proxyPort);//str为IP地址 port为端口号;
                using (var httpClient = new HttpClient(handler))
                {
                    var response = httpClient.GetAsync(url).Result;
                    if (response.IsSuccessStatusCode)
                    {
                        return response.Content.ReadAsStringAsync().Result;
                    }
                    else
                    {
                        return default;
                    }
                }
            }
        }

        public static string HttpPost(string url, Dictionary<string, object> dic, ContentType contentType, string proxyIp, int proxyPort)
        {
            using (var handler = new HttpClientHandler())
            {
                //绕过SSL
                handler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => { return true; };
                //设置代理
                handler.Proxy = new WebProxy(proxyIp, proxyPort);//str为IP地址 port为端口号;
                using (var httpClient = new HttpClient(handler))
                {
                    switch (contentType)
                    {
                        case ContentType.MultipartFormDataContent:
                            using (var content = new MultipartFormDataContent())
                            {
                                return AddContentItemAndGetResult(httpClient, url, dic, content);
                            }

                        case ContentType.FormUrlEncodedContent:
                            var keyValues = new Dictionary<string, string>();
                            foreach (var key in dic.Keys)
                            {
                                keyValues[key] = dic[key].ToString();
                            }
                            using (var content = new FormUrlEncodedContent(keyValues))
                            {
                                return AddContentItemAndGetResult(httpClient, url, dic, content);
                            }

                        case ContentType.StringContent:
                            return AddContentItemAndGetResult(httpClient, url, dic, new StringContent(JsonConvert.SerializeObject(dic), Encoding.UTF8, "application/json"));

                        case ContentType.StreamContent:
                            return AddContentItemAndGetResult(httpClient, url, dic, new StreamContent((MemoryStream)dic.Values.FirstOrDefault()));
                    }
                }

                return default;
            }
        }

        private static string AddContentItemAndGetResult(HttpClient httpClient, string url, Dictionary<string, object> dic, HttpContent httpContent)
        {
            if (httpContent is MultipartFormDataContent)
            {
                var multipartFormDataContent = httpContent as MultipartFormDataContent;
                foreach (var key in dic.Keys)
                {
                    multipartFormDataContent.Add(new StringContent(dic[key].ToString()), key);
                }
            }

            var response = httpClient.PostAsync(url, httpContent).Result;
            if (response.IsSuccessStatusCode)
            {
                return response.Content.ReadAsStringAsync().Result;
            }

            return default;
        }
    }

    public enum ContentType
    {
        MultipartFormDataContent,
        FormUrlEncodedContent,
        StringContent,
        StreamContent
    }
}