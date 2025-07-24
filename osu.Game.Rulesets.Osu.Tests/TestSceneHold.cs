// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using osu.Framework.Graphics;
using osu.Framework.Screens;
using osu.Game.Audio;
using osu.Game.Beatmaps;
using osu.Game.Beatmaps.ControlPoints;
using osu.Game.Replays;
using osu.Game.Rulesets.Judgements;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Osu.Objects;
using osu.Game.Rulesets.Osu.Objects.Drawables;
using osu.Game.Rulesets.Osu.Replays;
using osu.Game.Rulesets.Osu.UI;
using osu.Game.Rulesets.Replays;
using osu.Game.Rulesets.Scoring;
using osu.Game.Scoring;
using osu.Game.Screens.Play;
using osu.Game.Tests.Visual;
using osuTK;

namespace osu.Game.Rulesets.Osu.Tests
{
    [TestFixture]
    public partial class TestSceneHold : RateAdjustedBeatmapTestScene
    {
        private int depthIndex;

        // [Resolved]
        // private OsuConfigManager config { get; set; } = null!;

        private const double hold_time_start = 1000;
        private const double hold_time_end = 3000;
        private static readonly Vector2 hold_position = OsuPlayfield.BASE_SIZE / 2;
        private static readonly Vector2 inside_follow = hold_position + new Vector2(35, 0);
        private static readonly Vector2 outside_follow = hold_position + new Vector2(70, 0);

        private ScoreAccessibleReplayPlayer currentPlayer = null!;

        private readonly List<JudgementResult> judgementResults = new List<JudgementResult>();

        [Test]
        public void TestHitEarlyMoveIntoFollowRegion()
        {
            performTest(new List<ReplayFrame>
            {
                new OsuReplayFrame(hold_time_start - 150, hold_position, OsuAction.LeftButton),
                new OsuReplayFrame(hold_time_start - 100, inside_follow, OsuAction.LeftButton),
                new OsuReplayFrame(hold_time_end - 100, inside_follow, OsuAction.LeftButton),
            });

            assertHeadJudgement(HitResult.Meh);
            assertTickJudgement(HitResult.LargeTickHit);
            assertHoldJudgement(HitResult.IgnoreHit);
        }

        // [Test]
        // public void TestHits()
        // {
        //     AddStep("Hit Big Single", () => SetContents(_ => testSingle(2, true)));
        //     AddStep("Hit Medium Single", () => SetContents(_ => testSingle(5, true)));
        //     AddStep("Hit Small Single", () => SetContents(_ => testSingle(7, true)));
        //     AddStep("High combo index", () => SetContents(_ => testSingle(2, true, comboIndex: 15)));
        // }
        //
        // [Test]
        // public void TestMisses()
        // {
        //     AddStep("Miss Big Single", () => SetContents(_ => testSingle(2)));
        //     AddStep("Miss Medium Single", () => SetContents(_ => testSingle(5)));
        //     AddStep("Miss Small Single", () => SetContents(_ => testSingle(7)));
        // }
        //
        // [Test]
        // public void TestHitLighting()
        // {
        //     AddToggleStep("toggle hit lighting", v => config.SetValue(OsuSetting.HitLighting, v));
        //     AddStep("Hit Big Single", () => SetContents(_ => testSingle(2, true)));
        // }

        private Drawable testSingle(float circleSize, bool auto = false, double timeOffset = 0, Vector2? positionOffset = null, int comboIndex = 0)
        {
            var playfield = new TestOsuPlayfield();

            for (double t = timeOffset; t < timeOffset + 60000; t += 3000)
                playfield.Add(createSingle(circleSize, auto, t, positionOffset, comboIndex: comboIndex));

            return playfield;
        }

        private TestDrawableHold createSingle(float circleSize, bool auto, double timeOffset, Vector2? positionOffset, double hitOffset = 0, int comboIndex = 0)
        {
            positionOffset ??= Vector2.Zero;

            double startTime = Time.Current + 1000 + timeOffset;
            var hold = new Hold
            {
                StartTime = startTime,
                EndTime = startTime + 1000,
                Position = OsuPlayfield.BASE_SIZE / 4 + positionOffset.Value,
                IndexInCurrentCombo = comboIndex,
            };

            hold.ApplyDefaults(new ControlPointInfo(), new BeatmapDifficulty { CircleSize = circleSize });

            var drawable = CreateDrawableHold(hold, auto, hitOffset);

            foreach (var mod in SelectedMods.Value.OfType<IApplicableToDrawableHitObject>())
                mod.ApplyToDrawableHitObject(drawable);
            return drawable;
        }

        protected virtual TestDrawableHold CreateDrawableHold(Hold hold, bool auto, double hitOffset = 0) => new TestDrawableHold(hold, auto, hitOffset)
        {
            Depth = depthIndex++
        };

        protected partial class TestDrawableHold : DrawableHold
        {
            private readonly bool auto;
            private readonly double hitOffset;

            public TestDrawableHold(Hold hold, bool auto, double hitOffset)
                : base(hold)
            {
                this.auto = auto;
                this.hitOffset = hitOffset;
            }

            public void TriggerJudgement() => Schedule(() => UpdateResult(true));

            protected override void CheckForResult(bool userTriggered, double timeOffset)
            {
                if (auto && !userTriggered && timeOffset > hitOffset && CheckHittable?.Invoke(this, Time.Current, HitResult.Great) == ClickAction.Hit)
                {
                    // force success
                    ApplyResult(HitResult.Great);
                }
                else
                    base.CheckForResult(userTriggered, timeOffset);
            }
        }

        protected partial class TestOsuPlayfield : OsuPlayfield
        {
            public TestOsuPlayfield()
            {
                RelativeSizeAxes = Axes.Both;
            }
        }

        private void assertHeadJudgement(HitResult result)
        {
            AddAssert(
                "check head result",
                () => judgementResults.SingleOrDefault(r => r.HitObject is HoldHeadCircle)?.Type,
                () => Is.EqualTo(result));
        }

        private void assertTickJudgement(HitResult result)
        {
            AddAssert(
                "check tick result",
                () => judgementResults.Where(r => r.HitObject is HoldTick).Select(r => r.Type),
                () => Has.All.EqualTo(result));
        }

        private void assertHoldJudgement(HitResult result)
        {
            AddAssert(
                "check slider result",
                () => judgementResults.SingleOrDefault(r => r.HitObject is Hold)?.Type,
                () => Is.EqualTo(result));
        }

        private void performTest(List<ReplayFrame> frames, Action<Hold>? adjustHoldFunc = null)
        {
            Hold hold = new Hold
            {
                StartTime = hold_time_start,
                EndTime = hold_time_end,
                Position = hold_position,
                Samples =
                {
                    new HitSampleInfo(HitSampleInfo.HIT_NORMAL)
                },
            };

            adjustHoldFunc?.Invoke(hold);

            AddStep("load player", () =>
            {
                Beatmap.Value = CreateWorkingBeatmap(new Beatmap<OsuHitObject>
                {
                    HitObjects = { hold },
                    BeatmapInfo =
                    {
                        Difficulty = new BeatmapDifficulty
                        {
                            CircleSize = 5,
                            OverallDifficulty = 0,
                            SliderTickRate = 2,
                        },
                        Ruleset = new OsuRuleset().RulesetInfo,
                    }
                });

                var p = new ScoreAccessibleReplayPlayer(new Score { Replay = new Replay { Frames = frames } });

                p.OnLoadComplete += _ =>
                {
                    p.ScoreProcessor.NewJudgement += result =>
                    {
                        if (currentPlayer == p) judgementResults.Add(result);
                    };
                };

                LoadScreen(currentPlayer = p);
                judgementResults.Clear();
            });

            AddUntilStep("Beatmap at 0", () => Beatmap.Value.Track.CurrentTime == 0);
            AddUntilStep("Wait until player is loaded", () => currentPlayer.IsCurrentScreen());
            AddUntilStep("Wait for completion", () => currentPlayer.ScoreProcessor.HasCompleted.Value);
        }

        private partial class ScoreAccessibleReplayPlayer : ReplayPlayer
        {
            public new ScoreProcessor ScoreProcessor => base.ScoreProcessor;

            protected override bool PauseOnFocusLost => false;

            public ScoreAccessibleReplayPlayer(Score score)
                : base(score, new PlayerConfiguration
                {
                    AllowPause = false,
                    ShowResults = false,
                })
            {
            }
        }
    }
}
