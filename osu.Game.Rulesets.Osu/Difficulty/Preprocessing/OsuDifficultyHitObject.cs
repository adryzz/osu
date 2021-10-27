// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using osu.Game.Rulesets.Difficulty.Preprocessing;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Osu.Objects;
using osuTK;

namespace osu.Game.Rulesets.Osu.Difficulty.Preprocessing
{
    public class OsuDifficultyHitObject : DifficultyHitObject
    {
        private const int normalized_radius = 50; // Change radius to 50 to make 100 the diameter. Easier for mental maths.
        private const int min_delta_time = 25;

        protected new OsuHitObject BaseObject => (OsuHitObject)base.BaseObject;

        /// <summary>
        /// Normalized distance from the end position of the previous <see cref="OsuDifficultyHitObject"/> to the start position of this <see cref="OsuDifficultyHitObject"/>.
        /// </summary>
        public double JumpDistance { get; private set; }

        /// <summary>
        /// Minimum distance from the end position of the previous <see cref="OsuDifficultyHitObject"/> to the start position of this <see cref="OsuDifficultyHitObject"/>.
        /// </summary>
        public double MovementDistance { get; private set; }

        /// <summary>
        /// Normalized distance between the start and end position of the previous <see cref="OsuDifficultyHitObject"/>.
        /// </summary>
        public double TravelDistance { get; private set; }

        /// <summary>
        /// Angle the player has to take to hit this <see cref="OsuDifficultyHitObject"/>.
        /// Calculated as the angle between the circles (current-2, current-1, current).
        /// </summary>
        public double? Angle { get; private set; }

        /// <summary>
        /// Milliseconds elapsed since the end time of the Previous <see cref="OsuDifficultyHitObject"/>, with a minimum of 25ms.
        /// </summary>
        public double MovementTime { get; private set; }

        /// <summary>
        /// Milliseconds elapsed since from the start time of the Previous <see cref="OsuDifficultyHitObject"/> to the end time of the same Previous <see cref="OsuDifficultyHitObject"/>, with a minimum of 25ms.
        /// </summary>
        public double TravelTime { get; private set; }

        /// <summary>
        /// Milliseconds elapsed since the start time of the previous <see cref="OsuDifficultyHitObject"/>, with a minimum of 25ms.
        /// </summary>
        public readonly double StrainTime;

        private readonly OsuHitObject lastLastObject;
        private readonly OsuHitObject lastObject;

        public OsuDifficultyHitObject(HitObject hitObject, HitObject lastLastObject, HitObject lastObject, double clockRate)
            : base(hitObject, lastObject, clockRate)
        {
            this.lastLastObject = (OsuHitObject)lastLastObject;
            this.lastObject = (OsuHitObject)lastObject;

            // Capped to 25ms to prevent difficulty calculation breaking from simulatenous objects.
            StrainTime = Math.Max(DeltaTime, min_delta_time);

            setDistances(clockRate);
        }

        private void setDistances(double clockRate)
        {
            // We don't need to calculate either angle or distance when one of the last->curr objects is a spinner
            if (BaseObject is Spinner || lastObject is Spinner)
                return;

            // We will scale distances by this factor, so we can assume a uniform CircleSize among beatmaps.
            float scalingFactor = normalized_radius / (float)BaseObject.Radius;

            if (BaseObject.Radius < 30)
            {
                float smallCircleBonus = Math.Min(30 - (float)BaseObject.Radius, 5) / 50;
                scalingFactor *= 1 + smallCircleBonus;
            }

            if (lastObject is Slider lastSlider)
            {
                computeSliderCursorPosition(lastSlider);
                TravelDistance = 0;
                TravelTime = Math.Max(lastSlider.LazyTravelTime / clockRate, min_delta_time);
                MovementTime = Math.Max(StrainTime - lastSlider.LazyTravelTime / clockRate, min_delta_time);
                MovementDistance = Vector2.Subtract(lastSlider.TailCircle.StackedPosition, BaseObject.StackedPosition).Length * scalingFactor;

                int repeatCount = 0;

                for (int i = 1; i < lastSlider.NestedHitObjects.Count; i++)
                {
                    Vector2 currSlider = Vector2.Subtract(((OsuHitObject)lastSlider.NestedHitObjects[i]).StackedPosition, ((OsuHitObject)lastSlider.NestedHitObjects[i - 1]).StackedPosition);

                    if ((OsuHitObject)lastSlider.NestedHitObjects[i] is SliderRepeat && (OsuHitObject)lastSlider.NestedHitObjects[i - 1] is SliderRepeat)
                    {
                        repeatCount++;
                        TravelDistance += Math.Max(0, currSlider.Length * scalingFactor - 240); // remove 240 distance to avoid buffing overlapping followcircles.
                    }
                    else if ((OsuHitObject)lastSlider.NestedHitObjects[i] is SliderRepeat)
                    {
                        repeatCount++;
                        TravelDistance += Math.Max(0, currSlider.Length * scalingFactor - 120); // remove 120 distance to avoid buffing overlapping followcircles.
                    }
                    else if ((OsuHitObject)lastSlider.NestedHitObjects[i] is SliderEndCircle)
                    {
                        Vector2 possSlider = Vector2.Subtract((Vector2)lastSlider.LazyEndPosition, ((OsuHitObject)lastSlider.NestedHitObjects[i - 1]).StackedPosition);
                        TravelDistance += Math.Min(possSlider.Length, currSlider.Length) * scalingFactor; // Take the least distance from slider end vs lazy end.
                    }
                    else
                        TravelDistance += Math.Max(0, currSlider.Length * scalingFactor - 100); // remove 100 distance to avoid buffing overlapping ticks, mostly needed to prevent buffing slow sliders with high tick rate.
                }

                if (repeatCount == 0)
                {
                    TravelDistance = Math.Max(TravelDistance, scalingFactor * // idea here is to prevent ticks from dropping difficulty of slider by removing distance in calculation.
                                              Math.Min(Vector2.Subtract(((OsuHitObject)lastSlider.NestedHitObjects[lastSlider.NestedHitObjects.Count - 1]).StackedPosition, ((OsuHitObject)lastSlider.NestedHitObjects[0]).StackedPosition).Length,
                                                       Vector2.Subtract((Vector2)lastSlider.LazyEndPosition, ((OsuHitObject)lastSlider.NestedHitObjects[0]).StackedPosition).Length));
                }
                else
                    TravelDistance *= Math.Pow(1 + repeatCount / 2.0, 1.0 / 2.0); // Bonus for repeat sliders until a better per nested object strain system can be achieved.
            }

            Vector2 lastCursorPosition = getEndCursorPosition(lastObject);

            JumpDistance = (BaseObject.StackedPosition * scalingFactor - lastCursorPosition * scalingFactor).Length;
            MovementDistance = Math.Max(0, Math.Min(JumpDistance - 50, MovementDistance - 120)); // radius for jumpdistance is within 50 of maximum possible sliderLeniency, 120 for movement distance.

            if (lastLastObject != null && !(lastLastObject is Spinner))
            {
                Vector2 lastLastCursorPosition = getEndCursorPosition(lastLastObject);

                Vector2 v1 = lastLastCursorPosition - lastObject.StackedPosition;
                Vector2 v2 = BaseObject.StackedPosition - lastCursorPosition;

                float dot = Vector2.Dot(v1, v2);
                float det = v1.X * v2.Y - v1.Y * v2.X;

                Angle = Math.Abs(Math.Atan2(det, dot));
            }
        }

        private void computeSliderCursorPosition(Slider slider)
        {
            if (slider.LazyEndPosition != null)
                return;

            slider.LazyEndPosition = slider.StackedPosition;

            float approxFollowCircleRadius = (float)(slider.Radius * 1.4); // using 1.4 to better follow the real movement of a cursor.
            var computeVertex = new Action<double>(t =>
            {
                double progress = (t - slider.StartTime) / slider.SpanDuration;
                if (progress % 2 >= 1)
                    progress = 1 - progress % 1;
                else
                    progress %= 1;

                // ReSharper disable once PossibleInvalidOperationException (bugged in current r# version)
                var diff = slider.StackedPosition + slider.Path.PositionAt(progress) - slider.LazyEndPosition.Value;
                float dist = diff.Length;

                slider.LazyTravelTime = t - slider.StartTime;

                if (dist > approxFollowCircleRadius)
                {
                    // The cursor would be outside the follow circle, we need to move it
                    diff.Normalize(); // Obtain direction of diff
                    dist -= approxFollowCircleRadius;
                    slider.LazyEndPosition += diff * dist;
                    slider.LazyTravelDistance += dist;
                }
            });

            // Skip the head circle
            var scoringTimes = slider.NestedHitObjects.Skip(1).Select(t => t.StartTime);
            foreach (var time in scoringTimes)
                computeVertex(time);
        }

        private Vector2 getEndCursorPosition(OsuHitObject hitObject)
        {
            Vector2 pos = hitObject.StackedPosition;

            if (hitObject is Slider slider)
            {
                computeSliderCursorPosition(slider);
                pos = slider.LazyEndPosition ?? pos;
            }

            return pos;
        }
    }
}
