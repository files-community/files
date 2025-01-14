// Copyright (c) Files Community
// Licensed under the MIT License.

using System;

namespace Files.App.Helpers
{
	internal sealed class IntervalSampler
	{
		private DateTime recordPoint;
		private TimeSpan sampleInterval;

		public IntervalSampler(int millisecondsInterval)
		{
			sampleInterval = TimeSpan.FromMilliseconds(millisecondsInterval);
			recordPoint = DateTime.Now;
		}

		public IntervalSampler(TimeSpan interval)
		{
			sampleInterval = interval;
			recordPoint = DateTime.Now;
		}

		public void Reset()
		{
			recordPoint = DateTime.Now;
		}

		public bool CheckNow()
		{
			var now = DateTime.Now;
			if (now - sampleInterval >= recordPoint)
			{
				recordPoint = now;
				return true;
			}
			return false;
		}
	}
}