// Copyright (c) Files Community
// Licensed under the MIT License.

using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Storage;

namespace Files.App.Utils.Storage
{
	public sealed class BaseBasicStorageItemExtraProperties : BaseStorageItemExtraProperties
	{
		private readonly IStorageItem _item;

		public BaseBasicStorageItemExtraProperties(IStorageItem item)
		{
			_item = item;
		}

		public override IAsyncOperation<IDictionary<string, object>> RetrievePropertiesAsync(IEnumerable<string> propertiesToRetrieve)
		{
			return AsyncInfo.Run<IDictionary<string, object>>(async (cancellationToken) =>
			{
				var props = new Dictionary<string, object>();

				propertiesToRetrieve.ForEach(x => props[x] = null);

				// Fill out common properties
				var ret = _item.AsBaseStorageFile()?.GetBasicPropertiesAsync() ?? _item.AsBaseStorageFolder()?.GetBasicPropertiesAsync();

				var basicProps = ret is not null ? await ret : null;

				props["System.ParsingPath"] = _item?.Path;
				props["System.DateCreated"] = basicProps?.DateCreated;
				props["System.DateModified"] = basicProps?.DateModified;

				return props;
			});
		}
	}
}
