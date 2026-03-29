// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Trigger.Systems;
using Content.Trauma.Shared.Durability;
using Content.Trauma.Shared.Durability.Types.Thresholds;

namespace Content.Trauma.Server.Durability.Types.Thresholds.Behaviors;

[DataDefinition]
public sealed partial class TriggerBehavior : IDurabilityThresholdBehavior
{
    /// <summary>
    /// The trigger key to use when triggering.
    /// </summary>
    [DataField]
    public string? KeyOut { get; set; } = TriggerSystem.DefaultTriggerKey;

    public void Execute(EntityUid owner, SharedDurabilitySystem system, EntityUid? cause = null)
    {
        system.EntityManager.System<TriggerSystem>().Trigger(owner, cause, KeyOut);
    }
}
