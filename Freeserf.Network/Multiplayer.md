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

When the client gets serialized state data he has to create the correct current state.

To do so he has to deserialize the game state and patch the last synced game state with it.

This all has to be very quick (in one cycle). To avoid large lags the sync data should be small.

A full sync can be necessary and it may lag but following syncs should then be small and fast so the lag is short.

Syncing is done whenever the host or a client performs a user action of one of the following types:

- Change a setting which effects the game (so settings that are not only affecting the client like changing the audio volume)
- Start an attack
- Send a geologist
- Cycle knights
- Train knights
- Place or demolish a building, flag or road
- Surrender (note that leaving the game does not require a sync but surrendering should provide a notification for all participants)

If no user actions take place for a while the data to sync may be huge. To avoid this the server sends an InSync message to the clients
from time to time and resets all dirty flags. The InSync message tells the clients the last time everything was in sync so they can
update their last sync to that time. To make this work clients will update their last syncs from time to time.

Server and clients will do this in-sync tasks at specific times so they can sync better. These specific times are a multiple of
10 seconds in game time. So every 10 seconds of game time the last synced state is updated. But the client will also store the
verified last synced state from the server as a backup until the InSync message or a new sync is received. Note that the 10 seconds
don't start anew after a sync cause the game time must be a multiple of 10 seconds. After a sync at least 10 seconds are waited
and the next time the game time is a multiple of 10 seconds, the in-sync tasks are performed.


- Case 1: No user action for 10 seconds
  - Clients update their last sync
  - Server sends InSync message to all clients
  - Clients receive InSync and update their last verified sync to last sync
  - No state update is necessary at all

- Case 2: User action on host inside 10 seconds
  - Server sets next InSync message emission to 'now + at least 10 seconds' && 'gametime % 10 sec = 0'
  - Server sends game state update to all clients
  - Clients receive game state update and update their last verified sync to it
  - Clients also update their real game state accordingly
  - Clients set next last sync update time to 'now + at least 10 seconds' && 'gametime % 10 sec = 0'

- Case 3: User action on client inside 10 seconds
  - Client sends user action request to server
  - Server calculates the new game state (performs client user action in game)
  - Like Case 2


#### Data that can be send

C2S = Client to server \
S2C = Server to client

- Request
	- Heartbeat (S2C, C2S) \
        Request to get a life sign from a participant. Can be used as a "last chance to response" request.
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