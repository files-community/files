﻿using Files.Shared.Extensions;
using Files.Uwp.Filesystem;
using Files.Uwp.UserControls.FilePreviews;
using Files.Uwp.ViewModels.Properties;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Files.Uwp.ViewModels.Previews
{
    public class TextPreviewViewModel : BasePreviewModel
    {
        public TextPreviewViewModel(ListedItem item) : base(item) {}

        private string textValue;
        public string TextValue
        {
            get => textValue;
            private set => SetProperty(ref textValue, value);
        }

        public static bool ContainsExtensions(string extension) => extension is ".txt";

        public async override Task<List<FileProperty>> LoadPreviewAndDetails()
        {
            var details = new List<FileProperty>();

            try
            {
                var text = TextValue ?? await ReadFileAsText(Item.ItemFile);

                details.Add(GetFileProperty("PropertyLineCount", text.Split('\n').Length));
                details.Add(GetFileProperty("PropertyWordCount", text.Split(new[]{' ', '\n'}, StringSplitOptions.RemoveEmptyEntries).Length));

                TextValue = text.Left(Constants.PreviewPane.TextCharacterLimit);
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
            }

            return details;
        }

        public static async Task<TextPreview> TryLoadAsTextAsync(ListedItem item)
        {
            string extension = item.FileExtension?.ToLowerInvariant();
            if (ExcludedExtensions(extension) || item.FileSizeBytes is 0 || item.FileSizeBytes > Constants.PreviewPane.TryLoadAsTextSizeLimit)
            {
                return null;
            }

            try
            {
                item.ItemFile = await StorageFileExtensions.DangerousGetFileFromPathAsync(item.ItemPath);

                var text = await ReadFileAsText(item.ItemFile);
                bool isBinaryFile = text.Contains("\0\0\0\0", StringComparison.Ordinal);
                if (isBinaryFile)
                {
                    return null;
                }

                var model = new TextPreviewViewModel(item){ TextValue = text };
                await model.LoadAsync();

                return new TextPreview(model);
            }
            catch
            {
                return null;
            }
        }

        private static bool ExcludedExtensions(string extension) => extension is ".iso";
    }
}