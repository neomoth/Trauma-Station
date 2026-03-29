using Content.Trauma.Shared.Durability;
using Content.Trauma.Shared.Durability.Types.Thresholds;
using JetBrains.Annotations;

namespace Content.Trauma.Server.Durability.Types.Thresholds.Behaviors;

[UsedImplicitly]
[DataDefinition]
public sealed partial class DestroyBehavior : IDurabilityThresholdBehavior
{
    public void Execute(EntityUid owner, SharedDurabilitySystem system, EntityUid? cause = null)
    {
        system.DestroyEntity(owner);
    }
}
