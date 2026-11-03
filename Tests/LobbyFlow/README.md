# Main Menu multiplayer flow checks

Run from the repository root with .NET 10:

```powershell
dotnet run --project Tests/LobbyFlow/LobbyFlow.Tests.csproj
```

The harness compiles the production LobbyManager, MatchConnectionCoordinator,
MatchParticipantRoster and PlayerIdentityManager against fake Unity and UGS
boundaries. It never contacts Authentication, Lobby or Relay services.

Checks cover repeated actions, lost responses, Start/Leave ordering, final roster
validation, rollback, stale callbacks across sessions, host-loss timeouts, locked
membership recovery, stale Leave metadata, failed/pending cleanup and partial
authentication. One check waits for the real 10-second cleanup timeout.

These checks do not replace Unity Play Mode or multi-process host/client testing.
Still verify actual NGO scene synchronization, transport disconnects, service error
codes and UI input/navigation in a running Editor/build.
