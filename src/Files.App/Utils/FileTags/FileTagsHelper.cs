// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.UI.Xaml.Controls;
using Windows.Foundation.Metadata;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.Win32;
using IO = System.IO;

namespace Files.App.Utils.FileTags
{

	public static class FileTagsHelper
	{
		private static readonly Lazy<FileTagsDatabase> dbInstance = new(() => new());

		public static FileTagsDatabase GetDbInstance() => dbInstance.Value;

		public static string[] ReadFileTag(string filePath)
		{
			var tagString = Win32Helper.ReadStringFromFile($"{filePath}:files");
			return tagString?.Split(',', StringSplitOptions.RemoveEmptyEntries) ?? [];
		}

		public static async void WriteFileTag(string filePath, string[] tag)
		{
			var isDateOk = Win32Helper.GetFileDateModified(filePath, out var dateModified); // Backup date modified
			var isReadOnly = Win32Helper.HasFileAttribute(filePath, IO.FileAttributes.ReadOnly);
			if (isReadOnly) // Unset read-only attribute (#7534)
			{
				Win32Helper.UnsetFileAttribute(filePath, IO.FileAttributes.ReadOnly);
			}
			if (!tag.Any())
			{
				PInvoke.DeleteFileFromApp($"{filePath}:files");
			}
			else if (ReadFileTag(filePath) is not string[] arr || !tag.SequenceEqual(arr))
			{
				var result = Win32Helper.WriteStringToFile($"{filePath}:files", string.Join(',', tag));
				if (result == false)
				{
					ContentDialog dialog = new()
					{
						Title = "ErrorApplyingTagTitle".GetLocalizedResource(),
						Content = "ErrorApplyingTagContent".GetLocalizedResource(),
						PrimaryButtonText = "Ok".GetLocalizedResource()
					};

					if (ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 8))
						dialog.XamlRoot = MainWindow.Instance.Content.XamlRoot;

					await dialog.TryShowAsync();
				}
			}
			if (isReadOnly) // Restore read-only attribute (#7534)
			{
				Win32Helper.SetFileAttribute(filePath, IO.FileAttributes.ReadOnly);
			}
			if (isDateOk)
			{
				Win32Helper.SetFileDateModified(filePath, dateModified); // Restore date modified
			}
		}

		public static void UpdateTagsDb()
		{
			var dbInstance = GetDbInstance();
			foreach (var file in dbInstance.GetAll())
			{
				var pathFromFrn = Win32Helper.PathFromFileId(file.Frn ?? 0, file.FilePath);
				if (pathFromFrn is not null)
				{
					// Frn is valid, update file path
					var tag = ReadFileTag(pathFromFrn.Replace(@"\\?\", "", StringComparison.Ordinal));
					if (tag is not null && tag.Any())
					{
						dbInstance.UpdateTag(file.Frn ?? 0, null, pathFromFrn.Replace(@"\\?\", "", StringComparison.Ordinal));
						dbInstance.SetTags(pathFromFrn.Replace(@"\\?\", "", StringComparison.Ordinal), file.Frn, tag);
					}
					else
					{
						dbInstance.SetTags(pathFromFrn.Replace(@"\\?\", "", StringComparison.Ordinal), file.Frn, []);
					}
				}
				else
				{
					var tag = ReadFileTag(file.FilePath);
					if (tag is not null && tag.Any())
					{
						if (!SafetyExtensions.IgnoreExceptions(() =>
						{
							var frn = GetFileFRN(file.FilePath);
							dbInstance.UpdateTag(file.FilePath, frn, null);
							dbInstance.SetTags(file.FilePath, frn, tag);
						}, App.Logger))
						{
							dbInstance.SetTags(file.FilePath, null, []);
						}
					}
					else
					{
						dbInstance.SetTags(file.FilePath, null, []);
					}
				}
			}
		}

		public static ulong? GetFileFRN(string filePath) => Win32Helper.GetFileFRN(filePath);

		public static Task<ulong?> GetFileFRN(IStorageItem item)
		{
			return item switch
			{
				BaseStorageFolder { Properties: not null } folder => GetFileFRN(folder.Properties),
				BaseStorageFile { Properties: not null } file => GetFileFRN(file.Properties),
				_ => Task.FromResult<ulong?>(null),
			};

			static async Task<ulong?> GetFileFRN(IStorageItemExtraProperties properties)
			{
				var extra = await properties.RetrievePropertiesAsync(["System.FileFRN"]);
				return (ulong?)extra["System.FileFRN"];
			}
		}
	}
}