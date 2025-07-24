// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using osu.Framework.Extensions.IEnumerableExtensions;
using osu.Framework.Utils;
using osu.Game.Beatmaps;
using osu.Game.Beatmaps.Legacy;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Objects.Legacy;
using osu.Game.Rulesets.Objects.Types;
using osu.Game.Rulesets.Osu.Objects;
using osu.Game.Rulesets.Osu.UI;
using osuTK;

namespace osu.Game.Rulesets.Osu.Beatmaps
{
    public class OsuBeatmapConverter : BeatmapConverter<OsuHitObject>
    {
        public OsuBeatmapConverter(IBeatmap beatmap, Ruleset ruleset)
            : base(beatmap, ruleset)
        {
        }

        public override bool CanConvert() => Beatmap.HitObjects.All(h => h is IHasPosition);

        protected override IEnumerable<OsuHitObject> ConvertHitObject(HitObject original, IBeatmap beatmap, CancellationToken cancellationToken)
        {
            var positionData = original as IHasPosition;
            var comboData = original as IHasCombo;
            var sliderVelocityData = original as IHasSliderVelocity;
            var generateTicksData = original as IHasGenerateTicks;

            switch (original)
            {
                case IHasPathWithRepeats curveData:
                    return new Slider
                    {
                        StartTime = original.StartTime,
                        Samples = original.Samples,
                        Path = curveData.Path,
                        NodeSamples = curveData.NodeSamples,
                        RepeatCount = curveData.RepeatCount,
                        Position = positionData?.Position ?? Vector2.Zero,
                        NewCombo = comboData?.NewCombo ?? false,
                        ComboOffset = comboData?.ComboOffset ?? 0,
                        // prior to v8, speed multipliers don't adjust for how many ticks are generated over the same distance.
                        // this results in more (or less) ticks being generated in <v8 maps for the same time duration.
                        TickDistanceMultiplier = beatmap.BeatmapVersion < 8 ? 1f / ((LegacyControlPointInfo)beatmap.ControlPointInfo).DifficultyPointAt(original.StartTime).SliderVelocity : 1,
                        GenerateTicks = generateTicksData?.GenerateTicks ?? true,
                        SliderVelocityMultiplier = sliderVelocityData?.SliderVelocityMultiplier ?? 1,
                    }.Yield();

                case IHasDuration endTimeData:
                    LegacyHitObjectType type = 0;

                    // If the object has a legacy type of spinner or hold, use that to decide which hit object to convert to
                    if (original is IHasLegacyHitObjectType legacyData)
                        type = legacyData.LegacyType & (LegacyHitObjectType.Spinner | LegacyHitObjectType.Hold);

                    // Otherwise, use the duration to determine the hit object (>2 beats for spinner, otherwise hold)
                    if (type == 0)
                    {
                        double beatLength = beatmap.ControlPointInfo.TimingPointAt(original.StartTime).BeatLength;

                        type = Precision.DefinitelyBigger(endTimeData.Duration, beatLength * 2)
                            ? LegacyHitObjectType.Spinner
                            : LegacyHitObjectType.Hold;
                    }

                    if (type.HasFlag(LegacyHitObjectType.Hold))
                    {
                        return new Hold
                        {
                            StartTime = original.StartTime,
                            Samples = original.Samples,
                            EndTime = endTimeData.EndTime,
                            Position = positionData?.Position ?? Vector2.Zero,
                            NewCombo = comboData?.NewCombo ?? false,
                            ComboOffset = comboData?.ComboOffset ?? 0,
                        }.Yield();
                    }

                    return new Spinner
                    {
                        StartTime = original.StartTime,
                        Samples = original.Samples,
                        EndTime = endTimeData.EndTime,
                        Position = positionData?.Position ?? OsuPlayfield.BASE_SIZE / 2,
                        NewCombo = comboData?.NewCombo ?? false,
                        ComboOffset = comboData?.ComboOffset ?? 0,
                    }.Yield();

                default:
                    return new HitCircle
                    {
                        StartTime = original.StartTime,
                        Samples = original.Samples,
                        Position = positionData?.Position ?? Vector2.Zero,
                        NewCombo = comboData?.NewCombo ?? false,
                        ComboOffset = comboData?.ComboOffset ?? 0,
                    }.Yield();
            }
        }

        protected override Beatmap<OsuHitObject> CreateBeatmap() => new OsuBeatmap();
    }
}
