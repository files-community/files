﻿// Copyright (c) 2018-2024 Files Community
// Licensed under the MIT License.

namespace Files.Core.Storage.Storables
{
    /// <summary>
    /// Represents a file that resides within a traversable folder structure.
    /// </summary>
    public interface INestedFile : IFile, INestedStorable
    {
    }
}
