using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TennisBookings.ScoreProcessor
{
    public class Program
    {
        public static void Main(string[] args)
        {
            //After the host is created, the ".Build()" method builds the application while the ".Run()" method starts the application.
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            //The line of code below creates the host
            Host.CreateDefaultBuilder(args)
            //The line of code below lets us add services to the IServices collection by registering them within the method.
            //Dependency injection container makes the service available where needed, it's almost the same as registering services in the startup.cs class.
                .ConfigureServices((hostContext, services) =>
                {
                    //The host runs and manages the lifetime of an application which includes dependency injection, logging and configuration.
                    //The host turns a console into a long-running service.
                    //The host starts and stops hosted services
                    services.AddHostedService<Worker>();
                });

        //In summary, program.cs is where the host is built by leveraging IHost.cs which triggers "RunAsync()" then "StartAsync()" which then runs the different IHosted services one after the othe rin the order they were registered.
        //The "WaitForShutdownAsync()" is what hangs around, waiting for the application to be shut down.
        //The shutdown depends on where the application is running; CTRL + C can trigger a shutdown from the terminal.
        //Process termination and programmatic shutdown also occurs.
        //IHosted services typically perform their workload in the background while all of these are happening.

        //When the application is shutdown, the "WaitForShutdownAsync()" triggers the "StopAsync" which calls itself on each IHosted web service in the reverse order in which they were registered.
    }
}
