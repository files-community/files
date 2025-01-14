// Copyright (c) Files Community
// Licensed under the MIT License.

using Axe.Windows.Automation;
using Axe.Windows.Core.Enums;
using System.Diagnostics;
using System.Linq;

namespace Files.InteractionTests.Helper
{
	public sealed class AxeHelper
	{
		public static IScanner AccessibilityScanner;

		internal static void InitializeAxe()
		{
			var processes = Process.GetProcessesByName("Files");
			Assert.IsTrue(processes.Length > 0);

			var config = Config.Builder.ForProcessId(processes[0].Id).Build();

			AccessibilityScanner = ScannerFactory.CreateScanner(config);
		}

		public static void AssertNoAccessibilityErrors()
		{
			var testResult = AccessibilityScanner.Scan(null).WindowScanOutputs.SelectMany(output => output.Errors).Where(error => error.Rule.ID != RuleId.BoundingRectangleNotNull);
			if (testResult.Count() != 0)
			{
				var mappedResult = testResult.Select(result => "Element " + result.Element.Properties["ControlType"] + " violated rule '" + result.Rule.Description + "'.");
				Assert.Fail("Failed with the following accessibility errors \r\n" + string.Join("\r\n", mappedResult));
			}
		}
	}
}