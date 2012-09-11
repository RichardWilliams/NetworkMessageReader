using System;
using System.Net.Sockets;
using System.Reactive.Linq;
using ParameterValidator;

namespace NetworkMessageReader
{
    public class TcpReader
    {
        private readonly IMessageParser _messageParser;
        private readonly int _bufferSize;
        private Func<byte[], int, int, IObservable<int>> _readDataFunc;
        private readonly NetworkStream _networkStream;

        public TcpReader(TcpClient tcpClient, IMessageParser messageParser, int bufferSize)
        {
            ParameterValidator.ParameterValidator.EnsureParametersAreValid(new NullValidatorWithValue<TcpClient>(() => tcpClient, tcpClient),
                                                                           new NullValidatorWithValue<IMessageParser>(() => messageParser, messageParser),
                                                                           new MinValueValidatorWithValue<int>(() => bufferSize, bufferSize, 1));
            _messageParser = messageParser;
            _bufferSize = bufferSize;
            if (tcpClient.Connected == false)
                throw new InvalidOperationException("The tcpClient is not connected.");

            _networkStream = tcpClient.GetStream();
        }

        public IObservable<string> Read()
        {
            return Observable.Create<string>(observer => ReadBytes().Subscribe(bytesRead => _messageParser.Parse(bytesRead, observer), observer.OnCompleted));
        }

        private IObservable<DataChunk> ReadBytes()
        {
            _readDataFunc = Observable.FromAsyncPattern<byte[], int, int, int>(_networkStream.BeginRead, _networkStream.EndRead);
            var buffer = new byte[_bufferSize];
            return Observable
                    .Defer(() => _readDataFunc(buffer, 0, _bufferSize))
                    .Repeat()
                    .TakeWhile(bytesRead => bytesRead != 0)
                    .Select(bufferSize => new DataChunk(buffer, bufferSize));
        }  
    }
}
