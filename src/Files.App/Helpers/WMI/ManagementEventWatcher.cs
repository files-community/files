// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.Management.Infrastructure;
using Microsoft.Management.Infrastructure.Generic;
using System;
using System.Threading;

namespace Files.App.Helpers
{
	public delegate void EventArrivedEventHandler(object sender, EventArrivedEventArgs e);

	/// <summary>
	/// A public class used to start/stop the subscription to specific indication source,
	/// and listen to the incoming indications, event <see cref="EventArrived" />
	/// will be raised for each cimindication.
	/// Original Sourced from: https://codereview.stackexchange.com/questions/255055/trying-to-replace-managementeventwatcher-class-in-system-management-to-switch-to
	/// Adapted to newer versions of MMI
	/// </summary>
	public class ManagementEventWatcher : IDisposable, IObserver<CimSubscriptionResult>
	{
		internal enum CimWatcherStatus
		{
			Default,
			Started,
			Stopped
		}

		//Events
		public event EventArrivedEventHandler EventArrived = delegate { };

		//Fields
		private object _myLock;
		private bool _isDisposed;
		private CimWatcherStatus _cimWatcherStatus;
		private readonly string _computerName;
		private readonly string _nameSpace;
		private readonly string _queryDialect;
		private readonly string _queryExpression;
		private CimSession _cimSession;
		private CimAsyncMultipleResults<CimSubscriptionResult> _cimObservable;
		private IDisposable _subscription;
		internal const string DefaultNameSpace = @"root\cimv2";
		internal const string DefaultQueryDialect = "WQL";

		/// <summary>
		/// Initializes a new instance of the <see cref="ManagementEventWatcher" /> class.
		/// </summary>
		/// <param name="queryExpression"></param>
		public ManagementEventWatcher(WqlEventQuery query)
		{
			string queryExpression = query.QueryExpression;

			if (string.IsNullOrWhiteSpace(queryExpression))
			{
				throw new ArgumentNullException(nameof(queryExpression));
			}

			_nameSpace = DefaultNameSpace;
			_queryDialect = DefaultQueryDialect;
			_queryExpression = queryExpression;

			Initialize();
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="ManagementEventWatcher" /> class.
		/// </summary>
		/// <param name="queryDialect"></param>
		/// <param name="queryExpression"></param>
		public ManagementEventWatcher(string queryDialect, string queryExpression)
		{
			if (string.IsNullOrWhiteSpace(queryExpression))
			{
				throw new ArgumentNullException(nameof(queryExpression));
			}

			_nameSpace = DefaultNameSpace;
			_queryDialect = queryDialect ?? DefaultQueryDialect;
			_queryExpression = queryExpression;

			Initialize();
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="ManagementEventWatcher" /> class.
		/// </summary>
		/// <param name="nameSpace"></param>
		/// <param name="queryDialect"></param>
		/// <param name="queryExpression"></param>
		public ManagementEventWatcher(string nameSpace, string queryDialect, string queryExpression)
		{
			if (string.IsNullOrWhiteSpace(queryExpression))
			{
				throw new ArgumentNullException(nameof(queryExpression));
			}

			_nameSpace = nameSpace ?? DefaultNameSpace;
			_queryDialect = queryDialect ?? DefaultQueryDialect;
			_queryExpression = queryExpression;

			Initialize();
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="ManagementEventWatcher" /> class.
		/// </summary>
		/// <param name="computerName"></param>
		/// <param name="nameSpace"></param>
		/// <param name="queryDialect"></param>
		/// <param name="queryExpression"></param>
		public ManagementEventWatcher(string computerName, string nameSpace, string queryDialect, string queryExpression)
		{
			if (string.IsNullOrWhiteSpace(queryExpression))
			{
				throw new ArgumentNullException(nameof(queryExpression));
			}

			_computerName = computerName;
			_nameSpace = nameSpace ?? DefaultNameSpace;
			_queryDialect = queryDialect ?? DefaultQueryDialect;
			_queryExpression = queryExpression;

			Initialize();
		}

		public void Initialize()
		{
			_cimWatcherStatus = CimWatcherStatus.Default;
			_myLock = new object();

			_cimSession = CimSession
				.Create(_computerName);

			_cimObservable = _cimSession
				.SubscribeAsync(_nameSpace, _queryDialect, _queryExpression);
		}

		private void OnEventArrived(EventArrivedEventArgs cimWatcherEventArgs)
		{
			Volatile
				.Read(ref EventArrived)
				.Invoke(this, cimWatcherEventArgs);
		}

		public void OnCompleted()
		{
		}

		public void OnError(Exception error)
		{
		}

		public void OnNext(CimSubscriptionResult cimSubscriptionResult)
		{
			OnEventArrived(new EventArrivedEventArgs(cimSubscriptionResult));
		}

		public void Start()
		{
			lock (_myLock)
			{
				if (_isDisposed)
				{
					throw new ObjectDisposedException(nameof(ManagementEventWatcher));
				}

				if (_cimWatcherStatus != CimWatcherStatus.Default && _cimWatcherStatus != CimWatcherStatus.Stopped)
				{
					return;
				}

				_subscription = _cimObservable.Subscribe(this);

				_cimWatcherStatus = CimWatcherStatus.Started;
			}
		}

		public void Stop()
		{
			lock (_myLock)
			{
				if (_isDisposed)
				{
					throw new ObjectDisposedException(nameof(ManagementEventWatcher));
				}

				if (_cimWatcherStatus != CimWatcherStatus.Started)
				{
					return;
				}

				_subscription?.Dispose();

				_cimWatcherStatus = CimWatcherStatus.Stopped;
			}
		}

		protected virtual void Dispose(bool disposing)
		{
			if (!_isDisposed)
			{
				if (disposing)
				{
					_subscription?.Dispose();
					_cimSession?.Dispose();
				}

				_isDisposed = true;
			}
		}

		public void Dispose()
		{
			// Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
			Dispose(disposing: true);
			GC.SuppressFinalize(this);
		}
	}
}
