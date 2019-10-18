using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace CompilerInfrastructure.Utils{
    public class DependencyGraph<TModule>{
        readonly Dictionary<TModule, Node> nodeCache = new Dictionary<TModule, Node>();
        class Node : IEnumerable<Node>{
            readonly HashSet<Node> directDependencies = new HashSet<Node>();
            readonly TModule module;
            
            public Node(TModule mod){
                module = mod;
            }

            public TModule Module => module;
            
            public IEnumerator<Node> GetEnumerator(){
                return directDependencies.GetEnumerator();
            }
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            
            public bool AddDependency(Node nod){
                if(nod is null || nod == this)
                    return false;
                return directDependencies.Add(nod);
            }
            public int Degree => directDependencies.Count;
        }
        
        
        public DependencyGraph(){
            
        }
        
        Node NodeFromModule(TModule mod){
            if(nodeCache.TryGetValue(mod, out var ret))
                return ret;
            ret = new Node(mod);
            nodeCache[mod] = ret;
            return ret;
        }
        
        public bool AddModule(TModule mod){
            if(nodeCache.ContainsKey(mod))
                return false;
            nodeCache[mod] = new Node(mod);
            return true;
        }
        
        public bool AddDependency(TModule mod, TModule dependsOn){
            return NodeFromModule(mod).AddDependency(NodeFromModule(dependsOn));
        }
        
        public IEnumerable<ISet<TModule>> TopologicSorting(){
            
            var deg = new Dictionary<Node, int>();
            foreach(var kvp in nodeCache){
                deg[kvp.Value] = kvp.Value.Degree;
            }
            
            while(deg.Any()){
                var set = new HashSet<TModule>();
                foreach(var kvp in deg){
                    if(kvp.Value <= 0){
                        set.Add(kvp.Key.Module);
                        foreach(var dep in kvp.Key){
                            deg[dep]--;
                        }
                    }
                }
                
                if(set.Any())
                    yield return set;
                else{
                    throw new Exception("Circular dependency detected");
                    //yield break;
                }
                foreach(var nod in set){
                    deg.Remove(NodeFromModule(nod));
                }
            }
        }
    }
}