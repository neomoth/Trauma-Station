using Content.Shared.FixedPoint;
using Content.Trauma.Shared.Durability.Components;
using Robust.Shared.Serialization;

namespace Content.Trauma.Shared.Durability.Types.Thresholds.Triggers;

[DataDefinition, Serializable, NetSerializable]
public sealed partial class DurabilityDamageTrigger : IDurabilityThresholdTrigger
{
    [DataField(required: true)]
    public FixedPoint2 Damage = default!;

    public bool Reached(Entity<DurabilityComponent> ent, SharedDurabilitySystem system)
    {
        return ent.Comp.Damage >= Damage;
    }
}
