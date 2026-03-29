using Content.Trauma.Shared.Durability.Types.Thresholds.Triggers;

namespace Content.Trauma.Shared.Durability.Types.Thresholds;

/// <summary>
/// Reimplementation of <see cref="Content.Shared.Destructible.Thresholds.DamageThreshold"/> that is more
/// fine-tuned for durability.
/// </summary>
[DataDefinition]
public sealed partial class DurabilityDamageThreshold
{
    [ViewVariables]
    public bool OldTriggered;

    [DataField]
    public bool Triggered;

    [DataField]
    public bool TriggersOnce;

    [DataField]
    public IDurabilityThresholdTrigger? Trigger;

    [DataField(serverOnly: true)]
    public List<IDurabilityThresholdBehavior> Behaviors = [];
}
