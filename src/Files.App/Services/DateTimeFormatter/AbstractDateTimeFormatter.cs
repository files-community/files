// Copyright (c) Files Community
// Licensed under the MIT License.

using System.Globalization;
using Windows.Globalization;

namespace Files.App.Services.DateTimeFormatter
{
	internal abstract class AbstractDateTimeFormatter : IDateTimeFormatter
	{
		private static readonly CultureInfo cultureInfo = new(AppLanguageHelper.PreferredLanguage.Code);

		public abstract string Name { get; }

		public abstract string ToShortLabel(DateTimeOffset offset);

		public virtual string ToLongLabel(DateTimeOffset offset)
			=> ToShortLabel(offset);

		public ITimeSpanLabel ToTimeSpanLabel(DateTimeOffset offset, GroupByDateUnit unit)
		{
			var now = DateTimeOffset.Now;
			var time = offset.ToLocalTime();

			var diff = now - offset;

			return 0 switch
			{
				_ when now.Date < time.Date
					=> new Label("Future".GetLocalizedResource(), "\uED28", 1000000006),
				_ when now.Date == time.Date
					=> new Label("Today".GetLocalizedResource(), "\uE8D1", 1000000005),
				_ when now.AddDays(-1).Date == time.Date
					=> new Label("Yesterday".GetLocalizedResource(), "\uE8BF", 1000000004),

				// Group by day
				_ when unit == GroupByDateUnit.Day
					=> new Label(ToString(time, "D"), "\uE8BF", time.Year * 10000 + time.Month * 100 + time.Day),

				_ when diff.Days <= 7 && GetWeekOfYear(now) == GetWeekOfYear(time)
					=> new Label("EarlierThisWeek".GetLocalizedResource(), "\uE8C0", 1000000003),
				_ when diff.Days <= 14 && GetWeekOfYear(now.AddDays(-7)) == GetWeekOfYear(time)
					=> new Label("LastWeek".GetLocalizedResource(), "\uE8C0", 1000000002),
				_ when now.Year == time.Year && now.Month == time.Month
					=> new Label("EarlierThisMonth".GetLocalizedResource(), "\uE787", 1000000001),
				_ when now.AddMonths(-1).Year == time.Year && now.AddMonths(-1).Month == time.Month
					=> new Label("LastMonth".GetLocalizedResource(), "\uE787", 1000000000),

				// Group by month
				_ when unit == GroupByDateUnit.Month
					=> new Label(ToString(time, "Y"), "\uE787", time.Year * 10000 + time.Month * 100),

				// Group by year
				_ when now.Year == time.Year
					=> new Label("EarlierThisYear".GetLocalizedResource(), "\uEC92", 10000001),
				_ when now.AddYears(-1).Year == time.Year
					=> new Label("LastYear".GetLocalizedResource(), "\uEC92", 10000000),
				_
					=> new Label(string.Format("YearN".GetLocalizedResource(), time.Year), "\uEC92", time.Year),
			};
		}

		protected static string ToString(DateTimeOffset offset, string format)
			=> offset.ToLocalTime().ToString(format, cultureInfo);

		private static int GetWeekOfYear(DateTimeOffset t)
		{
			return cultureInfo.Calendar.GetWeekOfYear(t.DateTime, CalendarWeekRule.FirstFullWeek, cultureInfo.DateTimeFormat.FirstDayOfWeek);
		}

		private sealed class Label : ITimeSpanLabel
		{
			public string Text { get; }

			public string Glyph { get; }

			public int Index { get; }

			public Label(string text, string glyph, int index)
				=> (Text, Glyph, Index) = (text, glyph, index);
		}
	}
}
