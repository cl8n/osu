// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace osu.Game.Rulesets.Objects.Types
{
    /// <summary>
    /// A <see cref="HitObject"/> that belongs to a parent <see cref="HitObject"/>.
    /// </summary>
    public interface IHasParent
    {
        /// <summary>
        /// The parent <see cref="HitObject"/>.
        /// </summary>
        HitObject Parent { get; }
    }
}
