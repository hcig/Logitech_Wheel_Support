using System.Collections.Generic;
using UnityEngine;

using Logitech;
using TMPro;

namespace HCIG.Wheel {

    public enum GearState {
        Down = -1,
        None = 0,
        Up = 1
    }

    public enum ButtonType {
        A = 0,
        B = 1,
        X = 2,
        Y = 3,

        RB = 4,
        LB = 5,

        Start = 6,
        Select = 7,

        RSB = 8,
        LSB = 9,

        Xbox = 10,

        Cross_Up = 11,
        Cross_Up_L = 12,
        Cross_Up_R = 13,
        Cross_Left = 14,
        Cross_Right = 15,
        Cross_Down = 16,
        Cross_Down_L = 17,
        Cross_Down_R = 18,
    }

    public class LogitechManager : Singleton<LogitechManager> {

        public float MAX_STEER_ANGLE {
            get {
                return _MAX_STEER_ANGLE;
            }
        }
        private const float _MAX_STEER_ANGLE = 450;
        private const float _MAX_VALUE_SIDE = 32767;

        public bool Alternative {
            get {
                return _alternative;
            }
        }
        private bool _alternative = false;

        public bool HumanSteer {
            get {
                return _humanSteer;
            }
        }
        private bool _humanSteer = false;
        private bool _initialized = false;

        #region Feet - Pedals

        public float Accel {
            get {
                if (!_initialized) {
                    return 0;
                }
                if (_accel < 0.01f) {
                    // Otherwise we will move allways a liiiiiitttle bit and that's not so nice, when we dont have anyone driving right now
                    return 0;
                }
                return _accel;
            }
        }
        private float _accel;
        private float _oAccel;

        public float Brake {
            get {
                if (!_initialized) {
                    return 0;
                }
                return _brake;
            }
        }
        private float _brake;
        private float _oBrake;

        public float Clutch {
            get {
                if (!_initialized) {
                    return 0;
                }
                return _clutch;
            }
        }
        private float _clutch;
        private float _oClutch;

        /// <summary>
        /// Sets the requested steer angle and tries from now on that the wheel positions itself near this value.
        /// </summary>
        public float SteerReq {
            set {
                _reqSteer = value;
            }
        }
        private float _reqSteer = 0;

        /// <summary>
        /// Returns the current steer angle in degree [°]
        /// </summary>
        public float SteerAng {
            get {
                if (!_initialized) {
                    return 0;
                }
                return _curSteer;
            }

        }

        [Header("PID")]
        [SerializeField]
        private PIDController wheelController;

        private float _oldReq = 0;
        private float _curReq = 0;

        private float _oldAng = 0;
        private float _curPrf = 0;

        private float _allTimeDiffPerformedAngle = 0;
        private float _allTimeDiffRequestedAngle = 0;

        private int _allTimeDiffCounter = 0;

        private int _amplifier = 12;
        private float _driftAngle = 2.5f;

        [Header("Textfields")]
        [SerializeField]
        private TMP_Text _amplifierText;
        [SerializeField]
        private TMP_Text _driftAngleText;

        [Header("Logging")]
        [SerializeField]
        private bool _debug = false;

        [Header("Testing")]
        [Range(-450, 450)]
        [Tooltip("Sets the desired angle for the wheel")]
        [SerializeField]
        private float _angle = 0;

        /// <summary>
        /// Returns the current steer angle in absolut [-1 ... 1]
        /// </summary>
        public float SteerAbs {
            get {
                if (!_initialized) {
                    return 0;
                }
                return _curSteer / _MAX_STEER_ANGLE;
            }
        }
        private float _curSteer;
        private float _oldSteer;

        public GearState GearState {
            get {
                if (!_initialized) {
                    return 0;
                }
                return _gearState;
            }
        }
        private GearState _gearState = GearState.None;
        private bool _pressed = false;

        public bool InputSinceLastFrame {
            get {
                return _inputSinceLastFrame;
            }
        }
        private bool _inputSinceLastFrame = false;

        #endregion Hand - Pedals

        #region Buttons

        private List<ButtonType> _pressedCurrent = new List<ButtonType>();
        private List<ButtonType> _pressedBefore = new List<ButtonType>();

        #endregion Buttons

        private void OnValidate() {
            _reqSteer = _angle;
        }

        void Start() {

            // Prefs
            _amplifier = PlayerPrefs.GetInt("Amplifier", _amplifier);
            _driftAngle = PlayerPrefs.GetFloat("DriftAngle", _driftAngle);

            if (LogitechGSDK.LogiSteeringInitialize(false)) {
                Debug.Log("Logitech-Wheel - Initialized");
            }
        }

        private void OnDisable() {
            if (LogitechGSDK.LogiSteeringShutdown()) {
                Debug.Log("Logitech-Wheel - Initialized");
            }
        }

        void FixedUpdate() {

            _inputSinceLastFrame = false;

            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.D) || _alternative) {

                //#########################################
                //
                //             Alternate Input
                //
                //#########################################
                _initialized = true;

                if (Input.GetKey(KeyCode.W) && _brake == 0) {
                    // Accel
                    _accel = 1;
                    _alternative = true;
                } else if (Input.GetKey(KeyCode.S) && _accel == 0) {
                    // Brake
                    _brake = 1;
                    _alternative = true;
                } else {
                    // Off
                    _accel = 0;
                    _brake = 0;
                }

                // Steer
                float step = Time.fixedDeltaTime * _MAX_STEER_ANGLE;

                if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.A)) {

                    if (Input.GetKey(KeyCode.D)) {
                        // Right
                        _curSteer = Mathf.Clamp(_curSteer + step, -_MAX_STEER_ANGLE, _MAX_STEER_ANGLE);
                    }

                    if (Input.GetKey(KeyCode.A)) {
                        // Left
                        _curSteer = Mathf.Clamp(_curSteer - step, -_MAX_STEER_ANGLE, _MAX_STEER_ANGLE);
                    }

                    _humanSteer = true;
                    _alternative = true;
                } else {
                    // Off

                    _humanSteer = false;
                    _curSteer = (_curSteer < 0) ? Mathf.Clamp(_curSteer + step, -_MAX_STEER_ANGLE, 0) : Mathf.Clamp(_curSteer - step, 0, _MAX_STEER_ANGLE);
                }

                if (_curSteer == 0 && _accel == 0 && _brake == 0) {
                    _alternative = false;
                } else {
                    _inputSinceLastFrame = true;
                }
            } else {

                //#########################################
                //
                //             LogiWheel Input
                //
                //#########################################
                if (LogitechGSDK.LogiUpdate() && LogitechGSDK.LogiIsConnected(0)) {

                    LogitechGSDK.DIJOYSTATE2ENGINES wheel = LogitechGSDK.LogiGetStateUnity(0);

                    // Wheel
                    _curSteer = Mathf.Clamp(wheel.lX / _MAX_VALUE_SIDE, -1, 1) * _MAX_STEER_ANGLE;
                    _inputSinceLastFrame |= _curSteer != _oldSteer;
                    _oldSteer = _curSteer;

                    // Foot-Pedals
                    _accel = Mathf.Clamp(Mathf.Abs(wheel.lY - _MAX_VALUE_SIDE) / (_MAX_VALUE_SIDE * 2), 0, 1);
                    _inputSinceLastFrame |= _accel != _oAccel;
                    _oAccel = _accel;

                    _brake = Mathf.Clamp(Mathf.Abs(wheel.lRz - _MAX_VALUE_SIDE) / (_MAX_VALUE_SIDE * 2) * 8, 0, 1);
                    _inputSinceLastFrame |= _brake != _oBrake;
                    _oBrake = _brake;

                    _clutch = Mathf.Clamp(Mathf.Abs(wheel.rglSlider[0] - _MAX_VALUE_SIDE) / (_MAX_VALUE_SIDE * 2), 0, 1);
                    _inputSinceLastFrame |= _clutch != _oClutch;
                    _oClutch = _clutch;

                    // Hand-Pedals
                    switch (_gearState) {
                        case GearState.None:
                            if (_pressed) {
                                // Skip new check loop during sustaining press of pedals

                                if (wheel.rgbButtons[4] == 0 && wheel.rgbButtons[5] == 0) {
                                    // Reset state when none of the paddles is pressed anymore
                                    _pressed = false;
                                }

                                break;
                            }

                            // Up - Press
                            if (wheel.rgbButtons[4] == 128) {
                                _gearState = GearState.Up;
                            }

                            // Down - Press
                            if (wheel.rgbButtons[5] == 128) {
                                _gearState = GearState.Down;
                            }

                            _pressed = _gearState != GearState.None;
                            _inputSinceLastFrame |= _pressed;

                            break;
                        case GearState.Down:
                        case GearState.Up:
                            _gearState = GearState.None;

                            break;
                    }

                    _pressedBefore = new(_pressedCurrent);
                    _pressedCurrent.Clear();

                    // Buttons
                    for (int i = 0; i < wheel.rgbButtons.Length; i++) {

                        if (wheel.rgbButtons[i] != 0) {
                            _pressedCurrent.Add((ButtonType)i);
                        }
                    }

                    if (_pressedCurrent.Count > 0) {
                        _inputSinceLastFrame = true;
                    }

                    switch (wheel.rgdwPOV[0]) {
                        case (0):
                            _pressedCurrent.Add(ButtonType.Cross_Up);
                            break;
                        case (4500):
                            _pressedCurrent.Add(ButtonType.Cross_Up_R);
                            break;
                        case (9000):
                            _pressedCurrent.Add(ButtonType.Cross_Right);
                            break;
                        case (13500):
                            _pressedCurrent.Add(ButtonType.Cross_Down_R);
                            break;
                        case (18000):
                            _pressedCurrent.Add(ButtonType.Cross_Down);
                            break;
                        case (22500):
                            _pressedCurrent.Add(ButtonType.Cross_Down_L);
                            break;
                        case (27000):
                            _pressedCurrent.Add(ButtonType.Cross_Left);
                            break;
                        case (31500):
                            _pressedCurrent.Add(ButtonType.Cross_Up_L);
                            break;
                        default:
                            // CENTER
                            break;
                    }

                    //#########################################
                    //
                    //                INITIALIZE
                    //
                    //#########################################
                    if (!_initialized && (_accel != 0.5f || _brake != 1.0f || _curSteer != 0f)) {
                        _initialized = true;
                    }

                    //#########################################
                    //
                    //                FEEDBACK
                    //
                    //#########################################

                    // Request & Performed
                    _curReq = _reqSteer;
                    _curPrf = _curSteer;

                    // Calculation
                    float diffRTP = _curReq - _curPrf;

                    float requestedDiff = _curReq - _oldReq;
                    float performedDiff = _curPrf - _oldAng;

                    _allTimeDiffPerformedAngle += performedDiff;
                    _allTimeDiffRequestedAngle += requestedDiff;

                    // Check Human - Ehrn the wheel has a delta angle of 2.5° over the timespoan of 10 fixed timesteps [0.2 sec]
                    int driftCounts = 10;

                    if (Mathf.Abs(_allTimeDiffRequestedAngle - _allTimeDiffPerformedAngle) > _driftAngle) {
                        _allTimeDiffCounter++;
                    } else {
                        _allTimeDiffCounter = 0;
                    }

                    if (_allTimeDiffCounter > driftCounts) {
                        _humanSteer = true;
                    } else {
                        _humanSteer = false;
                    }

                    // Save Values
                    _oldReq = _curReq;
                    _oldAng = _curPrf;

                    // Steer 
                    int pid = Mathf.Clamp((int)(-95 * wheelController.Process(diffRTP, Time.fixedDeltaTime)), -100, 100);

                    if (pid != 0) {
                        pid -= (int)Mathf.Sign(diffRTP) * _amplifier;   // Amplifier - the performance of the wheel starts just around 13% of the wheel force.
                    }

                    // Update Wheel
                    LogitechGSDK.LogiPlayConstantForce(0, pid);
                }
            }

            //#########################################
            //
            //                  DEBUG
            //
            //#########################################
            if (_debug && _initialized) {

                Debug.Log("Steer-Angle: " + _curSteer + "\n");
                Debug.Log("Gas-Pedal: " + _accel + "\n");
                Debug.Log("Brake: " + _brake + "\n");
                Debug.Log("Clutch: " + _clutch + "\n");

                if (_gearState != GearState.None) {
                    Debug.Log("Gear: " + _gearState.ToString() + "\n");
                }
            }

            for (int i = 0; i < (int)ButtonType.Cross_Down_R; i++) {

                if (GetButtonDown((ButtonType)i)) {
                    Debug.Log("Button pressed: " + ((ButtonType)i).ToString());
                }

                if (GetButton((ButtonType)i)) {
                    Debug.Log("Button stay: " + ((ButtonType)i).ToString());
                }

                if (GetButtonUp((ButtonType)i)) {
                    Debug.Log("Button released: " + ((ButtonType)i).ToString());
                }
            }


            // Calibration
            if (GetButton(ButtonType.X) && GetButton(ButtonType.Y) && GetButton(ButtonType.LB) && !GetButton(ButtonType.RB)) {
                // AMPLIFIER

                if (GetButtonDown(ButtonType.Cross_Up)) {
                    _amplifier += 1;
                }

                if (GetButtonDown(ButtonType.Cross_Down)) {
                    _amplifier -= 1;
                }

                if (_amplifier > 20) {
                    _amplifier = 20;
                }

                if (_amplifier < 0) {
                    _amplifier = 0;
                }

                _amplifierText.text = "Amplifier: " + _amplifier;
                _amplifierText.enabled = true;

                PlayerPrefs.SetInt("Amplifier", _amplifier);
            } else {
                _amplifierText.enabled = false;
            }

            if (GetButton(ButtonType.X) && GetButton(ButtonType.Y) && GetButton(ButtonType.RB) && !GetButton(ButtonType.LB)) {
                // DRIFT-ANGLE

                if (GetButtonDown(ButtonType.Cross_Up)) {
                    _driftAngle += 0.1f;
                }

                if (GetButtonDown(ButtonType.Cross_Down)) {
                    _driftAngle -= 0.1f;
                }

                if (_driftAngle > 20) {
                    _driftAngle = 20;
                }

                if (_driftAngle < 1) {
                    _driftAngle = 1;
                }

                _driftAngleText.text = "Drift-Angle: " + _driftAngle.ToString("#.0");
                _driftAngleText.enabled = true;

                PlayerPrefs.SetFloat("DriftAngle", _driftAngle);
            } else {
                _driftAngleText.enabled = false;
            }
        }

        /// <summary>
        /// Returns whether the button is currently pressed.
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public bool GetButton(ButtonType type) {
            return _pressedCurrent.Contains(type);
        }

        /// <summary>
        /// Returns true, if the button got pressed right now.
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public bool GetButtonDown(ButtonType type) {
            return _pressedCurrent.Contains(type) && !_pressedBefore.Contains(type);
        }

        /// <summary>
        /// Returns true, if the button got released right now
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public bool GetButtonUp(ButtonType type) {
            return !_pressedCurrent.Contains(type) && _pressedBefore.Contains(type);
        }
    }
}
