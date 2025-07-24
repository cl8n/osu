// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Input;
using osu.Framework.Input.Events;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Objects.Drawables;
using osu.Game.Screens.Play;
using osuTK;

namespace osu.Game.Rulesets.Osu.Objects.Drawables
{
    public partial class HoldInputManager : Component, IRequireHighFrequencyMousePosition
    {
        /// <summary>
        /// Whether the hold is currently being tracked.
        /// </summary>
        public bool Tracking { get; private set; }

        [Resolved]
        private IGameplayClock? gameplayClock { get; set; }

        /// <summary>
        /// The point in time after which we can accept any key for tracking. Before this time, we may need to restrict tracking to the key used to hit the head circle.
        ///
        /// This is a requirement to stop the case where a player holds down one key (from before the hold) and taps the second key while maintaining full scoring (tracking) of holds.
        /// Visually, this special case can be seen below (time increasing from left to right):
        ///
        ///  Z  Z+X  Z
        ///      o========o
        ///
        /// Without this logic, tracking would continue through the entire hold even though no key hold action is directly attributing to it.
        ///
        /// In all other cases, no special handling is required (either key being pressed is allowable as valid tracking).
        ///
        /// The reason for storing this as a time value (rather than a bool) is to correctly handle rewind scenarios.
        /// </summary>
        private double? timeToAcceptAnyKeyAfter;

        /// <summary>
        /// The actions that were pressed in the previous frame.
        /// </summary>
        private readonly List<OsuAction> lastPressedActions = new List<OsuAction>();

        private Vector2? screenSpaceMousePosition;
        private readonly DrawableHold hold;

        public HoldInputManager(DrawableHold hold)
        {
            this.hold = hold;
            this.hold.HitObjectApplied += resetState;
        }

        /// <summary>
        /// This component handles all input of the hold, so it should receive input no matter the position.
        /// </summary>
        public override bool ReceivePositionalInputAt(Vector2 screenSpacePos) => true;

        protected override bool OnMouseMove(MouseMoveEvent e)
        {
            screenSpaceMousePosition = e.ScreenSpaceMousePosition;

            return base.OnMouseMove(e);
        }

        protected override void Update()
        {
            base.Update();

            updateTracking(IsMouseInFollowArea(Tracking));
        }

        public void PostProcessHeadJudgement(DrawableHoldHead head)
        {
            if (!head.Judged || !head.Result.IsHit)
                return;

            if (!IsMouseInFollowArea(true))
                return;

            // When the head is hit late:
            // - If the cursor has at all times been within range of the expanded follow area, hit all nested objects that have been passed through.
            // - If the cursor has at some point left the expanded follow area, miss those nested objects instead.
            foreach (var nested in hold.NestedHitObjects.OfType<DrawableOsuHitObject>())
            {
                // Skip nested objects that are already judged.
                if (nested.Judged)
                    continue;

                // Stop the process when a nested object is reached that can't be hit before the current time.
                if (nested.HitObject.StartTime > Time.Current)
                    break;

                nested.HitForcefully();
            }

            updateTracking(true);
        }

        public void TryJudgeTick(DrawableHoldTick drawableHoldTick, double timeOffset)
        {
            if (timeOffset < 0 || !hold.HeadCircle.Judged)
                return;

            if (Tracking)
                drawableHoldTick.HitForcefully();
            else
                drawableHoldTick.MissForcefully();
        }

        /// <summary>
        /// Whether the mouse is currently in the follow area.
        /// </summary>
        /// <param name="expanded">Whether to test against the maximum area of the follow circle.</param>
        public bool IsMouseInFollowArea(bool expanded)
        {
            if (screenSpaceMousePosition is not Vector2 pos)
                return false;

            float radius = (float)hold.HitObject.Radius;

            if (expanded)
                radius *= DrawableHold.TRACKING_AREA;

            return (hold.ToLocalSpace(pos) - hold.OriginPosition).LengthSquared <= radius * radius;
        }

        /// <summary>
        /// Updates the tracking state.
        /// </summary>
        /// <param name="isValidTrackingPosition">Whether the current mouse position is valid to begin tracking.</param>
        private void updateTracking(bool isValidTrackingPosition)
        {
            if (gameplayClock?.IsRewinding == true)
            {
                var trackingHistory = hold.Result.TrackingHistory;
                while (trackingHistory.TryPeek(out var historyEntry) && Time.Current < historyEntry.time)
                    trackingHistory.Pop();

                Debug.Assert(trackingHistory.Count > 0);

                Tracking = trackingHistory.Peek().tracking;
                return;
            }

            bool wasTracking = Tracking;

            // from the point at which the head circle is hit, this will be non-null.
            // it may be null if the head circle was missed.
            OsuAction? headCircleHitAction = getInitialHitAction();

            if (headCircleHitAction == null)
                timeToAcceptAnyKeyAfter = null;

            // if the head circle was hit with a specific key, tracking should only occur while that key is pressed.
            if (headCircleHitAction != null && timeToAcceptAnyKeyAfter == null)
            {
                var otherKey = headCircleHitAction == OsuAction.RightButton ? OsuAction.LeftButton : OsuAction.RightButton;

                // we can start accepting any key once all other keys have been released in the previous frame.
                if (!lastPressedActions.Contains(otherKey))
                    timeToAcceptAnyKeyAfter = Time.Current;
            }

            if (hold.OsuActionInputManager == null)
                return;

            lastPressedActions.Clear();
            bool validTrackingAction = false;

            foreach (OsuAction action in hold.OsuActionInputManager.PressedActions)
            {
                if (isValidTrackingAction(action))
                    validTrackingAction = true;

                lastPressedActions.Add(action);
            }

            Tracking =
                // even in an edge case where current time has exceeded the hold's time, we may not have finished judging.
                // we don't want to potentially update from Tracking=true to Tracking=false at this point.
                (!hold.AllJudged || Time.Current <= hold.HitObject.GetEndTime())
                // in valid position range
                && isValidTrackingPosition
                // valid action
                && validTrackingAction;

            if (wasTracking != Tracking)
                hold.Result.TrackingHistory.Push((Time.Current, Tracking));
        }

        private OsuAction? getInitialHitAction() => hold.HeadCircle?.HitAction;

        /// <summary>
        /// Check whether a given user input is a valid tracking action.
        /// </summary>
        private bool isValidTrackingAction(OsuAction action)
        {
            OsuAction? hitAction = getInitialHitAction();

            // if the head circle was hit, we may not yet be allowed to accept any key, so we must use the initial hit action.
            if (hitAction.HasValue && (!timeToAcceptAnyKeyAfter.HasValue || Time.Current <= timeToAcceptAnyKeyAfter.Value))
                return action == hitAction;

            return action == OsuAction.LeftButton || action == OsuAction.RightButton;
        }

        private void resetState(DrawableHitObject obj)
        {
            Tracking = false;
            timeToAcceptAnyKeyAfter = null;
            lastPressedActions.Clear();
            screenSpaceMousePosition = null;
        }

        protected override void Dispose(bool isDisposing)
        {
            base.Dispose(isDisposing);

            hold.HitObjectApplied -= resetState;
        }
    }
}
