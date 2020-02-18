# Freeserf.net

Freeserf.net is an authentic remake of the game **The Settlers I** by BlueByte.

To avoid copyright issues I won't provide any copyrighted data from the original game like music or graphics. To play the game you will therefore need the original DOS or Amiga data files.

Freeserf.net is a C# port and extension of [freeserf](https://github.com/freeserf/freeserf).

| Windows | Linux |
| ---- | ---- |
| [![Build status](https://ci.appveyor.com/api/projects/status/github/pyrdacor/freeserf.net?branch=master&svg=true)](https://ci.appveyor.com/project/Pyrdacor/freeserf-net/history?branch=master) | [![Build Status](https://travis-ci.org/Pyrdacor/freeserf.net.svg?branch=master)](https://travis-ci.org/Pyrdacor/freeserf.net/branches) |

## Download the game

| Windows | Linux |
| ---- | ---- |
| [v2.0.0-pre](https://github.com/Pyrdacor/freeserf.net/releases/download/v2.0.0-pre/Freeserf.net-Windows.zip "Windows v2.0.0 Pre-Release") | [v2.0.0-pre](https://github.com/Pyrdacor/freeserf.net/releases/download/v2.0.0-pre/Freeserf.net-Linux.tar.gz "Linux v2.0.0 Pre-Release") |
| [v1.9.35](https://github.com/Pyrdacor/freeserf.net/releases/download/v1.9.35/Freeserf.net-Windows.zip "Windows v1.9.35") | [v1.9.35](https://github.com/Pyrdacor/freeserf.net/releases/download/v1.9.35/Freeserf.net-Linux.tar.gz "Linux v1.9.35") |

Builds for other platforms will follow later.

Note: For now you need the DOS data file 'SPAx.PA' to run the game, where x stands for the language shortcut. It is not included in the zip file. Amiga support is in progress but doesn't work correctly yet. But you can give it a shot if you want.

For Ubuntu make sure you have installed libgdiplus via command `sudo apt-get install libgdiplus`.


## Support development

If you want to support this project you can donate with PayPal.

[![Donate](https://img.shields.io/badge/Donate-PayPal-green.svg)](https://www.paypal.com/cgi-bin/webscr?cmd=_s-xclick&hosted_button_id=76DV5MK5GNEMS&source=url)

You can also flattr.

[![Flattr](http://api.flattr.com/button/flattr-badge-large.png)](https://flattr.com/submit/auto?user_id=Pyrdacor&url=https://github.com/Pyrdacor/freeserf.net&title=Freeserf.net&language=C#&tags=github&category=software)

Thank you very much.


## Current State

19.02.2020: Release 2.0.0-pre which is no longer dependent of .NET Framework nor Mono and will run on Windows and Ubuntu.

Currently I am working on multiplayer support.

All the code from freeserf was ported or re-implemented. AI logic was added in addition.

The renderer is using [Silk.net](https://github.com/Ultz/Silk.NET) and netcore 3.1.

There is a small sound engine for Windows. Sound for other platforms will follow later. The sound system is not perfect yet.

Things that are missing are some minor parts of AI logic and tutorial games.

The game is playable for most parts. If you find any bugs please report it in the [Issue Tracker](https://github.com/Pyrdacor/freeserf.net/issues). You can also look for open issues there.

![Normal Game](https://github.com/Pyrdacor/freeserf.net/raw/master/images/Settlers_1.png "Start a normal game")
![Mission](https://github.com/Pyrdacor/freeserf.net/raw/master/images/Settlers_2.png "Start a mission")
![Ingame](https://github.com/Pyrdacor/freeserf.net/raw/master/images/Settlers_3.png "Build your settlement")
![Menus](https://github.com/Pyrdacor/freeserf.net/raw/master/images/Settlers_4.png "Change settings")


## Roadmap

### Phase 1: Porting (100%) - <span style="color:forestgreen">[Finished]</span>

The first step is to port everything from C++ to C# and ensure that the game runs.
There may be some quick&dirty implementations or things that could be done better.

### Phase 2: Optimizing (90%) - <span style="color:seagreen">[Active]</span>

This includes bug fixing and C#-specific optimizations.
Moreover this includes performance and stability optimizations if needed.
Also the plan is to make everything cross-plattform as much as possible.

### Phase 3: Extending (10%) - <span style="color:lightseagreen">[Active]</span>

This includes:

- New features
- Better usability
- Other things like mod support, tools and so on


## Future Goal

At the end this should become a stable and performant game that runs on many platforms and can be easily compiled and extended by .NET developers.

I am not sure how far this project will go as my time is very limited. I can not promise anything at this point.


## Implementation details

The core is implemented as a .NET Standard 2.1 DLL. The renderer is also a .NET Standard 2.1 DLL and uses Silk.NET for rendering. The sound engine is only implemented for Windows at the moment and uses the WinMM.dll with its WAVE and MIDI functionality.

At the moment the sound engine is part of the renderer. This will change in the future. But you can implement your own sound engine independent of the renderer already if you want.

The main program is based on netcore 3.1 and should run at least on Windows and Ubuntu.

If you need help or want to help developing, just [contact me](mailto:trobt@web.de). You can also contact me via [Issue Tracker](https://github.com/Pyrdacor/freeserf.net/issues) by adding a new issue and tag it as question.
