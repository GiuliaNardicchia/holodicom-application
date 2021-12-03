using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class IEnumerableExtensions
{

    public static void Deconstruct<T>(this IEnumerable<T> self, out T first, out IEnumerable<T> rest)
    {
        first = self.FirstOrDefault();
        rest = self.Skip(1);
    }

    public static void Deconstruct<T>(this IEnumerable<T> self, out T first, out T second, out IEnumerable<T> rest)
    {
        (first, (second, rest)) = self;
    }

    public static void Deconstruct<T>(this IEnumerable<T> self, out T first, out T second, out T third, out IEnumerable<T> rest)
    {
        (first, second, (third, rest)) = self;
    }

}