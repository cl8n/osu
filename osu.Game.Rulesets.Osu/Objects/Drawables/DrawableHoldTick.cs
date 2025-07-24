// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace osu.Game.Rulesets.Osu.Objects.Drawables
{
    public partial class DrawableHoldTick : DrawableOsuHitObject
    {
        public new HoldTick HitObject => (HoldTick)base.HitObject;
        protected new DrawableHold? ParentHitObject => (DrawableHold?)base.ParentHitObject;

        // todo check hit lighting for this / try to match what Slider does
        public override bool DisplayResult => HitObject.IsLastTick;

        public DrawableHoldTick()
            : this(null)
        {
        }

        public DrawableHoldTick(HoldTick? holdTick)
            : base(holdTick)
        {
        }

        protected override void CheckForResult(bool userTriggered, double timeOffset) => ParentHitObject?.HoldInputManager.TryJudgeTick(this, timeOffset);
    }
}
