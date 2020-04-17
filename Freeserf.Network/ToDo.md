- When server closes or a client disconnects the connection should not be closed immediately
  because the other side has no chance to retrieve the disconnect message then.
  Maybe wait for disconnect responses for a given timeout.
- Client states (received messages should be only processed in right state)
- Outro / game end / surrender
- Client and server crash when the other side disconnects / shuts down
- Sometimes black screen on server and client after starting the game (maybe timing of network events in relation to rendering / silk event processing?)
    - Maybe gather and memorize incoming network events and execute them in the next main loop iteration?
- Call heartbeat broadcast
- Call all user actions
- Check exceptions
- Longrun tests
- Server game updates (atm the client sees no game updates beside his own)