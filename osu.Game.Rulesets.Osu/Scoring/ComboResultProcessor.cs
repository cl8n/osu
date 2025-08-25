// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Objects.Types;
using osu.Game.Rulesets.Osu.Judgements;
using osu.Game.Rulesets.Osu.Objects;
using osu.Game.Rulesets.Scoring;

namespace osu.Game.Rulesets.Osu.Scoring
{
    public class ComboResultProcessor
    {
        private ComboResult currentComboResult = ComboResult.Perfect;

        public void ApplyResult(OsuJudgementResult result)
        {
            result.ComboResultAtJudgement = currentComboResult;

            if ((getNewComboCandidate(result) as OsuHitObject)?.NewCombo ?? false)
                currentComboResult = ComboResult.Perfect;

            switch (result.Type)
            {
                case HitResult.IgnoreMiss when result.HitObject is SliderTailCircle: // tail
                case HitResult.SmallTickMiss: // CL tail
                case HitResult.LargeTickMiss: // tick, repeat, CL head
                case HitResult.Ok:
                    updateComboResult(ComboResult.Good);
                    break;

                case HitResult.Meh:
                case HitResult.Miss:
                    updateComboResult(ComboResult.None);
                    break;
            }

            result.ComboResultAfterJudgement = currentComboResult;
        }

        public void RevertResult(OsuJudgementResult result)
        {
            currentComboResult = result.ComboResultAtJudgement;
        }

        /// <summary>
        /// If the given result's hit object is the first to be judged within its root hit object, get the root hit object.
        /// This object's <see cref="OsuHitObject.NewCombo"/> property will decide whether to reset the current combo result.
        /// </summary>
        /// <param name="result">The judgement result.</param>
        /// <returns>The root hit object, or <c>null</c> if the given result can't reset the current combo result.</returns>
        private static HitObject? getNewComboCandidate(OsuJudgementResult result)
        {
            HitObject hitObject = result.HitObject;

            if (hitObject.NestedHitObjects.Count > 0)
                return null;

            while (hitObject is IHasParent hasParent)
            {
                if (hasParent.Parent.NestedHitObjects.FirstOrDefault() != hitObject)
                    return null;

                hitObject = hasParent.Parent;
            }

            return hitObject;
        }

        private void updateComboResult(ComboResult comboResult)
        {
            if (comboResult < currentComboResult)
                currentComboResult = comboResult;
        }
    }
}
