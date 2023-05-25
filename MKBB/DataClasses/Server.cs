﻿using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace MKBB.Data
{
    public class GBTrackData
    {
        [Key] public int ID { get; set; }
        public string Name { get; set; }
        public string SHA1s { get; set; }
        public string GhostLink { get; set; }
    }
}
