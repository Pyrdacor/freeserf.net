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