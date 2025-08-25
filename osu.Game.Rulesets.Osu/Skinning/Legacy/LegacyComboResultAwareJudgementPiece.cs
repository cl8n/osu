// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Textures;
using osu.Game.Rulesets.Judgements;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Objects.Types;
using osu.Game.Rulesets.Osu.Judgements;
using osu.Game.Rulesets.Osu.Objects;
using osu.Game.Rulesets.Scoring;
using osu.Game.Skinning;

namespace osu.Game.Rulesets.Osu.Skinning.Legacy
{
    public partial class LegacyComboResultAwareJudgementPiece : CompositeDrawable, IAnimatableJudgement
    {
        private readonly Drawable? piece;
        private readonly Drawable? pieceGoodCombo;
        private readonly Drawable? piecePerfectCombo;

        private Drawable? currentPiece;

        public LegacyComboResultAwareJudgementPiece(ISkin skin, HitResult hitResult)
        {
            AutoSizeAxes = Axes.Both;
            Origin = Anchor.Centre;

            Texture? particle;

            switch (hitResult)
            {
                case HitResult.Great:
                    particle = skin.GetTexture("particle300");

                    piece = createJudgementPiece("hit300");
                    pieceGoodCombo = createJudgementPiece("hit300k");
                    piecePerfectCombo = createJudgementPiece("hit300g");

                    break;

                case HitResult.Ok:
                    particle = skin.GetTexture("particle100");

                    piece = createJudgementPiece("hit100");
                    pieceGoodCombo = createJudgementPiece("hit100k");

                    break;
            }

            Drawable? createJudgementPiece(string componentName)
            {
                var createMainDrawable = () => skin.GetAnimation(componentName, true, false);

                if (createMainDrawable() == null)
                    return null;

                Drawable innerPiece = particle != null
                    ? new LegacyJudgementPieceNew(hitResult, createMainDrawable!, particle)
                    : new LegacyJudgementPieceOld(hitResult, createMainDrawable!);

                AddInternal(innerPiece.With(p => p.Anchor = Anchor.Centre));
                return innerPiece;
            }
        }

        public void ApplyJudgementResult(JudgementResult result)
        {
            foreach (var child in InternalChildren)
                child.Alpha = 0;

            HitObject rootHitObject = result.HitObject;

            while (rootHitObject is IHasParent hasParent)
                rootHitObject = hasParent.Parent;

            // This should never happen in normal gameplay, but may be the case in testing scenarios where a generic result and/or hit object are provided
            if (result is not OsuJudgementResult osuResult || rootHitObject is not OsuHitObject rootOsuHitObject)
            {
                currentPiece = piece;
                return;
            }

            if (rootOsuHitObject.LastInCombo)
            {
                switch (osuResult.ComboResultAfterJudgement)
                {
                    case ComboResult.Perfect:
                        currentPiece = piecePerfectCombo ?? piece;
                        break;

                    case ComboResult.Good:
                        currentPiece = pieceGoodCombo ?? piece;
                        break;

                    default:
                        currentPiece = piece;
                        break;
                }
            }
            else
                currentPiece = piece;
        }

        public void PlayAnimation() => (currentPiece as IAnimatableJudgement)?.PlayAnimation();

        public Drawable? GetAboveHitObjectsProxiedContent() => new Container
        {
            AutoSizeAxes = Axes.Both,
            Origin = Anchor.Centre,
            ChildrenEnumerable = InternalChildren.Select(child => ((IAnimatableJudgement)child).GetAboveHitObjectsProxiedContent()),
        };
    }
}
