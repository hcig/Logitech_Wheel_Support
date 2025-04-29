using UnityEngine;

namespace HCIG {

    public class PIDController : MonoBehaviour {

        [Header("Patameters")]
        [SerializeField]
        private float _kp;
        [SerializeField]
        private float _ki;
        [SerializeField]
        private float _kd;

        private float _integral, _prevError;

        public PIDController(float kp, float ki, float kd) {
            _kp = kp;
            _ki = ki;
            _kd = kd;
        }

        public float Process(float error, float dt) {
            _integral += error * dt;
            float derivative = (error - _prevError) / dt;
            float output = _kp * error + _ki * _integral + _kd * derivative;
            _prevError = error;
            return Mathf.Clamp(output, -1, 1);
        }
    }
}








