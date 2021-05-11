using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace JETModUpdater
{
    class UpdateInfo
    {
        [JsonProperty("autoUpdate")]
        public bool AutoUpdate = true;
        [JsonProperty("forceDowngrade")]
        public bool ForceDowngrade = false;
        [JsonProperty("checkUpdateUrl")]
        public string CheckUpdateUrl = null;
        [JsonProperty("downloadUpdateUrl")]
        public string DownloadUpdateUrl = null;
        [JsonProperty("author")]
        public string Author = string.Empty;
        [JsonProperty("name")]
        public string Name = string.Empty;
        [JsonProperty("excludeFromUpdate")]
        public string[] ExcludeFromUpdate = { };
        [JsonProperty("version")]
        public Version Version = null;
    }
}
