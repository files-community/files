// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Files.App.Helpers
{
	public static class DependencyObjectHelpers
	{
		public static T FindChild<T>(DependencyObject startNode) where T : DependencyObject
		{
			int count = VisualTreeHelper.GetChildrenCount(startNode);
			for (int i = 0; i < count; i++)
			{
				DependencyObject current = VisualTreeHelper.GetChild(startNode, i);
				if (current.GetType().Equals(typeof(T)) || current.GetType().GetTypeInfo().IsSubclassOf(typeof(T)))
				{
					T asType = (T)current;
					return asType;
				}
				var retVal = FindChild<T>(current);
				if (retVal is not null)
				{
					return retVal;
				}
			}
			return null;
		}

		public static T FindChild<T>(DependencyObject startNode, Func<T, bool> predicate) where T : DependencyObject
		{
			int count = VisualTreeHelper.GetChildrenCount(startNode);
			for (int i = 0; i < count; i++)
			{
				DependencyObject current = VisualTreeHelper.GetChild(startNode, i);
				if (current.GetType().Equals(typeof(T)) || current.GetType().GetTypeInfo().IsSubclassOf(typeof(T)))
				{
					T asType = (T)current;
					if (predicate(asType))
					{
						return asType;
					}
				}
				var retVal = FindChild<T>(current, predicate);
				if (retVal is not null)
				{
					return retVal;
				}
			}
			return null;
		}

		public static IEnumerable<T> FindChildren<T>(DependencyObject startNode) where T : DependencyObject
		{
			int count = VisualTreeHelper.GetChildrenCount(startNode);
			for (int i = 0; i < count; i++)
			{
				DependencyObject current = VisualTreeHelper.GetChild(startNode, i);
				if (current.GetType().Equals(typeof(T)) || (current.GetType().GetTypeInfo().IsSubclassOf(typeof(T))))
				{
					T asType = (T)current;
					yield return asType;
				}
				foreach (var item in FindChildren<T>(current))
				{
					yield return item;
				}
			}
		}

		public static T FindParent<T>(DependencyObject child) where T : DependencyObject
		{
			T parent = null;
			if (child is null)
			{
				return parent;
			}
			DependencyObject CurrentParent = VisualTreeHelper.GetParent(child);
			while (CurrentParent is not null)
			{
				if (CurrentParent is T)
				{
					parent = (T)CurrentParent;
					break;
				}
				CurrentParent = VisualTreeHelper.GetParent(CurrentParent);
			}
			return parent;
		}
	}
}