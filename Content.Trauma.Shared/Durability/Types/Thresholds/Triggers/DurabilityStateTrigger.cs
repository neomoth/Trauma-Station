// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Shared.Durability.Components;
using Robust.Shared.Serialization;

namespace Content.Trauma.Shared.Durability.Types.Thresholds.Triggers;

[DataDefinition, Serializable, NetSerializable]
public sealed partial class DurabilityStateTrigger : IDurabilityThresholdTrigger
{
    [DataField(required: true)]
    public DurabilityState State = default!;

    public bool Reached(Entity<DurabilityComponent> ent, SharedDurabilitySystem system)
    {
        return ent.Comp.DurabilityState >= State;
    }
}
