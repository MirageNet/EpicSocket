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
4) Click on the plus sign on the left and click on "Add package from git URL..."
5) enter `https://github.com/MirageNet/EpicSocket.git?path=/Assets/EpicSocket#v1.0.0-beta.1`,
    - note `#v1.0.0-beta.1` can be replaced with branch, tag or commit hash. eg `#master`

#### Troubleshooting
If there are errors installing you may neeed to manually add the EOS plugin first:
```
https://github.com/PlayEveryWare/eos_plugin_for_unity_upm.git#v1.0.4
```


## Epic setup

Check [eos_plugin_for_unity](https://github.com/PlayEveryWare/eos_plugin_for_unity) for Epic setup and examples
