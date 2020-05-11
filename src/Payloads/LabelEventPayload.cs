using Newtonsoft.Json.Linq;
using System;
using System.Runtime.Serialization;

namespace Label.Synchronizer.Bot
{
    [DataContract()]
    public abstract class LabelEventPayload
    {
        protected const string ID    = "id";
        protected const string FROM  = "from";
        protected const string TYPE  = "type";
        protected const string NAME  = "name";
        protected const string LABEL = "label";
        protected const string COLOR = "color";
        protected const string LOGIN = "login";
        protected const string OWNER = "owner";
        protected const string SENDER = "sender";
        protected const string CHANGES = "changes";
        protected const string REPOSITORY = "repository";
        protected const string DESCRIPTION = "description";
        protected const string INSTALLATION = "installation";


        protected const string MARKETPLACE_PURCHASE = "marketplace_purchase";
        protected const string PLAN = "plan";

        private string _labelName;
        private string _senderName;
        private string _ownerName;

        protected JObject _data;

        public LabelEventPayload(JObject data)
        {
            _data = data;
        }


        #region IDs

        public long OwnerId => _data[REPOSITORY][OWNER][ID].Value<long>();

        public long RepositoryId => _data[REPOSITORY][ID].Value<long>();
        
        public long InstallationId => _data[INSTALLATION][ID].Value<long>();

        #endregion

        
        #region Identity

        public bool IsBot => "Bot" == _data[SENDER][TYPE].Value<string>();

        public string SenderLogin => _senderName ?? (_senderName = _data[SENDER][LOGIN].Value<string>());

        public string OwnerLogin => _ownerName ?? (_ownerName = _data[REPOSITORY][OWNER][LOGIN].Value<string>());

        #endregion


        #region Label

        public string LabelName => _labelName ?? (_labelName = _data[LABEL][NAME].Value<string>());

        public string LabelColor => _data[LABEL][COLOR].Value<string>();

        public string LabelDescription => _data[LABEL][DESCRIPTION].Value<string>();

        #endregion


        #region Plan

        public int Limit = int.MaxValue;

        public bool IgnorePrivate = false;

        #endregion
    }

    public static class LabelEventPayloadExtension
    {
        public static LabelEventPayload GetPayload(this JObject data)
        {
            return data["action"].Value<string>() switch
            {
                "created" => new LabelCreatedPayload(data),
                "edited"  => new LabelEditedPayload (data),
                "deleted" => new LabelDeletedPayload(data),
                _ => throw new InvalidOperationException("Unknown Action"),
            };
        }
    }
}
