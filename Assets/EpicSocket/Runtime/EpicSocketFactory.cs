using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Epic.OnlineServices;
using Epic.OnlineServices.P2P;
using Mirage.Logging;
using Mirage.SocketLayer;
using PlayEveryWare.EpicOnlineServices;
using UnityEngine;
using UnityEngine.Assertions;

namespace Mirage.Sockets.EpicSocket
{
    public class EpicSocketFactory : SocketFactory
    {
        [Header("Dev Auth")]
        public bool UseDevAuth;
        public DevAuthSettings DevAuth;

        [Header("Logging")]
        public Epic.OnlineServices.Logging.LogLevel LogLevel = Epic.OnlineServices.Logging.LogLevel.Warning;

        private static InitializeStatus s_status;
        private RelayHandle _relayHandle;
        private CommandHandler _commandHandler;

        private void Update()
        {
            if (_relayHandle != null && _relayHandle.CheckOpen())
                _commandHandler?.Update();
        }

        public void Initialize(Action<InitializeResult> callback, DevAuthSettings? devAuth, string displayName = null)
        {
            UniTask.Void(async () =>
            {
                InitializeResult result = default;
                try
                {
                    await InitializeAsync(devAuth, displayName);
                }
                catch (Exception e)
                {
                    result.Exception = e;
                }

                callback.Invoke(result);
            });
        }

        /// <summary>
        /// Call this before starting Mirage
        /// </summary>
        /// <returns></returns>
        public UniTask InitializeAsync(string displayName = null)
        {
            return InitializeAsync((UseDevAuth ? DevAuth : default(Nullable<DevAuthSettings>)), displayName);
        }

        /// <summary>
        /// Call this before starting Mirage
        /// </summary>
        /// <returns></returns>
        public async UniTask InitializeAsync(DevAuthSettings? devAuth, string displayName = null)
        {
            if (s_status == InitializeStatus.Initialized)
            {
                Debug.LogWarning("Already Initialize");
                return;
            }
            if (s_status == InitializeStatus.Initializing)
            {
                while (s_status == InitializeStatus.Initializing)
                    await UniTask.Yield();

                return;
            }

            s_status = InitializeStatus.Initializing;


            checkName(ref displayName);

            // add if missing
            if (!TryGetComponent(out EOSManager manager))
            {
                manager = gameObject.AddComponent<EOSManager>();
                // log must be called after init
                Epic.OnlineServices.Logging.LoggingInterface.SetLogLevel(Epic.OnlineServices.Logging.LogCategory.AllCategories, LogLevel);
            }

            // wait for sdk to finish
            while (!EpicHelper.IsLoaded())
                await UniTask.Yield();

            if (devAuth.HasValue)
            {
                await DevAuthLogin.LoginAndConnect(devAuth.Value);
            }
            else
            {
                await DeviceIdConnect.Connect(EOSManager.Instance, EOSManager.Instance.GetEOSConnectInterface(), displayName);
            }

            // todo do we need this?
            ChangeRelayStatus();

            var productId = EOSManager.Instance.GetProductUserId();
            Debug.Log($"<color=cyan>Connected to EOS, localUser={productId}, isNull={productId == null}</color>");

            s_status = InitializeStatus.Initialized;
        }

        public async UniTask StartAsClient(NetworkClient client, ProductUserId remoteHost)
        {
            if (s_status != InitializeStatus.Initialized)
                throw new InvalidOperationException("Most be Initialized before calling Start");

            _relayHandle = new RelayHandle(EOSManager.Instance);
            _relayHandle.OpenRelay();
            _relayHandle.ConnectToRemoteUser(remoteHost);

            _commandHandler = new CommandHandler(_relayHandle);

            // send join request to host
            _relayHandle.SendCommand(remoteHost, CommandHandler.REQUEST_JOIN);

            // wait for reply
            var waiter = new AsyncWaiter<ProductUserId>();
            _commandHandler.AddHandler(CommandHandler.ACCEPT_JOIN, waiter.Callback);
            var other = await waiter.Wait();
            Assert.AreEqual(remoteHost, other);

            client.Connect();
        }

        public void StartAsHost(NetworkServer server, NetworkClient client)
        {
            if (s_status != InitializeStatus.Initialized)
                throw new InvalidOperationException("Most be Initialized before calling Start");

            _relayHandle = new RelayHandle(EOSManager.Instance);
            _relayHandle.OpenRelay();

            _commandHandler = new CommandHandler(_relayHandle);

            // listen for join requests from users
            _commandHandler.AddHandler(CommandHandler.REQUEST_JOIN, AcceptUser);

            server.StartServer(client);
        }

        private void AcceptUser(ProductUserId remoteUser)
        {
            // accept user once they request
            _relayHandle.SendCommand(remoteUser, CommandHandler.ACCEPT_JOIN);
        }

        private static void checkName(ref string displayName)
        {
            if (string.IsNullOrEmpty(displayName))
                displayName = "Default User " + UnityEngine.Random.Range(0, 1000).ToString().PadLeft(4, '0');
        }

        private void ChangeRelayStatus()
        {
            var setRelayControlOptions = new SetRelayControlOptions();
            setRelayControlOptions.RelayControl = RelayControl.AllowRelays;

            var result = EOSManager.Instance.GetEOSP2PInterface().SetRelayControl(setRelayControlOptions);
            EpicLogger.logger.WarnResult("Set Relay Controls", result);
        }


        #region SocketFactory overrides
        public override int MaxPacketSize => P2PInterface.MaxPacketSize;

        public override ISocket CreateServerSocket()
        {
            if (_relayHandle == null || !_relayHandle.IsOpen)
                throw new InvalidOperationException("Relay not active, Call Initialize first");

            return new EpicSocket(_relayHandle);
        }

        public override ISocket CreateClientSocket()
        {
            if (_relayHandle == null || !_relayHandle.IsOpen)
                throw new InvalidOperationException("Relay not active, Call Initialize first");

            return new EpicSocket(_relayHandle);
        }

        public override IEndPoint GetBindEndPoint()
        {
            return new EpicEndPoint();
        }

        public override IEndPoint GetConnectEndPoint(string address = null, ushort? port = null)
        {
            // need to pass host user to endpoint here, so that peer has the user when it creates copy of endPoint
            return new EpicEndPoint()
            {
                UserId = _relayHandle.RemoteUser
            };
        }
        #endregion

        private enum InitializeStatus
        {
            None,
            Initializing,
            Initialized,
        }

        public struct InitializeResult
        {
            /// <summary>
            /// Exception thrown by Async task.
            /// </summary>
            public Exception Exception;

            public bool Successful => Exception == null;
        }
    }

    internal class CommandHandler
    {
        public const int REQUEST_JOIN = 0;
        public const int ACCEPT_JOIN = 1;
        private readonly Dictionary<int, Action<ProductUserId>> handlers = new Dictionary<int, Action<ProductUserId>>();
        private readonly RelayHandle _relayHandle;

        public CommandHandler(RelayHandle relayHandle)
        {
            _relayHandle = relayHandle;
        }

        public void AddHandler(int command, Action<ProductUserId> handle)
        {
            handlers.Add(command, handle);
        }

        public void Update()
        {
            while (_relayHandle.ReceiveCommand(out var packet))
            {
                var opcode = packet.data[0];
                if (handlers.TryGetValue(opcode, out var handler))
                {
                    handler.Invoke(packet.userId);
                }
                else
                {
                    EpicLogger.logger.LogWarning($"Unknown command {opcode}");
                }
            }
        }
    }
}

