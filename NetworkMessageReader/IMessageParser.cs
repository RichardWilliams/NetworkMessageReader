using System;

namespace NetworkMessageReader
{
    public interface IMessageParser
    {
        void Parse(DataChunk dataChunk, IObserver<string> observer);
    }
}