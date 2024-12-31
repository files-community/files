using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Files.App.Data.Models
{
	public class TerminalModel : IDisposable
	{
		public string Id { get; init; }
		public string Name { get; init; }
		public Control Control { get; init; }

		public void Dispose()
		{
			(Control as IDisposable)?.Dispose();
		}
	}
}
