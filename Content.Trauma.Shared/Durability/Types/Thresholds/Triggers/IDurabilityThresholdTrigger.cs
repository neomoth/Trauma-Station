using Content.Trauma.Shared.Durability.Components;

namespace Content.Trauma.Shared.Durability.Types.Thresholds.Triggers;

/// <summary>
/// Reimplementation of <see cref="Content.Shared.Destructible.Thresholds.Triggers.IThresholdTrigger"/> that is more
/// fine-tuned for durability.
/// </summary>
public interface IDurabilityThresholdTrigger
{
    bool Reached(Entity<DurabilityComponent> ent, SharedDurabilitySystem system);
}
