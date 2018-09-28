using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Base;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BaseTest
{
    [TestClass()]
    public class CommunicatorTest
    {
        [TestMethod()]
        public void Communicator_TestConstructor()
        {
            // Try creating a communicator bound to any available port
            var communicator = new Communicator();
            Assert.IsTrue(communicator.LocalPort > 0);

            communicator.Close();

            // Try creating a communicator bound to a specific port
            communicator = new Communicator(12345);
            Assert.AreEqual(12345, communicator.LocalPort);

            // Try creating another communicator bound to the smary port -- this shouldn't succeed
            try
            {
                var communicator2 = new Communicator(12345);
                Assert.Fail("Exception expected");
            }
            catch (SocketException e)
            {
                if (e.SocketErrorCode != SocketError.AddressAlreadyInUse)
                    Assert.Fail("Some exception, other than than the expected exception was thrown");
            }

        }

        /// <summary>
        /// Test the Communicate being used a passive-object 
        /// </summary>
        [TestMethod()]
        public void Communicator_TestAsPassiveObject()
        {
            var comm1 = new Communicator();
            var comm1EndPoint = new IPEndPoint(IPAddress.Loopback, comm1.LocalPort);
            var comm2 = new Communicator();
            var comm2EndPoint = new IPEndPoint(IPAddress.Loopback, comm2.LocalPort);

            comm1.Send("Hello", comm2EndPoint);
            IPEndPoint senderEndPoint;
            var message = comm2.GetMessage(out senderEndPoint);
            Assert.AreEqual("Hello", message);           
            Assert.AreEqual(comm1.LocalPort, senderEndPoint.Port);


            comm2.Send("Hello, there!", comm1EndPoint);
            message = comm1.GetMessage(out senderEndPoint);
            Assert.AreEqual("Hello, there!", message);
            Assert.AreEqual(comm2.LocalPort, senderEndPoint.Port);

            try
            {
                comm1.Send(null, comm2EndPoint);
                Assert.Fail("Expected exception not thrown");
            }
            catch (Exception e)
            {
                Assert.AreEqual("Cannot send an empty message", e.Message);
            }

            try
            {
                comm1.Send("Hello", null);
                Assert.Fail("Expected exception not thrown");
            }
            catch (Exception e)
            {
                Assert.AreEqual("Invalid target end point", e.Message);
            }

            comm1.Close();
            comm2.Close();
        }

        [TestMethod()]
        public void Communicator_TestAsActiveObject()
        {
            var comm1 = new Communicator();
            var comm1EndPoint = new IPEndPoint(IPAddress.Loopback, comm1.LocalPort);
            comm1.IncomingMessage += IncrementCount1;

            var comm2 = new Communicator();
            var comm2EndPoint = new IPEndPoint(IPAddress.Loopback, comm2.LocalPort);
            comm2.IncomingMessage += IncrementCount2;

            _expectedSenderPort1 = comm2.LocalPort;
            _expectedSenderPort2 = comm1.LocalPort;

            comm1.Start();
            comm2.Start();

            comm1.Send("Hello", comm2EndPoint);
            comm2.Send("Hello there!", comm1EndPoint);
            comm2.Send("What's up", comm1EndPoint);
            comm1.Send("Bye", comm2EndPoint);
            comm2.Send("Bye Bye", comm1EndPoint);

            Thread.Sleep(1000);
            Assert.AreEqual(3, _countOfMessagesReceivedAsComm1);
            Assert.AreEqual(2, _countOfMessagesReceivedAtComm2);

            comm1.Stop();
            Thread.Sleep(200);
            comm2.Send("Are you still there?", comm1EndPoint);
            Thread.Sleep(200);
            Assert.AreEqual(3, _countOfMessagesReceivedAsComm1);

            comm2.Send("Hello?", comm1EndPoint);
            Thread.Sleep(100);
            Assert.AreEqual(3, _countOfMessagesReceivedAsComm1);

            comm1.Start();
            Thread.Sleep(100);
            Assert.AreEqual(5, _countOfMessagesReceivedAsComm1);

            comm1.IncomingMessage -= IncrementCount1;
            comm1.Close();

            comm2.IncomingMessage -= IncrementCount2;
            comm2.Close();
        }

        private int _expectedSenderPort1;
        private int _expectedSenderPort2;
        private int _countOfMessagesReceivedAsComm1;
        private int _countOfMessagesReceivedAtComm2;

        private void IncrementCount1(string message, IPEndPoint senderEndPoint)
        {
            Assert.IsNotNull(message);
            Assert.AreEqual(_expectedSenderPort1, senderEndPoint.Port);
            _countOfMessagesReceivedAsComm1++;
        }

        private void IncrementCount2(string message, IPEndPoint senderEndPoint)
        {
            Assert.IsNotNull(message);
            Assert.AreEqual(_expectedSenderPort2, senderEndPoint.Port);
            _countOfMessagesReceivedAtComm2++;
        }

    }
}