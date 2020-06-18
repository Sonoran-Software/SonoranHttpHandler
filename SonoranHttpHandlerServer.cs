/*
 *  MIT License

    Copyright (c) 2020 SonoranCAD Software, Inc

    Permission is hereby granted, free of charge, to any person obtaining a copy
    of this software and associated documentation files (the "Software"), to deal
    in the Software without restriction, including without limitation the rights
    to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
    copies of the Software, and to permit persons to whom the Software is
    furnished to do so, subject to the following conditions:

    The above copyright notice and this permission notice shall be included in all
    copies or substantial portions of the Software.

    THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
    IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
    FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
    AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
    LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
    OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
    SOFTWARE.
 * 
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CitizenFX.Core.Native;
using System.Net.Http;
using CitizenFX.Core;
using System.Net;
using System.Dynamic;

namespace SonoranHttpHandler
{
    public class SonoranHttpHandlerServer : BaseScript
    {
        private static readonly HttpClient client = new HttpClient();
        public SonoranHttpHandlerServer()
        {
            ServicePointManager.ServerCertificateValidationCallback +=
                (sender, cert, chain, sslPolicyErrors) => true;
            Exports.Add("HandleHttpRequest", new Action<string, CallbackDelegate, string, string, dynamic>(HandleHttpRequest));
        }
        private void HandleHttpRequest(string url, CallbackDelegate callback, string method = "GET", string data = "", dynamic headers = null)
        {
            if (method != "GET" && method != "POST")
            {
                throw new InvalidOperationException("Invalid method supplied.");
            }
            var methodObj = (method == "POST" ? HttpMethod.Post : HttpMethod.Get);
            string contentType = "text/plain";
            using (var requestMessage = new HttpRequestMessage(methodObj, url))
            {
                foreach (var header in headers)
                {
                    if (header.Key == "Authorization" || header.Key == "Bearer")
                    {
                        requestMessage.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(header.Key, header.Value.ToString());
                    }
                    else if (header.Key == "Content-Type")
                    {
                        contentType = header.Value.ToString();
                    }
                    else
                    {
                        requestMessage.Headers.Add(header.Key, header.Value.ToString());
                    }
                }
                string response = null;
                int resultCode = 0;
                Dictionary<string, string> respHeaders = new Dictionary<string, string>();
                if (method == "GET")
                {
                    var task = client.SendAsync(requestMessage).ContinueWith((respMessage) =>
                    {
                        var resp = respMessage.Result;
                        resultCode = (int)resp.StatusCode;
                        foreach (var h in resp.Headers)
                        {
                            respHeaders.Add(h.Key, h.Value.ToString());
                        }
                        var strTask = resp.Content.ReadAsStringAsync();
                        response = strTask.Result;
                    });
                    task.Wait();
                }
                else
                {
                    var content = new StringContent(data, Encoding.UTF8, contentType);
                    var task = client.PostAsync(url, content).ContinueWith((respMessage) =>
                    {
                        try
                        {
                            var resp = respMessage.Result;
                            resultCode = (int)resp.StatusCode;
                            foreach (var h in resp.Headers)
                            {
                                respHeaders.Add(h.Key, h.Value.ToString());
                            }
                            var strTask = resp.Content.ReadAsStringAsync();
                            response = strTask.Result;
                        } catch (AggregateException ae)
                        {
                            foreach(var inner in ae.InnerExceptions)
                            {
                                Console.WriteLine(inner);
                            }
                        }
                    });
                    task.Wait();
                }
                dynamic resHeaders = new ExpandoObject();
                foreach(var header in respHeaders)
                {
                    if (!resHeaders.ContainsKey(header.Key))
                    {
                        resHeaders.Add(header.Key, header.Value);
                    }
                    
                }
                callback.Invoke(resultCode, response, resHeaders);
                
            } 
        }
    }
}
