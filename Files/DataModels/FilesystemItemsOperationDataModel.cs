﻿using Files.Enums;
using Files.Helpers;
using Files.ViewModels.Dialogs;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Files.DataModels
{
    public struct FilesystemItemsOperationItemModel
    {
        public FilesystemOperationType OperationType;

        public string SourcePath;

        public string DestinationPath;

        public string DisplayFileName;

        public FilesystemItemsOperationItemModel(FilesystemOperationType operationType, string sourcePath, string destinationPath, string displayFileName = null)
        {
            this.OperationType = operationType;
            this.SourcePath = sourcePath;
            this.DestinationPath = destinationPath;
            this.DisplayFileName = displayFileName;
        }
    }

    public struct FilesystemItemsOperationDataModel
    {
        public FilesystemOperationType OperationType;

        public bool MustResolveConflicts;

        public bool PermanentlyDelete;

        public bool PermanentlyDeleteEnabled;

        /// <summary>
        /// The items that are copied/moved/deleted from the source directory (to destination)
        /// </summary>
        public List<FilesystemItemsOperationItemModel> IncomingItems;

        /// <summary>
        /// The items that are conflicting between <see cref="IncomingItems"/> and the items that are in the destination directory
        /// </summary>
        public List<FilesystemItemsOperationItemModel> ConflictingItems;

        public FilesystemItemsOperationDataModel(FilesystemOperationType operationType, bool mustResolveConflicts, bool permanentlyDelete, bool permanentlyDeleteEnabled, List<FilesystemItemsOperationItemModel> incomingItems, List<FilesystemItemsOperationItemModel> conflictingItems)
        {
            this.OperationType = operationType;
            this.MustResolveConflicts = mustResolveConflicts;
            this.PermanentlyDelete = permanentlyDelete;
            this.PermanentlyDeleteEnabled = permanentlyDeleteEnabled;
            this.IncomingItems = incomingItems;
            this.ConflictingItems = conflictingItems;
        }

        public async Task<List<FilesystemOperationItemViewModel>> ToItems(Action updatePrimaryButtonEnabled, Action optionGenerateNewName, Action optionReplaceExisting, Action optionSkip)
        {
            ConcurrentBag<(int Index, FilesystemOperationItemViewModel Model)> items = new ConcurrentBag<(int Index, FilesystemOperationItemViewModel Model)>();

            List<FilesystemItemsOperationItemModel> nonConflictingItems = IncomingItems.Except(ConflictingItems).ToList();

            // Add conflicting items first
            await Task.WhenAll(ConflictingItems.Select(async (item, index) =>
            {
                var iconData = await FileThumbnailHelper.LoadIconFromPathAsync(item.SourcePath, 64u, Windows.Storage.FileProperties.ThumbnailMode.ListView);

                items.Add((index, new FilesystemOperationItemViewModel(updatePrimaryButtonEnabled, optionGenerateNewName, optionReplaceExisting, optionSkip)
                {
                    IsConflict = true,
                    ItemIcon = iconData != null ? await iconData.ToBitmapAsync() : null,
                    SourcePath = item.SourcePath,
                    DestinationPath = item.DestinationPath,
                    DisplayFileName = item.DisplayFileName,
                    ConflictResolveOption = FileNameConflictResolveOptionType.GenerateNewName,
                    ItemOperation = item.OperationType,
                    ActionTaken = false
                }));
            }));

            var baseIndex = ConflictingItems.Count;

            // Then add non-conflicting items
            await Task.WhenAll(nonConflictingItems.Select(async (item, index) =>
            {
                var iconData = await FileThumbnailHelper.LoadIconFromPathAsync(item.SourcePath, 64u, Windows.Storage.FileProperties.ThumbnailMode.ListView);

                items.Add((baseIndex + index, new FilesystemOperationItemViewModel(updatePrimaryButtonEnabled, optionGenerateNewName, optionReplaceExisting, optionSkip)
                {
                    IsConflict = false,
                    ItemIcon = iconData != null ? await iconData.ToBitmapAsync() : null,
                    SourcePath = item.SourcePath,
                    DestinationPath = item.DestinationPath,
                    DisplayFileName = item.DisplayFileName,
                    ConflictResolveOption = FileNameConflictResolveOptionType.NotAConflict,
                    ItemOperation = item.OperationType,
                    ActionTaken = true
                }));
            }));

            return items.OrderBy(i => i.Index).Select(i => i.Model).ToList();
        }

        private string GetOperationIconGlyph(FilesystemOperationType operationType)
        {
            switch (operationType)
            {
                case FilesystemOperationType.Copy:
                    return "\uE8C8";

                case FilesystemOperationType.Move:
                    return "\uE8C6";

                case FilesystemOperationType.Delete:
                    return "\uE74D";

                default:
                    return "\uE8FB";
            }
        }
    }
}