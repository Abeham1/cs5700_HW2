using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using log4net;

namespace Base
{
    class TrackingServer
    {

        static void Main(string[] args)
        {
            var server = new Communicator(12000);
            Console.WriteLine("Server listening at: 127.0.0.1:" + server.LocalPort);

            server.Start();
            
            Console.WriteLine("Press any key to exit.");
            Console.ReadKey();
            server.Stop();
            server.Close();
        }

    }
}
