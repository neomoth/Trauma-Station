using System.Linq;
using Content.Server.Hands.Systems;
using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Content.Shared.Hands.Components;
using Content.Shared.Humanoid;
using Content.Trauma.Shared.Durability;
using Content.Trauma.Shared.Durability.Components;
using Content.Trauma.Shared.Durability.Events;
using Content.Trauma.Shared.Durability.Types.Thresholds;
using Robust.Server.Containers;

namespace Content.Trauma.Server.Durability;

public sealed class DurabilitySystem : SharedDurabilitySystem
{
    [Dependency] private readonly ContainerSystem _container = default!;
    [Dependency] private readonly HandsSystem _hands = default!;
    [Dependency] private readonly ISharedAdminLogManager _aLog = default!;

    public override bool Triggered(DurabilityDamageThreshold threshold, Entity<DurabilityComponent> ent)
    {
        if (threshold.Trigger is null)
            return false;

        if (threshold.Triggered && threshold.TriggersOnce)
            return false;

        if (threshold.OldTriggered)
        {
            threshold.OldTriggered = threshold.Trigger.Reached(ent, this);
            return false;
        }

        if (!threshold.Trigger.Reached(ent, this))
            return false;

        threshold.OldTriggered = true;
        return true;
    }

    public void ExecuteThreshold(DurabilityDamageThreshold threshold, EntityUid owner, EntityUid? cause = null)
    {
        threshold.Triggered = true;

        foreach (var behavior in threshold.Behaviors)
        {
            if (!Exists(owner))
                return;

            behavior.Execute(owner, this, cause);
        }
    }

    /// <summary>
    /// Reimplementation of OnDamageChanged from DestructibleSystem in Content.Server.
    /// Would rather do this than modify that system's code to make it accessible here.
    /// </summary>
    protected override void TriggerThreshold(Entity<DurabilityComponent> ent, DurabilityDamageThreshold threshold)
    {
        RaiseLocalEvent(ent, new DurabilityBehaviorThresholdReached(ent.Comp, threshold));

        var logImpact = LogImpact.Low;
        var triggeredBehaviors = string.Join(", ",
            threshold.Behaviors.Select(b =>
            {
                if (logImpact <= b.Impact)
                    logImpact = b.Impact;
                return b.GetType().Name;
            }));

        if (logImpact > LogImpact.Medium && !HasComp<HumanoidProfileComponent>(ent))
            logImpact = LogImpact.Medium;

        // Right now, only the wielder can actually damage the entity. Should this ever change, this needs to be modified.
        // Assume something is holding this entity, if not then fuck knows what caused durability damage to it.
        if (_container.TryGetContainingContainer(ent.Owner, out var container) &&
            TryComp<HandsComponent>(container.Owner, out var hands) &&
            _hands.IsHolding((container.Owner, hands), ent))
        {
            _aLog.Add(LogType.Damaged,
                logImpact,
                $"{ToPrettyString(container.Owner):actor} caused {ToPrettyString(ent):subject} to trigger [{triggeredBehaviors}]");
        }
        else
        {
            _aLog.Add(LogType.Damaged,
                logImpact,
                $"Unknown damage source caused {ToPrettyString(ent):subject} to trigger [{triggeredBehaviors}]");
        }

        ExecuteThreshold(threshold, ent, container?.Owner);
    }
}
