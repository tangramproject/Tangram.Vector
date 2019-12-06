using System;
using System.Collections.Generic;

namespace Core.API.Extentions
{
    public static class EnumerableExtensions
    {

        public static IEnumerable<IEnumerable<T>> Split<T>(this IEnumerable<T> source, int len)
        {
            if (len == 0)
                throw new ArgumentNullException();

            var enumer = source.GetEnumerator();
            while (enumer.MoveNext())
            {
                yield return Take(enumer.Current, enumer, len);
            }
        }

        private static IEnumerable<T> Take<T>(T head, IEnumerator<T> tail, int len)
        {
            while (true)
            {
                yield return head;
                if (--len == 0)
                    break;
                if (tail.MoveNext())
                    head = tail.Current;
                else
                    break;
            }
        }

        public static IEnumerable<T> AsDepthFirstEnumerable<T>(this T head, Func<T, IEnumerable<T>> childrenFunc)
        {
            yield return head;
            foreach (var node in childrenFunc(head))
            {
                foreach (var child in AsDepthFirstEnumerable(node, childrenFunc))
                {
                    yield return child;
                }
            }
        }

        public static IEnumerable<T> AsBreadthFirstEnumerable<T>(this T head, Func<T, IEnumerable<T>> childrenFunc)
        {
            yield return head;
            var last = head;
            foreach (var node in AsBreadthFirstEnumerable(head, childrenFunc))
            {
                foreach (var child in childrenFunc(node))
                {
                    yield return child;
                    last = child;
                }
                if (last.Equals(node)) yield break;
            }
        }

    }
}
