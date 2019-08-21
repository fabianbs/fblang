/******************************************************************************
 * Copyright (c) 2019 Fabian Schiebel.
 * All rights reserved. This program and the accompanying materials are made
 * available under the terms of LICENSE.txt.
 *
 *****************************************************************************/

using System;
using System.Collections.Generic;
using System.Text;

namespace CompilerInfrastructure.Structure.Summaries {
    public interface ISummarizable {
        ISummarizer GetSummarizer();
    }
    public interface ISummarizer {
        void Add(ISummary ctx);
    }
    public interface ISummarizer<T> : ISummarizer {
        bool TryTake(ISummary ctx, out T obj);
    }
    public static class SumerizerHelper {
        public static bool TryTake<T>(this ISummarizer sum, ISummary ctx, out T obj) {
            if (sum is ISummarizer<T> tsum)
                return tsum.TryTake(ctx, out obj);
            obj = default;
            return false;
        }
    }
}
