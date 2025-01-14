// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Input;
using Windows.ApplicationModel.DataTransfer;
using Windows.ApplicationModel.DataTransfer.DragDrop;
using Windows.Storage;
using Windows.System;

namespace Files.App.ViewModels.Layouts
{
	/// <summary>
	/// Represents ViewModel for <see cref="BaseLayoutPage"/>.
	/// </summary>
	public sealed class BaseLayoutViewModel : IDisposable
	{
		protected ICommandManager Commands { get; } = Ioc.Default.GetRequiredService<ICommandManager>();
		private ILogger? Logger { get; } = Ioc.Default.GetRequiredService<ILogger<App>>();

		private readonly IShellPage _associatedInstance;

		private readonly ItemManipulationModel _itemManipulationModel;

		public ICommand CreateNewFileCommand { get; private set; }

		public ICommand ItemPointerPressedCommand { get; private set; }

		public ICommand PointerWheelChangedCommand { get; private set; }

		public ICommand DragOverCommand { get; private set; }

		public ICommand DropCommand { get; private set; }

		public BaseLayoutViewModel(IShellPage associatedInstance, ItemManipulationModel itemManipulationModel)
		{
			_associatedInstance = associatedInstance;
			_itemManipulationModel = itemManipulationModel;

			CreateNewFileCommand = new RelayCommand<ShellNewEntry>(CreateNewFile);
			ItemPointerPressedCommand = new AsyncRelayCommand<PointerRoutedEventArgs>(ItemPointerPressedAsync);
			PointerWheelChangedCommand = new RelayCommand<PointerRoutedEventArgs>(PointerWheelChanged);
			DragOverCommand = new AsyncRelayCommand<DragEventArgs>(DragOverAsync);
			DropCommand = new AsyncRelayCommand<DragEventArgs>(DropAsync);
		}

		private void CreateNewFile(ShellNewEntry f)
		{
			UIFilesystemHelpers.CreateFileFromDialogResultTypeAsync(AddItemDialogItemType.File, f, _associatedInstance);
		}

		private async Task ItemPointerPressedAsync(PointerRoutedEventArgs e)
		{
			// If a folder item was clicked, disable middle mouse click to scroll to cancel the mouse scrolling state and re-enable it
			if (e.GetCurrentPoint(null).Properties.IsMiddleButtonPressed &&
				e.OriginalSource is FrameworkElement { DataContext: ListedItem Item } &&
				Item.PrimaryItemAttribute == StorageItemTypes.Folder)
			{
				_associatedInstance.SlimContentPage.IsMiddleClickToScrollEnabled = false;
				_associatedInstance.SlimContentPage.IsMiddleClickToScrollEnabled = true;

				if (Item.IsShortcut)
					await NavigationHelpers.OpenPathInNewTab(((e.OriginalSource as FrameworkElement)?.DataContext as ShortcutItem)?.TargetPath ?? Item.ItemPath);
				else
					await NavigationHelpers.OpenPathInNewTab(Item.ItemPath);
			}
		}

		private void PointerWheelChanged(PointerRoutedEventArgs e)
		{
			if (e.KeyModifiers is VirtualKeyModifiers.Control &&
				_associatedInstance.IsCurrentInstance)
			{
				int delta = e.GetCurrentPoint(null).Properties.MouseWheelDelta;

				// Mouse wheel down
				if (delta < 0)
					Commands.LayoutDecreaseSize.ExecuteAsync();
				// Mouse wheel up
				else if (delta > 0)
					Commands.LayoutIncreaseSize.ExecuteAsync();

				e.Handled = true;
			}
		}

		public async Task DragOverAsync(DragEventArgs e)
		{
			var deferral = e.GetDeferral();

			if (_associatedInstance.InstanceViewModel.IsPageTypeSearchResults)
			{
				e.AcceptedOperation = DataPackageOperation.None;
				deferral.Complete();

				return;
			}

			if (FilesystemHelpers.HasDraggedStorageItems(e.DataView))
			{
				e.Handled = true;

				var draggedItems = await FilesystemHelpers.GetDraggedStorageItems(e.DataView);

				var pwd = _associatedInstance.ShellViewModel.WorkingDirectory.TrimPath();
				var folderName = Path.IsPathRooted(pwd) && Path.GetPathRoot(pwd) == pwd ? Path.GetPathRoot(pwd) : Path.GetFileName(pwd);

				// As long as one file doesn't already belong to this folder
				if (_associatedInstance.InstanceViewModel.IsPageTypeSearchResults || draggedItems.Any() && draggedItems.AreItemsAlreadyInFolder(_associatedInstance.ShellViewModel.WorkingDirectory))
				{
					e.AcceptedOperation = DataPackageOperation.None;
				}
				else if (!draggedItems.Any())
				{
					e.AcceptedOperation = DataPackageOperation.None;
				}
				else
				{
					try
					{
						e.DragUIOverride.IsCaptionVisible = true;
						if (pwd.StartsWith(Constants.UserEnvironmentPaths.RecycleBinPath, StringComparison.Ordinal))
						{
							e.DragUIOverride.Caption = string.Format("MoveToFolderCaptionText".GetLocalizedResource(), folderName);
							// Some applications such as Edge can't raise the drop event by the Move flag (#14008), so we set the Copy flag as well.
							e.AcceptedOperation = DataPackageOperation.Move | DataPackageOperation.Copy;
						}
						else if (e.Modifiers.HasFlag(DragDropModifiers.Alt) || e.Modifiers.HasFlag(DragDropModifiers.Control | DragDropModifiers.Shift))
						{
							e.DragUIOverride.Caption = string.Format("LinkToFolderCaptionText".GetLocalizedResource(), folderName);
							e.AcceptedOperation = DataPackageOperation.Link;
						}
						else if (e.Modifiers.HasFlag(DragDropModifiers.Control))
						{
							e.DragUIOverride.Caption = string.Format("CopyToFolderCaptionText".GetLocalizedResource(), folderName);
							e.AcceptedOperation = DataPackageOperation.Copy;
						}
						else if (e.Modifiers.HasFlag(DragDropModifiers.Shift))
						{
							e.DragUIOverride.Caption = string.Format("MoveToFolderCaptionText".GetLocalizedResource(), folderName);
							// Some applications such as Edge can't raise the drop event by the Move flag (#14008), so we set the Copy flag as well.
							e.AcceptedOperation = DataPackageOperation.Move | DataPackageOperation.Copy;
						}
						else if (draggedItems.Any(x =>
							x.Item is ZipStorageFile ||
							x.Item is ZipStorageFolder) ||
							ZipStorageFolder.IsZipPath(pwd))
						{
							e.DragUIOverride.Caption = string.Format("CopyToFolderCaptionText".GetLocalizedResource(), folderName);
							e.AcceptedOperation = DataPackageOperation.Copy;
						}
						else if (draggedItems.AreItemsInSameDrive(_associatedInstance.ShellViewModel.WorkingDirectory))
						{
							e.DragUIOverride.Caption = string.Format("MoveToFolderCaptionText".GetLocalizedResource(), folderName);
							// Some applications such as Edge can't raise the drop event by the Move flag (#14008), so we set the Copy flag as well.
							e.AcceptedOperation = DataPackageOperation.Move | DataPackageOperation.Copy;
						}
						else
						{
							e.DragUIOverride.Caption = string.Format("CopyToFolderCaptionText".GetLocalizedResource(), folderName);
							e.AcceptedOperation = DataPackageOperation.Copy;
						}

						_itemManipulationModel.ClearSelection();
					}
					catch (COMException ex) when (ex.Message.Contains("RPC server is unavailable"))
					{
						Logger?.LogDebug(ex, ex.Message);
					}
				}
			}

			deferral.Complete();
		}

		public async Task DropAsync(DragEventArgs e)
		{
			e.Handled = true;

			if (FilesystemHelpers.HasDraggedStorageItems(e.DataView))
			{
				var deferral = e.GetDeferral();

				try
				{
					await _associatedInstance.FilesystemHelpers.PerformOperationTypeAsync(e.AcceptedOperation, e.DataView, _associatedInstance.ShellViewModel.WorkingDirectory, false, true);
					await _associatedInstance.RefreshIfNoWatcherExistsAsync();
				}
				finally
				{
					deferral.Complete();
				}
			}
		}

		public void Dispose()
		{
		}
	}
}
