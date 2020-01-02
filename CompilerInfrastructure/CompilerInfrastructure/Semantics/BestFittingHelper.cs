// /******************************************************************************
//  * Copyright (c) 2020 Fabian Schiebel.
//  * All rights reserved. This program and the accompanying materials are made
//  * available under the terms of LICENSE.txt.
//  *
//  *****************************************************************************/

namespace CompilerInfrastructure.Semantics {
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using CompilerInfrastructure.Utils;
    using Structure;

    public static class BestFittingHelper {
        public delegate bool CompatibilityChecker<T, U>(T candidate, U expectedProperties, out int diff);
        public static T BestFitting<T, U>(Position pos, IEnumerable<T> candidates, U expectedProperties, CompatibilityChecker<T, U> isCompatible, T error, Func<T> errorOnEmptyFF = null, Func<IEnumerable<T>, T> errorOnAmbiguity = null) {
            if (candidates is null || !candidates.Any())
                return error;
            var firstFilter = new SortedSet<(T, int)>(RefEqualsOrOrderTupleComparer<T>.Instance);
            foreach (var candidate in candidates) {
                if (candidate is null)
                    continue;
                if (isCompatible(candidate, expectedProperties, out int diff))
                    firstFilter.Add((candidate, diff));
            }

            if (!firstFilter.Any()) {
                return errorOnEmptyFF is null ? error : errorOnEmptyFF();
            }

            using var it = firstFilter.GetEnumerator();
            it.MoveNext();
            var ret  = it.Current;
            var ret2 = new List<T>();
            while (it.MoveNext() && it.Current.Item2 == ret.Item2) {
                ret2.Add(it.Current.Item1);
            }
            if (ret2.Any()) {
                return errorOnAmbiguity is null ? error : errorOnAmbiguity(ret2.Prepend(ret.Item1));
            }

            return ret.Item1;
        }
        public static Error BestFitting<T, U>(Position pos, IEnumerable<T> candidates, U expectedProperties, CompatibilityChecker<T, U> isCompatible, T error, out T result, Func<T> errorOnEmptyFF = null, Func<IEnumerable<T>, T> errorOnAmbiguity = null) {
            if (candidates is null || !candidates.Any()) {
                result = error;
                return Error.NoValidTarget;
            }

            var firstFilter = new SortedSet<(T, int)>(RefEqualsOrOrderTupleComparer<T>.Instance);
            foreach (var candidate in candidates) {
                if (candidate is null)
                    continue;
                if (isCompatible(candidate, expectedProperties, out int diff))
                    firstFilter.Add((candidate, diff));
            }

            if (!firstFilter.Any()) {
                result = errorOnEmptyFF is null ? error : errorOnEmptyFF();
                return Error.NoValidTarget;
            }

            using var it = firstFilter.GetEnumerator();
            it.MoveNext();
            var ret  = it.Current;
            var ret2 = new List<T>();
            while (it.MoveNext() && it.Current.Item2 == ret.Item2) {
                ret2.Add(it.Current.Item1);
            }
            if (ret2.Any()) {
                result = errorOnAmbiguity is null ? error : errorOnAmbiguity(ret2.Prepend(ret.Item1));
                return Error.AmbiguousTarget;
            }

            result = ret.Item1;
            return Error.None;
        }
    }
}
