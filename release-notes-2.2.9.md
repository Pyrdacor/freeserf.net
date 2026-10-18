## Version 2.2.9

### New
- **Languages:** the game texts can be shown in English or German. The language can be changed in the options and is applied immediately. Thanks to kadrim!
- **Multiplayer:** the host also sees the map size and the random seed. Thanks to kadrim!
- **Android:** devices with Android 15+/16 and 16 KB memory pages are supported. Thanks to dos-ise!

### AI
- Keeps fewer knights in the castle: limited by the free knights, at least 3 if there are more than 3, always 1.
- Builds a weaponsmith in hard times if there is steel.
- Releases the knights of needless military buildings if it has no free knights (only if no land and no buildings are lost).
- Builds military buildings at the growing border.
- Splits roads at congested flags, e.g. at the castle, so the resources get through again.

### Fixes
- The emergency program blocked all construction sites forever when the stones ran out although the essential buildings didn't need any stones (e.g. if no stonecutter could be built). The AI could not expand anymore then.
- Mines got no food anymore although there was plenty of it in the castle. Old save games are repaired automatically.
- Flags were drawn in front of buildings after scrolling the map.
- Roads and other sprites were cut off at the top and left edge of the view.
- A special click on the build button toggles the possible building sites again. Thanks to dos-ise!
- The building filter's third page shows the mill and the butcher at their original positions. Thanks to dos-ise!
- Amiga graphics: the map tiles and roads are displayed properly now.
- Better performance with large settlements.
