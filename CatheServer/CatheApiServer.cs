using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Newtonsoft.Json;
using System.Net;
using DnsClient;
using CatheServer.Modules;
using CatheServer.Modules.Database;
using System.Security.Cryptography;
using CatheServer.Modules.ApiRoutes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Caching.Memory;

namespace CatheServer
{
    public class CatheApiServer
    {
        private WebApplication app;
        private Task? task;
        private DatabaseHandler database;
        internal static RSA rsa;

        static CatheApiServer()
        {
            rsa = RSA.Create(4096);
            if (File.Exists("rsa.priv")) rsa.ImportPkcs8PrivateKey(File.ReadAllBytes("rsa.priv"), out _);
            using Stream file = File.Create("rsa.priv");
            file.Write(rsa.ExportPkcs8PrivateKey());
        }

        public CatheApiServer(ushort port, X509Certificate2? cert = null)
        {
            WebApplicationBuilder builder = WebApplication.CreateBuilder();
            builder.WebHost.UseKestrel(options =>
            {
                options.ListenAnyIP(port, cert != null ? configure =>
                {
                    configure.UseHttps(cert);
                }
                : configure => { });
            });// 添加内存缓存服务
            builder.Services.AddMemoryCache();
            builder.Services.AddControllers();

            this.app = builder.Build();

            // 定义IP速率限制中间件
            app.Use(async (context, next) =>
            {
                var memoryCache = context.RequestServices.GetRequiredService<IMemoryCache>();
                var ipAddress = context.Connection.RemoteIpAddress?.ToString();
                if (ipAddress == null)
                {
                    await next();
                    return;
                }
                var cacheEntry = memoryCache.GetOrCreate(ipAddress, entry =>
                {
                    entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1);
                    return new IpRateLimitInfo { Count = 0, IsBanned = false };
                });

                if (cacheEntry.Count > 10)
                {
                    if (!cacheEntry.IsBanned)
                    {
                        cacheEntry.IsBanned = true;
                        memoryCache.Set(ipAddress, cacheEntry, new MemoryCacheEntryOptions
                        {
                            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
                        });
                    }

                    context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                    await context.Response.WriteAsync("You are banned for 10 minutes due to exceeding the rate limit.");
                    Utils.LogAccess(context, context.Response.StatusCode);
                    return;
                }

                cacheEntry.Count++;
                memoryCache.Set(ipAddress, cacheEntry);
                await next();
            });

            // 配置路由和控制器
            app.UseRouting();
            database = new DatabaseHandler();

            ConfigureMaps();
        }

        public void ConfigureMaps()
        {
            Register.RegisterRoutes(ref this.app, this.database);
        }

        public void Start() => this.task = this.app.RunAsync();
        
        public void Wait() => this.task?.Wait();
    }
}