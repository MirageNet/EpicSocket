using Cysharp.Threading.Tasks;
using Epic.OnlineServices;
using Epic.OnlineServices.Connect;
using PlayEveryWare.EpicOnlineServices;

namespace Mirage.Sockets.EpicSocket
{
    internal static class DeviceIdConnect
    {
        public static async UniTask Connect(EOSManager.EOSSingleton manager, ConnectInterface connectInterface, string displayName)
        {
            var createInfo = await CreateDeviceIdAsync(connectInterface);

            ThrowIfResultInvalid(createInfo);

            var loginInfo = await LoginAsync(manager, displayName);

            EpicLogger.logger.WarnResult("Login Callback", loginInfo.ResultCode);
        }

        private static void ThrowIfResultInvalid(CreateDeviceIdCallbackInfo createInfo)
        {
            if (createInfo.ResultCode == Result.Success)
                return;

            // already exists, this is ok
            if (createInfo.ResultCode == Result.DuplicateNotAllowed)
            {
                EpicLogger.logger.Log($"Device Id already exists");
                return;
            }

            if (createInfo.ResultCode != Result.Success && createInfo.ResultCode != Result.DuplicateNotAllowed)
                throw new EpicSocketException($"Failed to Create DeviceId, Result code: {createInfo.ResultCode}");
        }

        private static UniTask<CreateDeviceIdCallbackInfo> CreateDeviceIdAsync(ConnectInterface connect)
        {
            var createOptions = new CreateDeviceIdOptions()
            {
                // todo get device model
#if UNITY_EDITOR
                DeviceModel = "DemoModel_Editor"
#else
                DeviceModel = "DemoModel"
#endif
            };
            var waiter = new AsyncWaiter<CreateDeviceIdCallbackInfo>();
            connect.CreateDeviceId(createOptions, null, waiter.Callback);
            return waiter.Wait();
        }

        private static UniTask<LoginCallbackInfo> LoginAsync(EOSManager.EOSSingleton manager, string displayName)
        {
            var waiter = new AsyncWaiter<LoginCallbackInfo>();
            manager.StartConnectLoginWithDeviceToken(displayName, waiter.Callback);
            return waiter.Wait();
        }
    }
}

