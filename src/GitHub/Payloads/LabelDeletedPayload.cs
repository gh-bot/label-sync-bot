using Newtonsoft.Json.Linq;
using System.Runtime.Serialization;

namespace Label.Synchronizer.Bot
{
    [DataContract()]
    public class LabelDeletedPayload : LabelEventPayload
    {
        public LabelDeletedPayload(JObject data)
            : base(data)
        {

        }
    }
}
