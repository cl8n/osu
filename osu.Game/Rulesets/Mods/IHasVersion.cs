// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Localisation;

namespace osu.Game.Rulesets.Mods
{
    /// <summary>
    /// An interface for mods that have had their behaviour updated in a backward-incompatible way.
    /// </summary>
    public interface IHasVersion
    {
        /// <summary>
        /// The version identifier.
        /// </summary>
        ushort Version { get; set; }

        /// <summary>
        /// A description of how this version of the mod differs from other versions.
        /// </summary>
        LocalisableString VersionDescription { get; }
    }
}
