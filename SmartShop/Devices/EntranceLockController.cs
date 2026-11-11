namespace SmartShop.Devices;

public enum EntranceLockState
{
    Locked,
    Unlocked
}

/// <summary>Retains confirmed entrance-lock feedback and a manual override.</summary>
public sealed class EntranceLockController
{
    public const string DeviceId = "EntranceLockController";
    public const string DisplayName = "Entrance Lock";

    public EntranceLockState? LastKnownState { get; private set; }
    public DateTimeOffset? LastConfirmedAtUtc { get; private set; }
    public bool HasManualOverride { get; private set; }

    /// <summary>
    /// Applies valid feedback from this controller's simulation file. The shared
    /// acquisition service handles missing or invalid input and data quality.
    /// A manual override takes precedence until the simulation restarts.
    /// </summary>
    public bool TryAcceptBaseline(string? feedback, DateTimeOffset acquiredAtUtc)
    {
        if (!TryParseState(feedback, out var state))
        {
            return false;
        }

        if (!HasManualOverride)
        {
            LastKnownState = state;
            LastConfirmedAtUtc = acquiredAtUtc.ToUniversalTime();
        }

        return true;
    }

    /// <summary>
    /// Call only after the shared command service has authorized, dispatched,
    /// and confirmed a successful command. Rejected, failed, or timed-out
    /// commands must never call this method.
    /// </summary>
    public bool TryConfirmManualCommand(string? requestedState, DateTimeOffset confirmedAtUtc)
    {
        if (!TryParseState(requestedState, out var state))
        {
            return false;
        }

        LastKnownState = state;
        LastConfirmedAtUtc = confirmedAtUtc.ToUniversalTime();
        HasManualOverride = true;
        return true;
    }

    /// <summary>Clears the override when the simulation restarts.</summary>
    public void ClearManualOverride() => HasManualOverride = false;

    private static bool TryParseState(string? text, out EntranceLockState state)
    {
        if (string.Equals(text?.Trim(), "Locked", StringComparison.OrdinalIgnoreCase))
        {
            state = EntranceLockState.Locked;
            return true;
        }

        if (string.Equals(text?.Trim(), "Unlocked", StringComparison.OrdinalIgnoreCase))
        {
            state = EntranceLockState.Unlocked;
            return true;
        }

        state = default;
        return false;
    }
}
