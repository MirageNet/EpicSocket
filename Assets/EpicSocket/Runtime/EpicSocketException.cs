using System;

namespace Mirage.Sockets.EpicSocket
{
    [Serializable]
    public class EpicSocketException : Exception
    {
        public EpicSocketException(string message) : base(message) { }
    }
}

