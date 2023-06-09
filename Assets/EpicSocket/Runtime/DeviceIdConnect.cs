using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Epic.OnlineServices;
using Epic.OnlineServices.Connect;
using PlayEveryWare.EpicOnlineServices;
using UnityEngine;

namespace Mirage.Sockets.EpicSocket
{
    internal static class DeviceIdConnect
    {
        public static async UniTask Connect(EOSManager.EOSSingleton manager, ConnectInterface connectInterface, string displayName)
        {
            await CreateDeviceIdAsync(connectInterface);

            var loginInfo = await LoginAsync(manager, displayName);

            EpicLogger.logger.WarnResult("Login Callback", loginInfo.ResultCode);
        }

        private static async UniTask CreateDeviceIdAsync(ConnectInterface connect)
        {
            var createOptions = new CreateDeviceIdOptions()
            {
#if UNITY_EDITOR
                DeviceModel = SystemInfo.deviceModel + "_Editor"
#else
                DeviceModel = SystemInfo.deviceModel
#endif
            };
            var waiter = new AsyncWaiter<CreateDeviceIdCallbackInfo>();
            connect.CreateDeviceId(ref createOptions, null, waiter.Callback);
            var createInfo = await waiter.Wait();

            if (createInfo.ResultCode == Result.Success)
                return;

            if (createInfo.ResultCode == Result.DuplicateNotAllowed)
            {
                EpicLogger.logger.Log($"Device Id already exists");
                return;
            }

            if (createInfo.ResultCode != Result.Success && createInfo.ResultCode != Result.DuplicateNotAllowed)
                throw new EpicSocketException($"Failed to Create DeviceId, Result code: {createInfo.ResultCode}");
        }

        private static UniTask<LoginCallbackInfo> LoginAsync(EOSManager.EOSSingleton manager, string displayName)
        {
            var waiter = new AsyncWaiter<LoginCallbackInfo>();
            manager.StartConnectLoginWithDeviceToken(displayName, waiter.Callback);
            return waiter.Wait();
        }
    }
}

