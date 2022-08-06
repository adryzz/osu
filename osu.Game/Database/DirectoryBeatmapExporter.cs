// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using osu.Framework.Extensions.EnumExtensions;
using osu.Framework.Platform;
using osu.Game.Beatmaps;
using osu.Game.Extensions;

namespace osu.Game.Database
{
    public class DirectoryBeatmapExporter : IDisposable
    {
        private readonly Storage tempStorage;

        private readonly Storage UserFileStorage;

        private readonly FileSystemWatcher watcher;

        private readonly List<string> filesToRefresh = new List<string>();

        public DirectoryBeatmapExporter(Storage storage)
        {
            tempStorage = storage.GetStorageForDirectory(@"temp");
            UserFileStorage = storage.GetStorageForDirectory(@"files");

            watcher = new FileSystemWatcher();
            watcher.IncludeSubdirectories = true;
            watcher.Changed += WatcherOnChanged;
            watcher.Created += WatcherOnChanged;
            watcher.Deleted += WatcherOnChanged;
            //TODO: handle renamed files
        }

        /// <summary>
        /// Exports a beatmap to a temporary directory on disk and allows for manual editing of its files.
        /// </summary>
        /// <param name="item">The beatmap to export.</param>
        public void Export(WorkingBeatmap item)
        {
            string path = $"{item.GetDisplayString().GetValidArchiveContentFilename()}";

            Storage storage = tempStorage.GetStorageForDirectory(path);
            watcher.Path = storage.GetFullPath(string.Empty);

            // Export files
            foreach (var f in item.BeatmapSetInfo.Files)
            {
                using Stream s = storage.CreateFileSafely(f.Filename);
                UserFileStorage.GetStream(f.File.GetStoragePath()).CopyTo(s);
            }

            watcher.EnableRaisingEvents = true;
            storage.PresentExternally();
        }

        private void WatcherOnChanged(object sender, FileSystemEventArgs e)
        {
            if (!e.ChangeType.HasFlagFast(WatcherChangeTypes.Renamed))
            {
                if (!filesToRefresh.Contains(e.Name))
                    filesToRefresh.Add(e.Name);
            }
        }

        /// <summary>
        /// Re-imports a beatmap from the temporary directory after it has been manually edited.
        /// </summary>
        /// <param name="item">The beatmap to re-import</param>
        public void Reimport(WorkingBeatmap item)
        {
            string path = $"{item.GetDisplayString().GetValidArchiveContentFilename()}";

            Storage storage = tempStorage.GetStorageForDirectory(path);

            foreach (var file in filesToRefresh)
            {
                if (storage.Exists(file))
                {
                    if (item.BeatmapSetInfo.Files.Any(x => x.Filename.Equals(file)))
                    {
                        // The file isn't new
                    }
                    else
                    {
                        // The file is newly created
                    }
                }
                else
                {
                    // The file has been deleted
                }
            }
        }

        public void Dispose()
        {
            watcher.Dispose();
            tempStorage.DeleteDirectory(string.Empty);
        }
    }
}
