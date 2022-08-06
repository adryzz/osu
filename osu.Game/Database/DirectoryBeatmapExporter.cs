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

        /// <summary>
        /// Exports a beatmap (.osu and storyboard) to a temporary directory on disk and allows for manual editing of the files.
        /// </summary>
        /// <param name="item">The beatmap to export.</param>
        public void Export(WorkingBeatmap item)
        {
            string filename = $"{item.GetDisplayString().GetValidArchiveContentFilename()}";

            Storage storage = tempStorage.GetStorageForDirectory(filename);

            // Export .osu file
            Stream s = storage.CreateFileSafely(item.GetDisplayString().GetValidArchiveContentFilename() + ".osu");
            UserFileStorage.GetStream(item.BeatmapInfo.File?.File.GetStoragePath()).CopyTo(s);

            // Export storyboard file

            storage.PresentExternally();
        }

        /// <summary>
        /// Deletes the temporary directory from disk.
        /// </summary>
        public void Delete()
        {
            tempStorage.DeleteDirectory(string.Empty);
        }

        /// <summary>
        /// Re-imports a beatmap (.osu and storyboard) from the temporary directory after it has been manually edited.
        /// </summary>
        /// <param name="item">The bearmap to re-import</param>
        public void Reimport(WorkingBeatmap item)
        {
            string path = $"{item.GetDisplayString().GetValidArchiveContentFilename()}";

            //importFilesFromDir(item, tempStorage.GetStorageForDirectory(path));
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
