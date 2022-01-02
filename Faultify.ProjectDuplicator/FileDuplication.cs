﻿using System;
using System.IO;

namespace Faultify.ProjectDuplicator
{
    /// <summary>
    ///     Wrapper over duplicated testproject files.
    /// </summary>
    public class FileDuplication : IDisposable, IFileDuplication
    {
        private FileStream? _fileStream;

        public FileDuplication(string directory, string name)
        {
            Directory = directory;
            Name = name;
        }

        /// <summary>
        ///     Name of the file.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        ///     Directory in which the file is located.
        /// </summary>
        public string Directory { get; set; }

        public void Dispose()
        {
            _fileStream?.Close();
            _fileStream = null;
        }

        /// <summary>
        ///     Retrieves the full file path.
        /// </summary>
        /// <returns></returns>
        public string FullFilePath()
        {
            return Path.Combine(Directory, Name);
        }

        /// <summary>
        ///     Returns whether write mode for the file stream is enabled.
        /// </summary>
        /// <returns></returns>
        public bool IsWriteModeEnabled()
        {
            return _fileStream?.CanWrite ?? false;
        }

        /// <summary>
        ///     Returns whether read mode for the file stream is enabled.
        /// </summary>
        /// <returns></returns>
        public bool IsReadModeEnabled()
        {
            return _fileStream?.CanRead ?? false;
        }

        /// <summary>
        ///     Opens up a write access to the file and returns the stream.
        /// </summary>
        /// <returns></returns>
        public Stream OpenReadWriteStream()
        {
            if (_fileStream == null || IsReadModeEnabled()) EnableReadWriteOnly();
            return _fileStream!;
        }


        /// <summary>
        ///     Opens up a read access to the file and returns the stream.
        /// </summary>
        /// <returns></returns>
        public Stream OpenReadStream()
        {
            if (_fileStream == null || IsWriteModeEnabled()) EnableReadOnly();
            return _fileStream!;
        }

        /// <summary>
        ///     Enables write modes and closes any earlier initialized streams.
        /// </summary>
        public void EnableReadWriteOnly()
        {
            Dispose();

            _fileStream = new FileStream(
                path: FullFilePath(),
                mode: FileMode.Open,
                access: FileAccess.ReadWrite,
                share: FileShare.ReadWrite);
        }

        /// <summary>
        ///     Enables read modes and closes any earlier initialized streams.
        /// </summary>
        public void EnableReadOnly()
        {
            Dispose();

            _fileStream = new FileStream(
                path: FullFilePath(),
                mode: FileMode.Open,
                access: FileAccess.Read,
                share: FileShare.ReadWrite);
        }
    }
}