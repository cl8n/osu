// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Localisation;
using osu.Game.Rulesets.Catch.Judgements;
using osu.Game.Rulesets.Catch.Objects;
using osu.Game.Rulesets.Judgements;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Scoring;
using osu.Game.Rulesets.UI;
using osu.Game.Scoring;
using osuTK;

namespace osu.Game.Rulesets.Catch.Mods
{
    public class CatchModGolfScoring : Mod, IApplicableToHealthProcessor, IReversesScoreOrdering
    {
        public override string Name => "Golf Scoring";
        public override string Acronym => "GS";
        public override LocalisableString Description => "Aim for the lowest score!";
        public override double ScoreMultiplier => 1;
        public override IconUsage? Icon => FontAwesome.Solid.Flag;
        public override ModType Type => ModType.Conversion;

        public void ApplyToHealthProcessor(HealthProcessor healthProcessor)
        {
            healthProcessor.AdjustJudgementResult += (result) =>
            {
                if (result.Type == result.Judgement.MaxResult)
                    result.Type = result.Judgement.MinResult;
                else if (result.Type == result.Judgement.MinResult)
                    result.Type = result.Judgement.MaxResult;
            };
        }
    }
}
