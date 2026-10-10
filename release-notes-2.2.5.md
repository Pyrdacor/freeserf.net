## Version 2.2.5

### New: Android port (thanks to dos-ise)
- Freeserf.net now runs on Android and has its own APK build workflow.
- **Touch controls:** pinch-to-zoom, one-finger panning, long-press for the special click, a larger GUI scale and bigger tap areas.
- **Game data:** the APK doesn't include SPAE.PA. You can download it in the app by signing in to Ubisoft Connect, which opens Ubisoft's own login page.
- **Sound:** sound effects and MIDI music work through BASS.
- **Multiplayer:** no longer crashes on Android. You join a game by entering an IP address or hostname.
- **Other:** a loading indicator at startup, the on-screen keyboard opens when you name a save game, and the exit button closes the app.
- **Fixes:** startup crashes, crashes in the in-game menu, a black screen when resuming the app, and music still playing after you exit.

### Multiplayer
- Multiplayer games (LAN) now work: the games of all players stay in sync, also with AI players.
- Several players can join from the same computer or network.
- Servers in the local network are found automatically. Other addresses can be entered and are saved (press Delete to remove one).
- The server name can be chosen (3 to 12 letters or digits). It is saved and others see it in their server list.
- The game speed can't be changed in multiplayer games. Only the host can pause the game.
- Fixed games that didn't start when the host started right after a player joined.
- In the lobby, the values (supplies, reproduction) of the players are synced. The host can set them for all players.
- When a player leaves the game, the others get a notification.
- Fixed crashes and wrong buildings, serfs and resources after a game update from the host.
- Fixed crashes when a player is kicked from the lobby.
- Multiplayer is still experimental: there is no surrender or end of game yet.

### Widescreen support
- The game screen now uses wide aspect ratios, and the GUI scales evenly and stays centred.

### Game logic fixes (ported from C++ freeserf 0.4/0.5)
- A transporter no longer duplicates a resource when it switches at a flag.
- Removing a road only loses the serfs that are moving along that road.
- Serfs no longer deadlock waiting for each other at flags.
- Knights are made again while a Knight0 is in the stock.
- Transporter bits are no longer cleared at a crowded flag.
- The military score in the statistics is fixed (overflow and wrong formula).
- Pigs no longer oink too often.
- The DOS smelter sound is fixed.

### Save games
- Save games now store much more state, so a loaded game continues exactly like the saved one. This covers counts for all building types (gold smelters used to be lost), burning buildings, knight morale, castle inventory, player timers, messages, statistics history, military threat levels, and serfs leaving a stock.
- Older save games still load.

### Other changes
- The window title shows the app name and version.
- Opening a notification closes an open popup (and the other way round), so they no longer overlap.
- In the save dialog, the name field is focused right away and is separated from the list. Saving without a name is no longer possible.
- Text in the Save/Load dialog was drawn outside the list box. It is now drawn inside it.
