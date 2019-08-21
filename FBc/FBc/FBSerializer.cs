/******************************************************************************
 * Copyright (c) 2019 Fabian Schiebel.
 * All rights reserved. This program and the accompanying materials are made
 * available under the terms of LICENSE.txt.
 *
 *****************************************************************************/

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace FBc {
    class FBSerializer {
        //DataContractSerializer ser = new DataContractSerializer(typeof(FBModule));
        BinaryFormatter bf = new BinaryFormatter();
        public void Serialize(FBModule mod, Stream sOut) {
            //ser.WriteObject(sOut, mod);
            /*bf.Serialize(sOut, mod);
            using (var mem = new MemoryStream()) {
                bf.Serialize(mem, mod);
                mem.Position = 0;
                var mod2 = bf.Deserialize(mem);
            }*/
            //var res = SpanJson.JsonSerializer.Generic.Utf8.Serialize(mod);
            //sOut.Write(res);

            /*var serSettings = new Newtonsoft.Json.JsonSerializerSettings {
                 ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore,
                 Formatting = Newtonsoft.Json.Formatting.Indented,
                 PreserveReferencesHandling = Newtonsoft.Json.PreserveReferencesHandling.All,
                 TypeNameHandling = Newtonsoft.Json.TypeNameHandling.All,
                 ContractResolver = new DefaultContractResolver { IgnoreSerializableAttribute = false },

             };*/
            /*using (var tOut = new StreamWriter(sOut, Encoding.UTF8, 4096, true)) {

                tOut.Write(JsonConvert.SerializeObject(
                    mod,
                    serSettings
                ));
            }*/
            /*var ser = JsonConvert.SerializeObject(
                    mod,
                    serSettings
                );
           var deser = JsonConvert.DeserializeObject<FBModule>(ser, serSettings);*/
            var ser = new XmlSerializer(typeof(FBModule));
            
            using (var mem = new MemoryStream()) {
                ser.Serialize(mem, mod);
                mem.Position = 0;
                var deser = ser.Deserialize(mem);
            }

        }
        public FBModule Deserialize(Stream sIn) {
            //var reader = XmlDictionaryReader.CreateTextReader(sIn, new XmlDictionaryReaderQuotas());
            //return (FBModule)ser.ReadObject(reader, true);
            return (FBModule) bf.Deserialize(sIn);
            /*using (var mem = new MemoryStream()) {
                sIn.CopyTo(mem);
                return SpanJson.JsonSerializer.Generic.Utf8.Deserialize<FBModule>(mem.GetBuffer().AsSpan().Slice(0, (int)mem.Length));
            }*/
            /* using (var tIn = new StreamReader(sIn, Encoding.UTF8)) {
                 var serSettings = new Newtonsoft.Json.JsonSerializerSettings {
                     ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore,
                     Formatting = Newtonsoft.Json.Formatting.Indented,
                     PreserveReferencesHandling = Newtonsoft.Json.PreserveReferencesHandling.All,
                     TypeNameHandling = Newtonsoft.Json.TypeNameHandling.All,
                     ContractResolver = new DefaultContractResolver { IgnoreSerializableAttribute = false }
                 };
                 return JsonConvert.DeserializeObject<FBModule>(tIn.ReadToEnd(), serSettings);
             }*/
        }
    }
}
