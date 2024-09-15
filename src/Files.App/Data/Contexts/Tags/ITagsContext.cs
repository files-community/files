﻿// Copyright (c) 2018-2024 Files Community
// Licensed under the MIT License.

namespace Files.App.Data.Contexts
{
    interface ITagsContext: INotifyPropertyChanged
    {
		IEnumerable<(string path, bool isFolder)> TaggedItems { get; }
    }
}
