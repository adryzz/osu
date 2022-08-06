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
        /// Exports a beatmap to a temporary directory on disk and allows for manual editing of its files.
        /// </summary>
        /// <param name="item">The beatmap to export.</param>
        public void Export(WorkingBeatmap item)
        {
            string filename = $"{item.GetDisplayString().GetValidArchiveContentFilename()}";

            Storage storage = tempStorage.GetStorageForDirectory(filename);

            // Export files
            foreach (var f in item.BeatmapSetInfo.Files)
            {
                using Stream s = storage.CreateFileSafely(f.Filename);
                UserFileStorage.GetStream(f.File.GetStoragePath()).CopyTo(s);
            }

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
        /// Re-imports a beatmap from the temporary directory after it has been manually edited.
        /// </summary>
        /// <param name="item">The bearmap to re-import</param>
        public void Reimport(WorkingBeatmap item)
        {
            string path = $"{item.GetDisplayString().GetValidArchiveContentFilename()}";

            //TODO: reimport files
        }
    }
}
