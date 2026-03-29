// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.IdentityManagement;
using Content.Shared.Inventory;
using Content.Shared.Popups;
using Content.Trauma.Shared.Durability;
using Content.Trauma.Shared.Durability.Types.Thresholds;
using JetBrains.Annotations;
using Robust.Server.GameObjects;

namespace Content.Trauma.Server.Durability.Types.Thresholds.Behaviors;

[UsedImplicitly]
[DataDefinition]
public sealed partial class BurnBodyBehavior : IDurabilityThresholdBehavior
{
    [DataField]
    public LocId PopupMessage = "bodyburn-text-others";

    public void Execute(EntityUid bodyId, SharedDurabilitySystem system, EntityUid? cause = null)
    {
        var transformSystem = system.EntityManager.System<TransformSystem>();
        var inventorySystem = system.EntityManager.System<InventorySystem>();
        var sharedPopupSystem = system.EntityManager.System<SharedPopupSystem>();

        if (system.EntityManager.TryGetComponent<InventoryComponent>(bodyId, out var comp))
        {
            foreach (var item in inventorySystem.GetHandOrInventoryEntities(bodyId))
            {
                transformSystem.DropNextTo(item, bodyId);
            }
        }

        var bodyIdentity = Identity.Entity(bodyId, system.EntityManager);
        sharedPopupSystem.PopupCoordinates(Loc.GetString(PopupMessage, ("name", bodyIdentity)), transformSystem.GetMoverCoordinates(bodyId), PopupType.LargeCaution);

        system.EntityManager.QueueDeleteEntity(bodyId);
    }
}
