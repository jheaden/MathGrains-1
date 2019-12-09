# MathGrains
Calculator app that uses Orleans grains to do the arithmetic.

# MathGrains Tutorial: Writing your first service using Orleans
## Conceptual Overview
This Orleans project has four components:
**Silo** – contains Grains and communicates Client instructions to the appropriate Grain.
**Client** – sends requests to the Silo and receives results from the Grain
**Grain Interface** – blueprint for the Grain that will be instantiated in the Silo
**Grain** – performs a task and returns a result

In this instance, we are going to use these components to create a Silo that will contain one Grain. The Client will send an integer to the Grain and the Grain will return the square of that integer. 

We will create the Silo and the Client as .NET Core App files that will guide the application’s flow. Then, we will create the Grain and GrainInterface projects as .NET Standard Libraries. After that, we will create and configure a project for each component, and then replace the default code with the code provided in this tutorial.

# Creating the Projects
In Visual Studio, create a new Project. 
Choose to make it a Visual C# Console App (.NET Core).
Name the project Silo.
Name the Solution MathGrains.

Add a second .NET Core Console App project to the MathGrains solution and name it Client.
Add a new project, but this time choose .NET Standard – Class Library. Name it GrainInterfaces.

Add a new interface class file to the GrainInterfaces folder and name it ISquareGrain.

**Note:** For some project types, Visual Studio likes to add a default class named `Class1.cs`. You can delete this file, to keep things tidy. It will probably appear in your Grains project, too. 

For the fourth and final project, add a .NET Standard – Class Library project and name it Grains.
Inside the Grains project, add a class and name it SquareGrain.cs.

## Add Orleans Packages using NuGet
Right-click each project and select **Manage NuGet Packages…**
 
Search for and install the packages for each project, as listed here:

**Silo**
Microsoft.Orleans.OrleansProviders
Microsoft.Orleans.OrleansRuntime
Microsoft.Extensions.Logging.Console


**Client**
Microsoft.Orleans.Core
Microsoft.Extensions.Logging.Console

**Grains**
Microsoft.Orleans.Core.Abstractions
Microsoft.Orleans.OrleansCodeGenerator.Build
Microsoft.Extensions.Logging.Abstractions

**GrainInterfaces**
Microsoft.Orleans.Core.Abstractions
Microsoft.Orleans.OrleansCodeGenerator.Build

# Configure Project Dependencies
Silo depends on Grains and GrainInterfaces 
Client depends on GrainInterfaces
Grains depend on GrainInterfaces
GrainInterfaces have no dependencies

For the Projects that depend on other Projects, right-click and select **Add Reference…**
Check the boxes for the projects you need and click OK.

# Adding Code
## Silo – Program.cs
This is the code that creates and starts a silo.
```csharp
using System;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Orleans.Configuration;
using Orleans.Hosting;

namespace MathGrains
{
    class Program
    {
        public static int Main(string[] args)
        {
            return RunMainAsync().Result;
        }

        private static async Task<int> RunMainAsync()
        {
            try
            {
                var host = await StartSilo();
                Console.WriteLine("Press Enter to terminate...");
                Console.ReadLine();

                await host.StopAsync();

                return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return 1;
            }
        }

        private static async Task<ISiloHost> StartSilo()
        {
            // define the cluster configuration
            var builder = new SiloHostBuilder()
                .UseLocalhostClustering()
                .Configure<ClusterOptions>(options =>
                {
                    options.ClusterId = "dev";
                    options.ServiceId = "Orleans Square";
                })
                .Configure<EndpointOptions>(options => options.AdvertisedIPAddress = IPAddress.Loopback)
                .ConfigureLogging(logging => logging.AddConsole());

            var host = builder.Build();
            await host.StartAsync();
            return host;
        }
    }
}
```

## Client – Program.cs
```csharp
The Client code initializes the Orleans client runtime, prompts the user for an integer value, sends the integer to the grain, displays the result, and then waits for user input before terminating.
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Runtime;
using Orleans.Configuration;
using GrainInterfaces;

namespace OrleansClient
{
    /// <summary>
    /// Orleans test silo client
    /// </summary>
    public class Program
    {
        static int Main(string[] args)
        {
            return RunMainAsync().Result;
        }

        private static async Task<int> RunMainAsync()
        {
            try
            {
                using (var client = await StartClientWithRetries())
                {
                    await DoClientWork(client);
                    Console.ReadKey();
                }

                return 0;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                Console.ReadKey();
                return 1;
            }
        }

        private static async Task<IClusterClient> StartClientWithRetries(int initializeAttemptsBeforeFailing = 5)
        {
            int attempt = 0;
            IClusterClient client;
            while (true)
            {
                try
                {
                    client = new ClientBuilder()
                        .UseLocalhostClustering()
                        .Configure<ClusterOptions>(options =>
                        {
                            options.ClusterId = "dev";
                            options.ServiceId = "Orleans Square";
                        })
                        .ConfigureLogging(logging => logging.AddConsole())
                        .Build();

                    await client.Connect();
                    Console.WriteLine("Client successfully connect to silo host");
                    break;
                }
                catch (SiloUnavailableException)
                {
                    attempt++;
                    Console.WriteLine($"Attempt {attempt} of {initializeAttemptsBeforeFailing} failed to initialize the Orleans client.");
                    if (attempt > initializeAttemptsBeforeFailing)
                    {
                        throw;
                    }
                    await Task.Delay(TimeSpan.FromSeconds(4));
                }
            }
            return client;
        }

        private static async Task DoClientWork(IClusterClient client)
        {
            // example of calling grains from the initialized client
            ISquareGrain mySquareGrain = client.GetGrain<ISquareGrain>(Guid.NewGuid());

            Console.WriteLine("\n\nPlease enter an integer number to square:");
            int mathMe = 0;

            while (!int.TryParse(Console.ReadLine(), out mathMe))
            {
                Console.WriteLine("Please enter a valid numerical value!");
                Console.WriteLine("Please enter an integer to square and cube:");
            }

            int squaredResult = await mySquareGrain.SquareMe(mathMe);
            Console.WriteLine("\n\n The square of {0} is {1}.\n\n", mathMe, squaredResult);
        }
    }
}
```

## GrainInterfaces
ISquareGrain
For this example, we chose to use a Guid key instead of an Integer key because we are already passing around an integer in the function.

```csharp
using System.Threading.Tasks;
using Orleans;

namespace GrainInterfaces
{
    public interface ISquareGrain : IGrainWithGuidKey
    {
        Task<int> SquareMe(int input);
    }
}
```


## Grains
**SquareGrain**
This is where to put the grain’s squaring function. 

```csharp
using System.Threading.Tasks;
using GrainInterfaces;
using Orleans;

namespace Grains
{
    public class SquareGrain : Grain, ISquareGrain
    {
        
        public Task<int> SquareMe(int input)
        {
            return Task.FromResult(input * input);
        }
    }
}
```


## Running the Application
Start the Silo (this will be link to a separate document)
Start the Client (this will be link to a separate document)
What Success should look like

## Troubleshooting
This will be a link to a separate document about Troubleshooting, with information like this, so that I can remove this from the Orleans ReadMe page.

## Building and running tests in Visual Studio 2017
.NET Core 2.0 SDK is a pre-requisite to build Orleans.sln.
There might be errors trying to build from Visual Studio because of conflicts with the test discovery engine (error says could not copy xunit.abstractions.dll). The reason for that error is that you need to configure the test runner in VS like so (after opening the solution):

* Test -> Test Settings -> Uncheck Keep Test Execution Engine running
* Test -> Test Settings -> Default Processor Architecture -> Check X64

Either restart VS, or go to the task manager and kill the processes that starts with vstest.. 
Then, build the project again. It should succeed and tests should appear in the Test Explorer window.



