using Cysharp.Threading.Tasks;
using Epic.OnlineServices;
using Epic.OnlineServices.Connect;
using PlayEveryWare.EpicOnlineServices;
using UnityEngine;

namespace Mirage.Sockets.EpicSocket
{
    [System.Serializable]
    public struct DevAuthSettings
    {
        public string CredentialName;
        public int Port;
    }

    internal static class DevAuthLogin
    {
        public static async UniTask LoginAndConnect(DevAuthSettings settings)
        {
            // we must authenticate first,
            // and then connect to relay
            var user = await LogInWithDevAuth(settings);

            await Connect(user);
        }

        private static async UniTask<EpicAccountId> LogInWithDevAuth(DevAuthSettings settings)
        {
            var type = Epic.OnlineServices.Auth.LoginCredentialType.Developer;
            var id = $"localhost:{settings.Port}";
            var token = settings.CredentialName;

            var waiter = new AsyncWaiter<Epic.OnlineServices.Auth.LoginCallbackInfo>();
            EOSManager.Instance.StartLoginWithLoginTypeAndToken(type, id, token, waiter.Callback);
            var result = await waiter.Wait();

            var epicAccountId = result.LocalUserId;
            if (result.ResultCode == Result.InvalidUser)
                epicAccountId = await CreateNewAccount(result.ContinuanceToken);
            else if (result.ResultCode != Result.Success)
                throw new EpicSocketException($"Failed to login with Dev auth, result code={result.ResultCode}");

            return epicAccountId;
        }

        private static async UniTask<EpicAccountId> CreateNewAccount(ContinuanceToken continuanceToken)
        {
            Debug.Log($"Trying Auth link with external account: {continuanceToken}");

            var waiter = new AsyncWaiter<Epic.OnlineServices.Auth.LinkAccountCallbackInfo>();
            EOSManager.Instance.AuthLinkExternalAccountWithContinuanceToken(continuanceToken, Epic.OnlineServices.Auth.LinkAccountFlags.NoFlags, waiter.Callback);
            var result = await waiter.Wait();
            if (result.ResultCode != Result.Success)
                throw new EpicSocketException($"Failed to login with Dev auth, result code={result.ResultCode}");

            EpicLogger.Verbose($"Create New Account: [User:{result.ResultCode} Selected:{result.SelectedAccountId}]");
            return result.LocalUserId;
        }

        private static async UniTask Connect(EpicAccountId user)
        {
            var firstTry = await _connect(user);

            var result = firstTry.ResultCode;
            if (result == Result.InvalidUser)
            {
                // ask user if they want to connect; sample assumes they do
                var createWaiter = new AsyncWaiter<CreateUserCallbackInfo>();
                EOSManager.Instance.CreateConnectUserWithContinuanceToken(firstTry.ContinuanceToken, createWaiter.Callback);
                var createResult = await createWaiter.Wait();

                Debug.Log("Created new account");

                var secondTry = await _connect(user);
                result = secondTry.ResultCode;
            }

            if (result != Result.Success)
                throw new EpicSocketException($"Failed to login with Dev auth, result code={result}");
        }

        private static async UniTask<LoginCallbackInfo> _connect(EpicAccountId user)
        {
            var waiter = new AsyncWaiter<LoginCallbackInfo>();
            EOSManager.Instance.StartConnectLoginWithEpicAccount(user, waiter.Callback);
            var result = await waiter.Wait();
            return result;
        }
    }
}

