﻿using Microsoft.UI.Xaml.Input;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Files.App.Commands
{
	public interface IRichCommand : ICommand, INotifyPropertyChanging, INotifyPropertyChanged
	{
		CommandCodes Code { get; }

		string Label { get; }

		bool IsToggle { get; }
		bool IsOn { get; set; }
		bool IsExecutable { get; }

		Task ExecuteAsync();

		void ExecuteTapped(object sender, TappedRoutedEventArgs e);
	}
}
