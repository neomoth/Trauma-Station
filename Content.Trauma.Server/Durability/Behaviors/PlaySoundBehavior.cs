using Content.Trauma.Shared.Durability;
using Content.Trauma.Shared.Durability.Types.Thresholds;
using JetBrains.Annotations;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;

namespace Content.Trauma.Server.Durability.Types.Thresholds.Behaviors;

[UsedImplicitly]
[DataDefinition]
public sealed partial class PlaySoundBehavior : IDurabilityThresholdBehavior
{
    /// <summary>
    ///     Sound played upon destruction.
    /// </summary>
    [DataField("sound", required: true)] public SoundSpecifier Sound { get; set; } = default!;

    public void Execute(EntityUid owner, SharedDurabilitySystem system, EntityUid? cause = null)
    {
        var pos = system.EntityManager.GetComponent<TransformComponent>(owner).Coordinates;
        system.EntityManager.System<SharedAudioSystem>().PlayPvs(Sound, pos);
    }
}
