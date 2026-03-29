using Content.Server.Atmos.EntitySystems;
using Content.Shared.Atmos;
using Content.Trauma.Shared.Durability;
using Content.Trauma.Shared.Durability.Types.Thresholds;
using JetBrains.Annotations;

namespace Content.Trauma.Server.Durability.Types.Thresholds.Behaviors;

[UsedImplicitly]
[DataDefinition]
public sealed partial class SpawnGasBehavior : IDurabilityThresholdBehavior
{
    [DataField("gasMixture", required: true)]
    public GasMixture Gas = new();

    public void Execute(EntityUid owner, SharedDurabilitySystem system, EntityUid? cause = null)
    {
        var atmos = system.EntityManager.System<AtmosphereSystem>();
        var air = atmos.GetContainingMixture(owner, false, true);

        if (air != null)
            atmos.Merge(air, Gas);
    }
}
