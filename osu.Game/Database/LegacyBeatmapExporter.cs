// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System.IO;
using osu.Framework.Platform;
using osu.Game.Beatmaps;
using osu.Game.Extensions;

namespace osu.Game.Database
{
    public class LegacyBeatmapExporter : LegacyExporter<BeatmapSetInfo>
    {
        protected override string FileExtension => ".osz";

        public LegacyBeatmapExporter(Storage storage)
            : base(storage)
        {
        }
    }

    public class DirectoryBeatmapExporter : DirectoryExporter<BeatmapSetInfo>
    {
        public DirectoryBeatmapExporter(Storage storage)
            : base(storage)
        {
        }

        public void Import(BeatmapSetInfo item)
        {
            string filename = $"{item.GetDisplayString().GetValidArchiveContentFilename()}";

            foreach (var f in tempStorage.GetFiles(filename))
            {
                using Stream s = tempStorage.GetStream(f);
                using Stream ns = File.OpenWrite(UserFileStorage.GetFullPath(item.GetPathForFile(f), true));
                s.CopyTo(ns);
            }
        }
    }
}
