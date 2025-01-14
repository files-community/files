// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.InteractionTests.Tests
{
	[TestClass]
	public sealed class GeneralAppTests
	{
		[TestMethod]
		public void SessionGetsInitialized()
		{
			Assert.IsNotNull(SessionManager.Session);
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