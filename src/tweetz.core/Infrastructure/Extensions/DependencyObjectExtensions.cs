﻿using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;

namespace tweetz.core.Infrastructure.Extensions
{
    public static class DependencyObjectExtensions
    {
        public static IEnumerable<T> GetChildrenOfType<T>(this DependencyObject depObj)
            where T : DependencyObject
        {
            if (depObj == null) yield break;
            var queue = new Queue<DependencyObject>();
            queue.Enqueue(depObj);

            while (queue.Count > 0)
            {
                var currentElement = queue.Dequeue();
                var childrenCount = VisualTreeHelper.GetChildrenCount(currentElement);

                for (var i = 0; i < childrenCount; i++)
                {
                    var child = VisualTreeHelper.GetChild(currentElement, i);
                    if (child is T t)
                    {
                        yield return t;
                    }
                    queue.Enqueue(child);
                }
            }
        }

        public static T? FindVisualAncestorOfType<T>(this DependencyObject obj)
            where T : DependencyObject
        {
            for (var parent = VisualTreeHelper.GetParent(obj);
                parent != null;
                parent = VisualTreeHelper.GetParent(parent))
            {
                if (parent is T result) return result;
            }
            return null;
        }
    }
}