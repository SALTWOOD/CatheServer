using CatheServer.Modules;
using DnsClient;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Runtime.Intrinsics.X86;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace CatheServer
{
    public static class Utils
    {
        public static async Task HttpResponseWrapper(HttpContext context, Func<Task<HttpResponseEntity?>> action)
        {
            HttpResponseEntity? response = null;
            context.Response.Headers.Append("Content-Type", "application/json");
            response = ProcessResponse(() => action.Invoke().Result, response);
            await SendResponse(context, response);
            LogAccess(context, response);
        }

        public static async Task HttpResponseWrapper(HttpContext context, Func<HttpResponseEntity?> action)
        {
            HttpResponseEntity? response = null;
            context.Response.Headers.Append("Content-Type", "application/json");
            response = ProcessResponse(action, response);
            await SendResponse(context, response);
            LogAccess(context, response);
        }

        private static HttpResponseEntity? ProcessResponse(Func<HttpResponseEntity?> action, HttpResponseEntity? response)
        {
            try
            {
                response = action.Invoke();
                if (response == null) throw new InternalServerException("\"response\" is null.");
            }
            catch (Exception ex)
            {
                ProcessException(ref response, ex);
            }

            return response;
        }

        public static void LogAccess(HttpContext context, HttpResponseEntity? response)
        {
            Logger.Instance.LogInfo($"[{context.Connection.RemoteIpAddress?.ToString()}] {context.Request.Method} {context.Request.Path} - {response?.StatusCode} ({context.Request.Headers.UserAgent.ToString()})");
        }

        public static void LogAccess(HttpContext context, int status)
        {
            Logger.Instance.LogInfo($"[{context.Connection.RemoteIpAddress?.ToString()}] {context.Request.Method} {context.Request.Path} - {status} ({context.Request.Headers.UserAgent.ToString()})");
        }

        private static async ValueTask SendResponse(HttpContext context, HttpResponseEntity? response)
        {
            try
            {
                if (response != null)
                {
                    context.Response.StatusCode = response.Value.StatusCode;
                    await context.Response.Body.WriteAsync(
                        Encoding.UTF8.GetBytes(
                            JsonConvert.SerializeObject(response)
                        )
                    );
                }
                else
                {
                    await context.Response.Body.WriteAsync(
                        Encoding.UTF8.GetBytes(
                            JsonConvert.SerializeObject(new HttpResponseEntity
                            {
                                StatusCode = 500,
                                Data = null,
                                Error = new Error
                                {
                                    Message = null,
                                    Type = null
                                },
                                Message = "failed",
                                unknownError = true
                            })
                        )
                    );
                }
            }
            catch { }
        }

        private static void ProcessException(ref HttpResponseEntity? response, Exception ex)
        {
            if (ex is ClientRequestInvalidException)
            {
                response = new HttpResponseEntity
                {
                    StatusCode = 400,
                    Error = new Error
                    {
                        Message = ex.Message,
                        Type = ex.GetType().FullName
                    },
                    unknownError = false,
                    Data = null,
                    Message = "failed"
                };
            }
            else if (ex is InternalServerException)
            {
                response = new HttpResponseEntity
                {
                    StatusCode = 500,
                    Error = new Error
                    {
                        Message = ex.Message,
                        Type = ex.GetType().FullName
                    },
                    unknownError = false,
                    Data = null,
                    Message = "failed"
                };
            }
            else if (ex is Exception)
            {
                response = new HttpResponseEntity
                {
                    StatusCode = 500,
                    Error = new Error
                    {
                        Message = ex.Message,
                        Type = ex.GetType().FullName
                    },
                    unknownError = true,
                    Data = null,
                    Message = "failed"
                };
            }
        }

        public static void TryInvoke(Action action)
        {
            try
            {
                action.Invoke();
            }
            catch { }
        }

        public static void TryInvoke(Func<Task> action)
        {
            try
            {
                Task.Run(action).Wait();
            }
            catch { }
        }

        public static async ValueTask<bool> VerifyEmail(string email)
        {
            var temp = email.Split('@');
            string username = temp.First();
            string emailServer = temp.Last();
            try
            {
                var dns = new LookupClient(IPAddress.Parse("223.5.5.5"));
                var result = await dns.QueryAsync(emailServer, QueryType.MX);
                return result.Answers.Count > 0;
            }
            catch
            {
                return false;
            }
        }

        public unsafe static bool EqualsAll(this byte[] arr1, byte[]? arr2) //如果代码不是nullable就去掉"?"
        {
            if (Object.ReferenceEquals(arr1, arr2)) return true;

            int length = arr1.Length;
            if (arr2 == null || length != arr2.Length)
                return false;

            if (length < 4)
            {
                for (int i = 0; i < arr1.Length; i++)
                {
                    if (arr1[i] != arr2[i])
                        return false;
                }
                return true;
            }
            else
            {
                fixed (void* voidby1 = arr1)
                {
                    fixed (void* voidby2 = arr2)
                    {
                        const int cOneCompareSize = 8;

                        var blkCount = length / cOneCompareSize;
                        var less = length % cOneCompareSize;

                        byte* by1, by2;

                        long* lby1 = (long*)voidby1;
                        long* lby2 = (long*)voidby2;
                        while (blkCount > 0)
                        {
                            if (*lby1 != *lby2)
                                return false;
                            lby1++; lby2++;
                            blkCount--;
                        }

                        if (less >= 4) //此if和true的代码可以不要，性能差异不大
                        {
                            if (*((int*)lby1) != *((int*)lby2))
                                return false;

                            by1 = ((byte*)lby1 + 4);
                            by2 = ((byte*)lby2 + 4);

                            less = less - 4;
                        }
                        else
                        {
                            by1 = (byte*)lby1;
                            by2 = (byte*)lby2;
                        }

                        while (less-- > 0)
                        {
                            if (*by1 != *by2)
                                return false;
                            by1++; by2++;
                        }
                        return true;
                    }
                }
            }
        }

        public static async ValueTask<Dictionary<string, object>> GetBodyContent(HttpContext context)
        {
            StreamReader stream = new StreamReader(context.Request.Body);
            string body = await stream.ReadToEndAsync();
            if (body[0] == '{')
            {
                var content = JsonConvert.DeserializeObject<Dictionary<string, object>>(body);
                if (content == null)
                {
                    throw new ClientRequestInvalidException("Unable to deserialize body content to a valid JSON object.");
                }
                return content;
            }
            else
            {
                return DecodeUrlToDictionary(body);
            }
        }

        public static Dictionary<string, object> DecodeUrlToDictionary(string url)
        {
            // 创建一个Dictionary来存储键值对
            Dictionary<string, object> result = new Dictionary<string, object>();

            // 按照&符号分割参数
            string[] parameters = url.Split('&');

            // 遍历每个参数
            foreach (string parameter in parameters)
            {
                // 按照=符号分割键值对
                string[] keyValue = parameter.Split('=', 2); // 使用2来限制分割次数，确保值中有=也不会出错

                // 确保键值对是有效的
                if (keyValue.Length == 2)
                {
                    string key = HttpUtility.UrlDecode(keyValue[0]);
                    object value = HttpUtility.UrlDecode(keyValue[1]);

                    // 将键值对添加到Dictionary中
                    result[key] = value;
                }
                else
                {
                    throw new ClientRequestInvalidException($"Bad request: [{string.Join(", ", keyValue)}]");
                }
            }

            return result;
        }
    }
}
