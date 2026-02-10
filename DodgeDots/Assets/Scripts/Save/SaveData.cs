using System;
using System.Collections.Generic;

namespace DodgeDots.Save
{
    [Serializable]
    public class SaveData
    {
        public List<string> completedLevels = new List<string>();
        public string lastScene = "WorldMap";
        public string savedAtUtc = "";
    }
}
