using Content.Shared.Database;
using Content.Shared.Gibbing;
using Content.Trauma.Shared.Durability;
using Content.Trauma.Shared.Durability.Types.Thresholds;
using JetBrains.Annotations;

namespace Content.Trauma.Server.Durability.Types.Thresholds.Behaviors;

[UsedImplicitly]
[DataDefinition]
public sealed partial class GibBehavior : IDurabilityThresholdBehavior
{
    [DataField]
    public bool Recursive = true;

    [DataField]
    public bool DeleteGibs;

    public LogImpact Impact => LogImpact.Extreme;

    public void Execute(EntityUid owner, SharedDurabilitySystem system, EntityUid? cause = null)
    {
        var gibs = system.EntityManager.System<GibbingSystem>().Gib(owner, Recursive);
        if (!DeleteGibs)
            return;
        foreach (var gib in gibs)
        {
            system.EntityManager.QueueDeleteEntity(gib);
        }
    }
}
