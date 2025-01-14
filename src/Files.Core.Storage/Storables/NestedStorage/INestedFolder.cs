// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.Core.Storage.Storables
{
    /// <summary>
    /// Represents a folder that resides within a traversable folder structure.
    /// </summary>
    public interface INestedFolder : IFolder, INestedStorable
    {
    }
}
