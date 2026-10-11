## Version 2.2.6

### Multiplayer
- **Votes for the game speed:** every player can request to pause, resume or change the game speed (speed buttons or keys). The other players get a box with Yes/No and 5 seconds to answer. The request is accepted if more players agree than decline (no answer counts as decline). A player can request a vote every 10 seconds, and a rejected request can't be repeated for 30 seconds. The host can't pause the game on its own anymore.
- **Rejoining:** players who have left a running game (lost connection, crash, closed the game) can join it again and get their settlement back. Running games are shown in the server list. All players are notified when someone leaves or rejoins.
- **Saving and loading:** the host saves the game automatically every 10 minutes and when a player leaves. Every player can request a vote to save the game (F5). To continue a multiplayer game, the host selects its save game in the load screen. The players join again and get their old players back; missing players can rejoin later.
- **Fixed:** the game speed now is the same on all devices. The game was updated once per frame, so devices with a higher frame rate (e.g. Android) ran faster and got out of sync.
- **Fixed:** serfs were invisible after a game update from the host until they started a new task.
- The view starts at the own castle when a loaded game is continued or a running game is rejoined.

### Gameplay
- A road can end on an existing road again: double click (or left + right click) places a flag there, which splits the existing road. Thanks to iGEL!

### Desktop
- The window can be resized. It keeps the aspect ratio, and the new size is saved.
- On the first start the window size depends on the monitor (2/3 of it), and a window outside of the monitor (or with a hidden title bar) is centred.
- Fixed an error message when closing the game (the MIDI sound font was not freed).
- Fixed the OpenGL ES detection on Linux with Mesa drivers, which broke the shaders. Thanks to kadrim!
- OpenGL 3.0 (or OpenGL ES 3.0) is required. Older drivers now get a clear error message.
- The macOS app shows the correct version.

### Android (thanks to dos-ise)
- Fixed the game running much too fast.
- Fixed missing music and sound after restarting the app.
- Fixed a fast sound loop while the game is paused.
- Fixed an endless loading screen and a crash after closing and reopening the app.

### Save games
- Save games keep the game time, so it goes on after loading. Older save games still load.
