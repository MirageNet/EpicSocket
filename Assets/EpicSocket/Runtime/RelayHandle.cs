using System;
using Epic.OnlineServices;
using Epic.OnlineServices.P2P;
using Mirage.Logging;
using PlayEveryWare.EpicOnlineServices;
using UnityEngine.Assertions;

namespace Mirage.Sockets.EpicSocket
{
    internal struct ReceivedPacket
    {
        public ProductUserId userId;
        public ArraySegment<byte> data;
    }

    internal class RelayHandle
    {
        private const int CHANNEL_GAME = 0;
        private const int CHANNEL_COMMAND = 1;

        public readonly EOSManager.EOSSingleton Manager;
        public readonly P2PInterface P2P;
        public readonly ProductUserId LocalUser;
        private ulong _openId;
        private ulong _closeId;
        private ulong _establishedId;
        private ulong _queueFullId;
        private ProductUserId _remoteUser;
        private SendPacketOptions _sendOptions;
        private ReceivePacketOptions _receiveOptions;
        private byte[] _singleByteCommand = new byte[1];
        private byte[] _receiveBuffer = new byte[P2PInterface.MaxPacketSize];

        public bool IsOpen { get; private set; }
        /// <summary>User that is hosting relay</summary>
        public ProductUserId RemoteUser => _remoteUser;

        private static RelayHandle s_instance;

        public static RelayHandle GetOrCreate(EOSManager.EOSSingleton manager)
        {
            if (s_instance == null)
            {
                s_instance = new RelayHandle(manager);
            }
            Assert.AreEqual(manager, s_instance.Manager);

            return s_instance;
        }

        public RelayHandle(EOSManager.EOSSingleton manager)
        {
            Manager = manager;
            P2P = manager.GetEOSP2PInterface();
            LocalUser = manager.GetProductUserId();
        }

        public void OpenRelay()
        {
            if (IsOpen)
                throw new InvalidOperationException("Already open");

            IsOpen = true;
            enableRelay(connectionRequestCallback, connectionClosedCallback);
            _sendOptions = createSendOptions();
            _receiveOptions = createReceiveOptions();
        }

        public void CloseRelay()
        {
            if (!IsOpen)
                return;
            IsOpen = false;

            DisableRelay();
        }

        /// <summary>
        /// Limit incoming message from a single user
        /// <para>Useful for clients to only receive messager from Host</para>
        /// </summary>
        /// <param name="remoteUser"></param>
        public void ConnectToRemoteUser(ProductUserId remoteUser)
        {
            if (_remoteUser != null && _remoteUser != remoteUser)
                throw new InvalidOperationException("Already connected to another host");

            _remoteUser = remoteUser ?? throw new ArgumentNullException(nameof(remoteUser));
        }

        private void connectionRequestCallback(ref OnIncomingConnectionRequestInfo data)
        {
            var validHost = checkRemoteUser(data.RemoteUserId);
            if (!validHost)
            {
                EpicLogger.logger.LogError($"User ({data.RemoteUserId}) tried to connect to client");
                return;
            }

            var options = new AcceptConnectionOptions()
            {
                LocalUserId = LocalUser,
                RemoteUserId = data.RemoteUserId,
                // todo do we need to need to create new here
                SocketId = createSocketId()
            };
            var result = P2P.AcceptConnection(ref options);
            EpicLogger.logger.WarnResult("Accept Connection", result);
        }

        private bool checkRemoteUser(ProductUserId remoteUser)
        {
            // host remote user, no need to check it
            if (_remoteUser == null)
                return true;

            // check incoming message is from expected user
            return _remoteUser == remoteUser;
        }

        private void connectionClosedCallback(ref OnRemoteConnectionClosedInfo data)
        {
            // if we have set remoteUser, then close is probably from them, so we want to close the socket
            if (_remoteUser != null)
            {
                CloseRelay();
            }

            if (EpicLogger.logger.WarnEnabled()) EpicLogger.logger.LogWarning($"Connection closed with reason: {data.Reason}");
        }


        /// <summary>
        /// Starts relay as server, allows new connections
        /// </summary>
        /// <param name="notifyId"></param>
        /// <param name="socketName"></param>
        private void enableRelay(OnIncomingConnectionRequestCallback openCallback, OnRemoteConnectionClosedCallback closedCallback)
        {
            //EpicHelper.WarnResult("SetPacketQueueSize", p2p.SetPacketQueueSize(new SetPacketQueueSizeOptions { IncomingPacketQueueMaxSizeBytes = 64000, OutgoingPacketQueueMaxSizeBytes = 64000 }));
            //EpicHelper.WarnResult("SetRelayControl", p2p.SetRelayControl(new SetRelayControlOptions { RelayControl = RelayControl.ForceRelays }));

            var requestOption = new AddNotifyPeerConnectionRequestOptions { LocalUserId = LocalUser, };
            AddHandle(ref _openId, P2P.AddNotifyPeerConnectionRequest(ref requestOption, null, (ref OnIncomingConnectionRequestInfo info) =>
            {
                EpicLogger.Verbose($"Connection Request [User:{info.RemoteUserId} Socket:{info.SocketId}]");
                openCallback.Invoke(ref info);
            }));

            var establishedOptions = new AddNotifyPeerConnectionEstablishedOptions { LocalUserId = LocalUser, };
            AddHandle(ref _openId, P2P.AddNotifyPeerConnectionEstablished(ref establishedOptions, null, (ref OnPeerConnectionEstablishedInfo info) =>
            {
                EpicLogger.Verbose($"Connection Established: [User:{info.RemoteUserId} Socket:{info.SocketId} Type:{info.ConnectionType}]");
            }));


            var closedOptions = new AddNotifyPeerConnectionClosedOptions { LocalUserId = LocalUser, };
            AddHandle(ref _openId, P2P.AddNotifyPeerConnectionClosed(ref closedOptions, null, (ref OnRemoteConnectionClosedInfo info) =>
            {
                EpicLogger.Verbose($"Connection Closed [User:{info.RemoteUserId} Socket:{info.SocketId} Reason:{info.Reason}]");
                closedCallback.Invoke(ref info);
            }));


            var queueFullOptions = new AddNotifyIncomingPacketQueueFullOptions { };
            AddHandle(ref _openId, P2P.AddNotifyIncomingPacketQueueFull(ref queueFullOptions, null, (ref OnIncomingPacketQueueFullInfo info) =>
            {
                EpicLogger.Verbose($"Incoming Packet Queue Full");
            }));
        }

        private void DisableRelay()
        {
            // todo do we need to call close on p2p?
            // only disable if sdk is loaded
            if (!EpicHelper.IsLoaded())
                return;

            RemoveHandle(ref _openId, P2P.RemoveNotifyPeerConnectionRequest);
            RemoveHandle(ref _closeId, P2P.RemoveNotifyPeerConnectionClosed);
            RemoveHandle(ref _establishedId, P2P.RemoveNotifyPeerConnectionEstablished);
            RemoveHandle(ref _queueFullId, P2P.RemoveNotifyIncomingPacketQueueFull);
        }

        private static void AddHandle(ref ulong handle, ulong value)
        {
            if (value == Common.InvalidNotificationid)
                throw new EpicSocketException("Handle was invalid");

            handle = value;
        }

        private static void RemoveHandle(ref ulong handle, Action<ulong> removeAction)
        {
            if (handle != Common.InvalidNotificationid)
            {
                removeAction.Invoke(handle);
                handle = Common.InvalidNotificationid;
            }
        }

        private SocketId createSocketId()
        {
            return new SocketId() { SocketName = "Game" };
        }

        private SendPacketOptions createSendOptions()
        {
            return new SendPacketOptions()
            {
                AllowDelayedDelivery = true,
                Channel = 0,
                LocalUserId = LocalUser,
                Reliability = PacketReliability.UnreliableUnordered,
                SocketId = createSocketId(),

                RemoteUserId = null,
                Data = null,
            };
        }

        private ReceivePacketOptions createReceiveOptions()
        {
            return new ReceivePacketOptions
            {
                LocalUserId = LocalUser,
                MaxDataSizeBytes = P2PInterface.MaxPacketSize,
                RequestedChannel = 0,
            };
        }


        public bool CheckOpen()
        {
            // if handle is open and Eos is loaded
            return IsOpen && EpicHelper.IsLoaded();
        }


        // todo find way to send byte[] with length
        public void SendGameData(ProductUserId userId, byte[] data)
        {
            _sendOptions.Channel = CHANNEL_GAME;
            _sendOptions.Data = data;
            _sendOptions.RemoteUserId = userId;
            _sendOptions.Reliability = PacketReliability.UnreliableUnordered;
            sendUsingOptions();
        }

        public void SendCommand(ProductUserId userId, byte opcode)
        {
            _sendOptions.Channel = CHANNEL_COMMAND;
            _singleByteCommand[0] = opcode;
            _sendOptions.Data = _singleByteCommand;
            _sendOptions.RemoteUserId = userId;
            _sendOptions.Reliability = PacketReliability.ReliableUnordered;
            sendUsingOptions();
        }

        private void sendUsingOptions()
        {
            // check client is only sending to Host
            if (_remoteUser != null)
            {
                Assert.AreEqual(_remoteUser, _sendOptions.RemoteUserId);
            }

            var result = P2P.SendPacket(ref _sendOptions);
            EpicLogger.logger.WarnResult("Send Packet", result);
        }

        public bool ReceiveGameData(out ReceivedPacket receivedPacket)
        {
            _receiveOptions.RequestedChannel = CHANNEL_GAME;
            return receiveUsingOptions(out receivedPacket);
        }

        public bool ReceiveCommand(out ReceivedPacket receivedPacket)
        {
            _receiveOptions.RequestedChannel = CHANNEL_COMMAND;
            return receiveUsingOptions(out receivedPacket);
        }

        private bool receiveUsingOptions(out ReceivedPacket receivedPacket)
        {
            var result = P2P.ReceivePacket(ref _receiveOptions, out var userID, out var socketId, out var outChannel, new ArraySegment<byte>(_receiveBuffer), out var outBytesWritten);

            if (result != Result.Success && result != Result.NotFound) // log for results other than Success/NotFound
                EpicLogger.logger.WarnResult("Receive Packet", result);

            if (result == Result.Success)
            {
                receivedPacket = new ReceivedPacket
                {
                    data = new ArraySegment<byte>(_receiveBuffer, 0, (int)outBytesWritten),
                    userId = userID,
                };
                return true;
            }
            else
            {
                receivedPacket = default;
                return false;
            }
        }
    }
}

