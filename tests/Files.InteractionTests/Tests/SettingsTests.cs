// Copyright (c) Files Community
// Licensed under the MIT License.

using OpenQA.Selenium.Interactions;
using System;
using System.Threading;

namespace Files.InteractionTests.Tests
{
	[TestClass]
	public sealed class SettingsTests
	{

		[TestCleanup]
		public void Cleanup()
		{
			var action = new Actions(SessionManager.Session);
			action.SendKeys(OpenQA.Selenium.Keys.Escape).Build().Perform();
		}

		[TestMethod]
		public void VerifySettingsAreAccessible()
		{
			TestHelper.InvokeButtonById("SettingsButton");
			AxeHelper.AssertNoAccessibilityErrors();

			var settingsItems = new string[]
			{
				"SettingsItemGeneral",
				"SettingsItemAppearance",
				//"SettingsItemLayout", TODO find workaround for the "Group by" setting block issue
				"SettingsItemFolders",
				"SettingsItemActions",
				"SettingsItemTags",
				"SettingsItemDevTools",
				"SettingsItemAdvanced",
				"SettingsItemAbout"
			};

			foreach (var item in settingsItems)
			{
				for (int i = 0; i < 5; i++)
				{
					try
					{
						Console.WriteLine("Invoking button:" + item);
						Thread.Sleep(3000);
						TestHelper.InvokeButtonById(item);
						i = 1000;
					}
					catch (Exception exc)
					{
						Console.WriteLine("Failed to invoke the button:" + item + " with exception" + exc.Message);
					}

				}
				try
				{
					// First run can be flaky due to external components
					AxeHelper.AssertNoAccessibilityErrors();
				}
				catch (System.Exception) { }
				AxeHelper.AssertNoAccessibilityErrors();
			}
		}
	}
}
