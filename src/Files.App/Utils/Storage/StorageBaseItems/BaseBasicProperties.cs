// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Utils.Storage
{
	public class BaseBasicProperties : BaseStorageItemExtraProperties
	{
		public virtual ulong Size
			=> 0;

		public virtual DateTimeOffset DateCreated
			=> DateTimeOffset.Now;

		public virtual DateTimeOffset DateModified
			=> DateTimeOffset.Now;
	}
}
