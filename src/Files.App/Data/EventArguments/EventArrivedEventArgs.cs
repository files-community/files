// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.Management.Infrastructure;
using System;

namespace Files.App.Data.EventArguments
{
	/// <summary>
	/// CimWatcher event args, which contains CimSubscriptionResult
	/// </summary>
	public sealed class EventArrivedEventArgs : EventArgs
	{
		public CimSubscriptionResult NewEvent { get; }

		public EventArrivedEventArgs(CimSubscriptionResult cimSubscriptionResult)
		{
			NewEvent = cimSubscriptionResult;
		}
	}
}
