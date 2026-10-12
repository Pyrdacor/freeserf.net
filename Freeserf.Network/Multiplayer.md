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
- Answer: `'F' 'S' 'N' 'A' version inGame currentPlayers maxPlayers nameLength name [allowsRejoin]`

`allowsRejoin` is an optional byte (0 or 1) after the ASCII name. It is set for loaded
lobbies and running games. It allows clients to attempt a rejoin even if all four
game slots are occupied; the server still decides whether a saved or disconnected
human player is available. Player counts continue to include AI and disconnected
players. Loaded lobbies are shown as `saved`, separately from `running` games.

This extends discovery version 0 without changing the existing fields. Older clients
ignore the trailing byte; newer clients reading an answer without it infer rejoin
support from `inGame`. Older clients still cannot join a full loaded lobby through
the server list, so they must be updated for that fix (or enter the host address
manually).

The server name has 3 to 12 letters or digits. It is saved in the user config and can
be changed in the lobby while the server is running.

##### Rejoining a running game

Players who have left a running game (e.g. lost connection or crash) can join it again and get
their player back. Running games are shown in the server list and can be joined.

- When a client joins the lobby, the server sends it a session id and a token for its player
  (`JoinData`). The client keeps them in the user config (`rejoin_session`, `rejoin_token`) so it
  works after a restart of the app as well.
- After connecting, a client always sends its session id and token. A client which connects
  during the game is pending until it did so (at most 5 seconds).
- The server gives the client a player of a client who has left the game:
  1. the player of the token,
  2. otherwise a player which was used by the same IP before,
  3. otherwise the first free one.
  A player is also free if its client sent nothing for 2 seconds (the lost connection may not be
  detected yet). If there is no free player, the client is disconnected.
- Then the server sends the heartbeat (player index), a new token, the lobby data and the start
  game request. The client creates the game and gets the full game state as at the game start.
- All participants show a notification when a player has left or rejoined the game.

The test driver can test it with `-m join:ADDRESS:rejoin` (leaves the game after 10 seconds and rejoins it).

##### Saving and loading

Only the server saves multiplayer games. All its save games contain the section `multiplayer`
with the session id, the server name, the players of the clients and their tokens
(`MultiplayerSaveInfo` in `MultiplayerSave.cs`). Single player games ignore it.

- The server saves the game automatically every 10 minutes and when a player has left the game
  (`MultiplayerSaves.AutosaveInterval`, the last 3 automatic saves are kept per server name:
  `mp-NAME-auto-TIME.save`).
- Each player can request a vote to save the game (F5). If it is accepted, the server saves it
  (`mp-NAME-TIME.save`).
- A multiplayer save game is continued by selecting it in the load screen. This opens the server
  lobby of the loaded game. Its players and values can't be changed. Clients which join get their
  old players like when rejoining a running game (token, IP or the first free player).
- The game can be started before all players have joined. Missing players have left the game
  and can rejoin it later.
- At the start the server creates its game in the same way as the clients (from the lobby data)
  and applies the full game state of the loaded game to it. Only the AI players are read from the
  save game. Otherwise data which is not part of the game state (e.g. references between game
  objects) would be created in another way than on the clients and the games would get out of sync.
- The game time is part of the save games now, so it goes on after loading.

The test driver can load a game with `FREESERF_MPTEST_LOAD=PATH` (host) and use a shorter interval
of automatic saves with `FREESERF_MPTEST_AUTOSAVE=SECONDS`.

##### Votes

Some things need the agreement of the players, e.g. pausing the game or changing the
game speed (also for the server). A player requests a vote, all other players are asked
and can agree or decline. The vote is accepted if more players agree than decline (the
requester agrees). Players who don't answer in time decline.

- Each vote has a unique id so the answers can be assigned to it. Several votes can run at the same time.
- A player can request one vote every 10 seconds. After a rejected vote the same player can't
  request the same vote for 30 seconds. Players have 5 seconds to answer.
  The times are defined in `VoteSettings` (`Votes.cs`).
- The server decides about all votes (`VoteManager`). Accepted votes are applied by the server and
  the clients get the changes with the next game state update.
- Messages (`VoteData`): Request (C2S), Start (S2C, with the vote id), Answer (C2S, with the vote id),
  Result (S2C, accepted, rejected or refused if the request was not allowed).
- New types of votes (e.g. surrender or saving) can be added to `VoteType`. They need a text
  (`Vote.GetQuestion`, `Vote.GetOwnRequest`) and must be applied in `VoteController.Apply`.

The test driver can request and answer votes, see `FREESERF_MPTEST_VOTES` and `FREESERF_MPTEST_VOTEANSWER`
in `MultiplayerTestDriver`.


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