// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

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

        public void Export(IBeatmap item)
        {
            string filename = $"{item.GetDisplayString().GetValidArchiveContentFilename()}";

            Storage storage = tempStorage.GetStorageForDirectory(filename);

            // Export .osu file
            Stream s = storage.CreateFileSafely(item.GetDisplayString().GetValidArchiveContentFilename() + ".osu");
            UserFileStorage.GetStream(item.BeatmapInfo.File?.File.GetStoragePath()).CopyTo(s);

            // Export storyboard file

            storage.PresentExternally();
        }

        public void Delete()
        {
            tempStorage.DeleteDirectory(string.Empty);
        }

        public void Reimport(BeatmapSetInfo item)
        {
            string path = $"{item.GetDisplayString().GetValidArchiveContentFilename()}";

            //importFilesFromDir(item, tempStorage.GetStorageForDirectory(path));

            Delete();
        }

        private void importFilesFromDir(BeatmapSetInfo item, Storage storage)
        {
            foreach (var f in storage.GetFiles(string.Empty))
            {
                using Stream s = storage.GetStream(f);

                using Stream ns = UserFileStorage.GetStream(UserFileStorage.GetFullPath(item.GetPathForFile(f), true), FileAccess.Write);
                s.CopyTo(ns);
            }

            foreach (var d in storage.GetDirectories(string.Empty))
            {
                importFilesFromDir(item, storage.GetStorageForDirectory(d));
            }
        }
    }
}
