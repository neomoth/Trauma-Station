using Content.Server.Explosion.EntitySystems;
using Content.Trauma.Shared.Durability;
using Content.Trauma.Shared.Durability.Types.Thresholds;
using JetBrains.Annotations;

namespace Content.Trauma.Server.Durability.Types.Thresholds.Behaviors;

[UsedImplicitly]
[DataDefinition]
public sealed partial class ExplodeBehavior : IDurabilityThresholdBehavior
{
    public void Execute(EntityUid owner, SharedDurabilitySystem system, EntityUid? cause = null)
    {
        system.EntityManager.System<ExplosionSystem>().TriggerExplosive(owner, user: cause);
    }
}
