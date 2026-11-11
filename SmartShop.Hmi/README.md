# SmartShop HMI integration scaffold

This ASP.NET MVC project displays twelve device tiles. Run from the repository root:

```bash
dotnet run --project SmartShop.Hmi/SmartShop.Hmi.csproj
```

Open the localhost address printed in the terminal. If your Mac has only a newer
.NET runtime, the temporary local command is:

```bash
DOTNET_ROLL_FORWARD=Major dotnet run --project SmartShop.Hmi/SmartShop.Hmi.csproj
```

The dashboard starts with **No reading / Unavailable** because the team has not
merged its acquisition service. It makes `GET /Dashboard/Snapshot` requests once
per second; these return the current in-memory snapshot, not sample data.

## Team handoff

The shared one-second acquisition service should inject `DashboardStateBridge` and
call `PublishOccupancy(value, condition, acquiredAtUtc)` for occupancy readings,
where `condition` comes from the shared threshold engine. It should call
`PublishEntranceLockFeedback(state, acquiredAtUtc)` for lock feedback. For the
other ten devices it can call `PublishSharedTile(tile)` using the team's agreed
device IDs and server-computed quality/condition. Reconcile these IDs with the
final shared contract before publishing.

After the team's authorized command service confirms a lock command, it should
call `PublishConfirmedLockCommand(state, confirmedAtUtc)`. It may call
`PublishLockOutcome` for Pending, Rejected, Failed, or TimedOut. A simulation
restart calls `SimulationRestarted`. These methods are server-side only; the
dashboard has no command endpoint or enabled controls until authentication,
dispatch, confirmation, and audit logging exist.

This is an integration seam, not a complete live simulator. It neither reads
device files nor persists to SQLite. Runtime compilation and browser behavior
should be checked on a Mac with the .NET SDK; this prepared copy was made in an
environment without `dotnet`.
