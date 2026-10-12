## Version 2.2.7

### Gameplay
- **Castles and stocks with much traffic work again:** serfs which wanted to leave a castle (builders, knights, transporters with stones or planks) only got out when the castle flag was free. With many roads at the castle the flag was almost always in use, so buildings didn't get finished and military buildings stayed empty. Now serfs leaving and serfs approaching the flag take turns.
- **Fixed:** a crash in larger games (`CallTransporter`). The AI connected flags in a background thread while the game was updated. This is now done in the game update.
- **Fixed:** the game got stuck after conquering an enemy castle ([#180](https://github.com/Pyrdacor/freeserf.net/issues/180)).
- **Fixed:** a crash because a building was treated as a stock after one of its counters wrapped ([#156](https://github.com/Pyrdacor/freeserf.net/issues/156)).

### Multiplayer
- Loaded multiplayer games with four players can be joined again. Thanks to kadrim! ([#183](https://github.com/Pyrdacor/freeserf.net/pull/183))
- AI roads are synced to the other players right away.

### Android
- Save games are stored in `Android/data/net.freeserf.android/files/saves`, which is accessible e.g. via USB. Save games of older versions are still found.
- The save games are shown in the Android files app ("Freeserf" in the side bar). They can be copied, renamed and deleted there. Save games of older versions are in the folder "Old saves".

### Other
- The readme explains how to build the project from source (GLFW is needed, especially on Linux).
