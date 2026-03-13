// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.Shared.Damage.Components;
using Content.Shared.Destructible;
using Content.Shared.DoAfter;
using Content.Shared.Examine;
using Content.Shared.FixedPoint;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Random.Helpers;
using Content.Shared.Stacks;
using Content.Shared.Tools.Components;
using Content.Shared.Tools.Systems;
using Content.Shared.Weapons.Melee;
using Content.Shared.Weapons.Melee.Events;
using Content.Trauma.Shared.Durability.Components;
using Content.Trauma.Shared.Durability.Events;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;

namespace Content.Trauma.Shared.Durability;

public sealed class DurabilitySystem : EntitySystem
{
    [Dependency] private readonly SharedDestructibleSystem _destructible = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedStackSystem _stack = default!;
    [Dependency] private readonly SharedToolSystem _tool = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;

    private static readonly Dictionary<DurabilityState, Color> AssociatedColors = new()
    {
        {DurabilityState.Reinforced, new Color(98, 217, 195)},
        {DurabilityState.Pristine, new Color(117, 217, 98)},
        {DurabilityState.Worn, new Color(217, 191, 98)},
        {DurabilityState.Damaged, new Color(217, 140, 98)},
        {DurabilityState.Broken, new Color(217, 98, 98)},
        {DurabilityState.Destroyed, Color.Red},
    };

    private static readonly LocId ExamineTextCondition = new("durability-examine-condition");
    private static readonly LocId ExamineTextDamage = new("durability-examine-damage");
    private static readonly LocId ExamineTextColor = new("durability-repair-colortext");
    private static readonly LocId ExamineTextIrreparable = new("durability-repair-irreparable");
    private static readonly LocId ExamineTextRepairReqs = new("durability-repair-needed");
    private static readonly LocId ExamineTextRepairReqSingle = new("durability-repair-single");
    private static readonly LocId ExamineTextRepairReqMultiple = new("durability-repair-multiple");
    private static readonly LocId ToolQualityPrefix = new("durability-tool-");
    private static readonly LocId RepairPopup = new("durability-repair-popup");
    private static readonly LocId ReinforcePopup = new("durability-reinforce-popup");
    private static readonly LocId MaxRepairPopup = new("durability-repair-max");

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DurabilityComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<DurabilityComponent, AttemptMeleeEvent>(OnAttemptMelee);
        SubscribeLocalEvent<DurabilityComponent, MeleeHitEvent>(OnMeleeHit);
        SubscribeLocalEvent<DurabilityComponent, GetMeleeDamageEvent>(OnGetMeleeDamage);
        SubscribeLocalEvent<DurabilityComponent, DurabilityDamageChangedEvent>(OnDurabilityDamageChanged);
        SubscribeLocalEvent<DurabilityComponent, DurabilityStateChangedEvent>(OnDurabilityStateChanged);
        SubscribeLocalEvent<DurabilityComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<DurabilityComponent, RepairItemDoAfterEvent>(OnRepairItemDoAfter);
        SubscribeLocalEvent<DurabilityComponent, RepairToolDoAfterEvent>(OnRepairToolDoAfter);
    }

    public bool DamageEntity(EntityUid uid, FixedPoint2 amount, DurabilityComponent? comp = null, EntityUid? attacker = null, List<EntityUid>? targets = null)
    {
        if (!Resolve(uid, ref comp))
            return false;
        //Dealing negative damage should always succeed since well, that's a positive effect.
        if (Math.Sign(amount.Value) > 0 && !RollDamageChance(uid, comp))
            return false;

        // Check if anything may end up negating the damage. Negative damage heals, obviously.
        var beforeEv = new BeforeDurabilityDamageChangedEvent(uid, amount);
        RaiseLocalEvent(uid, ref beforeEv);
        amount = beforeEv.Damage;
        var oldDamage = comp.Damage;
        comp.Damage += amount;
        if (comp.Damage < -comp.MaxRepairBonus)
            comp.Damage = -comp.MaxRepairBonus; // cap lower bound

        var oldState = comp.DurabilityState;
        comp.DurabilityState = GetDurabilityState(comp);
        Dirty(uid, comp);
        // Don't raise the event if it didn't actually change.
        if (comp.DurabilityState != oldState)
        {
            var stateEv = new DurabilityStateChangedEvent(oldState, comp.DurabilityState, uid, attacker, targets);
            RaiseLocalEvent(uid, ref stateEv);
        }

        var afterEv = new DurabilityDamageChangedEvent(uid, comp.Damage, oldDamage);
        RaiseLocalEvent(uid, ref afterEv);
        return oldDamage != comp.Damage;
    }

    private bool RollDamageChance(EntityUid uid, DurabilityComponent comp)
    {
        return SharedRandomExtensions.PredictedProb(_timing,
            Math.Clamp(comp.DamageProbability, 0, 1),
            GetNetEntity(uid));
    }

    private DurabilityState GetDurabilityState(DurabilityComponent comp)
    {
        foreach (var (threshold, durabilityState) in comp.DurabilityThresholds.Reverse())
        {
            // handle reinforced if not defined
            if (durabilityState is DurabilityState.Pristine &&
                !comp.DurabilityThresholds.ContainsValue(DurabilityState.Reinforced) && comp.Damage < 0)
                return DurabilityState.Reinforced;

            if (comp.Damage < threshold)
                continue;

            return durabilityState;
        }

        return DurabilityState.Pristine;
    }

    private FixedPoint2 GetDamageModifier(DurabilityComponent comp)
    {
        if (!comp.DamageModifiers.TryGetValue(comp.DurabilityState, out var mod))
            return comp.DurabilityState is DurabilityState.Destroyed ? 0 : 1;
        return mod;
    }

    // Hello welcome to the super turbo shitcode inc™ string builder function of doom and gloom.
    private List<string> GetRepairMaterialString(DurabilityComponent comp)
    {
        List<EntityPrototype> seen = [];
        foreach (var protoId in comp.RepairMaterials.Keys)
        {
            if (!_proto.Resolve(protoId, out var proto))
                continue;
            if (proto.Parents is not null && proto.Parents.Any(parent => seen.Any(s=>s.ID == parent)))
                continue;
            seen.Add(proto);
        }

        if (comp.RepairTool is null && seen.Count == 0)
            // ReSharper disable once UseCollectionExpression | literally cant, client no likey
            return new List<string> {Loc.GetString(ExamineTextIrreparable)};
        var start = (seen.Count == 1 && comp.RepairTool is null) || (seen.Count == 0 && comp.RepairTool is not null)
            ? ExamineTextRepairReqSingle
            : ExamineTextRepairReqMultiple;
        // ReSharper disable once UseCollectionExpression | shut the fuck up I CANNNTTTTTT
        List<string> entries = new(){start};

        if (comp.RepairTool is not null)
        {
            entries.Add(Loc.GetString(ExamineTextColor,
                ("data", $"- {Loc.GetString($"{ToolQualityPrefix}{comp.RepairTool.Value.Id.ToLower()}")}")));
        }

        entries.AddRange(seen.Select(material => Loc.GetString(ExamineTextColor, ("data", $"- {material.Name}"))));

        // only one entry was added, first entry is just the starting text
        if (entries.Count == 2)
        {
            var dashIdx = entries[1].IndexOf("- ", StringComparison.Ordinal);
            // entries = new List<string> { $"{start}{entries[1].Remove(dashIdx, 2)}" }; // merge second into first, remove the dash
            // ReSharper disable once UseCollectionExpression | SHUT UUUUPPPPP
            entries = new List<string>
            {
                $"{Loc.GetString(ExamineTextRepairReqs, ("requirements", Loc.GetString(entries[0])))}{entries[1].Remove(dashIdx, 2)}",
            };
            return entries;
        }

        entries[0] = Loc.GetString(ExamineTextRepairReqs, ("requirements", Loc.GetString(entries[0])));
        return entries;
    }

    private void OnExamined(EntityUid uid, DurabilityComponent comp, ref ExaminedEvent args)
    {
        using (args.PushGroup("durability"))
        {
            args.PushMarkup(Loc.GetString(ExamineTextCondition,
                ("color", AssociatedColors[comp.DurabilityState].ToHex()),
                ("state", comp.DurabilityState.ToString())));
            args.PushMarkup(Loc.GetString(ExamineTextDamage,
                ("color", AssociatedColors[comp.DurabilityState].ToHex()),
                ("mod", GetDamageModifier(comp))));
            var entries = GetRepairMaterialString(comp);
            foreach (var entry in entries)
            {
                args.PushMarkup(entry);
            }
        }
    }

    private void OnAttemptMelee(EntityUid uid, DurabilityComponent comp, AttemptMeleeEvent args)
    {
        // Prohibit attacking with a destroyed weapon; it is in such a state of disrepair that it cannot be used.
        if (comp.DurabilityState is not DurabilityState.Destroyed)
            return;
        args.Cancelled = true;
        if (comp.DestroyedSwingAttemptPopup.HasValue)
            args.Message = Loc.GetString(comp.DestroyedSwingAttemptPopup, ("weapon", MetaData(uid).EntityName));
    }

    private void OnMeleeHit(EntityUid uid, DurabilityComponent comp, ref MeleeHitEvent args)
    {
        if (!args.IsHit)
            return;

        // Check if anything can even take damage here. You really shouldn't lose durability for misclicking a puddle or something.
        if (!args.HitEntities.Any(HasComp<DamageableComponent>))
            return;

        var random = SharedRandomExtensions.PredictedRandom(_timing, GetNetEntity(uid));
        var damage = random.NextFloat(comp.MinDamageRoll.Float(), comp.MaxDamageRoll.Float());
        DamageEntity(uid, damage, comp, args.User, args.HitEntities.ToList());
    }

    private void OnGetMeleeDamage(EntityUid uid, DurabilityComponent comp, ref GetMeleeDamageEvent args)
    {
        args.Damage *= GetDamageModifier(comp);
    }

    private void OnDurabilityDamageChanged(EntityUid uid, DurabilityComponent comp, DurabilityDamageChangedEvent args)
    {
        var diff = args.Damage - args.OldDamage;

        switch (Math.Sign(diff.Value))
        {
            case < 0:
            {
                var locId = args.OldDamage <= 0 && args.Damage <= 0 ? ReinforcePopup : RepairPopup;
                var amount = args.OldDamage - FixedPoint2.Max(args.Damage, -comp.MaxRepairBonus);
                _popup.PopupPredictedCoordinates(
                    Loc.GetString(locId, ("weapon", MetaData(uid).EntityName), ("amount", amount)),
                    Transform(uid).Coordinates,
                    null);
                break;
            }
            case > 0:
            {
                if (!comp.DamagePopups.TryGetValue(comp.DurabilityState, out var pool))
                    return;
                var locId = _random.Pick(pool);
                _popup.PopupPredictedCoordinates(Loc.GetString(locId),
                    Transform(uid).Coordinates,
                    null,
                    PopupType.SmallCaution);
                break;
            }
            case 0 when comp.Damage <= -comp.MaxRepairBonus:
            {
                _popup.PopupPredictedCoordinates(
                    Loc.GetString(MaxRepairPopup, ("weapon", MetaData(uid).EntityName)),
                    Transform(uid).Coordinates,
                    null);
                break;
            }
        }
    }

    private void OnDurabilityStateChanged(EntityUid uid, DurabilityComponent comp, DurabilityStateChangedEvent args)
    {
        if (args.NewState is not DurabilityState.Destroyed)
            return;

        comp.OnBreakBehavior?.Execute(uid, _destructible);
        if (!comp.DeleteOnDestroyed)
            return;
        PredictedQueueDel(uid);

        if (TryComp<MeleeWeaponComponent>(args.Attacker, out var userMelee))
        {
            userMelee.NextAttack = _timing.CurTime + TimeSpan.FromSeconds(1); // this makes me irrationally upset
            Dirty(args.Attacker.Value, userMelee);
        }
    }

    private void OnInteractUsing(EntityUid uid, DurabilityComponent comp, InteractUsingEvent args)
    {
        if (args.Target != uid || args.Handled)
            return;

        if (TryComp<ToolComponent>(args.Used, out var tool) && comp.RepairTool is not null)
        {
            if (_tool.HasQuality(args.Used, comp.RepairTool, tool))
            {
                _tool.UseTool(uid,
                    args.User,
                    args.Target,
                    comp.RepairDoAfter,
                    [comp.RepairTool],
                    new RepairToolDoAfterEvent(),
                    out _,
                    comp.FuelCost,
                    tool);
                args.Handled = true;
                return;
            }
            // fall through to see if it is an accepted """material"""
        }

        if (!comp.RepairMaterials.ContainsKey(MetaData(args.Used).EntityPrototype!))
            return;
        _doAfter.TryStartDoAfter(new DoAfterArgs(EntityManager,
            args.User,
            comp.RepairDoAfter,
            new RepairItemDoAfterEvent(),
            uid,
            args.Target,
            args.Used));
        args.Handled = true;
    }

    private void OnRepairItemDoAfter(EntityUid uid, DurabilityComponent comp, RepairItemDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || Deleted(args.Used))
            return;

        if (!comp.RepairMaterials.TryGetValue(MetaData(args.Used.Value).EntityPrototype!, out var minmax))
            return;

        var (min, max) = minmax;

        // deal negative damage to heal
        if (!DamageEntity(uid,
                -SharedRandomExtensions.PredictedRandom(_timing, GetNetEntity(uid)).NextFloat(min, max),
                comp))
            return;

        if (TryComp<StackComponent>(args.Used, out var stack))
            _stack.ReduceCount((args.Used.Value, stack), 1);
        else
            PredictedQueueDel(args.Used);

        args.Handled = true;
    }

    private void OnRepairToolDoAfter(EntityUid uid, DurabilityComponent comp, RepairToolDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || Deleted(args.Used))
            return;

        if (comp.RepairTool is null)
            return;

        var (min, max) = comp.ToolRepairAmount;

        DamageEntity(uid,
            -SharedRandomExtensions.PredictedRandom(_timing, GetNetEntity(uid)).NextFloat(min, max),
            comp);

        _tool.PlayToolSound(args.Used.Value, Comp<ToolComponent>(args.Used.Value), args.User);

        args.Handled = true;
    }
}

[Serializable, NetSerializable]
public enum DurabilityState
{
    Reinforced = -1,
    Pristine = 0,
    Worn = 1,
    Damaged = 2,
    Broken = 3,
    Destroyed = 4,
}
