[![Discord](https://img.shields.io/discord/809535064551456888.svg)](https://discordapp.com/invite/DTBPBYvexy)
[![Releases](https://img.shields.io/github/release/MirageNet/EpicSocket.svg?include_prereleases&sort=semver)](https://github.com/MirageNet/EpicSocket/releases/latest)
[![GitHub Sponsors](https://img.shields.io/github/sponsors/James-Frowen)](https://github.com/sponsors/James-Frowen)

# EpicSocket

Transport for Mirage using [Epic online services](https://dev.epicgames.com/en-US/services) relay

## Installation
The preferred installation method is Unity Package manager.

If you are using unity 2019.3 or later: 

1) Open your project in unity
2) Install [Mirage](https://github.com/MirageNet/Mirage)
3) Click on Windows -> Package Manager
4) Add `com.playeveryware.eos` as to `scopedRegistries`
    * **Important:** currently eos must be installed manually by adding `https://github.com/PlayEveryWare/eos_plugin_for_unity_upm.git#v1.0.4`
5) Add the `com.miragenet.epicsocket` package`

### Required Scoped Registers

This repo requires the following Scopes registers, These values can be set in project settings within the editor or by adding them to `manifest.json`
```json
"scopedRegistries": [
    {
      "name": "OpenUPM",
      "url": "https://package.openupm.com",
      "scopes": [
        "com.cysharp.unitask",
        "com.openupm",
        "com.miragenet",
        "com.playeveryware.eos"
      ]
    }
```

### Git install

Alternatively to OpenUPM you can install using git url by adding this to `manifest.json`
```json
"com.miragenet.epicsocket": "https://github.com/MirageNet/EpicSocket.git?path=/Assets/EpicSocket#v1.0.0-beta.1"
```


## Epic setup

Check [eos_plugin_for_unity](https://github.com/PlayEveryWare/eos_plugin_for_unity) for Epic setup and examples


## Basic usage

When using `EpicSocketFactory` with Mirage you should start the client/server via the `EpicSocketFactory` instead of by themselves. 
This is because the SocketFactory will make a connection to the relay first and then start the client/server when the relay is ready to use (this avoids timeout issues).


```cs
public static async UniTask HostWithEpic(NetworkManager manager, EpicSocketFactory epicFactory)
{
    await epicFactory.InitializeAsync();

    var lobbyHelper = new LobbyHelper(EOSManager.Instance.GetProductUserId(), EOSManager.Instance.GetEOSLobbyInterface());

    epicFactory.StartAsHost(manager.Server, manager.Client);
    await lobbyHelper.StartLobby(4, "Default");
}


public static async UniTask<LobbyDetails> GetFirstLobby(EpicSocketFactory epicFactory)
{
    await epicFactory.InitializeAsync();

    var lobbyHelper = new LobbyHelper(EOSManager.Instance.GetProductUserId(), EOSManager.Instance.GetEOSLobbyInterface());
    var lobbies = await lobbyHelper.GetAllLobbies();

    // most of the time you will want to have the user pick the lobby, but for this example we just pick the first
    return lobbies.First();
}

public static async UniTask ConnectToLobby(NetworkManager manager, EpicSocketFactory epicFactory, LobbyDetails lobby)
{
    var remoteUser = lobby.GetLobbyOwner(new LobbyDetailsGetLobbyOwnerOptions());

    await epicFactory.StartAsClient(manager.Client, remoteUser);
}
```
