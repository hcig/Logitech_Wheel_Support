using UnityEngine;

namespace HCIG {

    public static class Random {
        private static int _last = 0;

        private static System.Random _rnd = new System.Random();

        public static int Value(int max) {

            _last = (_rnd.Next(max) + _last) % max;
            //Debug.Log("VALUE - " + _last);
            return _last;
        }

        public static float Range(float start, float end) {

            float value = (Time.realtimeSinceStartup + _last) % (end - start) + start;
            _last = (int)(start * 1000) % (int)Mathf.Ceil(end);

            //Debug.Log("RANGE: " + start + " <--- " + value + " ---> " + end);
            return value;
        }

        public static int Range(int start, int end) {

            int value = _rnd.Next(end - start) + start;

            //Debug.Log("RANGE: " + start + " <--- " + value + " ---> " + end);
            return value;
        }
    }
}
