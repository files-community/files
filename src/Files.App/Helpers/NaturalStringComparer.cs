// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Helpers
{
	public sealed class NaturalStringComparer
	{
		public static IComparer<object> GetForProcessor()
		{
			return new NaturalComparer(StringComparison.CurrentCulture);
		}

		/// <summary>
		/// Provides functionality to compare and sort strings in a natural (human-readable) order.
		/// </summary>
		/// <remarks>
		/// This class implements string comparison that respects the natural numeric order in strings,
		/// such as "file10" being ordered after "file2".
		/// It is designed to handle cases where alphanumeric sorting is required.
		/// </remarks>
		private sealed class NaturalComparer : IComparer<object?>, IComparer<string?>, IComparer<ReadOnlyMemory<char>>
		{
		    private readonly StringComparison stringComparison;

		    public NaturalComparer(StringComparison stringComparison = StringComparison.Ordinal)
		    {
		        this.stringComparison = stringComparison;
		    }

		    public int Compare(object? x, object? y)
		    {
			    if (x == y) return 0;
			    if (x == null) return -1;
			    if (y == null) return 1;

			    return x switch
			    {
				    string x1 when y is string y1 => Compare(x1.AsSpan(), y1.AsSpan(), stringComparison),
				    IComparable comparable => comparable.CompareTo(y),
				    _ => StringComparer.FromComparison(stringComparison).Compare(x, y)
			    };
		    }

		    public int Compare(string? x, string? y)
		    {
		        if (ReferenceEquals(x, y)) return 0;
		        if (x is null) return -1;
		        if (y is null) return 1;

		        return Compare(x.AsSpan(), y.AsSpan(), stringComparison);
		    }

		    public int Compare(ReadOnlySpan<char> x, ReadOnlySpan<char> y)
		    {
		        return Compare(x, y, stringComparison);
		    }

		    public int Compare(ReadOnlyMemory<char> x, ReadOnlyMemory<char> y)
		    {
		        return Compare(x.Span, y.Span, stringComparison);
		    }

		    public static int Compare(ReadOnlySpan<char> x, ReadOnlySpan<char> y, StringComparison stringComparison)
		    {
		        var length = Math.Min(x.Length, y.Length);

		        for (var i = 0; i < length; i++)
		        {
		            if (char.IsDigit(x[i]) && char.IsDigit(y[i]))
		            {
		                var xOut = GetNumber(x.Slice(i), out var xNumAsSpan);
		                var yOut = GetNumber(y.Slice(i), out var yNumAsSpan);

		                var compareResult = CompareNumValues(xNumAsSpan, yNumAsSpan);

		                if (compareResult != 0) return compareResult;

		                i = -1;
		                length = Math.Min(xOut.Length, yOut.Length);

		                x = xOut;
		                y = yOut;
		                continue;
		            }

		            var charCompareResult = x.Slice(i, 1).CompareTo(y.Slice(i, 1), stringComparison);
		            if (charCompareResult != 0) return charCompareResult;
		        }

		        return x.Length.CompareTo(y.Length);
		    }

		    private static ReadOnlySpan<char> GetNumber(ReadOnlySpan<char> span, out ReadOnlySpan<char> number)
		    {
		        var i = 0;
		        while (i < span.Length && char.IsDigit(span[i]))
		        {
		            i++;
		        }

		        number = span.Slice(0, i);
		        return span.Slice(i);
		    }

		    private static int CompareNumValues(ReadOnlySpan<char> numValue1, ReadOnlySpan<char> numValue2)
		    {
		        var num1AsSpan = numValue1.TrimStart('0');
		        var num2AsSpan = numValue2.TrimStart('0');

		        if (num1AsSpan.Length < num2AsSpan.Length) return -1;

		        if (num1AsSpan.Length > num2AsSpan.Length) return 1;

		        var compareResult = num1AsSpan.CompareTo(num2AsSpan, StringComparison.Ordinal);

		        if (compareResult != 0) return Math.Sign(compareResult);

		        if (numValue2.Length == numValue1.Length) return compareResult;

		        return numValue2.Length < numValue1.Length ? -1 : 1; // "033" < "33" == true
		    }
		}
	}
}
