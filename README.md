# Freeserf.net

Freeserf.net is an authentic remake of the game **The Settlers I** by BlueByte.

_**Announcement**_: *Multiplayer is coming. The progress with multiplayer implementation took a huge step forward. Soon first test releases will come. Stay tuned. :)*

To avoid copyright issues I won't provide any copyrighted data from the original game like music or graphics. To play the game you will therefore need the original DOS or Amiga data files.

Freeserf.net is a C# port and extension of [freeserf](https://github.com/freeserf/freeserf).

| AppVeyor (Windows) | Travis (Linux) | Azure Pipelines (Windows) |
| ---- | ---- | ---- |
| [![Build status](https://ci.appveyor.com/api/projects/status/github/pyrdacor/freeserf.net?branch=master&svg=true)](https://ci.appveyor.com/project/Pyrdacor/freeserf-net/history?branch=master) | [![Build Status](https://travis-ci.org/Pyrdacor/freeserf.net.svg?branch=master)](https://travis-ci.org/Pyrdacor/freeserf.net/branches) | [![Build Status](https://dev.azure.com/Pyrdacor/Freeserf.net/_apis/build/status/Pyrdacor.freeserf.net?branchName=master)](https://dev.azure.com/Pyrdacor/Freeserf.net/_build/latest?definitionId=2&branchName=master) |

## Download the game

| Windows | Linux |
| ---- | ---- |
| [v2.0.0-preview6](https://github.com/Pyrdacor/freeserf.net/releases/download/v2.0.0-preview6/Freeserf.net-Windows.zip "Windows v2.0.0 Preview 6") | [v2.0.0-preview6](https://github.com/Pyrdacor/freeserf.net/releases/download/v2.0.0-preview6/Freeserf.net-Linux.tar.gz "Linux v2.0.0 Preview 6") |

Builds for other platforms will follow later. Only recent Ubuntu versions are tested for Linux version.

Note: For now you need the DOS data file 'SPAx.PA' to run the game, where x stands for the language shortcut. You can also use the Amiga files (either the disk files "*.adf" or the extracted files like "sounds" and "music" will work).
Amiga music and sounds work well but the map tiles are not displayed properly.

You can combine DOS and Amiga data (e.g. music from Amiga and graphics from DOS). There should be a user.cfg in your user directory with those settings
(%APPDATA%\freeserf.net on Windows, ~/.local/share/freeserf on Ubuntu). Change the following settings:

```
graphic_data_usage = PreferDos
sound_data_usage = PreferAmiga
music_data_usage = PreferAmiga
```

Possible values are `PreferDos`, `PreferAmiga`, `ForceDos` and `ForceAmiga`. The force options will disable music or sound if the specific data files are not present. In case of graphics the game won't even start then.

For Ubuntu make sure you have installed libgdiplus via command `sudo apt-get install libgdiplus`.

Audio is provided by [BASS](http://www.un4seen.com/ "BASS"). The assemblies are contained in the releases but they are for 64-bit systems only. If you have a 32-bit system you have to download them on your own or from [here](https://github.com/Pyrdacor/freeserf.net/tree/master/FreeserfNet/bass "Bass assemblies").


## Support development

If you want to support this project you can donate with PayPal.

[![Donate](https://img.shields.io/badge/Donate-PayPal-green.svg)](https://www.paypal.com/cgi-bin/webscr?cmd=_s-xclick&hosted_button_id=76DV5MK5GNEMS&source=url)

You can also flattr.

[![Flattr](http://api.flattr.com/button/flattr-badge-large.png)](https://flattr.com/submit/auto?user_id=Pyrdacor&url=https://github.com/Pyrdacor/freeserf.net&title=Freeserf.net&language=C#&tags=github&category=software)

Thank you very much.


## Current State

Currently I am working on multiplayer support.

All the code from freeserf was ported or re-implemented. AI logic was added in addition.

The renderer is using [Silk.net](https://github.com/Ultz/Silk.NET) and netcore 3.1.

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

The core is implemented as a .NET Standard 2.1 DLL. The renderer is also a .NET Standard 2.1 DLL and uses Silk.NET for rendering. The sound engine is using BASS and is capable of playing MIDI, MOD and SFX/WAV on Windows and Linux.

The main program is based on netcore 3.1 and should run at least on Windows and Ubuntu.

If you need help or want to help developing, just [contact me](mailto:trobt@web.de). You can also contact me via [Issue Tracker](https://github.com/Pyrdacor/freeserf.net/issues) by adding a new issue and tag it as question.
