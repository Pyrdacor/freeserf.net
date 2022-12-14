# Freeserf.net

Freeserf.net is an authentic remake of the game **The Settlers I** by BlueByte.

To avoid copyright issues I won't provide any copyrighted data from the original game like music or graphics. To play the game you will therefore need the original DOS or Amiga data files.

Freeserf.net is a C# port and extension of [freeserf](https://github.com/freeserf/freeserf).

[![Build status](https://ci.appveyor.com/api/projects/status/github/pyrdacor/freeserf.net?branch=master&svg=true)](https://ci.appveyor.com/project/Pyrdacor/freeserf-net/history?branch=master)


## Download the game

| Windows | Linux |
| ---- | ---- |
| [v2.1.1](https://github.com/Pyrdacor/freeserf.net/releases/download/v2.1.1/Freeserf.net-Windows.zip "Windows v2.1.1") | [v2.1.1](https://github.com/Pyrdacor/freeserf.net/releases/download/v2.1.1/Freeserf.net-Linux.tar.gz "Linux v2.1.1") |

Builds for other platforms will follow later. Only recent Ubuntu versions are tested for Linux version.

Note: For now you need the DOS data file 'SPAx.PA' to run the game, where x stands for the language shortcut. You can also use the Amiga files (either the disk files "*.adf" or the extracted files like "sounds" and "music" will work).
Amiga music and sounds work well but the map tiles are not displayed properly.

You can combine DOS and Amiga data (e.g. music from Amiga and graphics from DOS). See [configuration](https://github.com/Pyrdacor/freeserf.net/blob/master/Configuration.md) for more information about the Freeserf.net configuration.

For Ubuntu make sure you have installed libgdiplus via command `sudo apt-get install libgdiplus`.

Audio is provided by [BASS](http://www.un4seen.com/ "BASS"). The assemblies are contained in the releases but they are for 64-bit systems only. If you have a 32-bit system you have to download them on your own or from [here](https://github.com/Pyrdacor/freeserf.net/tree/master/FreeserfNet/bass "Bass assemblies").


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


## Contribution

If you need help or want to help developing, just [contact me](mailto:trobt@web.de). You can also contact me via [Issue Tracker](https://github.com/Pyrdacor/freeserf.net/issues) by adding a new issue and tag it as question.

There is a more or less up-to-date [list with open issues](https://github.com/Pyrdacor/freeserf.net/blob/master/Issues.md) of several relevances and importances.


## Ingame key shortcuts

Key|Description
--------|--------
DEL|Demolish active building, road or flag
ESC|Abort road building, close ingame windows
TAB|Open notification
Ctrl+TAB|Return to last map position after notification
Shift+M|Toggle music
Shift+S|Toggle sound effects
0|Reset game speed to normal
9|Maximize game speed
P|Pause or resume game
+|Increase game speed
-|Decrease game speed
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
