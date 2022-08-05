// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.IO;
using osu.Framework.Platform;
using osu.Game.Extensions;

namespace osu.Game.Database
{
    public abstract class DirectoryExporter<TModel>
        where TModel : class, IHasNamedFiles
    {

        protected readonly Storage tempStorage;

        protected readonly Storage UserFileStorage;

        protected DirectoryExporter(Storage storage)
        {
            tempStorage = storage.GetStorageForDirectory(@"temp");
            UserFileStorage = storage.GetStorageForDirectory(@"files");
        }

        public void Export(TModel item)
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
    }
}
