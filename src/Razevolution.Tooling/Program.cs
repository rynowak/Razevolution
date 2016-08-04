﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Microsoft.Extensions.CommandLineUtils;

namespace Razevolution.Tooling
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            var app = new CommandLineApplication();

            app.HelpOption("-h|--help");

            var portOption = app.Option("-p|--port", "port of host connection", CommandOptionType.SingleValue);

            var debugOption = app.Option("-d|--debug", "break into debugger", CommandOptionType.NoValue);

            app.OnExecute(() => Execute(portOption, debugOption));

            app.Execute(args);
        }

        public static int Execute(CommandOption portOption, CommandOption debugOption)
        {
            int port;
            if (!portOption.HasValue() || !int.TryParse(portOption.Value(), out port))
            {
                Console.WriteLine("port is required");
                return -1;
            }

            if (debugOption.HasValue())
            {
                Debugger.Launch();
                Debugger.Break();
            }

            Console.WriteLine($"connecting to {port}");

            var client = new TcpClient();
            client.ConnectAsync(IPAddress.Loopback, port).GetAwaiter().GetResult();

            var stream = client.GetStream();

            var queue = new MessageQueue();
            var reader = new DefaultMessageReader(new BinaryReader(stream), queue);
            reader.Start();

            Console.WriteLine($"connected to {port}");
            reader.Wait();

            return 0;
        }
    }
}
