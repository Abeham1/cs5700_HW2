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
    class Client
    {
        public IPEndPoint returnAddress { get; private set; }
        public List<int> subscribedBibs = new List<int>();

        public Client(IPEndPoint returnIP)
        {
            returnAddress = returnIP;
        }

        public void NewSubscription(int bibNumber)
        {
            bool alreadySubscribed = false;
            foreach(int bib in subscribedBibs)
            {
                if (bib == bibNumber)
                {
                    alreadySubscribed = true;
                    break;
                }
            }
            if(alreadySubscribed == false)
            {
                subscribedBibs.Add(bibNumber);
            }
        }

        public void cancelSubscription(int bibNumber)
        {
            foreach (int bib in subscribedBibs)
            {
                if (bib == bibNumber)
                {
                    subscribedBibs.Remove(bib);
                }
            }
        }
    }
}
