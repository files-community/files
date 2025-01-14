// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Utils.Shell
{
	/// <summary>
	/// Represents an item for Win32 context menu.
	/// </summary>
	public class ContextMenuItem : Win32ContextMenuItem, IDisposable
	{
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing)
		{
			if (disposing && SubItems is not null)
			{
				foreach (var subItem in SubItems)
					(subItem as IDisposable)?.Dispose();

				SubItems = null;
			}
		}
	}
}
