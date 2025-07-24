// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Game.Rulesets.Osu.UI;

namespace osu.Game.Rulesets.Osu.Objects.Drawables
{
    public partial class DrawableHoldHead : DrawableHitCircle
    {
        protected new DrawableHold? ParentHitObject => (DrawableHold?)base.ParentHitObject;

        public DrawableHoldHead()
            : this(null)
        {
        }

        public DrawableHoldHead(HoldHeadCircle? holdHeadCircle)
            : base(holdHeadCircle)
        {
        }

        protected override void UpdatePosition()
        {
            // Hold head is always drawn at (0,0).
        }

        protected override void OnApply()
        {
            base.OnApply();

            CheckHittable = (d, t, r) => ParentHitObject?.CheckHittable?.Invoke(d, t, r) ?? ClickAction.Hit;
        }

        protected override void CheckForResult(bool userTriggered, double timeOffset)
        {
            base.CheckForResult(userTriggered, timeOffset);

            ParentHitObject?.HoldInputManager.PostProcessHeadJudgement(this);
        }

        public override void Shake()
        {
            base.Shake();

            ParentHitObject?.Shake();
        }
    }
}
