using System;
using Epic.OnlineServices.P2P;
using Mirage.Logging;
using Mirage.SocketLayer;
using UnityEngine;

namespace Mirage.Sockets.EpicSocket
{
    internal sealed class EpicSocket : ISocket
    {
        private bool _isClosed;
        private RelayHandle _relayHandle;
        private SendPacketOptions _sendOptions;
        private ReceivePacketOptions _receiveOptions;
        private int _lastTickedFrame;
        private ReceivedPacket _nextPacket;
        private EpicEndPoint _receiveEndPoint;

        public EpicSocket(RelayHandle relayHandle)
        {
            _relayHandle = relayHandle ?? throw new ArgumentNullException(nameof(relayHandle));
        }

        private void ThrowIfRelayNotActive()
        {
            if (!_relayHandle.IsOpen)
                throw new InvalidOperationException("Relay not open, can not start socket");
        }

        public void Bind(IBindEndPoint endPoint)
        {
            ThrowIfRelayNotActive();
            _receiveEndPoint = (EpicEndPoint)endPoint;
        }

        public IConnectionHandle Connect(IConnectEndPoint endPoint)
        {
            ThrowIfRelayNotActive();
            _receiveEndPoint = (EpicEndPoint)endPoint;
            return _receiveEndPoint;
        }

        public void Close()
        {
            _relayHandle?.CloseRelay();
            _relayHandle = null;
            _isClosed = true;
        }

        private bool IsOpenAndLoaded()
        {
            if (_isClosed)
                return false;

            if (_relayHandle.CheckOpen())
            {
                return true;
            }
            else
            {
                EpicLogger.logger.LogError("Calling when when EOS is not loaded, Closing socket");
                Close();
                return false;
            }
        }

        public bool Poll()
        {
            if (!IsOpenAndLoaded()) return false;

            // first time this tick?
            if (_lastTickedFrame != Time.frameCount)
            {
                _relayHandle.Manager.Tick();
                _lastTickedFrame = Time.frameCount;
            }

            Debug.Assert(_nextPacket.data == null);
            return _relayHandle.ReceiveGameData(out _nextPacket);
        }

        public int Receive(Span<byte> outBuffer, out IConnectionHandle handle)
        {
            Debug.Assert(_nextPacket.data != null);

            _nextPacket.data.AsSpan().CopyTo(outBuffer);

            _receiveEndPoint.UserId = _nextPacket.userId;
            handle = _receiveEndPoint;
            var length = _nextPacket.data.Count;

            EpicLogger.Verbose($"Receive {length} bytes from {_nextPacket.userId}");

            // clear refs
            _nextPacket = default;
            return length;
        }

        public void Send(IConnectionHandle handle, ReadOnlySpan<byte> packet)
        {
            if (!IsOpenAndLoaded()) return;

            var endPoint = (EpicEndPoint)handle;

            // send option has no length field, we have to copy to new array
            // todo avoid allocation
            var data = packet.ToArray();
            _relayHandle.SendGameData(endPoint.UserId, data);

            EpicLogger.Verbose($"Send {packet.Length} bytes to {endPoint.UserId}");
        }

        void ISocket.SetTickEvents(int maxPacketSize, OnData onData, OnDisconnect onDisconnect) { }
        void ISocket.Tick() { }
        void ISocket.Flush() { }
    }
}

