// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics.Primitives;
using osu.Game.Configuration;
using osu.Game.Rulesets.Osu.Edit.Blueprints.Holds.Components;
using osu.Game.Rulesets.Osu.Objects;
using osu.Game.Rulesets.Osu.Objects.Drawables;
using osuTK;

namespace osu.Game.Rulesets.Osu.Edit.Blueprints.Holds
{
    public partial class HoldSelectionBlueprint : OsuSelectionBlueprint<Hold>
    {
        protected new DrawableHold DrawableObject => (DrawableHold)base.DrawableObject;

        private readonly HoldPiece piece;
        private readonly Bindable<bool> showHitMarkers = new Bindable<bool>();

        public HoldSelectionBlueprint(Hold hold)
            : base(hold)
        {
            InternalChild = piece = new HoldPiece();
        }

        [BackgroundDependencyLoader]
        private void load(OsuConfigManager config)
        {
            config.BindWith(OsuSetting.EditorShowHitMarkers, showHitMarkers);
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            showHitMarkers.BindValueChanged(_ =>
            {
                if (!showHitMarkers.Value)
                    DrawableObject.RestoreHitAnimations();
            });
        }

        protected override void Update()
        {
            base.Update();

            piece.UpdateFrom(HitObject);

            if (showHitMarkers.Value)
                DrawableObject.SuppressHitAnimations();
        }

        public override bool ReceivePositionalInputAt(Vector2 screenSpacePos) =>
            DrawableObject.HeadCircle.HitArea.ReceivePositionalInputAt(screenSpacePos);

        public override Quad SelectionQuad => piece.ScreenSpaceDrawQuad;
    }
}
