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

        [TestCase(1, DataMessage + MessageSeperator, DataMessage, Description = "Buffer Size 1: MessageSeperator terminated - Returns 1 Message")]
        [TestCase(5, DataMessage + MessageSeperator, DataMessage, Description = "Buffer Size 5: MessageSeperator terminated - Returns 1 Message")]
        [TestCase(10, DataMessage + MessageSeperator, DataMessage, Description = "Buffer Size 10: MessageSeperator terminated - Returns 1 Message")]
        [TestCase(15, DataMessage + MessageSeperator, DataMessage, Description = "Buffer Size 15: MessageSeperator terminated - Returns 1 Message")]
        [TestCase(100, DataMessage + MessageSeperator, DataMessage, Description = "Buffer Size 100: MessageSeperator terminated - Returns 1 Message")]
        [TestCase(150, DataMessage + MessageSeperator, DataMessage, Description = "Buffer Size 150: MessageSeperator terminated - Returns 1 Message")]
        [TestCase(1000, DataMessage + MessageSeperator, DataMessage, Description = "Buffer Size 1000: MessageSeperator terminated - Returns 1 Message")]
        [TestCase(1500, DataMessage + MessageSeperator, DataMessage, Description = "Buffer Size 1500: MessageSeperator terminated - Returns 1 Message")]
        [TestCase(1, DataMessage, "", Description = "Buffer Size 1: Not MessageSeperator terminated - Returns nothing")]
        [TestCase(5, DataMessage, "", Description = "Buffer Size 5: Not MessageSeperator terminated - Returns nothing")]
        [TestCase(10, DataMessage, "", Description = "Buffer Size 10: Not MessageSeperator terminated - Returns nothing")]
        [TestCase(15, DataMessage, "", Description = "Buffer Size 15: Not MessageSeperator terminated - Returns nothing")]
        [TestCase(100, DataMessage, "", Description = "Buffer Size 100: Not MessageSeperator terminated - Returns nothing")]
        [TestCase(150, DataMessage, "", Description = "Buffer Size 150: Not MessageSeperator terminated - Returns nothing")]
        [TestCase(1000, DataMessage, "", Description = "Buffer Size 1000: Not MessageSeperator terminated - Returns nothing")]
        [TestCase(1500, DataMessage, "", Description = "Buffer Size 1500: Not MessageSeperator terminated - Returns nothing")]
        [TestCase(1, DataMessage + MessageSeperator + DataMessage + MessageSeperator + DataMessage, DataMessage + DataMessage, Description = "Buffer Size 1: 3 Messages, 2 MessageSeperator and no terminating MessageSeperator - Returns 2 messages")]
        [TestCase(5, DataMessage + MessageSeperator + DataMessage + MessageSeperator + DataMessage, DataMessage + DataMessage, Description = "Buffer Size 5: 3 Messages, 2 MessageSeperator and no terminating MessageSeperator - Returns 2 messages")]
        [TestCase(10, DataMessage + MessageSeperator + DataMessage + MessageSeperator + DataMessage, DataMessage + DataMessage, Description = "Buffer Size 10: 3 Messages, 2 MessageSeperator and no terminating MessageSeperator - Returns 2 messages")]
        [TestCase(15, DataMessage + MessageSeperator + DataMessage + MessageSeperator + DataMessage, DataMessage + DataMessage, Description = "Buffer Size 15: 3 Messages, 2 MessageSeperator and no terminating MessageSeperator - Returns 2 messages")]
        [TestCase(100, DataMessage + MessageSeperator + DataMessage + MessageSeperator + DataMessage, DataMessage + DataMessage, Description = "Buffer Size 100: 3 Messages, 2 MessageSeperator and no terminating MessageSeperator - Returns 2 messages")]
        [TestCase(150, DataMessage + MessageSeperator + DataMessage + MessageSeperator + DataMessage, DataMessage + DataMessage, Description = "Buffer Size 150: 3 Messages, 2 MessageSeperator and no terminating MessageSeperator - Returns 2 messages")]
        [TestCase(1000, DataMessage + MessageSeperator + DataMessage + MessageSeperator + DataMessage, DataMessage + DataMessage, Description = "Buffer Size 1000: 3 Messages, 2 MessageSeperator and no terminating MessageSeperator - Returns 2 messages")]
        [TestCase(1500, DataMessage + MessageSeperator + DataMessage + MessageSeperator + DataMessage, DataMessage + DataMessage, Description = "Buffer Size 1500: 3 Messages, 2 MessageSeperator and no terminating MessageSeperator - Returns 2 messages")]
        [TestCase(1, DataMessage + MessageSeperator + DataMessage + MessageSeperator + DataMessage + MessageSeperator, DataMessage + DataMessage + DataMessage, Description = "Buffer Size 1: 3 Messages, 3 MessageSeperator including a terminating MessageSeperator - Returns 3 messages")]
        [TestCase(5, DataMessage + MessageSeperator + DataMessage + MessageSeperator + DataMessage + MessageSeperator, DataMessage + DataMessage + DataMessage, Description = "Buffer Size 5: 3 Messages, 3 MessageSeperator including a terminating MessageSeperator - Returns 3 messages")]
        [TestCase(10, DataMessage + MessageSeperator + DataMessage + MessageSeperator + DataMessage + MessageSeperator, DataMessage + DataMessage + DataMessage, Description = "Buffer Size 10: 3 Messages, 3 MessageSeperator including a terminating MessageSeperator - Returns 3 messages")]
        [TestCase(15, DataMessage + MessageSeperator + DataMessage + MessageSeperator + DataMessage + MessageSeperator, DataMessage + DataMessage + DataMessage, Description = "Buffer Size 15: 3 Messages, 3 MessageSeperator including a terminating MessageSeperator - Returns 3 messages")]
        [TestCase(100, DataMessage + MessageSeperator + DataMessage + MessageSeperator + DataMessage + MessageSeperator, DataMessage + DataMessage + DataMessage, Description = "Buffer Size 100: 3 Messages, 3 MessageSeperator including a terminating MessageSeperator - Returns 3 messages")]
        [TestCase(150, DataMessage + MessageSeperator + DataMessage + MessageSeperator + DataMessage + MessageSeperator, DataMessage + DataMessage + DataMessage, Description = "Buffer Size 150: 3 Messages, 3 MessageSeperator including a terminating MessageSeperator - Returns 3 messages")]
        [TestCase(1000, DataMessage + MessageSeperator + DataMessage + MessageSeperator + DataMessage + MessageSeperator, DataMessage + DataMessage + DataMessage, Description = "Buffer Size 1000: 3 Messages, 3 MessageSeperator including a terminating MessageSeperator - Returns 3 messages")]
        [TestCase(1500, DataMessage + MessageSeperator + DataMessage + MessageSeperator + DataMessage + MessageSeperator, DataMessage + DataMessage + DataMessage, Description = "Buffer Size 1500: 3 Messages, 3 MessageSeperator including a terminating MessageSeperator - Returns 3 messages")]
        [TestCase(1, MessageSeperator, "", Description = "Buffer Size 1: MessageSeperator only - Returns no message")]
        [TestCase(5, MessageSeperator, "", Description = "Buffer Size 5: MessageSeperator only - Returns no message")]
        [TestCase(10, MessageSeperator, "", Description = "Buffer Size 10: MessageSeperator only - Returns no message")]
        [TestCase(15, MessageSeperator, "", Description = "Buffer Size 15: MessageSeperator only - Returns no message")]
        [TestCase(100, MessageSeperator, "", Description = "Buffer Size 100: MessageSeperator only - Returns no message")]
        [TestCase(150, MessageSeperator, "", Description = "Buffer Size 150: MessageSeperator only - Returns no message")]
        [TestCase(1000, MessageSeperator, "", Description = "Buffer Size 1000: MessageSeperator only - Returns no message")]
        [TestCase(1500, MessageSeperator, "", Description = "Buffer Size 1500: MessageSeperator only - Returns no message")]
        [TestCase(1, MessageSeperator + MessageSeperator + MessageSeperator, "", Description = "Buffer Size 1: 3 MessageSeperators - Returns no message")]
        [TestCase(5, MessageSeperator + MessageSeperator + MessageSeperator, "", Description = "Buffer Size 5: 3 MessageSeperators - Returns no message")]
        [TestCase(10, MessageSeperator + MessageSeperator + MessageSeperator, "", Description = "Buffer Size 10: 3 MessageSeperators - Returns no message")]
        [TestCase(15, MessageSeperator + MessageSeperator + MessageSeperator, "", Description = "Buffer Size 15: 3 MessageSeperators - Returns no message")]
        [TestCase(100, MessageSeperator + MessageSeperator + MessageSeperator, "", Description = "Buffer Size 100: 3 MessageSeperators - Returns no message")]
        [TestCase(150, MessageSeperator + MessageSeperator + MessageSeperator, "", Description = "Buffer Size 150: 3 MessageSeperators - Returns no message")]
        [TestCase(1000, MessageSeperator + MessageSeperator + MessageSeperator, "", Description = "Buffer Size 1000: 3 MessageSeperators - Returns no message")]
        [TestCase(1500, MessageSeperator + MessageSeperator + MessageSeperator, "", Description = "Buffer Size 1500: 3 MessageSeperators - Returns no message")]
        [TestCase(1, MessageSeperator + MessageSeperator + MessageSeperator + DataMessage, "", Description = "Buffer Size 1: 3 MessagesSeperators, 1 DataMessage and no terminating MessageSeperator - Returns no message")]
        [TestCase(5, MessageSeperator + MessageSeperator + MessageSeperator + DataMessage, "", Description = "Buffer Size 5: 3 MessagesSeperators, 1 DataMessage and no terminating MessageSeperator - Returns no message")]
        [TestCase(10, MessageSeperator + MessageSeperator + MessageSeperator + DataMessage, "", Description = "Buffer Size 10: 3 MessagesSeperators, 1 DataMessage and no terminating MessageSeperator - Returns no message")]
        [TestCase(15, MessageSeperator + MessageSeperator + MessageSeperator + DataMessage, "", Description = "Buffer Size 15: 3 MessagesSeperators, 1 DataMessage and no terminating MessageSeperator - Returns no message")]
        [TestCase(100, MessageSeperator + MessageSeperator + MessageSeperator + DataMessage, "", Description = "Buffer Size 100: 3 MessagesSeperators, 1 DataMessage and no terminating MessageSeperator - Returns no message")]
        [TestCase(150, MessageSeperator + MessageSeperator + MessageSeperator + DataMessage, "", Description = "Buffer Size 150: 3 MessagesSeperators, 1 DataMessage and no terminating MessageSeperator - Returns no message")]
        [TestCase(1000, MessageSeperator + MessageSeperator + MessageSeperator + DataMessage, "", Description = "Buffer Size 1000: 3 MessagesSeperators, 1 DataMessage and no terminating MessageSeperator - Returns no message")]
        [TestCase(1500, MessageSeperator + MessageSeperator + MessageSeperator + DataMessage, "", Description = "Buffer Size 1500: 3 MessagesSeperators, 1 DataMessage and no terminating MessageSeperator - Returns no message")]
        [TestCase(1, MessageSeperator + MessageSeperator + MessageSeperator + DataMessage + MessageSeperator, DataMessage, Description = "Buffer Size 1: 3 MessagesSeperators, 1 DataMessage and terminating MessageSeperator - Returns DataMessage")]
        [TestCase(5, MessageSeperator + MessageSeperator + MessageSeperator + DataMessage + MessageSeperator, DataMessage, Description = "Buffer Size 5: 3 MessagesSeperators, 1 DataMessage and terminating MessageSeperator - Returns DataMessage")]
        [TestCase(10, MessageSeperator + MessageSeperator + MessageSeperator + DataMessage + MessageSeperator, DataMessage, Description = "Buffer Size 10: 3 MessagesSeperators, 1 DataMessage and terminating MessageSeperator - Returns DataMessage")]
        [TestCase(15, MessageSeperator + MessageSeperator + MessageSeperator + DataMessage + MessageSeperator, DataMessage, Description = "Buffer Size 15: 3 MessagesSeperators, 1 DataMessage and terminating MessageSeperator - Returns DataMessage")]
        [TestCase(100, MessageSeperator + MessageSeperator + MessageSeperator + DataMessage + MessageSeperator, DataMessage, Description = "Buffer Size 100: 3 MessagesSeperators, 1 DataMessage and terminating MessageSeperator - Returns DataMessage")]
        [TestCase(150, MessageSeperator + MessageSeperator + MessageSeperator + DataMessage + MessageSeperator, DataMessage, Description = "Buffer Size 150: 3 MessagesSeperators, 1 DataMessage and terminating MessageSeperator - Returns DataMessage")]
        [TestCase(1000, MessageSeperator + MessageSeperator + MessageSeperator + DataMessage + MessageSeperator, DataMessage, Description = "Buffer Size 1000: 3 MessagesSeperators, 1 DataMessage and terminating MessageSeperator - Returns DataMessage")]
        [TestCase(1500, MessageSeperator + MessageSeperator + MessageSeperator + DataMessage + MessageSeperator, DataMessage, Description = "Buffer Size 1500: 3 MessagesSeperators, 1 DataMessage and terminating MessageSeperator - Returns DataMessage")]
        [TestCase(1, MessageSeperator + MessageSeperator + MessageSeperator + DataMessage + MessageSeperator + MessageSeperator, DataMessage, Description = "Buffer Size 1: 3 MessagesSeperators, 1 DataMessage and 2 terminating MessageSeperator - Returns DataMessage")]
        [TestCase(5, MessageSeperator + MessageSeperator + MessageSeperator + DataMessage + MessageSeperator + MessageSeperator, DataMessage, Description = "Buffer Size 5: 3 MessagesSeperators, 1 DataMessage and 2 terminating MessageSeperator - Returns DataMessage")]
        [TestCase(10, MessageSeperator + MessageSeperator + MessageSeperator + DataMessage + MessageSeperator + MessageSeperator, DataMessage, Description = "Buffer Size 10: 3 MessagesSeperators, 1 DataMessage and 2 terminating MessageSeperator - Returns DataMessage")]
        [TestCase(15, MessageSeperator + MessageSeperator + MessageSeperator + DataMessage + MessageSeperator + MessageSeperator, DataMessage, Description = "Buffer Size 15: 3 MessagesSeperators, 1 DataMessage and 2 terminating MessageSeperator - Returns DataMessage")]
        [TestCase(100, MessageSeperator + MessageSeperator + MessageSeperator + DataMessage + MessageSeperator + MessageSeperator, DataMessage, Description = "Buffer Size 100: 3 MessagesSeperators, 1 DataMessage and 2 terminating MessageSeperator - Returns DataMessage")]
        [TestCase(150, MessageSeperator + MessageSeperator + MessageSeperator + DataMessage + MessageSeperator + MessageSeperator, DataMessage, Description = "Buffer Size 150: 3 MessagesSeperators, 1 DataMessage and 2 terminating MessageSeperator - Returns DataMessage")]
        [TestCase(1000, MessageSeperator + MessageSeperator + MessageSeperator + DataMessage + MessageSeperator + MessageSeperator, DataMessage, Description = "Buffer Size 1000: 3 MessagesSeperators, 1 DataMessage and 2 terminating MessageSeperator - Returns DataMessage")]
        [TestCase(1500, MessageSeperator + MessageSeperator + MessageSeperator + DataMessage + MessageSeperator + MessageSeperator, DataMessage, Description = "Buffer Size 1500: 3 MessagesSeperators, 1 DataMessage and 2 terminating MessageSeperator - Returns DataMessage")]
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

        [TestCase(1, Description = "Buffer Size 1: ")]
        [TestCase(5, Description = "Buffer Size 5: ")]
        [TestCase(10, Description = "Buffer Size 10: ")]
        [TestCase(15, Description = "Buffer Size 15: ")]
        [TestCase(100, Description = "Buffer Size 100: ")]
        [TestCase(150, Description = "Buffer Size 150: ")]
        [TestCase(1000, Description = "Buffer Size 1000: ")]
        [TestCase(1500, Description = "Buffer Size 1500: ")]
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

        [TestCase(1, Description = "Buffer Size 1: ")]
        [TestCase(5, Description = "Buffer Size 5: ")]
        [TestCase(10, Description = "Buffer Size 10: ")]
        [TestCase(15, Description = "Buffer Size 15: ")]
        [TestCase(100, Description = "Buffer Size 100: ")]
        [TestCase(150, Description = "Buffer Size 150: ")]
        [TestCase(1000, Description = "Buffer Size 1000: ")]
        [TestCase(1500, Description = "Buffer Size 1500: ")]
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

        [TestCase(1, Description = "Buffer Size 1: ")]
        [TestCase(5, Description = "Buffer Size 5: ")]
        [TestCase(10, Description = "Buffer Size 10: ")]
        [TestCase(15, Description = "Buffer Size 15: ")]
        [TestCase(100, Description = "Buffer Size 100: ")]
        [TestCase(150, Description = "Buffer Size 150: ")]
        [TestCase(1000, Description = "Buffer Size 1000: ")]
        [TestCase(1500, Description = "Buffer Size 1500: ")]
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
