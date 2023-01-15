﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MKBB.Commands
{
    public class Ghost
    {
        [JsonProperty("_links")]
        public Link LinkContainer { get; set; }
        [JsonProperty("finishTime")]
        public string FinishTime { get; set; }
        [JsonProperty("finishTimeSimple")]
        public string FinishTimeSimple { get; set; }
        [JsonProperty("dateSet")]
        public string DateSet { get; set; }
        [JsonProperty("vehicleId")]
        public int VehicleID { get; set; }
        [JsonProperty("driverId")]
        public int DriverID { get; set; }
        [JsonProperty("controller")]
        public int ControllerID { get; set; }
        [JsonProperty("trackId")]
        public string TrackID { get; set; }
        [JsonProperty("categoryId")]
        public int Category { get; set; }
        [JsonProperty("playerId")]
        public string PlayerId { get; set; }
        public string CategoryName { get; set; }
        [JsonProperty("playersFastest")]
        public bool IsPB { get; set; }
        [JsonProperty("200cc")]
        public bool Is200cc { get; set; }
        public ExtraInfo ExtraInfo { get; set; }
    }

    public class ExtraInfo
    {
        [JsonProperty("player")]
        public string MiiName { get; set; }
        [JsonProperty("splits")]
        public List<string> Splits { get; set; }
        [JsonProperty("splitsSimple")]
        public List<string> SimpleSplits { get; set; }
    }
}
