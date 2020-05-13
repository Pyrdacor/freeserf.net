# Open issues


## Non-working code

- Map clicks are not always at the right spot/tile (I guess this is caused by tile heights)
	- This also hinders road building sometimes
    - Also seen roads with invalid directions at one end (not even leading to the flag).
      Happened with double-click. Couldn't even remove it and no serfs could use it. But it blocked other road building.
- Minimap clicks will result in a map position that is a bit above the clicked spot
- Possible builds contain not all possible building locations (e.g. for the castle)
- After saving the quit confirm will not ask for saving even if the game progressed a good amount of time
- When loading a game and there is an exception during loading (e.g. in AI loading at end) the background intro game seems to be drawn messed up.
  I guess because map and stuff are loaded but then are discarded?
- If not debugging there is sometimes an exception in PopupBox.DrawResourceDirectionBox ("Not a knight among the castle defenders.").
  Not seen for a long time. Maybe only when setting the castle knights manually?
- When saving a game it is not present in load game menu. Only after game restart.


## Rendering

- In rare cases a flag seems to have the same baseline as an adjacent building. Due to animation baseline change it flickers.
	- This is fixed but remains true for military building white flags
- Notification Box
    - Bad display of text/image. (Test again because font char gaps should be smaller now!)
        - Text too long for:
	        - FoundStone
            - MineEmpty
            - OneHourSinceSave
            - UnderAttack
        - Total chaos for:
            - LostBuildings


## AI

### Flags / buildings / roads

- Linking flags should also allow for merging paths
- AIs with much resources build so much buildings at a time that no one is finished in a reasonable time. Maybe limit the number of constructed buildings. This will also relax traffic a bit.
- Building military buildings is either to slow for expansion or not well placed. In any case the territory is too full most of the time and expansion very slow.
- At some territory size the AIs will stop building military buildings and therefore expansion stops.
- There are too much mines at the same mountain after some time.
- Mine amount should be limited in relation to food sources. Saw games with dozens of mines but only one fisher. This should not happen.
- Sometimes the AI tries very hard to build something in a non-linkable place and burns it down and rebuilds it forever (e.g. a fisher behind the pond).
- Associated buildings should be well distributed (e.g. each lumberjack should have at least one forester and not one have all).
- Rarely captured buildings are not connected to own road system
- Positions for stocks are not so good any time. It should be far away from other stocks and near some production that needs to store goods in a stock (e.g. weapon, gold bars, tools, planks, food, etc).
- Sometimes additional paths should be built to avoid congestions

### Misc

- Castle position seem to be very important. Higher players should also care about the flag of the castle and try to create 5 roads out of it.
- Balduin builds much large buildings but no stock for a long time -> too much traffic.
    - Too much buildings and too few expansion to keep all those buildings inside borders.
    - A stock should be build after x buildings have been built
- Higher characters should be smarter in general than lower characters
- Non-hard-times AIs with low materials should not plan many buildings and then all materials are gone
- Maybe smart AIs should hold enough planks to rebuild a destroyed sawmill and lumberjack.
	- This is only necessary if an enemy is near enough.
- After hard times start the game time is very high so AI decisions depending on game time seem strange then (e.g. building many farms at once).
- Hard time should be also possible in later game (e.g. after losing land). In this case miners could not be available for iron or coal because
  they are used for gold or stone mines. This must be considered then and maybe those mines should be demolished to free miners.

### Mining / finding minerals

- Finding minerals and mine spots is not good yet. The search spots should change from time to time but changing doesn't mean putting hundreds of flags at the same mountain.
- In hard times the search for mountains should be better (look specific for coal and iron spots).
- Flags for geologists are too far away from mountains.
- Remove mines from AIStateBuildBuilding as AIStateFindMinerals handles them.
- Sometimes there are way too many geologist out there. This also causes traffic. It seems that every single hammer is used to train a geologist.

### Open points

- Finish attacking logic
- Test play performance of specific characters. Better characters should play better and win more often than weaker ones. They also should win 1vs1.

### Performance

- While debugging the game pauses from time to time (more game time = longer and more pauses). The game is unplayable very quick.
- There seem to be issues with progressed games and many serfs/buildings. Some performance tests are necessary.

### Long AI Games

- Sometimes no more builders are sent to construction site
- Sometimes no stones are sent to construction site
- Way too many mines are built
- Many resources are placed at flags and are never taken away
    - Saw a sawmiller who stopped working/moving planks out
    - Sometimes transporters seem to prefer other stuff
    - Too few stocks? Bad road system?

### Really smart AIs should

- Consider nearby enemies or closest enemy direction
- Consider mountains / water as natural defending shields
- Consider waiting till humans have placed their castle to avoid being crushed by a nearby placed castle
	- Either wait to build own castle
	- Or wait to build buildings inside territory that are too far away from the castle
    - If building castle first, choose a spot with more than enough resources only
- Put more thoughts into
	- Finding attack targets
	- Adjusting settings
	- Place buildings
	- etc


## Game Logic

- Serfs should not be leaving the stock/castle if there are waiting serfs around the flag
- Seen roads without a serf (previously there was one). Sometimes a new serf approaches after a while.
- A transporter first walks to the last flag and then goes back to the beginning of the road to go to idle mode.
    - I think in original game the transporter went straight to idle mode when he was at the right location.
    - Is this maybe caused by wrong road building direction? This is often seen in AIs. Maybe the linking of roads is reversed there?!
- Geologists give up too fast.
- Once a freewalking lumberjack blocks a road and stand there waiting. Also seen a forester. He was not even on the road but tried to walk towards it. The transporter on the road
  was also blocked by this.
- Sometimes there is a deadlock of walking serfs. Saw a digger and a transporter that were waiting at each other but could just switch positions.
    - In debug there were two diggers (one with state WaitIdleOnPath and one with Walking). A digger should never have WaitIdleOnPath I think as this is for transporters only!
    - Also seen a transporter or generic serf which waits for another generic serf which flickers and changes directions like crazy. This crazy serf is on top of an idle serf.
- Sometimes construction materials are not moved out of the castle. The construction site gets no resources. After demolishing/rebuilding it works again. The knight stayed there forever.
  This also happens if the building was already built a bit. And it seems to happen quite often lately.
    - Since a change in update logic this seem to be fixed. Further testing needed.
    - Sometimes resources are no longer delivered to a construction site even if there are enough in the castle
        - I guess this happens when the emergency program gets activated at a specific time.
        - The workaround was building.RemoveRequestedMaterials(); in Player.UpdateEmergencyProgram. But this can lead to an exception in Building.RequestedResourceDelivered.
- Seen a knight that was turning left and right on the same map spot (either it was only a display bug or a logic bug). It was after capturing a building.
- Approaching serfs at flags should change. Sometimes one path will always win and there might be so much resources, that others have no choice at all.
- When the castle is blocked by fighting knights, the transporter will go back into the castle and won't ever come back


## Missing stuff

- Multiplayer
    - Multiplayer should support coop mode (one player is played by two or more clients)
- Ground analysis for local spectators?
- Possible builds for local spectators?
- Disable some features in multiplayer (e.g. game speed)
- Add more game options (like tutorial)
- End game screens, Intro, Outro, etc
	- AI players and other human players should be able to surrender
	- Missions should have targets to reach to get the win (e.g. less than x enemy buildings left, etc)


## Nice to have

- The list of savegames should also allow subdirs with savegames in them. But then there must be a possibility to go up and down the directory hierarchy.
  Maybe just add a directory called ".." in every directory inside the savegame list which performs a "up" operation.
- When switching player in AIvsAI (e.g. with key 'j') the currently opened menu should be updated.
- Later think of a strategy to avoid the tactic to demolish a hut when own knights lose and the opponent is capturing it. Players could do this to get a better fight outcome.
    - Maybe buildings under attack can't be demolished?
- Localization (texts should be of the right language later)
- Rework whole serf, flag and path logic. Flag searches should be eased by the stored road instances of each flag.
- See also "New feature candidates" below
- Add log filename and max log size to command line
- Store last game setings in user config (map size, game type, map seed, players and their values, etc)


## New feature candidates

- Improve usability
    - More infos in UI / better menus
    - Indicators like "how many trees are left for a lumberjack" or even a visual search area if the building is selected
- Get materials after demolishing
- Fog of war (optional, as game start setting)
- Extended construction infos
    - Resources on the way / scheduled
    - Resources already used
    - Total resources, etc
- Extended building infos
    - Resources on the way / scheduled
    - Workload in percent
- Feature to record a game and replay/watch it (like watching an AIvsAI game)
