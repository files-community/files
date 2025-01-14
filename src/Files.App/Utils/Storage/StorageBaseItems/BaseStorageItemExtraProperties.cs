// Copyright (c) Files Community
// Licensed under the MIT License.

using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Metadata;
using Windows.Storage.FileProperties;

namespace Files.App.Utils.Storage
{
	public class BaseStorageItemExtraProperties : IStorageItemExtraProperties
	{
		public virtual IAsyncOperation<IDictionary<string, object>> RetrievePropertiesAsync(IEnumerable<string> propertiesToRetrieve)
		{
			return
				AsyncInfo.Run((cancellationToken) =>
				{
					var props = new Dictionary<string, object>();
					propertiesToRetrieve.ForEach(x => props[x] = null);

					return Task.FromResult<IDictionary<string, object>>(props);
				});
		}

		public virtual IAsyncAction SavePropertiesAsync()
		{
			return Task.CompletedTask.AsAsyncAction();
		}

		public virtual IAsyncAction SavePropertiesAsync([HasVariant] IEnumerable<KeyValuePair<string, object>> propertiesToSave)
		{
			return Task.CompletedTask.AsAsyncAction();
		}
	}
}
