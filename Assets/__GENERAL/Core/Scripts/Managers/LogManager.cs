using System.IO;
using UnityEngine;

namespace HCIG {

    enum LogType { Info, Warning, Error };

    public class LogManager : Singleton<LogManager> {

        private string _path;
        private string _ending = ".txt";

        protected override void Awake() {
            base.Awake();

            _path = Application.persistentDataPath;

            if (!Directory.Exists(_path)) {
                Directory.CreateDirectory(_path);
            }

            _path += "\\LogFile";

            if (File.Exists(_path + _ending)) {
                File.Copy(_path + _ending, _path + "-prev" + _ending, true);
                File.Delete(_path + _ending);
            }
        }

        public void LogInfo(string message) {
            LogMessage(LogType.Info, message);
        }

        public void LogWarning(string message) {
            LogMessage(LogType.Warning, message);
        }

        public void LogError(string message) {
            LogMessage(LogType.Error, message);
        }

        private void LogMessage(LogType type, string message) {

            string line = "";

            switch (type) {
                case LogType.Info:
                    line = "Info    :";
                    Debug.Log(message);
                    break;

                case LogType.Warning:
                    line = "Warning :";
                    Debug.LogWarning(message);
                    break;

                case LogType.Error:
                    line = "Error   :";
                    Debug.LogError(message);
                    break;
            }

            //if (!ApplicationManager.Instance.IsEditor && !ApplicationManager.Instance.IsAndroid) {

                //if (File.Exists(_path + _ending)) {
                //    File.Create(_path + _ending);
                //}

                StreamWriter writer = new StreamWriter(_path + _ending, true);

                line += message;

                writer.WriteLine(line);

                writer.Close();
            //}
        }
    }
}
