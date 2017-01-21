using System;
using UnityEngine;

namespace AnalogControl
{
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class AnalogControl : MonoBehaviour
    {
    	
    	const float STEERING_ABSOLUTE_DEAD_ZONE = 0.001f;

    	Configuration config;
        
        bool firstStart = true;
        // state
        bool isRollMode = true;
        ActivationState controlState = ActivationState.Inactive;
        
        // display
        static Texture2D target;

        static Texture2D markerSpot;
        Rect markerRect;        

        bool lockInput = false;
        
        ActuatorOverride actuatorOverride = new ActuatorOverride();    
        
        /// <summary>
        /// Initialise control region size and other user specific params
        /// </summary>
        public void Start()
        {
            config = Configuration.loadConfig();

            try
            {
                if (target == null)
                {
                    target = new Texture2D(500, 500);
                    target.LoadImage(System.IO.File.ReadAllBytes(KSPUtil.ApplicationRootPath + "GameData/Analog Control Continued/PluginData/AnalogControl/crosshair-white.png"));
                }
                setTransparency(target, config.transparency);
                
            }
            catch
            {
                Debug.Log("Target overlay setup failed");
            }
            try
            {
                if (markerSpot == null)
                {
                    markerSpot = new Texture2D(20, 20);
                    markerSpot.LoadImage(System.IO.File.ReadAllBytes(KSPUtil.ApplicationRootPath + "GameData/Analog Control Continued/PluginData/AnalogControl/spot-white.png"));
                }
                    setTransparency(markerSpot, config.transparency);
                    markerRect = new Rect(config.controlZone.center.x - 10, config.controlZone.center.y - 10, 20, 20);
            }
            catch
            {
                Debug.Log("Marker overlay setup failed");
            }
        }
        
        public void OnDestroy()
        {
            //MonoBehaviour.Destroy(markerSpot);
            //MonoBehaviour.Destroy(target);
            config.saveConfig();
        }

        /// <summary>
        /// Handle interfacing in update
        /// </summary>
        public void Update()
        {
            config.dragWindow();
            if (Input.GetKeyDown(config.lockKey.currentBind) && GameSettings.MODIFIER_KEY.GetKey())
                lockInput = !lockInput;
            if (lockInput)
                return;
            if (Input.GetKeyDown(config.activate.currentBind)) {
            	if (controlState == ActivationState.Inactive) {
            		controlState = ActivationState.Paused;
            		actuatorOverride.apply(FlightGlobals.ActiveVessel, config.aeroActuatorScale, config.steeringSpeedScale, isRollMode);
            	} else {
            		controlState = ActivationState.Inactive;
            		actuatorOverride.restore(FlightGlobals.ActiveVessel);
            	}
            }
            if (controlState == ActivationState.Inactive)
                return;

            if (Input.GetKeyDown(config.modeSwitch.currentBind)) {
            	actuatorOverride.restore(FlightGlobals.ActiveVessel);
                isRollMode = !isRollMode;
                actuatorOverride.apply(FlightGlobals.ActiveVessel, config.aeroActuatorScale, config.steeringSpeedScale, isRollMode);
            }

            if (Input.GetKeyDown(config.pauseKey.currentBind))
            {
                controlState = controlState == ActivationState.Paused ? ActivationState.Active : ActivationState.Paused;
                firstStart = false;
            }
            
            if (Input.GetKeyDown(KeyCode.Mouse1) && controlState == ActivationState.Active) {
            	controlState = ActivationState.TemporaryPaused;
            }
            
            if (Input.GetKeyUp(KeyCode.Mouse1) && controlState == ActivationState.TemporaryPaused) {
            	controlState = ActivationState.Active;
            }
        }        

        public void OnGUI()
        {
        	config.onGUI(GetInstanceID(), controlState);
            if (controlState != ActivationState.Inactive || config.showWindow)
            {
            	GUI.color = isRollMode ? Color.green : Color.yellow;
                GUI.DrawTexture(config.targetRect, target);
                GUI.color = controlState == ActivationState.Paused || controlState == ActivationState.TemporaryPaused ? Color.red : Color.green;
                GUI.DrawTexture(markerRect, markerSpot);              
            }
        }
        
        /// <summary>
        /// fixed update for the actual control
        /// </summary>
        public void FixedUpdate()
        {
            if (firstStart || controlState == ActivationState.Inactive)
                return;
            
            FlightGlobals.ActiveVessel.ctrlState = mouseControlVessel(FlightGlobals.ActiveVessel.ctrlState);
        }

        /// <summary>
        /// vessel control output according to mouse position
        /// </summary>
        /// <param name="state"></param>
        /// <returns></returns>
        private FlightCtrlState mouseControlVessel(FlightCtrlState state)
        {
            // (0,0) is bottom left of screen for mouse pos, top left for UI
            if (controlState == ActivationState.Active)
                markerRect.center = new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y);

            float vertDisplacement = 2 * (markerRect.center.y - config.controlZone.center.y) / config.controlZone.height;
            state.pitch = (config.isPitchInverted ? 1 : -1) * response(vertDisplacement, config.deadzone.y, state.pitchTrim);

            float hrztDisplacement = 2 * (markerRect.center.x - config.controlZone.center.x) / config.controlZone.width;
            if (isRollMode)
                state.roll = response(hrztDisplacement, config.deadzone.x, state.rollTrim);
            else {
                state.yaw = response(hrztDisplacement, config.deadzone.x, state.yawTrim);
                float wheelSteer = -response(hrztDisplacement, config.deadzone.x, state.wheelSteerTrim);
                if (Math.Abs(wheelSteer) < STEERING_ABSOLUTE_DEAD_ZONE) { //this eliminates steering wobble on very low steer values
                	wheelSteer = 0;
                }
                state.wheelSteer = wheelSteer;
            }

            return state;
        }

        private float response(float displacement, float deadzone, float trim)
        {
            if (Math.Abs(displacement) < deadzone) // deadzone
                return trim;
            
            if (displacement > 0) // +ve displacement
            	displacement = Mathf.Clamp((displacement - deadzone), 0, float.MaxValue) / (1 - deadzone);
            else // -ve displacement
            	displacement = Mathf.Clamp((displacement + deadzone), float.MinValue, 0) / (1 - deadzone);

            float response = displacement * displacement * Math.Sign(displacement); // displacement^2 gives nice fine control
            // trim compensation
            if (response > 0)
                response = trim + response * (1 - trim);
            else
                response = trim + response * (1 + trim);

            return Mathf.Clamp(response, -1, 1);
        }

        private void setTransparency(Texture2D tex, float transparency)
        {
            Color32[] pixels = tex.GetPixels32();
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i].a = (byte)((float)pixels[i].a * Mathf.Clamp01(transparency));
            }
            tex.SetPixels32(pixels);
            tex.Apply();
        }
    }
}
