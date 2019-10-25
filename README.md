# Freeserf.net

Freeserf.net is an authentic remake of the game **The Settlers I** from BlueByte.

To avoid copyright issues I won't provide any copyrighted data from the original game like music or graphics. To play the game you will therefore need the original DOS or Amiga data files.

Freeserf.net is a C# port and extension of [freeserf](https://github.com/freeserf/freeserf).

| Windows | Linux/Mono |
| ---- | ---- |
| [![Build status](https://ci.appveyor.com/api/projects/status/mfja74779tdsajv7?svg=true)](https://ci.appveyor.com/project/Pyrdacor/freeserf-net) | [![Build Status](https://travis-ci.org/Pyrdacor/freeserf.net.svg?branch=master)](https://travis-ci.org/Pyrdacor/freeserf.net) |

## Download the game

| Windows | Linux/Mono |
| ---- | ---- |
| [v1.9.23](https://github.com/Pyrdacor/freeserf.net/releases/download/v1.9.23/Freeserf.net-Windows.zip "Windows v1.9.23") | [v1.9.23](https://github.com/Pyrdacor/freeserf.net/releases/download/v1.9.23/Freeserf.net-Linux.tar.gz "Linux v1.9.23") |

Builds for other platforms will follow later.

Note: For now you need the DOS data file 'SPAx.PA' to run the game, where x stands for the language shortcut. It is not included in the zip file. Amiga support is in progress but doesn't work correctly yet.

You have to install .NET Framework 4.6.1 to run the game. For Windows you can download directly from Microsoft. Here is the link: https://www.microsoft.com/download/details.aspx?id=49982.

Since April 25, 2019 there is another project inside the solution which uses netcore2.1 and does no longer require .NET Framework nor WinForms. Moreover it uses the netstandard version of OpenTK.

Since May 4, 2019 the game is running on ubuntu.


## Patches

Since version 1.7 there is a patcher for the Windows assembly which will allow me to provide fast patches for the game. This will ease bugfixing and testing without the need to download a new version manually. If you don't want to receive patches you may run the game with the command line option "--no-updates".

Moreover the patcher will ask you to confirm patch download so you can also skip it.

This feature may be removed later if it bothers people but in this phase it really helps to get rid of bugs and provide patches for testers that can't compile the game by theirselves.


## Support development

If you want to support this project you can donate with PayPal.

[![Donate](https://img.shields.io/badge/Donate-PayPal-green.svg)](https://www.paypal.com/cgi-bin/webscr?cmd=_s-xclick&hosted_button_id=76DV5MK5GNEMS&source=url)

You can also flattr.

[![Flattr](http://api.flattr.com/button/flattr-badge-large.png)](https://flattr.com/submit/auto?user_id=Pyrdacor&url=https://github.com/Pyrdacor/freeserf.net&title=Freeserf.net&language=C#&tags=github&category=software)

Thank you very much.


## Current State

**Update 25.10.2019: Currently I am working on multiplayer support.**

Most of the code is ported or re-implemented. There is a working OpenTK renderer and a small sound engine for Windows. Sound for other platforms will follow later. The sound system is not perfect yet.

Things that are missing are some minor parts of AI logic and tutorial games. Serf fighting is not fully tested yet.
Multiplayer support is prepared but not really implemented yet. But I'm working on this now.

The game is playable for most parts. If you find any bugs please report it in the [Issue Tracker](https://github.com/Pyrdacor/freeserf.net/issues). You can also look for open issues there.

![Normal Game](https://github.com/Pyrdacor/freeserf.net/raw/master/images/Settlers_1.png "Start a normal game")
![Mission](https://github.com/Pyrdacor/freeserf.net/raw/master/images/Settlers_2.png "Start a mission")
![Ingame](https://github.com/Pyrdacor/freeserf.net/raw/master/images/Settlers_3.png "Build your settlement")
![Menus](https://github.com/Pyrdacor/freeserf.net/raw/master/images/Settlers_4.png "Change settings")


## Roadmap

### Phase 1: Porting (100%) - <span style="color:forestgreen">[Finished]</span>

The first step is to port everything from C++ to C# and ensure that the game runs.
There may be some quick&dirty implementations or things that could be done better.

### Phase 2: Optimizing (85%) - <span style="color:seagreen">[Active]</span>

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

The core is implemented as a .NET Standard 2.0 DLL. The renderer is also a .NET Standard 2.0 DLL and uses OpenTK for rendering. The sound engine is only implemented for Windows at the moment and uses the WinMM.dll with its WAVE and MIDI functionality.

There are now two versions of the main executable. One (`Freeserf.net`) is a .NET Framework 4.6.1 project that depends on OpenTK and OpenTK.GLControl. The other one (`FreeserfNet`) is a netcore project that depends on OpenTK.NetStandard. It is easy to create another executable project with a different .NET version as the project only contains a basic OpenTK window and forwards input events.

You can even implement your own renderer if you want. There are a bunch of interfaces in the Freeserf.Render namespace inside the core project that you can use for that.

At the moment the sound engine is part of the renderer. This will change in the future. But you can implement your own sound engine independent of the renderer already if you want.

If you need help or want to help developing, just [contact me](mailto:trobt@web.de). You can also contact me via [Issue Tracker](https://github.com/Pyrdacor/freeserf.net/issues) by adding a new issue and tag it as question.
