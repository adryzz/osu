// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Graphics.Sprites;
using osu.Game.Overlays.Dialog;

namespace osu.Game.Screens.Edit
{
    public class ManualBeatmapChangesDialog : PopupDialog
    {
        public ManualBeatmapChangesDialog(Action exit, Action saveAndExit)
        {
            HeaderText = "When you are done with your manual changes press \"Reimport beatmap\"";

            Icon = FontAwesome.Regular.File;

            Buttons = new PopupDialogButton[]
            {
                new PopupDialogOkButton
                {
                    Text = @"Reimport beatmap",
                    Action = saveAndExit
                },
                new PopupDialogDangerousButton
                {
                    Text = @"Forget all manual changes",
                    Action = exit
                },
            };
        }
    }
}
