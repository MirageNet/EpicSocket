using Mirage.Authentication;
using UnityEngine;

namespace Mirage.Sockets.EpicSocket
{
    public class EpicNetworkAuthenticator : NetworkAuthenticator<EpicNetworkAuthenticator.AuthMessage>
    {
        [SerializeField] private NetworkClient _client;
        [SerializeField] private bool _automaticallyAuthenticate = true;

        protected override AuthenticationResult Authenticate(INetworkPlayer player, AuthMessage message)
        {
            var address = player.Address;

            var epicEndPoint = (EpicEndPoint)address;

            var user = epicEndPoint.UserId;
            return AuthenticationResult.CreateSuccess(this, user);
        }

        private void Awake()
        {
            _client.Connected.AddListener(OnClientConnected);
        }

        private void OnClientConnected(INetworkPlayer player)
        {
            if (!_automaticallyAuthenticate)
                return;

            // if we connect via epic, then we should send the auth message
            // this will cause server to authenticate the player via the product id by the userId they are using to connect
            var address = player.Address;
            if (address is EpicEndPoint)
            {
                SendAuthentication(_client, new AuthMessage());
            }
        }

        [NetworkMessage]
        public struct AuthMessage
        {
            // we dont need to send any thing, we will get the product id from the connection
        }
    }
}
