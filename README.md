# Freeserf.net

Freeserf.net is an authentic remake of the game **The Settlers I** by BlueByte.

To avoid copyright issues I won't provide any copyrighted data from the original game like music or graphics. To play the game you will therefore need the original DOS or Amiga data files.

Freeserf.net is a C# port and extension of [freeserf](https://github.com/freeserf/freeserf).

[![Build status](https://github.com/Pyrdacor/freeserf.net/actions/workflows/ci.yml/badge.svg?branch=master)](https://github.com/Pyrdacor/freeserf.net/actions/workflows/ci.yml)


## Download the game

| Windows | Linux | macOS | Android |
| ---- | ---- | ---- | ---- |
| [v2.2.5](https://github.com/Pyrdacor/freeserf.net/releases/download/v2.2.5/Freeserf.net-Windows.zip "Windows v2.2.5") | [v2.2.5](https://github.com/Pyrdacor/freeserf.net/releases/download/v2.2.5/Freeserf.net-Linux.tar.gz "Linux v2.2.5") | [v2.2.5](https://github.com/Pyrdacor/freeserf.net/releases/download/v2.2.5/Freeserf.dmg "macOS v2.2.5") | [v2.2.5](https://github.com/Pyrdacor/freeserf.net/releases/download/android-2.2.5/net.freeserf.android-Signed.apk "Android v2.2.5 APK") |

Latest version 2.2.5 was released on 11th of October 2026. See the [changelog](https://github.com/Pyrdacor/freeserf.net/blob/master/changelog.txt) for what's new and all [releases](https://github.com/Pyrdacor/freeserf.net/releases) for older versions.

Only recent Ubuntu versions are tested for the Linux version. The macOS version is a universal app (Apple Silicon and Intel).

You need the DOS data file 'SPAx.PA' to run the game, where x stands for the language shortcut (e.g. SPAE.PA for English). You can also use the Amiga files (either the disk files "*.adf" or the extracted files like "sounds" and "music" will work).
Amiga music and sounds work well but the map tiles are not displayed properly.

You can combine DOS and Amiga data (e.g. music from Amiga and graphics from DOS). See [configuration](https://github.com/Pyrdacor/freeserf.net/blob/master/Configuration.md) for more information about the Freeserf.net configuration.

Audio is provided by [BASS](https://www.un4seen.com/ "BASS"). The libraries are contained in the releases but they are for 64-bit systems only. If you have a 32-bit system you have to download them on your own or from [here](https://github.com/Pyrdacor/freeserf.net/tree/master/FreeserfNet/bass "Bass libraries").

### Android

Install the [APK](https://github.com/Pyrdacor/freeserf.net/releases/download/android-2.2.5/net.freeserf.android-Signed.apk "Android v2.2.5 APK") on your device (you may have to allow the installation from unknown sources). Android 5.0 or newer with OpenGL ES 3.0 is needed.

The game data is not included. On the first start you can either download it from Ubisoft Connect if you own the game there (e.g. The Settlers - History Edition) or select your own data file. The game is controlled by touch: pinch to zoom, pan with one finger and long-press for the special click.

### Multiplayer

Multiplayer games in the local network are possible (still experimental). One player creates a server (Multiplayer -> Create server), the others join it. Servers in the local network are found automatically, other servers can be added by their IP address or host name. The game uses port 5067 (TCP and UDP). Players with different platforms (e.g. Windows and Android) can play together.

In multiplayer games the game speed can't be changed and only the host can pause the game.


## Support development

If you want to support this project or other projects of me you can do so here.

<a href="https://www.patreon.com/bePatron?u=44764566"><img src="https://github.com/Pyrdacor/github-images/blob/main/patreon.svg" width="140" height="28" alt="Become a patron" /></a>
<a href="https://github.com/sponsors/Pyrdacor"><img src="https://github.com/Pyrdacor/github-images/blob/main/sponsor.svg" width="70" height="24" alt="Sponsor" /></a>
[![Donate](https://img.shields.io/badge/Donate-PayPal-green.svg)](https://www.paypal.com/cgi-bin/webscr?cmd=_s-xclick&hosted_button_id=76DV5MK5GNEMS&source=url) [![Flattr](http://api.flattr.com/button/flattr-badge-large.png)](https://flattr.com/submit/auto?user_id=Pyrdacor&url=https://github.com/Pyrdacor/freeserf.net&title=Freeserf.net&language=C#&tags=github&category=software) \
Thank you very much!

You may also be interested in my other projects:

- [Ambermoon.net](https://github.com/Pyrdacor/Ambermoon.net) - a C# rework of the Amiga classic Ambermoon
- [Ambermoon](https://github.com/Pyrdacor/Ambermoon) - a research project to track findings of Ambermoon data decryption


## Current State

All the code from freeserf was ported or re-implemented. AI logic was added in addition. Bug fixes of the C++ freeserf releases 0.4 and 0.5 were ported as well.

The game runs on Windows, Linux, macOS and Android. Multiplayer games in the local network work but are still experimental (e.g. there is no surrender or end of game yet).

The renderer is using [Silk.NET](https://github.com/dotnet/Silk.NET) and .NET 9. Wide screens are supported.

Things that are missing are some minor parts of AI logic and tutorial games.

The game is playable for most parts. If you find any bugs please report it in the [Issue Tracker](https://github.com/Pyrdacor/freeserf.net/issues). You can also look for open issues there.

![Normal Game](https://github.com/Pyrdacor/freeserf.net/raw/master/images/Settlers_1.png "Start a normal game")
![Mission](https://github.com/Pyrdacor/freeserf.net/raw/master/images/Settlers_2.png "Start a mission")
![Ingame](https://github.com/Pyrdacor/freeserf.net/raw/master/images/Settlers_3.png "Build your settlement")
![Menus](https://github.com/Pyrdacor/freeserf.net/raw/master/images/Settlers_4.png "Change settings")
![Map](https://github.com/Pyrdacor/freeserf.net/raw/master/images/Settlers_5.png "View the map")


## Roadmap

### Phase 1: Porting (100%) - <span style="color:forestgreen">[Finished]</span>

The first step is to port everything from C++ to C# and ensure that the game runs.
There may be some quick&dirty implementations or things that could be done better.

### Phase 2: Optimizing (100%) - <span style="color:forestgreen">[Finished]</span>

This includes bug fixing and C#-specific optimizations.
Moreover this includes performance and stability optimizations if needed.
Also the plan is to make everything cross-plattform as much as possible.

### Phase 3: Extending (15%) - <span style="color:lightseagreen">[Active]</span>

This includes:

- New features
- Better usability
- Other things like mod support, tools and so on


## Future Goal

At the end this should become a stable and performant game that runs on many platforms and can be easily compiled and extended by .NET developers.

I am not sure how far this project will go as my time is very limited. I can not promise anything at this point.


## Implementation details

The core, the renderer, the network and the audio parts are .NET 9 libraries. The renderer uses Silk.NET (OpenGL / OpenGL ES) for rendering. The sound engine is using BASS (via ManagedBass) and is capable of playing MIDI, MOD and SFX/WAV.

The desktop program is based on .NET 9 and runs on Windows, Linux and macOS. The Android app (FreeserfNet.Android) is based on .NET 10 and uses the same libraries. See [Android.md](https://github.com/Pyrdacor/freeserf.net/blob/master/Android.md) for details about the Android build.

For local multiplayer tests you can start several instances with `start-multiplayer.ps1`. `test-multiplayer.ps1` runs automated multiplayer tests.


## Contribution

If you need help or want to help developing, just [contact me](mailto:trobt@web.de). You can also contact me via [Issue Tracker](https://github.com/Pyrdacor/freeserf.net/issues) by adding a new issue and tag it as question.

There is a more or less up-to-date [list with open issues](https://github.com/Pyrdacor/freeserf.net/blob/master/Issues.md) of several relevances and importances.

Thanks to the following devs for their contributions:

- dos-ise (https://github.com/dos-ise)
- Andrzej J. Skalski (https://github.com/njskalski)
- Christopherianos
- noxdk
- athach
- shinra-electric (https://github.com/shinra-electric)


## Ingame key shortcuts

Key|Description
--------|--------
DEL|Demolish active building, road or flag
ESC|Abort road building, close ingame windows
TAB|Open notification
Ctrl+TAB|Return to last map position after notification
Shift+M|Toggle music
Shift+S|Toggle sound effects
0|Reset game speed to normal (not in multiplayer)
9|Maximize game speed (not in multiplayer)
P|Pause or resume game (only the host in multiplayer)
+|Increase game speed (not in multiplayer)
-|Decrease game speed (not in multiplayer)
&gt;|Zoom in
&lt;|Zoom out
F11|Toggle fullscreen mode
Ctrl+F|Toggle fullscreen mode
F5|Quick save
F6|Open save dialog
Shift+Q|Open quit dialog
Shift+P|Open player overview
B|Toggle possible builds
H|Go to your own castle
J|Jump between players (AIvsAI and spectators only)
M|Toggle minimap
