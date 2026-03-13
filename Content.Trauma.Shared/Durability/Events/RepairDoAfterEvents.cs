// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Numerics;
using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Trauma.Shared.Durability.Events;

[Serializable, NetSerializable]
public sealed partial class RepairItemDoAfterEvent : SimpleDoAfterEvent
{
    public Vector2 MinMax { get; init; }
};

[Serializable, NetSerializable]
public sealed partial class RepairToolDoAfterEvent : SimpleDoAfterEvent;
