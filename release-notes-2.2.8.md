## Version 2.2.8

### New (thanks to dos-ise, ported from the C++ freeserf project)
- **Tutorials:** the original's six tutorials can be selected as game type in the game init box. Each tutorial has its own map, goals and castle supplies, and the tutorial orders box shows the current goal. Tutorial 6 adds a passive enemy.
- **End of the game:** the game decides a winner and shows the original's result pictures and texts with the winner's face, and after a won mission the mission's picture.
- **Special click:** left + right mouse button (long press on Android) works as in the original. On a build button it shows the possible building sites, on the map button it shows the castle, and only a special click demolishes. In the map box a special click on the buildings button opens the building filter, so the minimap only shows the chosen building type.
- Save games keep the game type, the mission or tutorial level and the winner.

### Gameplay
- **Fishing:** fish now only spawn in shallow water next to the shore, and the more land around, the more fish. Before, every lake had endless fish. Now 2 or 3 fishers can share a lake of the size of a screen, a 4th one overfishes it, and a small pond gives about one fish out of five trips ([#47](https://github.com/Pyrdacor/freeserf.net/issues/47)).
- **Mines** also mine the ground below their own building, their flag and other buildings, so a geologist no longer finds resources at a mined out spot ([#158](https://github.com/Pyrdacor/freeserf.net/issues/158)).

### Fixes
- Games in which a castle was destroyed can be loaded again ([#142](https://github.com/Pyrdacor/freeserf.net/issues/142)).
- The serf counts were doubled with every load ([#139](https://github.com/Pyrdacor/freeserf.net/issues/139)).
- A crash in games with a wrong castle knight count ([#130](https://github.com/Pyrdacor/freeserf.net/issues/130)).
- The possible building sites are updated while they are shown, e.g. after building something ([#131](https://github.com/Pyrdacor/freeserf.net/issues/131)).
- The resource chart shows the newest values on the right and ends at the x axis ([#138](https://github.com/Pyrdacor/freeserf.net/issues/138), [#152](https://github.com/Pyrdacor/freeserf.net/issues/152)).
- The scroll wheel no longer turns a chart into the minimap ([#128](https://github.com/Pyrdacor/freeserf.net/issues/128)).
- Sounds of serfs and buildings outside of the screen are no longer played ([#129](https://github.com/Pyrdacor/freeserf.net/issues/129)).
- Parts of a burning mine no longer stay on the screen while scrolling ([#127](https://github.com/Pyrdacor/freeserf.net/issues/127)).
- Serfs waiting inside a building are no longer drawn in front of it.
- Linux: the audio libraries are found on more distributions (no `libdl.so` needed anymore) ([#87](https://github.com/Pyrdacor/freeserf.net/issues/87)).
