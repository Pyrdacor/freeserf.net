## Version 2.2.10

### Performance
- **Large maps are playable now.** With the largest map size the game stuttered heavily: all trees and stones of the whole map were updated every tick. Now only the visible ones get render objects.
- Creating a large map takes a few seconds instead of up to half a minute.
- The AI searches its territory much faster on large maps.

### AI
- The AI looks for coal and iron with priority if it has no mine for them. Without both there is no steel and so no weapons for new knights.
- Smart AIs know where the minerals are on the map. They build their mines at the best spots and expand towards the minerals if there are none in their territory.
- If there is no miner and no pick left for a coal or iron mine, the AI gives up a mine of a mineral it has plenty of, so that its miner can work in the new mine.

### Fixes
- A special click (left + right mouse button) no longer triggers a normal click afterwards. E.g. the map button opened the map after jumping to the castle.
- ImageSharp was replaced by StbImageSharp to get rid of its known vulnerabilities.
