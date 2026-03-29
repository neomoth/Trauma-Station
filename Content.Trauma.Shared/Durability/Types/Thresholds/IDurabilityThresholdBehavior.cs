using Content.Shared.Database;

namespace Content.Trauma.Shared.Durability.Types.Thresholds;

/// <summary>
/// Reimplementation of <see cref="Content.Shared.Destructible.Thresholds.Behaviors.IThresholdBehavior"/> as most
/// behaviors implementing that require the usage of <see cref="Content.Shared.Destructible.SharedDestructibleSystem"/>.
/// As such, a lot of those behaviors are reimplemented here as well.
/// </summary>
public interface IDurabilityThresholdBehavior
{
    LogImpact Impact => LogImpact.Low;

    /// <summary>
    /// Executes the behavior.
    /// </summary>
    /// <param name="owner">The entity that owns this behavior.</param>
    /// <param name="system">Reference to <see cref="SharedDurabilitySystem"/>.</param>
    /// <param name="cause">The entity responsible for executing this behavior.</param>
    void Execute(EntityUid owner, SharedDurabilitySystem system, EntityUid? cause = null);
}
