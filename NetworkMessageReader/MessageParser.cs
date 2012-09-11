using System;
using System.Linq;
using System.Text;
using ParameterValidator;

namespace NetworkMessageReader
{
    public class MessageParser : IMessageParser
    {
        private readonly string _seperator;
        private string _remainingFromPrevious;

        public MessageParser(string seperator)
        {
            ParameterValidator.ParameterValidator.EnsureParametersAreValid(new StringValidatorWithValue(() => seperator, seperator));
            _seperator = seperator;
        }

        public void Parse(DataChunk dataChunk, IObserver<string> observer)
        {
            ParameterValidator.ParameterValidator.EnsureParametersAreValid(new NullValidatorWithValue<DataChunk>(() => dataChunk, dataChunk),
                                                                           new NullValidatorWithValue<IObserver<string>>(() => observer, observer));

            var exactSizedByteData = GetExactSizedByteData(dataChunk.Chunk, dataChunk.Size);
            var exactSizedByteDataAsString = ConvertBytesToString(exactSizedByteData);
            LookForMessageAndNotifyObserver(exactSizedByteDataAsString, observer);
        }

        private byte[] GetExactSizedByteData(byte[] bytes, int numberOfBytes)
        {
            var exactSizedByteData = new byte[numberOfBytes];
            Array.Copy(bytes, exactSizedByteData, numberOfBytes);
            return exactSizedByteData;
        }

        private string ConvertBytesToString(byte[] bytes)
        {
            return Encoding.UTF8.GetString(bytes);
        }

        private void LookForMessageAndNotifyObserver(string newDataChunk, IObserver<string> observer)
        {
            var fullData = _remainingFromPrevious + newDataChunk;
            var splitData = fullData.Split(new[] { _seperator }, StringSplitOptions.None);
            var splitCount = splitData.Count();

            if (splitCount == 0 || splitCount == 1)
            {
                _remainingFromPrevious = fullData;
                return;
            }

            for (var i = 0; i < splitCount - 1; i++)
            {
                observer.OnNext(splitData[i]);
            }

            _remainingFromPrevious = splitData[splitCount - 1];
        }
    }
}