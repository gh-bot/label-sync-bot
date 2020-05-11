using Newtonsoft.Json.Linq;
using System.Runtime.Serialization;

namespace Label.Synchronizer.Bot
{
    [DataContract()]
    public class LabelCreatedPayload : LabelEventPayload
    {
        public LabelCreatedPayload(JObject data)
            : base(data)
        {

        }

    }
}
