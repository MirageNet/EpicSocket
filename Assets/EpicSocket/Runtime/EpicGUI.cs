using System;
using Cysharp.Threading.Tasks;
using Epic.OnlineServices;
using UnityEngine;

namespace Mirage.Sockets.EpicSocket
{
    public class EpicGUI : MonoBehaviour
    {
        public NetworkManager Manager;
        public EpicSocketFactory EpicSocket;

        public string HostAddress;
        public bool UseDevAuth;
        public DevAuthSettings DevAuth;

        private bool isStarting;

        [Range(0.01f, 10f)]
        public float Scale = 1f;
        public TextAnchor GUIAnchor = TextAnchor.UpperLeft;
        //private Config config;

        private void Awake()
        {
            // debug values
            UseDevAuth = true;
#if UNITY_EDITOR
            DevAuth.CredentialName = "MirageEditor";
#else
            DevAuth.CredentialName = "MiragePlayer";
#endif


            Application.runInBackground = true;
            Application.targetFrameRate = 60;

            //config = new Config
            //{
            //    ConnectAttemptInterval = 0.5f,
            //    MaxConnectAttempts = 20,
            //};
        }

        private void OnGUI()
        {
            GUIUtility.ScaleAroundPivot(Vector2.one * Scale, GetPivotFromAnchor(GUIAnchor));

            var rect = GetRectFromAnchor(GUIAnchor, 100);
            using (new GUILayout.AreaScope(rect))
            {
                if (isStarting)
                {
                    GUILayout.Label("Is starting...");
                }
                else
                {
                    UseDevAuth = GUILayout.Toggle(UseDevAuth, "Use Dev Auth");
                    if (UseDevAuth)
                    {
                        using (new GUILayout.HorizontalScope())
                        {
                            GUILayout.Label("Credential");
                            DevAuth.CredentialName = GUILayout.TextField(DevAuth.CredentialName);
                        }
                    }

                    switch (Manager.NetworkMode)
                    {
                        case NetworkManagerMode.None:
                            StartHud();
                            break;
                        case NetworkManagerMode.Client:
                            ClientHud();
                            break;
                        case NetworkManagerMode.Server:
                        case NetworkManagerMode.Host:
                            HostHud();
                            break;
                    }
                }
            }
        }

        private void StartHud()
        {
            if (GUILayout.Button("Start Host"))
            {
                StartHost();
            }
            using (new GUILayout.HorizontalScope())
            {
                HostAddress = GUILayout.TextField(HostAddress);
                if (GUILayout.Button("Start Client"))
                {
                    StartClient(HostAddress);
                }
            }
        }

        private void HostHud()
        {
            if (GUILayout.Button("Stop"))
            {
                Manager.Server.Stop();
            }
        }

        private void ClientHud()
        {
            if (GUILayout.Button("Stop"))
            {
                Manager.Client.Disconnect();
            }
        }

        private void startWrapper(Action inner)
        {
            UniTask.Void(async () =>
            {
                isStarting = true;
                try
                {
                    DevAuthSettings? devAuth = default;
                    if (UseDevAuth)
                    {
                        devAuth = DevAuth;
                    }
                    await EpicSocket.InitializeAsync(devAuth, null);
                    inner.Invoke();
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
                finally
                {
                    isStarting = false;
                }
            });
        }
        public void StartHost()
        {
            startWrapper(() =>
            {
                EpicSocket.StartAsHost(Manager.Server, Manager.Client);
            });
        }

        public void StartClient(string hostProductId)
        {
            startWrapper(UniTask.Action(async () =>
            {
                var hostId = ProductUserId.FromString(hostProductId);
                await EpicSocket.StartAsClient(Manager.Client, hostId);
            }));
        }



        private const int WIDTH = 500;
        private const int PADDING_X = 10;
        private const int PADDING_Y = 10;

        private static Rect GetRectFromAnchor(TextAnchor anchor, int height)
        {
            switch (anchor)
            {
                case TextAnchor.UpperLeft:
                    return new Rect(PADDING_X, PADDING_Y, WIDTH, height);
                case TextAnchor.UpperCenter:
                    return new Rect(Screen.width / 2 - (WIDTH / 2), PADDING_Y, WIDTH, height);
                case TextAnchor.UpperRight:
                    return new Rect(Screen.width - (WIDTH + PADDING_X), PADDING_Y, WIDTH, height);
                case TextAnchor.MiddleLeft:
                    return new Rect(PADDING_X, Screen.height / 2 - (height / 2), WIDTH, height);
                case TextAnchor.MiddleCenter:
                    return new Rect(Screen.width / 2 - (WIDTH / 2), Screen.height / 2 - (height / 2), WIDTH, height);
                case TextAnchor.MiddleRight:
                    return new Rect(Screen.width - (WIDTH + PADDING_X), Screen.height / 2 - (height / 2), WIDTH, height);
                case TextAnchor.LowerLeft:
                    return new Rect(PADDING_X, Screen.height - (height + PADDING_Y), WIDTH, height);
                case TextAnchor.LowerCenter:
                    return new Rect(Screen.width / 2 - (WIDTH / 2), Screen.height - (height + PADDING_Y), WIDTH, height);
                default: // Lower right
                    return new Rect(Screen.width - (WIDTH + PADDING_X), Screen.height - (height + PADDING_Y), WIDTH, height);
            }
        }

        private static Vector2 GetPivotFromAnchor(TextAnchor anchor)
        {
            switch (anchor)
            {
                case TextAnchor.UpperLeft:
                    return Vector2.zero;
                case TextAnchor.UpperCenter:
                    return new Vector2(Screen.width / 2f, 0f);
                case TextAnchor.UpperRight:
                    return new Vector2(Screen.width, 0f);
                case TextAnchor.MiddleLeft:
                    return new Vector2(0f, Screen.height / 2f);
                case TextAnchor.MiddleCenter:
                    return new Vector2(Screen.width / 2f, Screen.height / 2f);
                case TextAnchor.MiddleRight:
                    return new Vector2(Screen.width, Screen.height / 2f);
                case TextAnchor.LowerLeft:
                    return new Vector2(0f, Screen.height);
                case TextAnchor.LowerCenter:
                    return new Vector2(Screen.width / 2f, Screen.height);
                default: // Lower right
                    return new Vector2(Screen.width, Screen.height);
            }
        }
    }
}

