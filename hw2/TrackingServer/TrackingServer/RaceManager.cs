using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using log4net;
using Base;

namespace TrackingServer
{
    class RaceManager
    {
        enum MessageType {Hello, Subscribe, Unsubscribe, RaceStart, Register, DidNotStart, Started, OnCourse, Update, DidNotFinish, Finished};

        private UdpClient _udpClient;
        private IPEndPoint _localEndPoint;
        private List<Client> clientList = new List<Client>();
        private List<Athlete> athleteList = new List<Athlete>();

        public RaceManager()
        {
            Console.WriteLine("New Race Manager");
            _localEndPoint = new IPEndPoint(IPAddress.Any, 8888);
            _udpClient = new UdpClient(_localEndPoint);
            _localEndPoint = _udpClient.Client.LocalEndPoint as IPEndPoint;
            List<Client> clientList = new List<Client>();
            List<Athlete> athleteList = new List<Athlete>();
        }
        
        public void addClient(IPEndPoint senderEndPoint)
        {
            Client newClient = new Client(senderEndPoint);
            clientList.Add(newClient);
        }

        public void addAthlete(Athlete athlete)
        {
            athleteList.Add(athlete);
        }

        public void ReceiveMessage(string message, IPEndPoint senderEndPoint)
        {
            var elements = SplitMessage(message);
            switch (elements[0])
            {
                case "Hello":
                    addClient(senderEndPoint);
                    break;
                case "Subscribe":
                    Subscribe(message, senderEndPoint);
                    break;
                case "Unsubscribe":
                    Unsubscribe(message, senderEndPoint);
                    break;
                case "Race":
                    RaceStart(message, senderEndPoint);
                    break;
                case "Registered":
                    Registered(message, senderEndPoint);
                    break;
                case "DidNotStart":
                    DidNotStart(message, senderEndPoint);
                    break;
                case "Started":
                    Started(message, senderEndPoint);
                    break;
                case "OnCourse":
                    OnCourse(message, senderEndPoint);
                    break;
                case "DidNotFinish":
                    DidNotFinish(message, senderEndPoint);
                    break;
                case "Finished":
                    Finish(message, senderEndPoint);
                    break;
            }
        }

        public void SendMessage(string message, IPEndPoint senderEndPoint)
        {
                if (string.IsNullOrEmpty(message))
                    throw new ApplicationException("Cannot send an empty message");

                if (senderEndPoint == null || senderEndPoint.Address.ToString() == "0.0.0.0")
                    throw new ApplicationException("Invalid target end point");

                var data = Encoding.BigEndianUnicode.GetBytes(message);
                _udpClient.Send(data, data.Length, senderEndPoint);
        }

        public string[] SplitMessage(string message)
        {
            var elements = message.Split(new[] { ',' });
            return elements;
        }

        public void Hello(string message, IPEndPoint senderEndPoint)
        {
            addClient(senderEndPoint);
        }

        public void Subscribe(string message, IPEndPoint senderEndPoint)
        {
            var elements = SplitMessage(message);
            foreach (Client client in clientList)
            { 
                if(senderEndPoint.Equals(client.returnAddress))
                {
                    client.NewSubscription(Int32.Parse(elements[1]));
                    break;
                }
            }
        }

        public void Unsubscribe(string message, IPEndPoint senderEndPoint)
        {
            var elements = SplitMessage(message);
            foreach(Client client in clientList)
            {
                if(senderEndPoint == client.returnAddress)
                {
                    client.cancelSubscription(Int32.Parse(elements[1]));
                    break;
                }
            }
        }

        public void RaceStart(string message, IPEndPoint senderEndPoint)
        {
            foreach(Client client in clientList)
            {
                SendMessage(message, client.returnAddress);
            }
        }

        public void Registered(string message, IPEndPoint senderEndPoint)
        {
            var elements = SplitMessage(message);
            Athlete newAthlete = new Athlete();
            newAthlete.BibNumber = Int32.Parse(elements[1]);
            newAthlete.LastUpdatedTime = Int32.Parse(elements[2]);
            newAthlete.FirstName = elements[3];
            newAthlete.LastName = elements[4];
            newAthlete.Gender = elements[5];
            newAthlete.Age = Int32.Parse(elements[6]);
            addAthlete(newAthlete);
            foreach(Client client in clientList)
            {
                SendMessage("Athlete," + elements[1] + ',' + elements[3] + ',' + elements[4] + ',' + elements[5] + ',' + elements[6], client.returnAddress);
            }
        }

        public void DidNotStart(string message, IPEndPoint senderEndPoint)
        {
            var elements = SplitMessage(message);
            Athlete subject = new Athlete();
            foreach(Athlete athlete in athleteList)
            {
                if(athlete.BibNumber == Int32.Parse(elements[1]))
                {
                    athlete.CurrentStatus = AthleteRaceStatus.DidNotStart;
                    athlete.FinishedTime = 0;
                    athlete.LastUpdatedTime = Int32.Parse(elements[2]);
                    subject = athlete;
                    break;
                }
            }
            SendStatusUpdate(subject, senderEndPoint);
        }

        public void Started(string message, IPEndPoint senderEndPoint)
        {
            var elements = SplitMessage(message);
            Athlete subject = new Athlete();
            foreach(Athlete athlete in athleteList)
            {
                if(athlete.BibNumber == Int32.Parse(elements[1]))
                {
                    athlete.CurrentStatus = AthleteRaceStatus.Started;
                    athlete.LastUpdatedTime = Int32.Parse(elements[2]);
                    subject = athlete;
                    break;
                }
            }
            SendStatusUpdate(subject, senderEndPoint);
        }

        public void OnCourse(string message, IPEndPoint senderEndPoint)
        {
            var elements = SplitMessage(message);
            Athlete subject = new Athlete();
            foreach (Athlete athlete in athleteList)
            {
                if (athlete.BibNumber == Int32.Parse(elements[1]))
                {
                    athlete.CurrentStatus = AthleteRaceStatus.OnCourse;
                    athlete.LastUpdatedTime = Int32.Parse(elements[2]);
                    athlete.DistanceCovered = Double.Parse(elements[3]);
                    subject = athlete;
                    break;
                }
            }
            SendStatusUpdate(subject, senderEndPoint);
        }

        public void DidNotFinish(string message, IPEndPoint senderEndPoint)
        {
            var elements = SplitMessage(message);
            Athlete subject = new Athlete();
            foreach (Athlete athlete in athleteList)
            {
                if (athlete.BibNumber == Int32.Parse(elements[1]))
                {
                    athlete.CurrentStatus = AthleteRaceStatus.DidNotFinish;
                    athlete.LastUpdatedTime = Int32.Parse(elements[2]);
                    athlete.FinishedTime = 0;
                    subject = athlete;
                    break;
                }
            }
            SendStatusUpdate(subject, senderEndPoint);
        }

        public void Finish(string message, IPEndPoint senderEndPoint)
        {
            var elements = SplitMessage(message);
            Athlete subject = new Athlete();
            foreach (Athlete athlete in athleteList)
            {
                if (athlete.BibNumber == Int32.Parse(elements[1]))
                {
                    athlete.CurrentStatus = AthleteRaceStatus.Finished;
                    athlete.LastUpdatedTime = athlete.FinishedTime = Int32.Parse(elements[2]);
                    subject = athlete;
                    break;
                }
            }
            SendStatusUpdate(subject, senderEndPoint);
        }

        public void SendStatusUpdate(Athlete athlete, IPEndPoint senderEndPoint)
        {
            foreach(Client client in clientList)
            {
                if (client.subscribedBibs.Contains(athlete.BibNumber))
                    SendMessage("Status," + athlete.BibNumber.ToString() + ',' + athlete.CurrentStatus.ToString() + ',' + (athlete.StartTime).ToString() + ',' + athlete.DistanceCovered.ToString() + ',' + athlete.LastUpdatedTime.ToString() + ',' + athlete.FinishedTime.ToString(), client.returnAddress);
            }
        }
    }
}
