﻿namespace Files.App.Contexts
{
	public interface IWindowContext : INotifyPropertyChanged
	{
		bool IsCompactOverlay { get; }
	}
}
