// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Game.Rulesets.Judgements;
using osu.Game.Rulesets.Objects;

namespace osu.Game.Rulesets.Osu.Judgements
{
    public class OsuJudgementResult : JudgementResult
    {
        /// <summary>
        /// The result of the combo so far prior to this <see cref="OsuJudgementResult"/> occurring.
        /// </summary>
        public ComboResult ComboResultAtJudgement;

        /// <summary>
        /// The result of the combo so far after this <see cref="OsuJudgementResult"/> occurred.
        /// </summary>
        public ComboResult ComboResultAfterJudgement;

        public OsuJudgementResult(HitObject hitObject, Judgement judgement)
            : base(hitObject, judgement)
        {
        }
    }
}
