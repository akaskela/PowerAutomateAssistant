using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;

namespace Kaskela.PowerAutomateAssistant
{
    [DataContract]
    public class TrueFieldConfiguration
    {
        [DataMember]
        public string TextFieldName { get; set; }
        [DataMember]
        public bool IncludeGhostChanges { get; set; }
        [DataMember]
        public string[] TriggerFields { get; set; }

        public static TrueFieldConfiguration Deserialize(string json)
        {
            using (var stream = new MemoryStream())
            {
                using (var writer = new StreamWriter(stream))
                {
                    writer.Write(json);
                    writer.Flush();
                    stream.Position = 0;

                    var serializer = new DataContractJsonSerializer(typeof(TrueFieldConfiguration), new DataContractJsonSerializerSettings
                    {
                        UseSimpleDictionaryFormat = true
                    });
                    return (TrueFieldConfiguration)serializer.ReadObject(stream);
                }
            }
        }
    }
}
