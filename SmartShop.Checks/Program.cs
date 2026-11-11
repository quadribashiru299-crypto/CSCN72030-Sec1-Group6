using SmartShop.Devices;

var failures = 0;

Check("occupancy accepts valid whole counts and records UTC", () =>
{
    var sensor = new OccupancySensor();
    var sampleTime = new DateTimeOffset(2026, 11, 11, 1, 0, 0, TimeSpan.FromHours(-5));
    Require(sensor.LastKnownCount is null, "A new sensor must have no confirmed count.");
    Require(sensor.TryAcceptReading(42, sampleTime), "A whole nonnegative count should be accepted.");
    Require(sensor.LastKnownCount == 42, "The accepted count should be retained.");
    Require(sensor.LastValidAtUtc == sampleTime.ToUniversalTime(), "The sample time should be UTC.");
    Require(sensor.TryAcceptReading(0, sampleTime), "Zero is a valid occupancy count.");
    Require(sensor.LastKnownCount == 0, "Zero should replace the previous valid count.");
});

Check("occupancy rejects bad counts without changing last valid state", () =>
{
    var sensor = new OccupancySensor();
    var validAt = new DateTimeOffset(2026, 11, 11, 6, 0, 0, TimeSpan.Zero);
    Require(sensor.TryAcceptReading(10, validAt), "Setup reading failed.");

    foreach (var invalid in new[] { -1d, 3.5d, double.NaN, double.PositiveInfinity, double.NegativeInfinity, (double)int.MaxValue + 1 })
    {
        Require(!sensor.TryAcceptReading(invalid, validAt.AddSeconds(1)), $"Invalid count {invalid} was accepted.");
        Require(sensor.LastKnownCount == 10 && sensor.LastValidAtUtc == validAt,
            "An invalid reading changed the last valid count or timestamp.");
    }
});

Check("lock preserves confirmed manual command over later baseline feedback", () =>
{
    var controller = new EntranceLockController();
    var at = new DateTimeOffset(2026, 11, 11, 6, 0, 0, TimeSpan.Zero);
    Require(controller.TryAcceptBaseline(" unlocked ", at), "Valid baseline feedback was rejected.");
    Require(controller.LastKnownState == EntranceLockState.Unlocked, "Initial baseline was not retained.");
    Require(controller.TryConfirmManualCommand("LOCKED", at.AddSeconds(1)), "Confirmed manual command was rejected.");
    Require(controller.HasManualOverride && controller.LastKnownState == EntranceLockState.Locked,
        "A successful command should establish a locked override.");

    Require(controller.TryAcceptBaseline("Unlocked", at.AddSeconds(2)), "Valid baseline feedback was rejected.");
    Require(controller.LastKnownState == EntranceLockState.Locked && controller.LastConfirmedAtUtc == at.AddSeconds(1),
        "Baseline feedback replaced the manual override.");

    controller.ClearManualOverride();
    Require(controller.TryAcceptBaseline("Unlocked", at.AddSeconds(3)), "Baseline did not resume after restart.");
    Require(!controller.HasManualOverride && controller.LastKnownState == EntranceLockState.Unlocked,
        "Restart did not clear the override.");
});

Check("invalid lock feedback or command does not change confirmed state", () =>
{
    var controller = new EntranceLockController();
    var at = new DateTimeOffset(2026, 11, 11, 6, 0, 0, TimeSpan.Zero);
    Require(controller.TryAcceptBaseline("Locked", at), "Setup feedback failed.");
    Require(!controller.TryAcceptBaseline("Open", at.AddSeconds(1)), "Invalid baseline was accepted.");
    Require(!controller.TryConfirmManualCommand("Open", at.AddSeconds(2)), "Invalid command was accepted.");
    Require(controller.LastKnownState == EntranceLockState.Locked && controller.LastConfirmedAtUtc == at,
        "Invalid input changed the last confirmed state.");
    Require(!controller.HasManualOverride, "Invalid command established an override.");
});

Console.WriteLine(failures == 0 ? "All SmartShop device checks passed." : $"{failures} SmartShop device check(s) failed.");
return failures == 0 ? 0 : 1;

void Check(string name, Action assertion)
{
    try
    {
        assertion();
        Console.WriteLine($"PASS: {name}");
    }
    catch (Exception error)
    {
        failures++;
        Console.Error.WriteLine($"FAIL: {name} — {error.Message}");
    }
}

static void Require(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
}
