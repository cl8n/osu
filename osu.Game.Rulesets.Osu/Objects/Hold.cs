// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using osu.Game.Audio;
using osu.Game.Beatmaps;
using osu.Game.Beatmaps.ControlPoints;
using osu.Game.Beatmaps.Legacy;
using osu.Game.Rulesets.Judgements;
using osu.Game.Rulesets.Objects.Legacy;
using osu.Game.Rulesets.Objects.Types;
using osu.Game.Rulesets.Osu.Judgements;
using osu.Game.Rulesets.Scoring;

namespace osu.Game.Rulesets.Osu.Objects
{
    public class Hold : OsuHitObject, IHasDuration, IHasLegacyHitObjectType
    {
        public double EndTime
        {
            get => StartTime + Duration;
            set => Duration = value - StartTime;
        }

        public double Duration { get; set; }

        // public override Vector2 Position
        // {
        //     set
        //     {
        //         base.Position = value;
        //
        //         foreach (var nested in NestedHitObjects.OfType<OsuHitObject>())
        //             nested.Position = value;
        //     }
        // }

        // This is necessary to differentiate spinners and holds, because they implement the same interfaces
        public LegacyHitObjectType LegacyType => LegacyHitObjectType.Hold;

        public override IList<HitSampleInfo> AuxiliarySamples => CreateHoldSamples();

        private List<HitSampleInfo>? headSamples;
        private List<HitSampleInfo>? tailSamples;

        private double durationBetweenTicks;

        public Hold()
        {
            SamplesBindable.CollectionChanged += (_, _) => updateNestedSamples();
        }

        public HitSampleInfo[] CreateHoldSamples()
        {
            HitSampleInfo? holdSample = (Samples.FirstOrDefault(s => s.Name == HitSampleInfo.HIT_NORMAL) ??
                                         Samples.FirstOrDefault())?.With("spinnerspin");

            return holdSample == null ? Array.Empty<HitSampleInfo>() : new[] { holdSample };
        }

        protected override void ApplyDefaultsToSelf(ControlPointInfo controlPointInfo, IBeatmapDifficultyInfo difficulty)
        {
            base.ApplyDefaultsToSelf(controlPointInfo, difficulty);

            durationBetweenTicks = controlPointInfo.TimingPointAt(StartTime).BeatLength / difficulty.SliderTickRate;
        }

        protected override void CreateNestedHitObjects(CancellationToken cancellationToken)
        {
            base.CreateNestedHitObjects(cancellationToken);

            AddNested(new HoldHeadCircle
            {
                StartTime = StartTime,
                // Position = Position,
                // StackHeight = StackHeight,
            });

            for (double time = StartTime + durationBetweenTicks; time <= EndTime - 10; time += durationBetweenTicks)
            {
                cancellationToken.ThrowIfCancellationRequested();

                AddNested(new HoldTick
                {
                    IsLastTick = false,
                    StartTime = time,
                });
            }

            AddNested(new HoldTick
            {
                IsLastTick = true,
                StartTime = EndTime,
            });

            updateNestedSamples();
        }

        private void updateNestedSamples()
        {
            headSamples ??= Samples.Select(o => o.With()).ToList();
            tailSamples ??= Samples.Select(o => o.With()).ToList();

            // TODO: remove this when guaranteed sort is present for samples (https://github.com/ppy/osu/issues/1933)
            HitSampleInfo? tickSample = (Samples.FirstOrDefault(s => s.Name == HitSampleInfo.HIT_NORMAL) ?? Samples.FirstOrDefault())?.With("slidertick");

            foreach (var nested in NestedHitObjects)
            {
                switch (nested)
                {
                    case HoldHeadCircle:
                        nested.Samples = headSamples;
                        break;

                    case HoldTick tick when tick.IsLastTick:
                        tick.Samples = tailSamples;
                        break;

                    case HoldTick tick:
                        tick.SamplesBindable.Clear();

                        if (tickSample != null)
                            tick.SamplesBindable.Add(tickSample);

                        break;
                }
            }
        }

        public override Judgement CreateJudgement() => new OsuIgnoreJudgement();

        protected override HitWindows CreateHitWindows() => HitWindows.Empty;
    }
}
