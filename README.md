# <img width="24px" src="./Logo/256.png" alt="Sonarr"></img> Sonarr 

Sonarr is a PVR for Usenet and BitTorrent users. It can monitor multiple RSS feeds for new episodes of your favorite shows and will grab, sort and rename them. It can also be configured to automatically upgrade the quality of files already downloaded when a better quality format becomes available.

# This fork

![continuous integration](https://github.com/diveflo/Sonarr/workflows/continuous%20integration/badge.svg?branch=phantom-develop)

This fork tracks the phantom-develop branch of the original Sonarr/Sonarr repo but adds HEVC/x265 quality types. The following qualities are added:

* Bluray-2160p-HEVC
* WEBDL-2160p-HEVC
* HDTV-2160p-HEVC
* Bluray-1080p-HEVC
* WEBDL-1080p-HEVC
* HDTV-1080p-HEVC
* Bluray-720p-HEVC
* WEBDL-720p-HEVC
* HDTV-720p-HEVC
* DVD-HEVC

Sonarr V3 added prefered tags, similar to Radarr, to download HEVC/x265 content. However, this doesn't allow for the same configuration, e.g., cut-off, as adding specific quality types. Additionally, it will not upgrade your local file to the HEVC version once it comes available.

The current build is always availble in the [Releases](https://github.com/diveflo/Sonarr/releases) section. Additionally, these releases are used to automatically build multi-arch docker images. These can be found on [dockerhub](https://hub.docker.com/r/floriang89/sonarr-hevc) for *linux/amd64*, *linux/arm/v7* and *linux/arm64/v8*.

## Getting Started

- [Download/Installation](https://sonarr.tv/#downloads-v3)
- [FAQ](https://wiki.servarr.com/Sonarr_FAQ)
- [Wiki](https://wiki.servarr.com/Sonarr)
- [(WIP) API Documentation](https://github.com/Sonarr/Sonarr/wiki/API)
- [Donate](https://sonarr.tv/donate)

## Support
Note: GitHub Issues are for Bugs and Feature Requests Only

- [Forums](https://forums.sonarr.tv/)
- [Discord](https://discord.gg/M6BvZn5)
- [GitHub - Bugs and Feature Requests Only](https://github.com/Sonarr/Sonarr/issues)
- [IRC ](http://webchat.freenode.net/?channels=#sonarr)
- [Reddit](https://www.reddit.com/r/sonarr)
- [Wiki](https://wiki.servarr.com/Sonarr)



## Features

### Current Features

- Support for major platforms: Windows, Linux, macOS, Raspberry Pi, etc.
- Automatically detects new episodes
- Can scan your existing library and download any missing episodes
- Can watch for better quality of the episodes you already have and do an automatic upgrade. *eg. from DVD to Blu-Ray*
- Automatic failed download handling will try another release if one fails
- Manual search so you can pick any release or to see why a release was not downloaded automatically
- Fully configurable episode renaming
- Full integration with SABnzbd and NZBGet
- Full integration with Kodi, Plex (notification, library update, metadata)
- Full support for specials and multi-episode releases
- And a beautiful UI

## Contributing

### Development
This project exists thanks to all the people who contribute. [Contribute](CONTRIBUTING.md).
<a href="https://github.com/Sonarr/Sonarr/graphs/contributors"><img src="https://opencollective.com/Sonarr/contributors.svg?width=890&button=false" /></a>

### Supporters

This project would not be possible without the support of our users and software providers. 
[**Become a sponsor or backer**](https://opencollective.com/sonarr) to help us out!

#### Sponsors

[![Sponsors](https://opencollective.com/sonarr/tiers/sponsor.svg)](https://opencollective.com/sonarr/contribute/sponsor-21443/checkout)

#### Flexible Sponsors

[![Flexible Sponsors](https://opencollective.com/sonarr/tiers/flexible-sponsor.svg?avatarHeight=54)](https://opencollective.com/sonarr/contribute/flexible-sponsor-21457/checkout)

#### Backers

[![Backers](https://opencollective.com/sonarr/tiers/backer.svg?avatarHeight=48)](https://opencollective.com/sonarr/contribute/backer-21442/checkout)

#### JetBrains

Thank you to [<img src="/Logo/Jetbrains/jetbrains.svg" alt="JetBrains" width="32"> JetBrains](http://www.jetbrains.com/) for providing us with free licenses to their great tools

* [<img src="/Logo/Jetbrains/teamcity.svg" alt="TeamCity" width="32"> TeamCity](http://www.jetbrains.com/teamcity/)
* [<img src="/Logo/Jetbrains/resharper.svg" alt="ReSharper" width="32"> ReSharper](http://www.jetbrains.com/resharper/)
* [<img src="/Logo/Jetbrains/dottrace.svg" alt="dotTrace" width="32"> dotTrace](http://www.jetbrains.com/dottrace/)

### Licenses

- [GNU GPL v3](http://www.gnu.org/licenses/gpl.html)	
- Copyright 2010-2021
