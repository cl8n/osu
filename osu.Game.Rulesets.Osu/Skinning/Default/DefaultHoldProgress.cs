// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Diagnostics;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Utils;
using osu.Game.Graphics;
using osu.Game.Graphics.Sprites;
using osu.Game.Rulesets.Objects.Drawables;
using osu.Game.Rulesets.Osu.Objects.Drawables;
using osuTK;
using osuTK.Graphics;

namespace osu.Game.Rulesets.Osu.Skinning.Default
{
    public partial class DefaultHoldProgress : CompositeDrawable
    {
        private readonly Container background;
        private readonly CircularProgress backgroundProgress;
        private readonly SpriteText holdText;
        private readonly Container trackingContainer;
        private readonly CircularProgress trackingProgress;

        private DrawableHold? drawableHold;
        private readonly IBindable<bool> tracking = new Bindable<bool>();
        private readonly IBindable<Color4> colour = new Bindable<Color4>();

        public DefaultHoldProgress()
        {
            RelativeSizeAxes = Axes.Both;

            InternalChildren = new Drawable[]
            {
                background = new Container
                {
                    RelativeSizeAxes = Axes.Both,
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    Colour = Color4.Gray,
                    Children = new Drawable[]
                    {
                        new Circle
                        {
                            RelativeSizeAxes = Axes.Both,
                            Alpha = 0.5f,
                        },
                        backgroundProgress = new CircularProgress
                        {
                            RelativeSizeAxes = Axes.Both,
                        },
                    },
                },
                trackingContainer = new Container
                {
                    RelativeSizeAxes = Axes.Both,
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    Children = new Drawable[]
                    {
                        trackingProgress = new CircularProgress
                        {
                            RelativeSizeAxes = Axes.Both,
                            Alpha = 0.5f,
                            Blending = BlendingParameters.Additive,
                        },
                        new CircularContainer
                        {
                            BorderColour = Color4.White,
                            BorderThickness = 12f / DrawableHold.TRACKING_AREA,
                            Masking = true,
                            RelativeSizeAxes = Axes.Both,
                            Child = new Box
                            {
                                RelativeSizeAxes = Axes.Both,
                                Alpha = 0,
                                AlwaysPresent = true,
                            },
                        },
                    },
                },
                holdText = new OsuSpriteText
                {
                    Text = "Hold!",
                    Font = OsuFont.Default.With(size: 30),
                    Anchor = Anchor.TopCentre,
                    Origin = Anchor.BottomCentre,
                },
            };
        }

        [BackgroundDependencyLoader]
        private void load(DrawableHitObject? drawableHitObject)
        {
            drawableHold = drawableHitObject as DrawableHold;

            if (drawableHold == null)
                return;

            colour.BindTo(drawableHold.AccentColour);
            colour.BindValueChanged(colour =>
            {
                trackingContainer.Colour = colour.NewValue;
            }, true);

            tracking.BindTo(drawableHold.Tracking);
            tracking.BindValueChanged(tracking =>
            {
                if (drawableHold.Judged)
                    return;

                using (BeginAbsoluteSequence(Math.Max(Time.Current, drawableHold.HitObject?.StartTime ?? 0)))
                {
                    if (tracking.NewValue)
                        onPress();
                    else
                        onRelease();
                }
            }, true);
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            if (drawableHold != null)
            {
                drawableHold.HitObjectApplied += onHitObjectApplied;
                onHitObjectApplied(drawableHold);

                drawableHold.ApplyCustomUpdateState += updateStateTransforms;
                updateStateTransforms(drawableHold, drawableHold.State.Value);
            }

            setInnerRadiusAsBorderThickness(backgroundProgress, 12);
            // setInnerRadiusAsBorderThickness(trackingProgress, 40);

            static void setInnerRadiusAsBorderThickness(CircularProgress progress, float thickness)
            {
                float finalRadius = progress.DrawWidth * DrawableHold.TRACKING_AREA * 0.5f;

                progress.InnerRadius = thickness / finalRadius;
            }
        }

        protected override void Update()
        {
            base.Update();

            Debug.Assert(drawableHold != null);

            backgroundProgress.Progress = drawableHold.Progress;
            trackingProgress.Progress = drawableHold.Progress;
        }

        private void onHitObjectApplied(DrawableHitObject drawableObject)
        {
            background.ScaleTo(1)
                      .FadeOut();
            trackingContainer.ScaleTo(1)
                             .FadeOut();

            holdText.ScaleTo(1)
                    .MoveToY(0)
                    .FadeOut()
                    .TransformSpacingTo(Vector2.Zero);

            // Immediately play out any pending transforms from press/release
            FinishTransforms(true);
        }

        private void updateStateTransforms(DrawableHitObject d, ArmedState state)
        {
            if (d is DrawableHold hold && state == ArmedState.Idle)
            {
                using (BeginAbsoluteSequence(hold.AnimationStartTime.Value))
                {
                    background.ScaleTo(DrawableHold.TRACKING_AREA, hold.HitObject.TimeFadeIn, Easing.OutQuint)
                              .FadeIn(hold.HitObject.TimeFadeIn);

                    holdText.MoveToY(-30, hold.HitObject.TimeFadeIn, Easing.OutQuint)
                            .FadeIn(hold.HitObject.TimeFadeIn)
                            .TransformSpacingTo(new Vector2(6, 0), hold.HitObject.TimePreempt, Easing.Out)
                            .ScaleTo(1.3f, hold.HitObject.TimePreempt, Easing.Out)
                            .Then()
                            .FadeOut(hold.HitObject.TimeFadeIn);
                }

                return;
            }

            if (d is not DrawableHoldTick drawableHoldTick)
                return;

            switch (state)
            {
                case ArmedState.Hit:
                    using (BeginAbsoluteSequence(d.HitStateUpdateTime))
                    {
                        if (drawableHoldTick.HitObject.IsLastTick)
                            onEnd();
                        else
                            onTick();
                    }

                    break;

                case ArmedState.Miss:
                    using (BeginAbsoluteSequence(d.HitStateUpdateTime))
                        onBreak();

                    break;
            }
        }

        protected override void Dispose(bool isDisposing)
        {
            base.Dispose(isDisposing);

            if (drawableHold != null)
            {
                drawableHold.HitObjectApplied -= onHitObjectApplied;
                drawableHold.ApplyCustomUpdateState -= updateStateTransforms;
            }
        }

        private void onPress()
        {
            const float duration = 300;

            if (Precision.AlmostEquals(0, trackingContainer.Alpha))
                trackingContainer.ScaleTo(1);

            background.FadeOut(duration, Easing.OutQuint);
            trackingContainer.ScaleTo(DrawableHold.TRACKING_AREA, duration, Easing.OutQuint)
                             .FadeIn(duration, Easing.OutQuint);
        }

        private void onRelease()
        {
            const float duration = 150;

            background.FadeIn(duration, Easing.OutQuint);
            trackingContainer.ScaleTo(DrawableHold.TRACKING_AREA * 1.2f, duration, Easing.OutQuint)
                             .FadeOut(duration, Easing.OutQuint);
        }

        private void onEnd()
        {
            const float duration = 300;

            background.FadeIn(duration / 2, Easing.OutQuint);
            trackingContainer.ScaleTo(1, duration, Easing.OutQuint)
                             .FadeOut(duration / 2, Easing.OutQuint);
        }

        private void onTick()
        {
            if (trackingContainer.Scale.X >= DrawableHold.TRACKING_AREA * 0.98f)
            {
                trackingContainer.ScaleTo(DrawableHold.TRACKING_AREA * 1.08f, 40, Easing.OutQuint)
                                 .Then()
                                 .ScaleTo(DrawableHold.TRACKING_AREA, 200, Easing.OutQuint);
            }
        }

        private void onBreak()
        {
        }
    }
}
