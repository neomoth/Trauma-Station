using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Trauma.Shared.Durability.Events;

[Serializable, NetSerializable]
public sealed partial class RepairItemDoAfterEvent : SimpleDoAfterEvent;

[Serializable, NetSerializable]
public sealed partial class RepairToolDoAfterEvent : SimpleDoAfterEvent;
