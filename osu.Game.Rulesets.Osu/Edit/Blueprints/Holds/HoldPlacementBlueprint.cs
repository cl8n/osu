// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Input.Events;
using osu.Game.Configuration;
using osu.Game.Rulesets.Edit;
using osu.Game.Rulesets.Osu.Edit.Blueprints.Holds.Components;
using osu.Game.Rulesets.Osu.Objects;
using osu.Game.Screens.Edit;
using osuTK;
using osuTK.Input;

namespace osu.Game.Rulesets.Osu.Edit.Blueprints.Holds
{
    public partial class HoldPlacementBlueprint : HitObjectPlacementBlueprint
    {
        public new Hold HitObject => (Hold)base.HitObject;

        private readonly HoldPiece piece;

        private bool isPlacingEnd;

        [Resolved]
        private IBeatSnapProvider? beatSnapProvider { get; set; }

        [Resolved]
        private OsuHitObjectComposer? composer { get; set; }

        [Resolved]
        private EditorClock? editorClock { get; set; }

        private Bindable<bool> limitedDistanceSnap { get; set; } = null!;

        public HoldPlacementBlueprint()
            : base(new Hold())
        {
            InternalChild = piece = new HoldPiece(pendingPlacement: true);
        }

        [BackgroundDependencyLoader]
        private void load(OsuConfigManager config)
        {
            limitedDistanceSnap = config.GetBindable<bool>(OsuSetting.EditorLimitedDistanceSnap);
        }

        protected override void Update()
        {
            base.Update();

            if (isPlacingEnd)
                updateEndTimeFromCurrent();

            piece.UpdateFrom(HitObject);
        }

        protected override bool OnMouseDown(MouseDownEvent e)
        {
            if (isPlacingEnd)
            {
                if (e.Button != MouseButton.Right)
                    return false;

                updateEndTimeFromCurrent();
                EndPlacement(true);
            }
            else
            {
                if (e.Button != MouseButton.Left)
                    return false;

                BeginPlacement(commitStart: true);
                piece.BeginPlacement();

                isPlacingEnd = true;
            }

            return true;
        }

        public override SnapResult UpdateTimeAndPosition(Vector2 screenSpacePosition, double fallbackTime)
        {
            if (isPlacingEnd)
                return base.UpdateTimeAndPosition(screenSpacePosition, fallbackTime);

            var result = composer?.TrySnapToNearbyObjects(screenSpacePosition, fallbackTime);
            result ??= composer?.TrySnapToDistanceGrid(screenSpacePosition, limitedDistanceSnap.Value && editorClock != null ? editorClock.CurrentTime : null);
            if (composer?.TrySnapToPositionGrid(result?.ScreenSpacePosition ?? screenSpacePosition, result?.Time ?? fallbackTime) is SnapResult gridSnapResult)
                result = gridSnapResult;
            result ??= new SnapResult(screenSpacePosition, fallbackTime);

            base.UpdateTimeAndPosition(result.ScreenSpacePosition, result.Time ?? fallbackTime);
            HitObject.Position = ToLocalSpace(result.ScreenSpacePosition);
            return result;
        }

        private void updateEndTimeFromCurrent()
        {
            HitObject.EndTime = beatSnapProvider == null
                ? Math.Max(HitObject.StartTime, EditorClock.CurrentTime)
                : Math.Max(HitObject.StartTime + beatSnapProvider.GetBeatLengthAtTime(HitObject.StartTime), beatSnapProvider.SnapTime(EditorClock.CurrentTime));
        }
    }
}
