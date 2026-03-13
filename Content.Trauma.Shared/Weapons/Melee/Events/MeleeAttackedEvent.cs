namespace Content.Trauma.Shared.Weapons.Melee.Events;

/// <summary>
/// Event raised on the melee weapon after a user attacks with it, regardless of whether it hit anything.
/// Counterpart to <see cref="Content.Shared.Weapons.Melee.Events.MeleeAttackEvent"/>.
/// </summary>
[ByRefEvent]
public record struct MeleeAttackedEvent(EntityUid Attacker);
