// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Shared.Durability.Components;
using Content.Trauma.Shared.Durability.Types.Thresholds;

namespace Content.Trauma.Shared.Durability.Events;

/// <summary>
/// Raised when a durability threshold is reached. Primarily for tests, should one be made in the future.
/// </summary>
public sealed class DurabilityBehaviorThresholdReached(DurabilityComponent parent, DurabilityDamageThreshold threshold) : EntityEventArgs
{
    public readonly DurabilityComponent Parent = parent;
    public readonly DurabilityDamageThreshold Threshold = threshold;
}
