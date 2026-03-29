using Content.Shared.FixedPoint;
using Robust.Shared.Random;

namespace Content.Trauma.Shared.Durability.Types;

/*
 * Note: this is not actually used here due to damage values being FixedPoint2s, but I felt that having this in general
 * is a good thing, and other systems are free to use this. The class below it is the one that is actually used by
 * the DurabilitySystem.
 */
/// <summary>
/// Similar to <see cref="Content.Shared.Destructible.Thresholds.MinMax"/> but allows for floats.<br/>
/// Additionally, allows for specifying a single value if a minimum/maximum is not desired.
/// </summary>
[DataDefinition, Serializable]
public partial struct MinMaxF
{
    [DataField("value")]
    public float? SingleValue = null;

    [DataField]
    public float Min;

    [DataField]
    public float Max;

    public MinMaxF(float min, float max)
    {
        Min = min;
        Max = max;
    }

    public MinMaxF(float value)
    {
        SingleValue = value;
    }

    public readonly float Next(IRobustRandom random)
    {
        return SingleValue ?? random.NextFloat(Min, Max + 1);
    }

    public readonly float Next(System.Random random)
    {
        return SingleValue ?? random.NextFloat(Min, Max + 1);
    }
}

/// <summary>
/// Similar to <see cref="Content.Shared.Destructible.Thresholds.MinMax"/> but allows for <see cref="Content.Shared.FixedPoint.FixedPoint2"/>s.<br/>
/// Additionally, allows for specifying a single value if a minimum/maximum is not desired.
/// </summary>
[DataDefinition, Serializable]
public partial struct MinMaxFixedPoint2
{
    [DataField("value")]
    public FixedPoint2? SingleValue = null;

    [DataField]
    public FixedPoint2 Min;

    [DataField]
    public FixedPoint2 Max;

    public MinMaxFixedPoint2(FixedPoint2 min, FixedPoint2 max)
    {
        Min = min;
        Max = max;
    }

    public MinMaxFixedPoint2(FixedPoint2 value)
    {
        SingleValue = value;
    }

    public readonly FixedPoint2 Next(IRobustRandom random)
    {
        return SingleValue ?? random.NextFloat(Min.Float(), Max.Float() + 1);
    }

    public readonly FixedPoint2 Next(System.Random random)
    {
        return SingleValue ?? random.NextFloat(Min.Float(), Max.Float() + 1);
    }
}
