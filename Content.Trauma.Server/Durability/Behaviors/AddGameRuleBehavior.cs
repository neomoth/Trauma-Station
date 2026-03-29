using Content.Goobstation.Shared.GameTicking;
using Content.Server.GameTicking;
using Content.Shared.Destructible;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;

namespace Content.Trauma.Server.Durability.Types.Thresholds.Behaviors;

[UsedImplicitly]
[DataDefinition]
public sealed partial class AddGameRuleBehavior
{
    [DataField(required: true)]
    public EntProtoId Rule;

    public void Execute(EntityUid owner, SharedDestructibleSystem system, EntityUid? cause = null)
    {
        var ev = new AddGameRuleItemEvent(cause);
        system.EntityManager.EventBus.RaiseLocalEvent(owner, ref ev);

        system.EntityManager.System<GameTicker>().StartGameRule(Rule);
    }
}
