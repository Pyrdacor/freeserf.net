- When server closes or a client disconnects the connection should not be closed immediately
  because the other side has no chance to retrieve the disconnect message then.
  Maybe wait for disconnect responses for a given timeout.
- Outro / game end / surrender
- Test client and server shutdowns in different states
- Call all user actions
- Check exceptions
- No heartbeats are sent while in lobby nor are timeouts checked in lobby
- Server does not check for client timeouts yet
- When a server game is started (enter game init mp server screen is enough) and then a game as client is joined on same app instance, there are several bugs (seems that the client has still server view)
- If something goes wrong on client side
    - Pause game
    - Disable all input
    - Send a disconnect to the server
    - Close all previous popups
    - Open a popup with the error message or at least "Error ..."
    - Limit user input to this popup (disable viewport input)
    - After closing this popup open game init box (and close current game)
- Syncs take too long
    - If game time differs too much after sync, a partial game state update request would be nice
- Reconnects are identified by IP. Several clients on the same machine or behind the
  same router share an IP, so a reconnect may kick another client.
- Only full game states are sent (about 200 KB for a small map). Partial states
  (only dirty values) would reduce the traffic, especially for AI actions.
- Client.Disconnected may be raised on a network thread but closes the game (ClientViewer).
- A failed sync on the client disconnects and rethrows the exception (crash).
- The server only listens on the LAN IP (not on the loopback address).
