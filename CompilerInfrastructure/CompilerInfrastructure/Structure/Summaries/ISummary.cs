/******************************************************************************
 * Copyright (c) 2019 Fabian Schiebel.
 * All rights reserved. This program and the accompanying materials are made
 * available under the terms of LICENSE.txt.
 *
 *****************************************************************************/

using System;
using System.Collections.Generic;
using System.Text;
using CompilerInfrastructure.Utils;
using Newtonsoft.Json.Linq;
namespace CompilerInfrastructure.Structure.Summaries {
    // A simple Serialization context, which is able to process primitive data
    public interface ISummary {
        void Add<T>(string name, T obj) where T : struct;
        void AddObject<T>(string name, T obj) where T : ISummarizable;
        void AddArray<T>(string name, params T[] objs) where T : ISummarizable;
        bool TryTake<T>(string name, out T obj) where T : struct;
        bool TryTakeObject<T>(string name, out T obj) where T : ISummarizable;
        bool TryTakeArray<T>(string name, out T[] objs);
    }
    public abstract class AbstractSummary : ISummary {

        readonly Dictionary<object, uint> objIDCache= new Dictionary<object, uint>();
        readonly Dictionary<uint,object> objCache= new Dictionary<uint, object>();
        readonly protected Func<System.Type, ISummarizer> summarizerGen;

        // When null, this summary can only be used for serialization
        public AbstractSummary(Func<System.Type, ISummarizer> _summarizerGen) {
            summarizerGen = _summarizerGen;
        }


        uint currentObjID=0;
        protected uint GetObjectID(object o) {
            return o is null ? 0 : objIDCache[o];
        }
        protected bool TryGetObjectID(object o, out uint id) {
            if (o is null) {
                id = 0;
                return true;
            }
            return objIDCache.TryGetValue(o, out id);
        }
        protected bool GetOrCreateObjectID(object o, out uint id) {
            if (o is null) {
                id = 0;
                return true;
            }

            if (objIDCache.TryGetValue(o, out id))
                return true;
            id = currentObjID++;
            objIDCache.Add(o, id);
            return false;
        }
        protected uint GetOrCreateObjectID(object o) {
            if (o is null)
                return 0;
            if (objIDCache.TryGetValue(o, out var ret))
                return ret;
            ret = ++currentObjID;
            objIDCache.Add(o, ret);
            return ret;
        }

        protected bool TryGetObject(uint id, out object o) {
            return objCache.TryGetValue(id, out o);
        }

        public abstract void Add<T>(string name, T obj) where T : struct;
        public abstract void AddArray<T>(string name, params T[] objs) where T : ISummarizable;
        public virtual void AddObject<T>(string name, T obj) where T : ISummarizable {
            if (!GetOrCreateObjectID(obj, out var id)) {
                AddObjectInternal(id, obj);
            }
            Add(name, id);
        }
        //Assuming obj nonnull
        protected abstract void AddObjectInternal<T>(uint id, T obj) where T : ISummarizable;
        public abstract bool TryTake<T>(string name, out T obj) where T : struct;
        public abstract bool TryTakeArray<T>(string name, out T[] objs);
        public virtual bool TryTakeObject<T>(string name, out T obj) where T : ISummarizable {

            if (TryTake(name, out uint id)) {
                if (TryGetObject(id, out object o)) {
                    if (o is T to) {
                        obj = to;
                        return true;
                    }
                }
                else
                    return TryTakeObjectInternal(id, out obj);
            }

            obj = default;
            return false;
        }
        protected abstract bool TryTakeObjectInternal<T>(uint id, out T obj) where T : ISummarizable;
    }
    public class JsonSummary : AbstractSummary {
        readonly Stack< JObject> jobj= new Stack<JObject>();

        public JObject Root { get; }
        public JObject ReferenceCache { get; } = new JObject();

        public JsonSummary() : base(null) {
            jobj.Push(Root = new JObject());
        }
        public JsonSummary(JObject jo, JObject refs, Func<System.Type, ISummarizer> _summarizerGen) : base(_summarizerGen) {
            jobj.Push(Root = jo ?? throw new ArgumentNullException(nameof(jo)));
            ReferenceCache = refs ?? throw new ArgumentNullException(nameof(refs));
        }

        public override void Add<T>(string name, T obj) {
            jobj.Peek().Add(new JProperty(name, obj));
        }

        public override void AddArray<T>(string name, params T[] objs) {
            var arr = new JArray();
            foreach (var x in objs) {
                if (!GetOrCreateObjectID(x, out var id)) {
                    AddObjectInternal(id, x);
                }
            }
        }

        public override bool TryTake<T>(string name, out T obj) {
            if (jobj.Peek().TryGetValue(name, out var tok)) {
                obj = tok.Value<T>();
                return true;
            }
            obj = default;
            return false;
        }

        public override bool TryTakeArray<T>(string name, out T[] objs) {
            throw new NotImplementedException();
        }

        protected override void AddObjectInternal<T>(uint id, T obj) {
            var nwobj = new JObject();
            using (jobj.PushFrame(nwobj)) {
                obj.GetSummarizer().Add(this);
            }
            ReferenceCache.Add(id.ToString(), nwobj);
        }
        protected override bool TryTakeObjectInternal<T>(uint id, out T obj) {
            if (summarizerGen is null)
                throw new InvalidOperationException("Deserialization is not possible when the summarizerGen is null");
            if (ReferenceCache.TryGetValue(id.ToString(), out var tok) && tok is JObject jo) {
                var sum = summarizerGen(typeof(T));
                using (jobj.PushFrame(jo)) {
                    return sum.TryTake(this, out obj);
                }
            }
            obj = default;
            return false;
        }
    }
}
