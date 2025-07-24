// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Shapes;
using osu.Game.Graphics;
using osu.Game.Rulesets.Osu.Objects;
using osu.Game.Rulesets.Osu.Objects.Drawables;
using osu.Game.Rulesets.Osu.Skinning.Default;
using osuTK;

namespace osu.Game.Rulesets.Osu.Edit.Blueprints.Holds.Components
{
    public partial class HoldPiece : BlueprintPiece<Hold>
    {
        private readonly Circle circle;
        private readonly RingPiece ring;

        public HoldPiece(bool pendingPlacement = false)
        {
            Origin = Anchor.Centre;

            // Size = OsuHitObject.OBJECT_DIMENSIONS;
            AutoSizeAxes = Axes.Both;

            InternalChildren = new Drawable[]
            {
                circle = new Circle
                {
                    // RelativeSizeAxes = Axes.Both,
                    Size = OsuHitObject.OBJECT_DIMENSIONS,
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    Scale = new Vector2(DrawableHold.TRACKING_AREA),
                    Alpha = pendingPlacement ? 0.25f : 0.5f,
                },
                ring = new RingPiece(),
            };
        }

        [BackgroundDependencyLoader]
        private void load(OsuColour colours)
        {
            Colour = colours.Yellow;
        }

        public void BeginPlacement()
        {
            circle.FadeTo(0.5f, 150, Easing.OutQuint);
        }

        public override void UpdateFrom(Hold hitObject)
        {
            base.UpdateFrom(hitObject);

            Scale = new Vector2(hitObject.Scale);
        }

        public override bool ReceivePositionalInputAt(Vector2 screenSpacePos) => ring.ReceivePositionalInputAt(screenSpacePos);
    }
}
