// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.IO;
using osu.Framework.Platform;
using osu.Game.Beatmaps;
using osu.Game.Extensions;

namespace osu.Game.Database
{
    public class DirectoryBeatmapExporter
    {
        private readonly Storage tempStorage;

        private readonly Storage UserFileStorage;

        public DirectoryBeatmapExporter(Storage storage)
        {
            tempStorage = storage.GetStorageForDirectory(@"temp");
            UserFileStorage = storage.GetStorageForDirectory(@"files");
        }

        public void Export(BeatmapSetInfo item)
        {
            string filename = $"{item.GetDisplayString().GetValidArchiveContentFilename()}";

            Storage storage = tempStorage.GetStorageForDirectory(filename);

            foreach (var f in item.Files)
            {
                Console.WriteLine(f.Filename);
                using Stream s = storage.CreateFileSafely(f.Filename);
                UserFileStorage.GetStream(f.File.GetStoragePath()).CopyTo(s);
            }

            storage.PresentExternally();
        }

        public void Import(BeatmapSetInfo item)
        {
            string filename = $"{item.GetDisplayString().GetValidArchiveContentFilename()}";

            foreach (var f in tempStorage.GetFiles(filename))
            {
                using Stream s = tempStorage.GetStream(f);

                using Stream ns = UserFileStorage.GetStream(UserFileStorage.GetFullPath(item.GetPathForFile(f), true), FileAccess.Write);
                s.CopyTo(ns);
            }
        }
    }
}
