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
            rsa = RSA.Create(8192);
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
            });
            app = builder.Build();
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