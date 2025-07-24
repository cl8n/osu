// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Audio;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Game.Audio;
using osu.Game.Graphics.Containers;
using osu.Game.Rulesets.Judgements;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Objects.Drawables;
using osu.Game.Rulesets.Osu.Judgements;
using osu.Game.Rulesets.Osu.Skinning;
using osu.Game.Rulesets.Osu.Skinning.Default;
using osu.Game.Skinning;
using osuTK;

namespace osu.Game.Rulesets.Osu.Objects.Drawables
{
    public partial class DrawableHold : DrawableOsuHitObject
    {
        public const float TRACKING_AREA = 2.4f;
        private const double fade_out_duration = 240;

        public new Hold HitObject => (Hold)base.HitObject;

        // todo rename type now that it's shared
        public new OsuSliderJudgementResult Result => (OsuSliderJudgementResult)base.Result;

        public DrawableHoldHead HeadCircle => headContainer.Child;
        public SkinnableDrawable ProgressPiece = null!;

        private PausableSkinnableSound holdSample = null!;
        private bool frequencyModulate;
        private const float sample_initial_frequency = 1.0f;
        private const float sample_modulated_base_frequency = 0.5f;

        protected override IEnumerable<Drawable> DimmablePieces => new Drawable[]
        {
            // HeadCircle should not be added to this list, as it handles dimming itself
            ProgressPiece,
        };

        public override bool DisplayResult => false;

        public readonly HoldInputManager HoldInputManager;

        private ShakeContainer shakeContainer = null!;
        private Container<DrawableHoldHead> headContainer = null!;
        private Container<DrawableHoldTick> tickContainer = null!;

        public DrawableHold()
            : this(null)
        {
        }

        public DrawableHold(Hold? hold)
            : base(hold)
        {
            HoldInputManager = new HoldInputManager(this);
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            Origin = Anchor.Centre;

            AddRangeInternal(new Drawable[]
            {
                HoldInputManager,
                tickContainer = new Container<DrawableHoldTick>(),
                shakeContainer = new ShakeContainer
                {
                    ShakeDuration = 30,
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    RelativeSizeAxes = Axes.Both,
                    Children = new[]
                    {
                        ProgressPiece = new SkinnableDrawable(new OsuSkinComponentLookup(OsuSkinComponents.HoldProgress), _ => new DefaultHoldProgress())
                        {
                            Origin = Anchor.Centre,
                            Anchor = Anchor.Centre,
                        },
                    },
                },
                // hold head is not included in shake as it handles hit detection, and handles its own shaking.
                // headContainer = new Container<DrawableHoldHead> { RelativeSizeAxes = Axes.Both },
                headContainer = new Container<DrawableHoldHead>
                {
                    Anchor = Anchor.Centre,
                },
                holdSample = new PausableSkinnableSound
                {
                    Volume = { Value = 0 },
                    MinimumSampleVolume = MINIMUM_SAMPLE_VOLUME,
                    Looping = true,
                    Frequency = { Value = sample_initial_frequency }
                },
            });

            PositionBindable.BindValueChanged(_ => Position = HitObject.StackedPosition);
            StackHeightBindable.BindValueChanged(_ => Position = HitObject.StackedPosition);
            ScaleBindable.BindValueChanged(scale => shakeContainer.Scale = new Vector2(scale.NewValue));

            AccentColour.BindValueChanged(colour =>
            {
                foreach (var nested in NestedHitObjects)
                    nested.AccentColour.Value = colour.NewValue;
            }, true);
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            Tracking.BindValueChanged(updateHoldSample);
        }

        protected override JudgementResult CreateResult(Judgement judgement) => new OsuSliderJudgementResult(HitObject, judgement);

        public override void Shake() => shakeContainer.Shake();

        protected override void OnFree()
        {
            base.OnFree();

            holdSample.ClearSamples();
        }

        protected override void LoadSamples()
        {
            base.LoadSamples();

            holdSample.Samples = HitObject.CreateHoldSamples().Cast<ISampleInfo>().ToArray();
            holdSample.Frequency.Value = sample_initial_frequency;
        }

        private void updateHoldSample(ValueChangedEvent<bool> tracking)
        {
            // todo only play if Time.Current >= HitObject.StartTime ?

            if (tracking.NewValue)
            {
                if (!holdSample.RequestedPlaying)
                    holdSample.Play();

                holdSample.VolumeTo(1, 300);
            }
            else
            {
                holdSample.VolumeTo(0, fade_out_duration);
            }
        }

        public override void StopAllSamples()
        {
            base.StopAllSamples();

            holdSample.Stop();
        }

        protected override void AddNestedHitObject(DrawableHitObject hitObject)
        {
            base.AddNestedHitObject(hitObject);

            switch (hitObject)
            {
                case DrawableHoldHead head:
                    headContainer.Child = head;
                    Size = head.Size;
                    break;

                case DrawableHoldTick tick:
                    tickContainer.Add(tick);
                    break;
            }
        }

        protected override void ClearNestedHitObjects()
        {
            base.ClearNestedHitObjects();

            headContainer.Clear(false);
            tickContainer.Clear(false);
        }

        protected override DrawableHitObject CreateNestedHitObject(HitObject hitObject)
        {
            switch (hitObject)
            {
                case HoldHeadCircle holdHeadCircle:
                    return new DrawableHoldHead(holdHeadCircle);

                case HoldTick holdTick:
                    return new DrawableHoldTick(holdTick);
            }

            return base.CreateNestedHitObject(hitObject);
        }

        public readonly Bindable<bool> Tracking = new Bindable<bool>();

        protected override void Update()
        {
            base.Update();

            Tracking.Value = HoldInputManager.Tracking;

            if (frequencyModulate)
                holdSample.Frequency.Value = sample_modulated_base_frequency + Progress;

            holdSample.Balance.Value = CalculateSamplePlaybackBalance(SamplePlaybackPosition);
        }

        public double Progress => Math.Clamp((Time.Current - HitObject.StartTime) / HitObject.Duration, 0, 1);

        protected override void ApplySkin(ISkinSource skin, bool allowFallback)
        {
            base.ApplySkin(skin, allowFallback);

            frequencyModulate = skin.GetConfig<OsuSkinConfiguration, bool>(OsuSkinConfiguration.SpinnerFrequencyModulate)?.Value ?? true;
        }

        protected override void CheckForResult(bool userTriggered, double timeOffset)
        {
            if (userTriggered || Time.Current < HitObject.EndTime)
                return;

            if (NestedHitObjects.Any(h => !h.Judged))
                return;

            ApplyResult(static (r, hitObject) =>
            {
                r.Type = hitObject.NestedHitObjects.Any(h => h.Result.IsHit) ? r.Judgement.MaxResult : r.Judgement.MinResult;
            });
        }

        public override void PlaySamples()
        {
            // Nested objects handle sample playback
        }

        // protected override void UpdateStartTimeStateTransforms()
        // {
        //     base.UpdateStartTimeStateTransforms();
        //
        //     Ball.FadeIn();
        //     Ball.ScaleTo(HitObject.Scale);
        // }

        protected override void UpdateHitStateTransforms(ArmedState state)
        {
            base.UpdateHitStateTransforms(state);

            this.FadeOut(fade_out_duration).OnComplete(_ =>
            {
                // looping sample should be stopped here as it is safer than running in the OnComplete
                // of the volume transition above.
                holdSample.Stop();
            });

            Expire();

            // skin change does a rewind of transforms, which will stop the spinning sound from playing if it's currently in playback.
            Tracking.TriggerChange();
        }

        // tODO: check
        public override bool ReceivePositionalInputAt(Vector2 screenSpacePos) => HeadCircle.ReceivePositionalInputAt(screenSpacePos);

        #region FOR EDITOR USE ONLY, DO NOT USE FOR ANY OTHER PURPOSE

        internal void SuppressHitAnimations()
        {
            UpdateState(ArmedState.Idle);
            HeadCircle.SuppressHitAnimations();

            // This method is called every frame in editor contexts, thus the lack of need for transforms.

            if (Time.Current >= HitStateUpdateTime)
            {
                // Apply the slider's alpha to *only* the body.
                // This allows start and – more importantly – end circles to fade slower than the overall slider.
                if (Alpha < 1)
                    // Body.Alpha = Alpha;
                    ProgressPiece.Alpha = Alpha;
                Alpha = 1;
            }

            LifetimeEnd = HitStateUpdateTime + 700;
        }

        internal void RestoreHitAnimations()
        {
            UpdateState(ArmedState.Hit);
            HeadCircle.RestoreHitAnimations();
        }

        #endregion
    }
}
