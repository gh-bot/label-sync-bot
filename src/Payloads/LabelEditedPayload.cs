using Newtonsoft.Json.Linq;
using System.Runtime.Serialization;

namespace Label.Synchronizer.Bot
{
    [DataContract()]
    public class LabelEditedPayload : LabelEventPayload
    {
        private JToken _changedName = null;

        public LabelEditedPayload(JObject data)
            : base(data)
        { }

        public string ChangedNameFrom => ChangedName[FROM].Value<string>();

        public JToken ChangedName => _changedName ?? (_changedName = _data[CHANGES][NAME]);
    }
}
