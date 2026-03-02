/*
 * Copyright (C) 2016, Jaguar Land Rover
 * This program is licensed under the terms and conditions of the
 * Mozilla Public License, version 2.0.  The full text of the
 * Mozilla Public License is at https://www.mozilla.org/MPL/2.0/
 */

using System;
using Unity.Netcode;
using UnityEngine;
using System.Collections.Generic;
using Core.Networking;
using Core.SceneEntities;
using StrangeLand.Steering;
using UnityEngine.InputSystem;


public class NetworkVehicleController : InteractableObject {
    public enum VehicleOpperationMode {
        KEYBOARD,
        STEERINGWHEEL,
        AUTONOMOUS,
        REMOTEKEYBOARD
    }

    [SerializeField] public VehicleOpperationMode VehicleMode;
    public Transform CameraPosition;

    private VehicleController controller;
   
    public Transform[] Left;
    public Transform[] Right;
    public Transform[] BrakeLightObjects;

    public Material IndicatorOn;
    public Material LightsOff;
    public Material BrakelightsOn;

    public List<Renderer> beamLightGlasses;
    private List<Material> _beamLightGlassMaterialInstances = new List<Material>();
    public Color beamLightGlassOnColor;
    public Color beamLightGlassOffColor;

    public List<Renderer> beamLights;
    private List<Material> _beamLightMaterialInstances = new List<Material>();


    public AudioSource HonkSound;
    public float SteeringInput;
    public float ThrottleInput;


    private ulong CLID;

    Speedometer m_Speedometer;

    /// <summary>
    /// SoundRelevant Variables
    /// </summary>
    public NetworkVariable<bool> IsShifting;

    public NetworkVariable<float> accellInput;
    public NetworkVariable<float> RPM;
    public NetworkVariable<float> traction;
    public NetworkVariable<float> MotorWheelsSlip;

    public NetworkVariable<float> CurrentSpeed;
    public NetworkVariable<RoadSurface> CurrentSurface;

    private bool REMOTEKEYBOARD_NewData = false;

    void UpdateSounds() {
        if (controller == null) return;
        IsShifting.Value = controller.IsShifting;
        accellInput.Value = controller.accellInput;
        RPM.Value = controller.RPM;
        traction.Value = (controller.traction + controller.tractionR + controller.rtraction + controller.rtractionR) /
                         4.0f;
        MotorWheelsSlip.Value = controller.MotorWheelsSlip;
        CurrentSpeed.Value = controller.CurrentSpeed;
        CurrentSurface.Value = controller.CurrentSurface;
    }

    public override void Stop_Action() {
        GetComponent<Rigidbody>().linearVelocity = Vector3.zero;
        GetComponent<Rigidbody>().angularVelocity = Vector3.zero;
    }


    public override void OnNetworkSpawn() {
        m_Speedometer = GetComponentInChildren<Speedometer>();
        CurrentSpeed.OnValueChanged += NewSpeedRecieved;
        if (IsServer) {
            controller = GetComponent<VehicleController>();
            if (IsServer) {
                NetworkManager.SceneManager.OnSceneEvent += SceneManager_OnSceneEvent;
                GetComponent<ForceFeedback>()?.Init(transform.GetComponent<Rigidbody>(), _participantOrder.Value);
            }
          
        }
        else {
            GetComponent<VehicleController>().enabled = false;
            if (GetComponent<ForceFeedback>() != null) {
                GetComponent<ForceFeedback>().enabled = false;
            }

            foreach (var wc in GetComponentsInChildren<WheelCollider>()) wc.enabled = false;
            if (VehicleMode == VehicleOpperationMode.AUTONOMOUS) {
               //ToDo adjust
            }
        }
    }

    private void NewSpeedRecieved(float previousvalue, float newvalue) {
        if (m_Speedometer != null) m_Speedometer.UpdateSpeed(newvalue);
    }

    private void Start() {
        indicaterStage = 0;

        foreach (Renderer tmpRenderer in beamLights) {
            Debug.Log(tmpRenderer.materials[0]);
            _beamLightMaterialInstances.Add(tmpRenderer.materials[0]);
        }

        foreach (Renderer tmpRenderer in beamLightGlasses) {
            _beamLightGlassMaterialInstances.Add(tmpRenderer.materials[0]);
        }


        HonkSound = GetComponent<AudioSource>();

        foreach (Transform t in Left) {
            t.GetComponent<MeshRenderer>().material = LightsOff;
        }

        foreach (Transform t in Right) {
            t.GetComponent<MeshRenderer>().material = LightsOff;
        }

        foreach (Transform t in BrakeLightObjects) {
            t.GetComponent<MeshRenderer>().material = LightsOff;
        }

        if (VehicleMode == VehicleOpperationMode.AUTONOMOUS) {
         //ToDO Was removed for getting it to work in n=nornal driver mode
        }

        if (SteeringWheelManager.Instance == null) {
            VehicleMode = VehicleOpperationMode.KEYBOARD;
        }

        i_HighBeamMyCar(false);//set opacity to 0
    }


    [ClientRpc]
    public void TurnOnLeftClientRpc(bool Leftl_) {
        TurnOnLeft(Leftl_);
    }

    private void TurnOnLeft(bool Leftl_) {
        if (Leftl_) {
            foreach (Transform t in Left) {
                t.GetComponent<MeshRenderer>().material = IndicatorOn;
            }
        }
        else {
            foreach (Transform t in Left) {
                t.GetComponent<MeshRenderer>().material = LightsOff;
            }
        }
    }

    [ClientRpc]
    public void TurnOnRightClientRpc(bool Rightl_) {
        TurnOnRight(Rightl_);
    }

    private void TurnOnRight(bool Rightl_) {
        if (Rightl_) {
            foreach (Transform t in Right) {
                t.GetComponent<MeshRenderer>().material = IndicatorOn;
            }
        }
        else {
            foreach (Transform t in Right) {
                t.GetComponent<MeshRenderer>().material = LightsOff;
            }
        }
    }

    [ClientRpc]
    public void TurnOnBrakeLightClientRpc(bool Active) {
        TurnOnBrakeLight(Active);
    }

    private void TurnOnBrakeLightLocalServer(bool Active) {
        TurnOnBrakeLightClientRpc(Active);
        TurnOnBrakeLight(Active);
    }

    private void TurnOnBrakeLight(bool Active) {
        if (Active) {
            foreach (Transform t in BrakeLightObjects) {
                t.GetComponent<MeshRenderer>().material = BrakelightsOn;
            }
        }
        else {
            foreach (Transform t in BrakeLightObjects) {
                t.GetComponent<MeshRenderer>().material = LightsOff;
            }
        }
    }

    public delegate void HonkDelegate();

    public HonkDelegate HonkHook;

    public void registerHonk(HonkDelegate val) {
        HonkHook += val;
    }

    public void DeRegisterHonk(HonkDelegate val) {
        HonkHook -= val;
    }

    [ClientRpc]
    public void HonkMyCarClientRpc() {
        // Debug.Log("HonkMyCarClientRpc");
        HonkSound.Play();
        if (HonkHook != null) {
            HonkHook.Invoke();
        }
    }

    private void LateUpdate() {
        if (IsServer && controller != null) {
            controller.steerInput = SteeringInput;
            controller.accellInput = ThrottleInput;
        }

        // test
         /*
        if (Input.GetKeyDown(KeyCode.Keypad1)) {
            foreach (Material mat in _beamLightMaterialInstances) {
                mat.SetFloat("_Opacity", 2);
            }

            foreach (Material mat in _beamLightGlassMaterialInstances) {
                mat.SetColor("_Color", beamLightGlassOnColor);
            }
        }

        if (Input.GetKeyDown(KeyCode.Keypad2)) {
            foreach (Material mat in _beamLightMaterialInstances) {
                mat.SetFloat("_Opacity", 0);
            }

            foreach (Material mat in _beamLightGlassMaterialInstances) {
                mat.SetColor("_Color", beamLightGlassOffColor);
            }
        }
        */
    }


    float _steeringAngle;
    public Transform SteeringWheel;


    public ParticipantOrder getParticipantOrder() {
        return _participantOrder.Value;
    }

    void Update() {
        if (!IsServer) return;

        if (ConnectionAndSpawning.Instance.ServerStateEnum.Value == EServerState.Interact) {
            bool tempLeft = false, tempRight = false, tempHonk = false, tempHighBeam = false;

            switch (VehicleMode) {
                case VehicleOpperationMode.KEYBOARD:
                    SteeringInput = GetAxis1D(Key.A, Key.D, Key.LeftArrow, Key.RightArrow);
                    ThrottleInput = GetAxis1D(Key.S, Key.W, Key.DownArrow, Key.UpArrow); 
                    break;
                case VehicleOpperationMode.STEERINGWHEEL:
                    SteeringInput = SteeringWheelManager.Instance.GetSteerInput(_participantOrder.Value);
                    ThrottleInput = SteeringWheelManager.Instance.GetAccelInput(_participantOrder.Value);
                    tempLeft =
                        SteeringWheelManager.Instance
                            .GetLeftIndicatorInput(_participantOrder.Value);
                    tempRight =
                        SteeringWheelManager.Instance
                            .GetRightIndicatorInput(_participantOrder.Value);
                    tempHonk = SteeringWheelManager.Instance.GetHornButtonInput(_participantOrder.Value);
                    tempHighBeam = SteeringWheelManager.Instance.GetHighBeamButtonInput(_participantOrder.Value);

                    break;
                case VehicleOpperationMode.AUTONOMOUS:
/*
                    SteeringInput = _autonomousVehicleDriver.GetSteerInput();
                    ThrottleInput = _autonomousVehicleDriver.GetAccelInput();
                    tempLeft = _autonomousVehicleDriver
                        .GetLeftIndicatorInput();
                    tempRight = _autonomousVehicleDriver.GetRightIndicatorInput();
                    tempHonk = _autonomousVehicleDriver.GetHornInput();

                    if (_autonomousVehicleDriver.StopIndicating()) {
                        _StopIndicating();
                    }
*/

                    break;
                case VehicleOpperationMode.REMOTEKEYBOARD:
                    if (REMOTEKEYBOARD_NewData) {
                        REMOTEKEYBOARD_NewData = false;

                        tempLeft = leftInput;
                        tempRight = rightInput;
                        tempHonk = honkInput;
                    }

                    ;


                    break;
                default:
                    break;
            }


            SteeringWheel.RotateAround(SteeringWheel.position, SteeringWheel.up,
                _steeringAngle - SteeringInput * -450f);
            _steeringAngle = SteeringInput * -450f;


            if (NewButtonPress && (tempLeft || tempRight)) {
                NewButtonPress = false;
                if (tempLeft && !tempRight) {
                    toggleBlinking(true, false);
                    LeftIndicatorDebounce = true;
                }

                else if (tempRight && !tempLeft) {
                    toggleBlinking(false, true);
                    RightIndicatorDebounce = true;
                }
                else {
                    toggleBlinking(true, true);
                    BothIndicatorDebounce = true;
                }
            }
            else if (NewButtonPress == false && !BothIndicatorDebounce &&
                     ((tempLeft && !LeftIndicatorDebounce) || (tempRight && !RightIndicatorDebounce))) {
                toggleBlinking(true, true);
                BothIndicatorDebounce = true;
            }
            else if (NewButtonPress == false && !tempLeft && !tempRight) {
                NewButtonPress = true;
                LeftIndicatorDebounce = false;
                RightIndicatorDebounce = false;
                BothIndicatorDebounce = false;
            }

            UpdateIndicator();


            if (tempHonk) {
                HonkMyCar();
            }

            HighBeamMyCar(tempHighBeam);

            if (ThrottleInput < 0 && !breakIsOn) {
                TurnOnBrakeLightLocalServer(true);
                breakIsOn = true;
            }
            else if (ThrottleInput >= 0 && breakIsOn) {
                TurnOnBrakeLightLocalServer(false);
                breakIsOn = false;
            }
        }
        else if (ConnectionAndSpawning.Instance.ServerStateEnum.Value == EServerState.Questions) {
            SteeringInput = 0;
            ThrottleInput = -1;
        }

        UpdateSounds();
    }


    bool leftInput, rightInput, honkInput;


    public void NewDataToCome(float i_steering, float i_throttle, bool i_left, bool i_right, bool i_honk) {
        REMOTEKEYBOARD_NewData = true;
        SteeringInput = i_steering;
        ThrottleInput = i_throttle;
        leftInput = i_left;
        rightInput = i_right;
        honkInput = i_honk;
    }

   

    public override void AssignClient(ulong clid, ParticipantOrder participantOrder)
    {
        /*
        Debug.Log($"Assign CLient!");
        if (IsServer) {
            NetworkManager.SceneManager.OnSceneEvent += SceneManager_OnSceneEvent;
            CLID = clid;
            _participantOrder.Value = participantOrder;
            Debug.Log($"Assigning a Client and starting ForceFeedback for CLID{clid} and po {participantOrder}");
            GetComponent<ForceFeedback>()?.Init(transform.GetComponent<Rigidbody>(), _participantOrder.Value);
        }
        else {
            Debug.LogWarning("Tried to execute something that should never happen.");
        }*/ // Deprecated function call in StrangeLand-Package
    }
    
    

    public override Transform GetCameraPositionObject() {
        return transform.Find("CameraPosition");
    }

    public override void SetStartingPose(Pose _pose) {
        if (!IsServer) return;
        transform.GetComponent<Rigidbody>().linearVelocity =
            Vector3.zero; // Unsafe we are not sure that it has a rigid body
        transform.GetComponent<Rigidbody>().angularVelocity = Vector3.zero;
        transform.position = _pose.position;
        transform.rotation = _pose.rotation;
    }

    public override bool HasActionStopped() {
        if (transform.GetComponent<Rigidbody>().linearVelocity.magnitude < 0.01f) {
            return true;
        }

        return false;
    }


    private void SceneManager_OnSceneEvent(SceneEvent sceneEvent) {
        //   Debug.Log("SceneManager_OnSceneEvent called with event:" + sceneEvent.SceneEventType.ToString());
        switch (sceneEvent.SceneEventType) {
            case SceneEventType.SynchronizeComplete: {
                //  Debug.Log("Scene event change by Client: " + sceneEvent.ClientId);
                if (sceneEvent.ClientId == CLID) {
                    // Debug.Log("Server: " + IsServer.ToString() + "  IsClient: " + IsClient.ToString() +
                    //  "  IsHost: " + IsHost.ToString());
                    //SetPlayerParent(sceneEvent.ClientId);
                }

                break;
            }
            default:
                break;
        }
    }


    private bool breakIsOn;


    private bool m_HighBeams;

    private void HighBeamMyCar(bool tempHighBeam) {
        if (m_HighBeams == tempHighBeam) return;
        m_HighBeams = tempHighBeam;
        if (IsServer) {
            i_HighBeamMyCar(tempHighBeam);
            HighBeamMyCarClientRpc(tempHighBeam);
        }
    }

    [ClientRpc]
    private void HighBeamMyCarClientRpc(bool tempHighBeam) {
        i_HighBeamMyCar(tempHighBeam);
    }

    private void i_HighBeamMyCar(bool tempHighBeam) {
        if (tempHighBeam) {
            StrangeLandLogger.Instance.LogEvent($"ParticipantOrder: {GetParticipantOrder()} StartHighBeam!");
            foreach (Material mat in _beamLightMaterialInstances) {
                mat.SetFloat("_Opacity", 2);
            }

            foreach (Material mat in _beamLightGlassMaterialInstances) {
                mat.SetColor("_Color", beamLightGlassOnColor);
            }
        }
        else {
            StrangeLandLogger.Instance.LogEvent($"ParticipantOrder: {GetParticipantOrder()} StopHighBeam!");
            foreach (Material mat in _beamLightMaterialInstances) {
                mat.SetFloat("_Opacity", 0);
            }

            foreach (Material mat in _beamLightGlassMaterialInstances) {
                mat.SetColor("_Color", beamLightGlassOffColor);
            }
        }
    }


    public void HonkMyCar() {
        if (HonkSound.isPlaying) {
            return;
        }

        HonkSound.Play();
        HonkMyCarClientRpc();
        StrangeLandLogger.Instance.LogEvent($"ParticipantOrder: {GetParticipantOrder()} Honk!");
    }


    public void SetNewNavigationInstructions(Dictionary<ParticipantOrder, NavigationScreen.Direction> Directions) {
        if (Directions.ContainsKey(_participantOrder.Value)) {
            GetComponentInChildren<NavigationScreen>().SetDirection(Directions[_participantOrder.Value]);
            SetNavigationScreenClientRPC(Directions[_participantOrder.Value]);
        }
    }

    [ClientRpc]
    private void SetNavigationScreenClientRPC(NavigationScreen.Direction Direction) {
        if (IsClient)
            GetComponentInChildren<NavigationScreen>().SetDirection(Direction);
    }
    #region collisionLog
    private void OnCollisionEnter(Collision collision)
    {
        StrangeLandLogger.Instance.LogEvent($"Participant: {GetParticipantOrder()} " +
                                            $"collided with {collision.transform.name} " +
                                            $"at contact point {collision.contacts[0].point} " +
                                            $"with a relative velocity of {collision.relativeVelocity} " +
                                            $"at pos {transform.position} rot {transform.rotation}");

    }
    #endregion
#region NewInputSystemLogic
private static float GetAxis1D(Key neg, Key pos)
{
    var kb = Keyboard.current;
    if (kb == null) return 0f;

    float v = 0f;
    if (kb[neg].isPressed) v -= 1f;
    if (kb[pos].isPressed) v += 1f;
    return Mathf.Clamp(v, -1f, 1f);
}

private static float GetAxis1D(Key negA, Key posA, Key negB, Key posB)
{
    // e.g., WASD + Arrows
    return Mathf.Clamp(GetAxis1D(negA, posA) + GetAxis1D(negB, posB), -1f, 1f);
}

#endregion
    #region IndicatorLogic

    private bool LeftActive;
    private bool RightActive;


    public bool LeftIsActuallyOn;
    public bool RightIsActuallyOn;
    public bool ActualLightOn;
    private float indicaterTimer;
    public float interval;
    public int indicaterStage;

    public bool NewButtonPress;
    public bool LeftIndicatorDebounce;
    public bool RightIndicatorDebounce;
    public bool BothIndicatorDebounce;

    void toggleBlinking(bool left, bool right) {
        if (indicaterStage == 0) {
            indicaterStage = 1;
        }

        if (left && right) {
            if (LeftIsActuallyOn != true || RightIsActuallyOn != true) {
                LeftIsActuallyOn = true;
                RightIsActuallyOn = true;
            }
            else if (LeftIsActuallyOn == RightIsActuallyOn == true) {
                RightIsActuallyOn = false;
                LeftIsActuallyOn = false;
                indicaterStage = 4;
            }
        }

        if (left != right) {
            if (LeftIsActuallyOn && RightIsActuallyOn) {
                LeftIsActuallyOn = false;
                RightIsActuallyOn = false;
                indicaterStage = 4;
            }

            if (left) {
                if (!LeftIsActuallyOn) {
                    LeftIsActuallyOn = true;
                    RightIsActuallyOn = false;
                }
                else {
                    LeftIsActuallyOn = false;
                    indicaterStage = 4;
                }
            }

            if (right) {
                if (!RightIsActuallyOn) {
                    LeftIsActuallyOn = false;
                    RightIsActuallyOn = true;
                }
                else {
                    RightIsActuallyOn = false;
                    indicaterStage = 4;
                }
            }
        }
    }

    public string GetIndicatorString() {
        return "Left" + LeftIsActuallyOn.ToString() + " - " + "Right" + RightIsActuallyOn.ToString();
    }

    public void GetIndicatorState(out bool Left, out bool right) {
        Left = LeftIsActuallyOn;
        right = RightIsActuallyOn;
    }

    void UpdateIndicator() {
        if (indicaterStage == 1) {
            indicaterStage = 2;
            indicaterTimer = interval;
            ActualLightOn = false;
        }
        else if (indicaterStage == 2 || indicaterStage == 3) {
            indicaterTimer += Time.deltaTime;

            if (indicaterTimer > interval) {
                indicaterTimer = 0;
                ActualLightOn = !ActualLightOn;
                if (ActualLightOn) {
                    LeftIndicatorChanged(LeftIsActuallyOn);
                    RightIndicatorChanged(RightIsActuallyOn);
                }
                else {
                    RightIndicatorChanged(false);
                    LeftIndicatorChanged(false);
                }
            }

            if (indicaterStage == 2) {
                switch (VehicleMode) {
                    case VehicleOpperationMode.KEYBOARD:
                        break;
                    case VehicleOpperationMode.STEERINGWHEEL:
                        if (SteeringWheelManager.Instance != null &&
                            Mathf.Abs(SteeringWheelManager.Instance.GetSteerInput(_participantOrder.Value) * -450f) >
                            90) {
                            indicaterStage = 3;
                        }

                        break;
                    case VehicleOpperationMode.AUTONOMOUS:
                        break;
                    case VehicleOpperationMode.REMOTEKEYBOARD:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            else if (indicaterStage == 3) {
                switch (VehicleMode) {
                    case VehicleOpperationMode.KEYBOARD:
                        break;
                    case VehicleOpperationMode.STEERINGWHEEL:
                        if (SteeringWheelManager.Instance != null &&
                            Mathf.Abs(SteeringWheelManager.Instance.GetSteerInput(_participantOrder.Value) * -450f) <
                            10) {
                            indicaterStage = 4;
                        }

                        break;
                    case VehicleOpperationMode.AUTONOMOUS:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }
        else if (indicaterStage == 4) {
            indicaterStage = 0;
            ActualLightOn = false;
            LeftIsActuallyOn = false;
            RightIsActuallyOn = false;
            RightIndicatorChanged(false);
            LeftIndicatorChanged(false);
            // UpdateIndicatorLightsServerRpc(false, false);
        }
    }

    private void _StopIndicating() {
        if (indicaterStage > 1) {
            indicaterStage = 4;
        }
    }

    private void RightIndicatorChanged(bool newvalue) {
        if (!IsServer) {
            return;
        }

        TurnOnRight(newvalue);
        TurnOnRightClientRpc(newvalue);
    }

    private void LeftIndicatorChanged(bool newvalue) {
        TurnOnLeft(newvalue);
        TurnOnLeftClientRpc(newvalue);
    }

    #endregion
}