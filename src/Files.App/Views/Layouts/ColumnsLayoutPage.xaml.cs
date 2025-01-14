// Copyright (c) Files Community
// Licensed under the MIT License.

using CommunityToolkit.WinUI.UI;
using CommunityToolkit.WinUI.UI.Controls;
using Files.App.ViewModels.Layouts;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Windows.Storage;
using static Files.App.Helpers.PathNormalization;

namespace Files.App.Views.Layouts
{
	/// <summary>
	/// Represents the browser page of Column View
	/// </summary>
	public sealed partial class ColumnsLayoutPage : BaseLayoutPage
	{
		// Properties

		protected override ItemsControl ItemsControl => ColumnHost;

		public string? OwnerPath { get; private set; }

		public int FocusIndex { get; private set; }

		// Constructor

		public ColumnsLayoutPage() : base()
		{
			InitializeComponent();
		}

		// Methods

		public void HandleSelectionChange(ColumnLayoutPage initiator)
		{
			foreach (var blade in ColumnHost.ActiveBlades.ToList())
			{
				var columnView = blade.FindDescendant<ColumnLayoutPage>();
				if (columnView != null && columnView != initiator)
					columnView.ClearSelectionIndicator();
			}
		}

		protected override void HookEvents()
		{
		}

		protected override void UnhookEvents()
		{
		}

		protected override bool CanGetItemFromElement(object element)
			=> false;

		private void ColumnViewBase_ItemInvoked(object? sender, EventArgs e)
		{
			var column = sender as ColumnParam;
			if (column?.ListView.FindAscendant<ColumnsLayoutPage>() != this)
				return;

			var nextBladeIndex = ColumnHost.ActiveBlades.IndexOf(column.ListView.FindAscendant<BladeItem>()) + 1;
			var nextBlade = ColumnHost.ActiveBlades.ToList().ElementAtOrDefault(nextBladeIndex);
			var arePathsDifferent = ((nextBlade?.Content as Frame)?.Content as IShellPage)?.ShellViewModel?.WorkingDirectory != column.NavPathParam;

			if (nextBlade is null || arePathsDifferent)
			{
				DismissOtherBlades(column.ListView);

				var (frame, newblade) = CreateAndAddNewBlade();

				frame.Navigate(typeof(ColumnShellPage), new ColumnParam
				{
					Column = ColumnHost.ActiveBlades.IndexOf(newblade),
					NavPathParam = column.NavPathParam
				});
				navigationArguments.NavPathParam = column.NavPathParam;
				ParentShellPageInstance.TabBarItemParameter.NavigationParameter = column.NavPathParam;
			}
		}

		private void ContentChanged(IShellPage p)
		{
			(ParentShellPageInstance as ModernShellPage)?.RaiseContentChanged(p, p.TabBarItemParameter);
		}

		protected override void OnNavigatedTo(NavigationEventArgs eventArgs)
		{
			base.OnNavigatedTo(eventArgs);

			var path = navigationArguments.NavPathParam;
			var pathRoot = GetPathRoot(path);
			var pathStack = new Stack<string>();

			if (!string.IsNullOrEmpty(pathRoot))
			{
				var rootPathList = App.QuickAccessManager.Model.PinnedFolders.Select(NormalizePath)
					.Concat(CloudDrivesManager.Drives.Select(x => NormalizePath(x.Path))).ToList()
					.Concat(App.LibraryManager.Libraries.Select(x => NormalizePath(x.Path))).ToList();
				rootPathList.Add(NormalizePath(pathRoot));

				while (!rootPathList.Contains(NormalizePath(path)))
				{
					pathStack.Push(path);
					path = GetParentDir(path);
				}
			}

			OwnerPath = navigationArguments.NavPathParam;
			FocusIndex = pathStack.Count;

			MainPageFrame.Navigated += Frame_Navigated;
			MainPageFrame.Navigate(typeof(ColumnShellPage), new ColumnParam
			{
				Column = 0,
				IsSearchResultPage = navigationArguments.IsSearchResultPage,
				SearchQuery = navigationArguments.SearchQuery,
				SearchPathParam = navigationArguments.SearchPathParam,
				NavPathParam = path,
				SelectItems = path == navigationArguments.NavPathParam ? navigationArguments.SelectItems : null
			});

			var index = 0;
			while (pathStack.TryPop(out path))
			{
				var (frame, _) = CreateAndAddNewBlade();

				frame.Navigate(typeof(ColumnShellPage), new ColumnParam
				{
					Column = ++index,
					NavPathParam = path,
					SelectItems = path == navigationArguments.NavPathParam ? navigationArguments.SelectItems : null
				});
			}
		}

		protected override void InitializeCommandsViewModel()
		{
			CommandsViewModel = new BaseLayoutViewModel(ParentShellPageInstance, ItemManipulationModel);
		}

		protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
		{
			base.OnNavigatingFrom(e);

			Dispose();
		}

		public override void Dispose()
		{
			base.Dispose();

			var columnHostItems = ColumnHost.Items.OfType<BladeItem>().Select(blade => blade.Content as Frame);
			foreach (var frame in columnHostItems)
			{
				if (frame?.Content is ColumnShellPage shPage)
				{
					shPage.ContentChanged -= ColumnViewBrowser_ContentChanged;
					if (shPage.SlimContentPage is ColumnLayoutPage viewBase)
					{
						viewBase.ItemInvoked -= ColumnViewBase_ItemInvoked;
						viewBase.ItemTapped -= ColumnViewBase_ItemTapped;
						viewBase.KeyUp -= ColumnViewBase_KeyUp;
					}
				}

				if (frame?.Content is UIElement element)
					element.GotFocus -= ColumnViewBrowser_GotFocus;

				if (frame?.Content is IDisposable disposable)
					disposable.Dispose();
			}

			UnhookEvents();
			CommandsViewModel?.Dispose();
		}

		private void DismissOtherBlades(ListView listView)
		{
			DismissOtherBlades(listView.FindAscendant<BladeItem>());
		}

		private void DismissOtherBlades(BladeItem blade)
		{
			DismissOtherBlades(ColumnHost.ActiveBlades.IndexOf(blade));
		}

		public void DismissOtherBlades(int index)
		{
			if (index >= 0)
			{
				SafetyExtensions.IgnoreExceptions(() =>
				{
					while (ColumnHost.ActiveBlades.Count > index + 1)
					{
						var frame = ColumnHost.ActiveBlades[index + 1].Content as Frame;

						if (frame?.Content is IDisposable disposableContent)
							disposableContent.Dispose();

						if ((frame?.Content as ColumnShellPage)?.SlimContentPage is ColumnLayoutPage columnLayout)
						{
							columnLayout.ItemInvoked -= ColumnViewBase_ItemInvoked;
							columnLayout.ItemTapped -= ColumnViewBase_ItemTapped;
							columnLayout.KeyUp -= ColumnViewBase_KeyUp;
						}

						(frame?.Content as UIElement).GotFocus -= ColumnViewBrowser_GotFocus;
						(frame?.Content as ColumnShellPage).ContentChanged -= ColumnViewBrowser_ContentChanged;

						ColumnHost.Items.RemoveAt(index + 1);
						ColumnHost.ActiveBlades.RemoveAt(index + 1);
					}

					if ((ColumnHost.ActiveBlades[index].Content as Frame)?.Content is ColumnShellPage s)
					{
						navigationArguments.NavPathParam = s.ShellViewModel.WorkingDirectory;
						ParentShellPageInstance.TabBarItemParameter.NavigationParameter = s.ShellViewModel.WorkingDirectory;
					}
				});
			}

			ContentChanged(ActiveColumnShellPage);
		}

		private void Frame_Navigated(object sender, NavigationEventArgs e)
		{
			var f = sender as Frame;
			f.Navigated -= Frame_Navigated;
			(f.Content as IShellPage).ContentChanged += ColumnViewBrowser_ContentChanged;
			(f.Content as UIElement).GotFocus += ColumnViewBrowser_GotFocus;
		}

		private void ColumnViewBrowser_GotFocus(object sender, RoutedEventArgs e)
		{
			if (sender is not IShellPage shPage || shPage.IsCurrentInstance)
				return;

			var currentBlade = ColumnHost.ActiveBlades.ToList().Single(x => (x.Content as Frame)?.Content == sender);
			currentBlade.StartBringIntoView();
			if (ColumnHost.ActiveBlades is not null)
			{
				ColumnHost.ActiveBlades.ToList().ForEach(x =>
				{
					var shellPage = (x.Content as Frame)?.Content as ColumnShellPage;
					shellPage.IsCurrentInstance = false;
				});
			}

			shPage.IsCurrentInstance = true;
			ContentChanged(shPage);
		}

		private void ColumnViewBrowser_ContentChanged(object sender, TabBarItemParameter e)
		{
			var c = sender as IShellPage;
			var columnView = c?.SlimContentPage as ColumnLayoutPage;
			if (columnView is not null)
			{
				columnView.ItemInvoked -= ColumnViewBase_ItemInvoked;
				columnView.ItemInvoked += ColumnViewBase_ItemInvoked;
				columnView.ItemTapped -= ColumnViewBase_ItemTapped;
				columnView.ItemTapped += ColumnViewBase_ItemTapped;
				columnView.KeyUp -= ColumnViewBase_KeyUp;
				columnView.KeyUp += ColumnViewBase_KeyUp;
			}

			ContentChanged(c);
		}

		private void ColumnViewBase_KeyUp(object sender, Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e)
		{
			var shPage = ActiveColumnShellPage as ColumnShellPage;
			if (shPage?.SlimContentPage?.SelectedItem?.PrimaryItemAttribute is not StorageItemTypes.Folder)
				CloseUnnecessaryColumns(shPage?.ColumnParams);
		}

		public void NavigateBack()
		{
			(ParentShellPageInstance as ModernShellPage)?.Back_Click();
		}

		public void NavigateForward()
		{
			(ParentShellPageInstance as ModernShellPage)?.Forward_Click();
		}

		public void NavigateUp()
		{
			if (ColumnHost.ActiveBlades?.Count > 1)
				DismissOtherBlades(ColumnHost.ActiveBlades[ColumnHost.ActiveBlades.Count - 2]);
			else
			{
				var workingDirectory = ((ColumnHost.ActiveBlades?.ToList().FirstOrDefault()?.Content as Frame)?.Content as ColumnShellPage)?.ShellViewModel.WorkingDirectory;
				if (workingDirectory is null || string.Equals(workingDirectory, GetPathRoot(workingDirectory), StringComparison.OrdinalIgnoreCase))
					ParentShellPageInstance?.NavigateHome();
				else
					ParentShellPageInstance?.NavigateToPath(GetParentDir(workingDirectory));
			}
		}

		public void MoveFocusToPreviousBlade(int currentBladeIndex)
		{
			if (currentBladeIndex <= 0)
				return;

			DismissOtherBlades(currentBladeIndex);

			var activeBlade = ColumnHost.ActiveBlades[currentBladeIndex - 1];
			activeBlade.Focus(FocusState.Programmatic);
			FocusIndex = currentBladeIndex - 1;

			var activeBladeColumnViewBase = RetrieveBladeColumnViewBase(activeBlade);
			if (activeBladeColumnViewBase is null)
				return;

			// This allows to deselect and reselect the parent folder, hence forcing the refocus.
			var selectedItem = activeBladeColumnViewBase.FileList.SelectedItem;
			activeBladeColumnViewBase.FileList.SelectedItem = null;
			activeBladeColumnViewBase.FileList.SelectedItem = selectedItem;
		}

		public void MoveFocusToNextBlade(int currentBladeIndex)
		{
			if (currentBladeIndex >= ColumnHost.ActiveBlades.Count)
				return;

			var activeBlade = ColumnHost.ActiveBlades[currentBladeIndex];
			activeBlade.Focus(FocusState.Programmatic);
			FocusIndex = currentBladeIndex;

			var activeBladeColumnViewBase = RetrieveBladeColumnViewBase(activeBlade);
			if (activeBladeColumnViewBase is not null)
			{
				activeBladeColumnViewBase.FileList.SelectedIndex = 0;
			}
		}

		private ColumnLayoutPage? RetrieveBladeColumnViewBase(BladeItem blade)
		{
			if (blade.Content is not Frame activeBladeFrame ||
				activeBladeFrame.Content is not ColumnShellPage activeBladePage)
				return null;

			return activeBladePage.SlimContentPage as ColumnLayoutPage;
		}

		public void SetSelectedPathOrNavigate(string navigationPath, Type sourcePageType, NavigationArguments navArgs = null)
		{
			if (navArgs is not null && navArgs.IsSearchResultPage)
			{
				ParentShellPageInstance?.NavigateToPath(navArgs.SearchPathParam, typeof(DetailsLayoutPage), navArgs);
				return;
			}

			var destPath = navArgs is not null ? navArgs.NavPathParam : navigationPath;
			var columnPath = ((ColumnHost.ActiveBlades.ToList().LastOrDefault()?.Content as Frame)?.Content as ColumnShellPage)?.ShellViewModel.WorkingDirectory;
			var columnFirstPath = ((ColumnHost.ActiveBlades.ToList().FirstOrDefault()?.Content as Frame)?.Content as ColumnShellPage)?.ShellViewModel.WorkingDirectory;

			if (string.IsNullOrEmpty(destPath) || string.IsNullOrEmpty(columnPath) || string.IsNullOrEmpty(columnFirstPath))
			{
				ParentShellPageInstance?.NavigateToPath(navigationPath, sourcePageType, navArgs);
				return;
			}

			var destComponents = StorageFileExtensions.GetDirectoryPathComponents(destPath);
			var (lastCommonItemIndex, relativeIndex) = GetLastCommonAndRelativeIndex(destComponents, columnPath, columnFirstPath);
			if (relativeIndex < 0 || destComponents.Count - (lastCommonItemIndex + 1) > 1) // Going above parent or too deep down
			{
				ParentShellPageInstance?.NavigateToPath(navigationPath, sourcePageType, navArgs);
			}
			else
			{
				DismissOtherBlades(relativeIndex);

				for (int ii = lastCommonItemIndex + 1; ii < destComponents.Count; ii++)
				{
					var (frame, newblade) = CreateAndAddNewBlade();

					var columnParam = new ColumnParam
					{
						Column = ColumnHost.ActiveBlades.IndexOf(newblade),
						NavPathParam = destComponents[ii].Path
					};

					if (navArgs is not null)
					{
						columnParam.IsSearchResultPage = navArgs.IsSearchResultPage;
						columnParam.SearchQuery = navArgs.SearchQuery;
						columnParam.SearchPathParam = navArgs.SearchPathParam;
					}

					frame.Navigate(typeof(ColumnShellPage), columnParam);
				}
			}
		}

		public void SetSelectedPathOrNavigate(PathNavigationEventArgs e)
		{
			if (ColumnHost.ActiveBlades?.Count > 1)
			{
				foreach (var item in ColumnHost.ActiveBlades.ToList())
				{
					if ((item.Content as Frame)?.Content is ColumnShellPage s &&
						NormalizePath(s.ShellViewModel?.WorkingDirectory) == NormalizePath(e.ItemPath))
					{
						DismissOtherBlades(item);
						return;
					}
				}
			}

			if (ParentShellPageInstance is null)
				return;

			if (NormalizePath(ParentShellPageInstance.ShellViewModel?.WorkingDirectory) != NormalizePath(e.ItemPath))
				ParentShellPageInstance.NavigateToPath(e.ItemPath);
			else
				DismissOtherBlades(0);
		}

		public IShellPage ActiveColumnShellPage
		{
			get
			{
				if (ColumnHost.ActiveBlades?.Count > 0)
				{
					var shellPages = ColumnHost.ActiveBlades.ToList().Select(x => (x.Content as Frame).Content as IShellPage);
					var activeInstance = shellPages.SingleOrDefault(x => x.IsCurrentInstance);
					return activeInstance ?? shellPages.Last();
				}

				return ParentShellPageInstance;
			}
		}

		private void ColumnViewBase_ItemTapped(object? sender, EventArgs e)
		{
			var column = sender as ColumnParam;
			if (column?.ListView.FindAscendant<ColumnsLayoutPage>() != this || string.IsNullOrEmpty(column.NavPathParam))
				return;

			CloseUnnecessaryColumns(column);
		}

		private void CloseUnnecessaryColumns(ColumnParam column)
		{
			if (string.IsNullOrEmpty(column.NavPathParam))
				return;

			var relativeIndex = column.Column is not 0 ? column.Column : -1;

			if (column.Source is not null)
			{
				for (var i = 0; i < ColumnHost.ActiveBlades.Count && relativeIndex is -1; i++)
				{
					var bladeColumn = ColumnHost.ActiveBlades[i].FindDescendant<ColumnLayoutPage>();
					if (bladeColumn is not null && bladeColumn == column.Source)
						relativeIndex = i;
				}
			}

			if (relativeIndex is -1)
			{
				// Get the index of the blade with the same path as the requested
				var blade = ColumnHost.ActiveBlades.ToList().FirstOrDefault(b =>
					column.NavPathParam.Equals(((b.Content as Frame)?.Content as ColumnShellPage)?.ShellViewModel?.WorkingDirectory));

				if (blade is not null)
					relativeIndex = ColumnHost.ActiveBlades.IndexOf(blade);
			}

			if (relativeIndex >= 0)
			{
				ColumnHost.ActiveBlades[relativeIndex].FindDescendant<ColumnLayoutPage>()?.ClearOpenedFolderSelectionIndicator();
				DismissOtherBlades(relativeIndex);
			}
		}

		private (int, int) GetLastCommonAndRelativeIndex(List<PathBoxItem> destComponents, string columnPath, string columnFirstPath)
		{
			var columnComponents = StorageFileExtensions.GetDirectoryPathComponents(columnPath);
			var columnFirstComponents = StorageFileExtensions.GetDirectoryPathComponents(columnFirstPath);

			var lastCommonItemIndex = columnComponents
				.Select((value, index) => new { value, index })
				.LastOrDefault(x => x.index < destComponents.Count && x.value.Path == destComponents[x.index].Path)?.index ?? -1;

			var relativeIndex = lastCommonItemIndex - (columnFirstComponents.Count - 1);

			return (lastCommonItemIndex, relativeIndex);
		}

		private (Frame, BladeItem) CreateAndAddNewBlade()
		{
			var frame = new Frame();
			frame.Navigated += Frame_Navigated;
			var newblade = new BladeItem()
			{
				Content = frame
			};

			ColumnHost.Items.Add(newblade);
			return (frame, newblade);
		}
	}
}
