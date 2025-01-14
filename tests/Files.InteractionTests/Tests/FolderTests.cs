// Copyright (c) Files Community
// Licensed under the MIT License.

using OpenQA.Selenium;
using OpenQA.Selenium.Interactions;
using System.Threading;

namespace Files.InteractionTests.Tests
{
	[TestClass]
	public sealed class FolderTests
	{

		[TestCleanup]
		public void Cleanup()
		{
			// Navigate back home
			TestHelper.InvokeButtonById("Home");
		}

		[TestMethod]
		public void TestFolders()
		{
			NavigationTest();

			CreateFolderTest();

			RenameFolderTest();

			CopyPasteFolderTest();

			DeleteFolderTest();
		}

		/// <summary>
		/// Tests folder navigation
		/// </summary>
		private void NavigationTest()
		{
			// Click on the desktop item in the sidebar
			TestHelper.InvokeButtonById("Desktop");

			// Wait for the desktop folder to load
			Thread.Sleep(2000);
		}


		/// <summary>
		/// Tests folder creation and checks for accessibility issues along the way
		/// </summary>
		private void CreateFolderTest()
		{
			// Click the "New" button on the toolbar
			TestHelper.InvokeButtonById("InnerNavigationToolbarNewButton");

			// Wait for the flyout to load
			Thread.Sleep(1000);

			// Click the "Folder" item from the menu flyout
			TestHelper.InvokeButtonById("InnerNavigationToolbarNewFolderButton");

			// Wait for the content dialog to load
			Thread.Sleep(3000);

			// Check for accessibility issues in the new folder prompt
			AxeHelper.AssertNoAccessibilityErrors();

			// Type the folder name
			var action = new Actions(SessionManager.Session);
			action.SendKeys("New Folder").Perform();

			// Press the enter button to confirm
			action = new Actions(SessionManager.Session);
			action.SendKeys(Keys.Enter).Perform();

			// Wait for folder to be created
			Thread.Sleep(3000);

			// Check for accessibility issues in the file area
			AxeHelper.AssertNoAccessibilityErrors();
		}

		/// <summary>
		/// Tests renaming a folder
		/// </summary>
		private void RenameFolderTest()
		{
			// Click the "Rename" button on the toolbar
			TestHelper.InvokeButtonById("InnerNavigationToolbarRenameButton");

			// Type the new name into the inline text box
			var action = new Actions(SessionManager.Session);
			action.SendKeys("Renamed Folder").Perform();

			// Press the enter button to save the new name
			action = new Actions(SessionManager.Session);
			action.SendKeys(Keys.Enter).Perform();

			// Wait for the folder to be renamed
			Thread.Sleep(3000);
		}

		/// <summary>
		/// Tests copying and pasting a folder
		/// </summary>
		private void CopyPasteFolderTest()
		{
			// Click the "copy" button on the toolbar
			TestHelper.InvokeButtonById("InnerNavigationToolbarCopyButton");

			// Wait for folder to be copied
			Thread.Sleep(3000);

			// Click the "paste" button on the toolbar
			TestHelper.InvokeButtonById("InnerNavigationToolbarPasteButton");

			// Wait for folder to be pasted
			Thread.Sleep(3000);
		}

		/// <summary>
		/// Tests deleting folders
		/// </summary>
		private void DeleteFolderTest()
		{
			// Select the "Renamed Folder" folder and clicks the "delete" button on the toolbar
			TestHelper.InvokeButtonByName("Renamed Folder");
			TestHelper.InvokeButtonById("InnerNavigationToolbarDeleteButton");

			// Wait for prompt to show
			Thread.Sleep(3000);

			// Check for accessibility issues in the confirm delete prompt
			AxeHelper.AssertNoAccessibilityErrors();

			// Press the enter key to confirm
			var action = new Actions(SessionManager.Session);
			action.SendKeys(Keys.Enter).Perform();


			// Select the "Renamed Folder - Copy" folder and clicks the "delete" button on the toolbar
			TestHelper.InvokeButtonByName("Renamed Folder - Copy");
			TestHelper.InvokeButtonById("InnerNavigationToolbarDeleteButton");

			// Wait for prompt to show
			Thread.Sleep(3000);

			// Check for accessibility issues in the confirm delete prompt
			AxeHelper.AssertNoAccessibilityErrors();

			// Press the enter key to confirm
			action = new Actions(SessionManager.Session);
			action.SendKeys(Keys.Enter).Perform();

			// Wait for items to finish being deleted
			Thread.Sleep(3000);
		}
	}
}
