using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ActivityClient
{
    [Serializable()]
    public class Settings
    {
        public string IPAddress { get; set; }
        public bool TeamDisplayEnabled { get; set; }
        public bool MusicEffectsEnabled { get; set; }
        public bool AnimationsEnabled { get; set; }
        public string FontSize { get; set; }
    }
}
