#### Ingame updates

In contrast to heartbeats or lobby data, the ingame data is kinda big.
Therefore it makes sense to only transfer differences called diffs.

The ingame states are tracked for dirty state. And the state serializer
is capable of serializing only the dirty state parts. So for normal
syncing the state diffs will be transfered. To allow this the clients
have to keep the latest synced state as the diff is always based on the
last sync state and not the current game state.

There are basically these states:

- Serf states
- Building states
- Inventory states
- Flag states
- Player states

In addition there are some map states and game settings.

- Borders
- Roads / Paths
- Trees
- Stones
- Fish resource amounts
- Ore resource amounts
- Game time / ticks / current random seed
- Game counters
    - knightMoraleCounter (counter for updating the knight morale)
    - inventoryScheduleCounter (counter for updating the inventories)
    - stat and history counters
- Histories
- flagSearchCounter (search id for flag searches)

Some things that are not in the states (and maybe should be):

- Player notifications (they are only important for the client/host itself but the triggering must be transmitted/updated correctly)


The game state contains all relevant state data mentioned above.
So each client will keep an additional game state as the last sync
state.

Full syncs are also possible. They can be requested by clients (i.e. if there was a huge timeout or out of sync cause).
The state serializer has an option for serializing full states.


##### Syncing

The server is authoritative. All participants simulate the game with the same
deterministic game logic. The server sends its full game state when the games
may differ, and the clients verify their state at regular checkpoints.

- Game start: The clients create their games from the lobby data. Some parts
  (e.g. the random state) differ from the server's game, so the server sends its
  full game state as soon as the game has started.
- User actions: A client performs its action locally (so the user sees the result
  at once) and sends it to the server. The server applies it and sends its full
  game state to all clients afterwards, because the client applied the action at
  another game time. Actions of the host lead to a full game state update as well.
- AI players: Only the server runs the AI. The AI uses its own random generator so
  that the game's random state does not depend on it. All AI code runs through
  `Game.RunAI`, which detects game state changes (see `State.ChangeCount`). If the
  AI has changed the game state, the server sends its full game state.
- Checkpoints: Every `Game.CheckpointInterval` ticks (`ConstTick`) the server sends
  an in-sync message with a hash of its game state. Each client compares it with the
  hash of its own state at the same tick (the server may be ahead or behind). If the
  hashes differ and no game state update arrives within a second, the client
  requests one. So a divergence is fixed within a few seconds whatever its reason.

Only full game states are sent at the moment (about 200 KB for a small map). The
serializer still supports partial states (only dirty values), which could reduce
the amount of data later.

For the checkpoints to work, the game simulation has to be deterministic and all
data that affects it must be part of the serialized state:

- Rendering and sound must not change the game state or use the game's random generator.
- Change tracking data for partial states is marked with `[Data(OnlyInPartialState = true)]`
  so it is not part of full states (and their hashes).
- Game object collections are enumerated in index order. The order of a dictionary
  would depend on the history of insertions and removals.


##### Finding servers

Servers answer UDP queries on the game port (5067). While the multiplayer screen is
shown, the game sends a query every 2 seconds as broadcast into the local networks, to
the own machine and to all manually entered addresses (saved in the user config). Servers
which did not answer for 7 seconds are removed from the list.

- Query: `'F' 'S' 'N' 'Q' version`
- Answer: `'F' 'S' 'N' 'A' version inGame currentPlayers maxPlayers nameLength name`

The server name has 3 to 12 letters or digits. It is saved in the user config and can
be changed in the lobby while the server is running.

In multiplayer games the game speed can't be changed. Only the server can pause and resume the game.


##### Local test environment

`test-multiplayer.ps1` in the repository root starts a server and up to three
clients on one machine (`-Clients`, `-AI`, `-Seconds`). The instances are driven by
`MultiplayerTestDriver` (command line option `-m host:CLIENTS[:AI]` or `-m join:ADDRESS`):
they create or join the server and each player builds a castle, a lumberjack and a road
through the regular interface code. At each checkpoint the state hash is logged, and the
script compares the hashes of all instances afterwards. With `-Dump` the members of all
game objects are written at each checkpoint so a divergence can be located.


#### Data that can be send

C2S = Client to server \
S2C = Server to client

- Request
	- Heartbeat (S2C, C2S) \
        Request to get a life sign from a participant. Can be used as a "last chance to respond" request.
    - StartGame (S2C, C2S) \
        Request for starting the game. Server sends this to all clients when it starts the game. \
        All clients will then start their local games and pause it immediately. Then they send a StartGame back to the host. \
        When the host game is started it is also paused. When all clients send their StartGame request back and the host game \
        is ready, the host sends a Resume request and resumes its game. The client games are resumed by the Request as well \
        and the game starts for all participants.
    - Disconnect (S2C, C2S) \
        Server closes or client leaves game.
    - LobbyData (C2S) \
        Requests lobby data from the server.
    - PlayerData (S2C, C2S) \
        Sync player data request.        
    - MapData (S2C, C2S) \
        Sync map data request.
    - GameData (S2C, C2S) \
        Sync game data request.
    - AllowUserInput (S2C) \
        Notice client that user input is processed again.
    - DisallowUserInput (S2C) \
        Notice client that user input is not processed anymore.
    - Pause (S2C) \
        Notice client that the game is paused.
    - Resume (S2C) \
        Notice client that the game is resumed.
- Response
    - Ok \
        The request was processed successfully.
    - BadRequest \
        The request was invalid.
    - BadState \
        The request was not possible in the current state.
    - BadDestination \
        The request was not for this destination.
    - Failed \
        Request could not be processed successfully.
    - Invalid \
        Invalid response code
- Heartbeat
	- Serves as a life sign so the server and clients can determine if another participant or the host is down.
- LobbyData
    - Values (Supplies, Intelligence, Reproduction) for all players.
    - Player types and faces
    - Map size and seed
    - Server settings
- PlayerData
    - Settings
    - Buildings
    - Serfs
    - Inventories
    - Flags
    - Resources (through Serfs/Flags/Inventories/Buildings)
- MapData
    - Seed
    - Trees
    - Stones
    - Fish resource amounts
    - Ore resource amounts
- GameData
    - Map and all players?
    - GameTime
    - Additional AI state values?
    - Savegame stuff?
 - UserActionData
    - Change setting
    - Create / demolish building
    - Create / demolish flag
    - Create / demolish road
    - Attack
    - Send geologist
    - Cycle knights
    - Train knights