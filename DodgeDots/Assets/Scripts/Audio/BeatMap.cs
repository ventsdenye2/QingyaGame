using UnityEngine;

namespace DodgeDots.Audio
{
    [CreateAssetMenu(fileName = "BeatMap", menuName = "Audio/BeatMap")]
    public class BeatMap : ScriptableObject
    {
        [Tooltip("节拍时间点，单位：秒（相对于播放起点）")]
        public double[] beatTimes = new double[0];
    }
}
