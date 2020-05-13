# Freeserf configuration

## User config

The user config file is used to store the users settings. This is basically the content of the options menu.

The file is located inside the users application data path in the sub-directory *'freeserf'* and is called *'user.cfg'*.

### Location

- **Windows**: C:\Users\USERNAME\AppData\Roaming\freeserf\user.cfg (%APPDATA%\freeserf\user.cfg)
- **Linux**: HOME/.local/share/freeserf/user.cfg
- **OSX**: HOME/Library/Application Support/freeserf/user.cfg

### Usage

The user config is overwritten every time the game is closed. The settings are the same as in the option menu.
You may change the user config manually with a text editor. But the changes will only be used on next game start.
So manual changes should be done while the game is not running.

Boolean values are expressed as 0 and 1. Integers are expressed in decimal format.

### Content

```
[game]
  options = 57
  graphic_data_usage = PreferDos
  sound_data_usage = PreferDos
  music_data_usage = PreferAmiga
[audio]
  music = 1
  sound = 1
  volume = 1.0
[video]
  resolution_width = 1280
  resolution_height = 960
  fullscreen = 0
[logging]
  level = Error
  max_log_size = 10485760
  log_file = freeserf.log
  log_to_console = 0
```

Most of the values are self-explanatory. The options value is a bit flag value which means each bit enables (1) or disables (0) an option.
The game options are as follows:

```
MessagesImportant = Bit 0 -> 1 // This is always set as these messages will always be notified.
InvertScrolling = Bit 1 -> 2 // Map scrolling is inverted
FastBuilding = Bit 2 -> 4 // Fast building
MessagesAll = Bit 3 -> 8 // Alert on all notifications
MessagesMost = Bit 4 -> 16 // Alert on most notifications
MessagesFew = Bit 5 -> 32 // Alert on only a few notifications
PathwayScrolling = Bit 6 -> 64 // Scrolls the map while building roads
FastMapClick = Bit 7 -> 128 // Fast map click
HideCursorWhileScrolling = Bit 8 -> 256 // Hide cursor while scrolling the map
ResetCursorAfterScrolling = Bit 9 -> 512 // After map scrolling ends, place the cursor at the center of the screen
```

The data usage values can be one of the following: PreferDos, PreferAmiga, ForceDos, ForceAmiga.
If both data formats are found (DOS and Amiga) the preferred data is used for the Prefer\* versions.
If the Force\* versions are used the forced data has to exist. Otherwise the data is not loaded.
This may lead to no sound, no music or no graphics. In the latter case the game is not started at all.

Log levels can be one of the following: Info, Warn, Error.

- Error: Log only errors.
- Warn: Log only warnings and errors.
- Info: Log warnings, errors and informations.

The max log size is given in bytes and is only used if `log_to_console` is 0. If the log file exceeds the given
size, lines at the top of the log are removed. Note that removing log lines from the top may cause decreases in
performance as the whole log file content has to be copied on every new log entry. A bigger max log size may be
safer for long games. But note that with log level `Error`, only errors are logged and in most cases these would
lead to application close anyway. So it should be no issue by default.

You can change the log file to your liking. If you specify a fully-qualified rooted path for `log_file` it will
be created there. Otherwise the path is treated as a relative path to the path of the freeserf executable.

If you want to log to the console instead of a file you can set `log_to_console` to 1.


## Command line

The user may run Freeserf from the command line and pass additional options through command line arguments to the application.

```
Usage:  [-d NUM] [-f] [-g DATA-PATH] [-h] [-l FILE] [-r RES] [-c]

        -d NUM          Set debug output level
        -f              Run in fullscreen mode
        -g DATA-PATH    Use specified data directory
        -h              Show this help text
        -l FILE         Load saved game
        -r RES          Set display resolution (e.g. 800x600)
        -c              Log to console window

```

Note that the command line arguments take always precedence over user config settings.

The debug output levels are:
- 0: Verbose (only for debug version)
- 1: Debug (only for debug version)
- 2: Info
- 3: Warn
- 4: Error

### Examples

```
FreeserfNet -d 3 -c
FreeserfNet -r 1024x768 -f
```