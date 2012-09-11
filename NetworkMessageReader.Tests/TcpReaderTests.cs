using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using NUnit.Framework;

namespace NetworkMessageReader.Tests
{
    [TestFixture]
    public class TcpReaderTests
    {
        private static TcpListener _tcpListner;
        private const string MessageSeperator = "<TEST>";
        private TcpClient _tcpClient;
        private TcpReader _tcpReader;
        private const string DataMessage = "Hello World!";
        private ManualResetEventSlim _completed;
        private string _stringDataReturned;
        private TcpClient _serverSideTcpClient;

        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public void TcpReader_WhenTcpClientIsNull_ThrowsException()
        {
            new TcpReader(null, null, 1);
        }

        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public void TcpReader_WhenMessageParserIsNull_ThrowsException()
        {
            var tcpClient = new TcpClient();
            new TcpReader(tcpClient, null, 1);
        }

        [Test]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void TcpReader_WhenBufferSizeIsZero_ThrowsException()
        {
            var tcpClient = new TcpClient();
            new TcpReader(tcpClient, new MessageParser("something"), 0);
        }

        [Test]
        [ExpectedException(typeof(InvalidOperationException))]
        public void TcpReader_WhenTcpClientIsNotConencted_ThrowsException()
        {
            var tcpClient = new TcpClient();
            new TcpReader(tcpClient, new MessageParser("something"), 1);
        }

        [Test]
        public void TcpReader_WhenBufferSizeIsGreaterThanZero_ThenContinues()
        {
            new TcpReader(_tcpClient, new MessageParser("something"), 1);
        }

        [SetUp]
        public void Setup()
        {
            _tcpClient = new TcpClient();
            _serverSideTcpClient = MakeConnection(_tcpClient);
            _completed = new ManualResetEventSlim(false);
            _stringDataReturned = string.Empty;            
        }

        [TearDown]
        public void Teardown()
        {
            CloseConnection();
        }

        [TestCase(1, DataMessage + MessageSeperator, DataMessage)]
        [TestCase(5, DataMessage + MessageSeperator, DataMessage)]
        [TestCase(10, DataMessage + MessageSeperator, DataMessage)]
        [TestCase(15, DataMessage + MessageSeperator, DataMessage)]
        [TestCase(100, DataMessage + MessageSeperator, DataMessage)]
        [TestCase(150, DataMessage + MessageSeperator, DataMessage)]
        [TestCase(1000, DataMessage + MessageSeperator, DataMessage)]
        [TestCase(1500, DataMessage + MessageSeperator, DataMessage)]
        [TestCase(1, DataMessage, "")]
        [TestCase(5, DataMessage, "")]
        [TestCase(10, DataMessage, "")]
        [TestCase(15, DataMessage, "")]
        [TestCase(100, DataMessage, "")]
        [TestCase(150, DataMessage, "")]
        [TestCase(1000, DataMessage, "")]
        [TestCase(1500, DataMessage, "")]
        [TestCase(1, DataMessage + MessageSeperator + DataMessage + MessageSeperator + DataMessage, DataMessage + DataMessage)]
        [TestCase(5, DataMessage + MessageSeperator + DataMessage + MessageSeperator + DataMessage, DataMessage + DataMessage)]
        [TestCase(10, DataMessage + MessageSeperator + DataMessage + MessageSeperator + DataMessage, DataMessage + DataMessage)]
        [TestCase(15, DataMessage + MessageSeperator + DataMessage + MessageSeperator + DataMessage, DataMessage + DataMessage)]
        [TestCase(100, DataMessage + MessageSeperator + DataMessage + MessageSeperator + DataMessage, DataMessage + DataMessage)]
        [TestCase(150, DataMessage + MessageSeperator + DataMessage + MessageSeperator + DataMessage, DataMessage + DataMessage)]
        [TestCase(1000, DataMessage + MessageSeperator + DataMessage + MessageSeperator + DataMessage, DataMessage + DataMessage)]
        [TestCase(1500, DataMessage + MessageSeperator + DataMessage + MessageSeperator + DataMessage, DataMessage + DataMessage)]
        [TestCase(1, DataMessage + MessageSeperator + DataMessage + MessageSeperator + DataMessage + MessageSeperator, DataMessage + DataMessage + DataMessage)]
        [TestCase(5, DataMessage + MessageSeperator + DataMessage + MessageSeperator + DataMessage + MessageSeperator, DataMessage + DataMessage + DataMessage)]
        [TestCase(10, DataMessage + MessageSeperator + DataMessage + MessageSeperator + DataMessage + MessageSeperator, DataMessage + DataMessage + DataMessage)]
        [TestCase(15, DataMessage + MessageSeperator + DataMessage + MessageSeperator + DataMessage + MessageSeperator, DataMessage + DataMessage + DataMessage)]
        [TestCase(100, DataMessage + MessageSeperator + DataMessage + MessageSeperator + DataMessage + MessageSeperator, DataMessage + DataMessage + DataMessage)]
        [TestCase(150, DataMessage + MessageSeperator + DataMessage + MessageSeperator + DataMessage + MessageSeperator, DataMessage + DataMessage + DataMessage)]
        [TestCase(1000, DataMessage + MessageSeperator + DataMessage + MessageSeperator + DataMessage + MessageSeperator, DataMessage + DataMessage + DataMessage)]
        [TestCase(1500, DataMessage + MessageSeperator + DataMessage + MessageSeperator + DataMessage + MessageSeperator, DataMessage + DataMessage + DataMessage)]
        [TestCase(1, MessageSeperator, "")]
        [TestCase(5, MessageSeperator, "")]
        [TestCase(10, MessageSeperator, "")]
        [TestCase(15, MessageSeperator, "")]
        [TestCase(100, MessageSeperator, "")]
        [TestCase(150, MessageSeperator, "")]
        [TestCase(1000, MessageSeperator, "")]
        [TestCase(1500, MessageSeperator, "")]
        [TestCase(1, MessageSeperator + MessageSeperator + MessageSeperator, "")]
        [TestCase(5, MessageSeperator + MessageSeperator + MessageSeperator, "")]
        [TestCase(10, MessageSeperator + MessageSeperator + MessageSeperator, "")]
        [TestCase(15, MessageSeperator + MessageSeperator + MessageSeperator, "")]
        [TestCase(100, MessageSeperator + MessageSeperator + MessageSeperator, "")]
        [TestCase(150, MessageSeperator + MessageSeperator + MessageSeperator, "")]
        [TestCase(1000, MessageSeperator + MessageSeperator + MessageSeperator, "")]
        [TestCase(1500, MessageSeperator + MessageSeperator + MessageSeperator, "")]
        [TestCase(1, MessageSeperator + MessageSeperator + MessageSeperator + DataMessage, "")]
        [TestCase(5, MessageSeperator + MessageSeperator + MessageSeperator + DataMessage, "")]
        [TestCase(10, MessageSeperator + MessageSeperator + MessageSeperator + DataMessage, "")]
        [TestCase(15, MessageSeperator + MessageSeperator + MessageSeperator + DataMessage, "")]
        [TestCase(100, MessageSeperator + MessageSeperator + MessageSeperator + DataMessage, "")]
        [TestCase(150, MessageSeperator + MessageSeperator + MessageSeperator + DataMessage, "")]
        [TestCase(1000, MessageSeperator + MessageSeperator + MessageSeperator + DataMessage, "")]
        [TestCase(1500, MessageSeperator + MessageSeperator + MessageSeperator + DataMessage, "")]
        [TestCase(1, MessageSeperator + MessageSeperator + MessageSeperator + DataMessage + MessageSeperator, DataMessage)]
        [TestCase(5, MessageSeperator + MessageSeperator + MessageSeperator + DataMessage + MessageSeperator, DataMessage)]
        [TestCase(10, MessageSeperator + MessageSeperator + MessageSeperator + DataMessage + MessageSeperator, DataMessage)]
        [TestCase(15, MessageSeperator + MessageSeperator + MessageSeperator + DataMessage + MessageSeperator, DataMessage)]
        [TestCase(100, MessageSeperator + MessageSeperator + MessageSeperator + DataMessage + MessageSeperator, DataMessage)]
        [TestCase(150, MessageSeperator + MessageSeperator + MessageSeperator + DataMessage + MessageSeperator, DataMessage)]
        [TestCase(1000, MessageSeperator + MessageSeperator + MessageSeperator + DataMessage + MessageSeperator, DataMessage)]
        [TestCase(1500, MessageSeperator + MessageSeperator + MessageSeperator + DataMessage + MessageSeperator, DataMessage)]
        [TestCase(1, MessageSeperator + MessageSeperator + MessageSeperator + DataMessage + MessageSeperator + MessageSeperator, DataMessage)]
        [TestCase(5, MessageSeperator + MessageSeperator + MessageSeperator + DataMessage + MessageSeperator + MessageSeperator, DataMessage)]
        [TestCase(10, MessageSeperator + MessageSeperator + MessageSeperator + DataMessage + MessageSeperator + MessageSeperator, DataMessage)]
        [TestCase(15, MessageSeperator + MessageSeperator + MessageSeperator + DataMessage + MessageSeperator + MessageSeperator, DataMessage)]
        [TestCase(100, MessageSeperator + MessageSeperator + MessageSeperator + DataMessage + MessageSeperator + MessageSeperator, DataMessage)]
        [TestCase(150, MessageSeperator + MessageSeperator + MessageSeperator + DataMessage + MessageSeperator + MessageSeperator, DataMessage)]
        [TestCase(1000, MessageSeperator + MessageSeperator + MessageSeperator + DataMessage + MessageSeperator + MessageSeperator, DataMessage)]
        [TestCase(1500, MessageSeperator + MessageSeperator + MessageSeperator + DataMessage + MessageSeperator + MessageSeperator, DataMessage)]
        public void TcpReader_WhenDataIsSentToStreamWithFullMessageSeperator_ThenDataIsObserved(int tcpReaderBufferSize, string dataToSend, string dataToExpect)
        {
            //Arrange
            _tcpReader = new TcpReader(_serverSideTcpClient, new MessageParser(MessageSeperator), tcpReaderBufferSize);

            //Act
            var streamWriter = new StreamWriter(_tcpClient.GetStream());
            _tcpReader.Read().Subscribe(data =>
                {
                    _stringDataReturned += data;
                }, _completed.Set);

            streamWriter.Write(dataToSend);
            streamWriter.Flush();
            streamWriter.Close();

            //Assert
            _completed.Wait();
            Assert.AreEqual(dataToExpect, _stringDataReturned);
        }

        [TestCase(1)]
        [TestCase(5)]
        [TestCase(10)]
        [TestCase(15)]
        [TestCase(100)]
        [TestCase(150)]
        [TestCase(1000)]
        [TestCase(1500)]
        public void TcpReader_WhenSeveralDataMessageSentAcrossMoreThanOneWriteAndNoMessageSeperators_ThenNoMessagesIsObserved(int tcpReaderBufferSize)
        {
            //Arrange
            _tcpReader = new TcpReader(_serverSideTcpClient, new MessageParser(MessageSeperator), tcpReaderBufferSize);
            const string dataToSend = DataMessage;

            //Act
            var streamWriter = new StreamWriter(_tcpClient.GetStream());
            _tcpReader.Read().Subscribe(data =>
            {
                _stringDataReturned += data;
            }, _completed.Set);

            streamWriter.Write(dataToSend);
            streamWriter.Flush();
            streamWriter.Write(dataToSend);
            streamWriter.Flush();
            streamWriter.Close();

            //Assert
            _completed.Wait();
            Assert.AreEqual(string.Empty, _stringDataReturned);
        }

        [TestCase(1)]
        [TestCase(5)]
        [TestCase(10)]
        [TestCase(15)]
        [TestCase(100)]
        [TestCase(150)]
        [TestCase(1000)]
        [TestCase(1500)]
        public void TcpReader_WhenSeveralDataMessageSentAcrossMoreThanOneWriteAndMessageSeperatorsUsed_ThenAllMessagesAreObserved(int tcpReaderBufferSize)
        {
            //Arrange
            const string dataToSend = DataMessage;
            _tcpReader = new TcpReader(_serverSideTcpClient, new MessageParser(MessageSeperator), tcpReaderBufferSize);

            //Act
            var streamWriter = new StreamWriter(_tcpClient.GetStream());
            _tcpReader.Read().Subscribe(data =>
            {
                _stringDataReturned += data;
            }, _completed.Set);

            streamWriter.Write(dataToSend);
            streamWriter.Flush();
            streamWriter.Write(MessageSeperator);
            streamWriter.Flush();
            streamWriter.Write(dataToSend);
            streamWriter.Flush();
            streamWriter.Write(MessageSeperator);
            streamWriter.Flush();
            streamWriter.Close();

            //Assert
            _completed.Wait();
            Assert.AreEqual(dataToSend+dataToSend, _stringDataReturned);
        }

        [TestCase(1)]
        [TestCase(5)]
        [TestCase(10)]
        [TestCase(15)]
        [TestCase(100)]
        [TestCase(150)]
        [TestCase(1000)]
        [TestCase(1500)]
        public void TcpReader_WhenDataMessageSentAndMessageSeperatorSentIn2Fragments_Then1MessagesIsObserved(int tcpReaderBufferSize)
        {
            //Arrange
            const string dataToSend = DataMessage;
            _tcpReader = new TcpReader(_serverSideTcpClient, new MessageParser(MessageSeperator), tcpReaderBufferSize);

            //Act
            var streamWriter = new StreamWriter(_tcpClient.GetStream());
            _tcpReader.Read().Subscribe(data =>
            {
                _stringDataReturned += data;
            }, _completed.Set);

            streamWriter.Write(dataToSend);
            streamWriter.Flush();
            streamWriter.Write(MessageSeperator.Substring(0,2));
            streamWriter.Flush();
            streamWriter.Write(MessageSeperator.Substring(2, MessageSeperator.Length-2));
            streamWriter.Flush();
            streamWriter.Close();

            //Assert
            _completed.Wait();
            Assert.AreEqual(dataToSend, _stringDataReturned);
        }

        private static TcpClient MakeConnection(TcpClient tcpClient)
        {
            TcpClient serverSideTcpClient = null;
            var ipAddress = new IPAddress(new byte[] { 127, 0, 0, 1 });
            const int portNumber = 181;
            _tcpListner = new TcpListener(ipAddress, portNumber);
            _tcpListner.Start();
            _tcpListner.BeginAcceptTcpClient(
                asyncResult => { serverSideTcpClient = ((TcpListener)(asyncResult.AsyncState)).EndAcceptTcpClient(asyncResult); },
                _tcpListner);
            tcpClient.Connect(ipAddress, portNumber);
            while (serverSideTcpClient == null || tcpClient.Connected == false)
            {
            }
            serverSideTcpClient.NoDelay = true;
            return serverSideTcpClient;
        }

        private static void CloseConnection()
        {
            _tcpListner.Stop();
        }
    }
}
