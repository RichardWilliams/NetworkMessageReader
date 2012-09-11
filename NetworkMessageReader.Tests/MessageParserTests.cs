using System;
using System.Reactive.Subjects;
using System.Text;
using NUnit.Framework;

namespace NetworkMessageReader.Tests
{
    [TestFixture]
    public class MessageParserTests
    {
        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public void MessageParser_WhenSeperatorIsNull_ThrowsException()
        {
            new MessageParser(null);
        }

        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public void MessageParser_WhenSeperatorIsEmpty_ThrowsException()
        {
            new MessageParser(string.Empty);
        }

        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Parse_WhenDataChunkIsNull_ThrowsException()
        {
            var messageParser = new MessageParser("<Seperator>");
            messageParser.Parse(null, null);
        }

        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Parse_WhenObserverIsNull_ThrowsException()
        {
            var messageParser = new MessageParser("<Seperator>");
            messageParser.Parse(new DataChunk(new byte[1], 1), null);
        }

        [Test]
        public void Parse_WhenDataChunkContainsNoSeperator_ThenNothingIsObserved()
        {
            //Arrange
            const string dataToSend = "Hello World!";
            const string seperator = "<seperator>";
            const string dataToSendIncludingSeperator = dataToSend + seperator;
            var dataToSendInBytes = Encoding.UTF8.GetBytes(dataToSendIncludingSeperator);
            var dataToSendInBytesCount = Encoding.UTF8.GetByteCount(dataToSendIncludingSeperator);
            var messageParser = new MessageParser(seperator);
            var subject = new Subject<string>();
            var dataRead = string.Empty;
            subject.Subscribe(testDataMessage => dataRead = testDataMessage);

            //Act
            messageParser.Parse(new DataChunk(dataToSendInBytes, dataToSendInBytesCount), subject);
            
            //Assert
            Assert.AreEqual(dataToSend, dataRead);
        }
    }
}