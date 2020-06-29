using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore;
using NLog.Web;
namespace Library.API
{
    public class Program
    {
        public static void Main(string[] args)
        {
            BuildWebHost(args).Run();
        }

        public static IWebHost BuildWebHost(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
           // .UseNLog()//we want to log issues even before hitting our controllers.
                        // we can trace issues occuring during bootstrapping of app
                .UseStartup<Startup>()
                .Build();        
    }
}
