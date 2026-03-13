using Content.Shared.FixedPoint;

namespace Content.Trauma.Shared.Durability.Events;

[ByRefEvent]
public record struct BeforeDurabilityDamageChangedEvent(EntityUid Uid, FixedPoint2 Damage);

[ByRefEvent]
public record struct DurabilityDamageChangedEvent(EntityUid Uid, FixedPoint2 Damage, FixedPoint2 OldDamage);
