// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Allocation;
using osu.Framework.Platform;

namespace osu.Game.Screens.Utility
{
    public class CustomLatencyCertifierScreen : LatencyCertifierScreen
    {
        [Resolved]
        private GameHost host { get; set; } = null!;

        protected override int mapDifficultyToTargetFrameRate(int difficulty)
        {
            switch (difficulty)
            {
                case 1:
                    return getHz(1);

                case 2:
                    return getHz(2);

                case 3:
                    return getHz(3);

                case 4:
                    return getHz(4);

                case 5:
                    return getHz(8);

                case 6:
                    return getHz(16);

                case 7:
                    return getHz(32);

                case 8:
                    return getHz(48);

                case 9:
                    return getHz(64);

                default:
                    return 1000 + ((difficulty - 10) * 500);
            }
        }

        private int getHz(double multiplier)
        {
            var displayMode = host.Window!.CurrentDisplayMode.Value;
            return Convert.ToInt32(displayMode.RefreshRate / Math.Pow(2, Math.Floor(Math.Log(displayMode.RefreshRate / 10d, 2))) * multiplier);
        }
    }
}
