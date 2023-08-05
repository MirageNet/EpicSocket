using Mirage.Authentication;
using PlayEveryWare.EpicOnlineServices;
using UnityEngine;

namespace Mirage.Sockets.EpicSocket
{
    public class EpicNetworkAuthenticator : NetworkAuthenticator<EpicNetworkAuthenticator.AuthMessage>
    {
        [SerializeField] private NetworkClient _client;
        [SerializeField] private NetworkServer _server;
        [SerializeField] private bool _automaticallyAuthenticate = true;

        protected override AuthenticationResult Authenticate(INetworkPlayer player, AuthMessage message)
        {
            // host wont be using epic endpoint, so need to get user different way
            if (player == _server.LocalPlayer)
            {
                var localUser = EOSManager.Instance.GetProductUserId();
                return AuthenticationResult.CreateSuccess("host player, using local EOS user to authenticate", this, localUser);
            }
            else  // remote player
            {
                var address = player.Address;

                var epicEndPoint = (EpicEndPoint)address;

                var user = epicEndPoint.UserId;
                return AuthenticationResult.CreateSuccess(this, user);
            }
        }

        private void Awake()
        {
            _client.Connected.AddListener(OnClientConnected);
        }

        private void OnClientConnected(INetworkPlayer player)
        {
            if (!_automaticallyAuthenticate)
                return;

            // only send if client is using EpicSocketFactory
            if (_client.SocketFactory is not EpicSocketFactory)
                return;

            SendAuthentication(_client, new AuthMessage());
        }

        [NetworkMessage]
        public struct AuthMessage
        {
            // we dont need to send any thing, we will get the product id from the connection
        }
    }
}
