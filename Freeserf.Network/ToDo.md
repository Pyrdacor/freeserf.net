- When server closes or a client disconnects the connection should not be closed immediately
  because the other side has no chance to retrieve the disconnect message then.
  Maybe wait for disconnect responses for a given timeout.
- Client states (received messages should be only processed in right state)
- Outro / game end / surrender
- Test client and server shutdowns in different states
- Call all user actions
- Check exceptions
- Longrun tests
- Server game updates (atm the client sees no game updates beside his own)
- No heartbeats are sent while in lobby nor are timeouts checked in lobby
- Server does not check for client timeouts yet
- Once a MP game was running another started MP game can be started by the server but clients are rejected then