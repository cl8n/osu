// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Game.Rulesets.Judgements;
using osu.Game.Rulesets.Osu.Judgements;
using osu.Game.Rulesets.Scoring;

namespace osu.Game.Rulesets.Osu.Objects
{
    public class HoldTick : OsuHitObject
    {
        public required bool IsLastTick { get; init; }

        public override Judgement CreateJudgement() => new HoldTickJudgement();

        protected override HitWindows CreateHitWindows() => HitWindows.Empty;

        private class HoldTickJudgement : OsuJudgement
        {
            public override HitResult MaxResult => HitResult.LargeTickHit;
        }
    }
}
