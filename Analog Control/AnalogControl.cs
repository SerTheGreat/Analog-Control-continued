using System;
using UnityEngine;

using System.Collections.Generic;
using ModuleWheels;

namespace AnalogControl
{
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class AnalogControl : MonoBehaviour
    {
    	
    	const float STEERING_OUTPUT_GAIN = 0.001f; //This must be added to output because below this value stock steering goes mad acquiring NaN angles several times per second 
    	const string PERSISTENCE_NODE_NAME = "AnalogControl";

    	Configuration config;
        
        bool firstStart = false;
        static bool valuesLoaded; //is set when state values were acquired on game load
        
        // state
        static Rect markerRect;
        static ActivationState controlState = ActivationState.Inactive;
        static bool isRollMode = true;
        
        // display
        static Texture2D target;
        static Texture2D markerSpot;
		static Texture2D thr;        

        bool lockInput = false;
        
        ActuatorOverride actuatorOverride = new ActuatorOverride();
        MouseWheelOverride mouseWheelOverride;
        
        /// <summary>
        /// Initialise control region size and other user specific params
        /// </summary>
        public void Start()
        {
       	
            config = Configuration.loadConfig();
            
            mouseWheelOverride = MouseWheelOverride.instance(config);

            try
            {
                if (target == null)
                {
                    target = new Texture2D(500, 500);
                    target.LoadImage(System.IO.File.ReadAllBytes(KSPUtil.ApplicationRootPath + "GameData/Analog Control Continued/PluginData/AnalogControl/crosshair-white.png"));
                }
                //setTransparency(target, config.transparency);
                
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
                //setTransparency(markerSpot, config.transparency);
            }
            catch
            {
                Debug.Log("Marker overlay setup failed");
            }
            try
            {
                if (thr == null)
                {
                    thr = new Texture2D(20, 9);
                    thr.LoadImage(System.IO.File.ReadAllBytes(KSPUtil.ApplicationRootPath + "GameData/Analog Control Continued/PluginData/AnalogControl/thr.png"));
                }
                //setTransparency(thr, config.transparency);
            }
            catch
            {
                Debug.Log("THR overlay setup failed");
            }
            
	        if (!valuesLoaded) {
            	controlState = ActivationState.Inactive;
            	isRollMode = true;
            	markerRect = new Rect(config.controlZone.center.x - 10, config.controlZone.center.y - 10, 20, 20);
            } else {
            	if (controlState == ActivationState.Paused) {
					actuatorOverride.apply(FlightGlobals.ActiveVessel, config.aeroActuatorScale, config.steeringSpeedScale, isRollMode);            		
            	}
            	valuesLoaded = false;
            }
            
            GameEvents.onGameStateSave.Add(onSave);
            GameEvents.onGameStatePostLoad.Add(onLoad);         
        }
        
        void onSave(ConfigNode savedNode) {
        	ConfigNode node = new ConfigNode(PERSISTENCE_NODE_NAME);
        	node.AddValue("controlState", controlState);
        	node.AddValue("rollMode", isRollMode);
        	node.AddValue("controlPosition", markerRect.center);
        	savedNode.AddNode(node);
        }
        
        void onLoad(ConfigNode loadedNode) {
        	applyLoadedValues(loadedNode.GetNode("GAME").GetNode(PERSISTENCE_NODE_NAME).CreateCopy());
        }
        
        private void applyLoadedValues(ConfigNode node) {
        	ActivationState savedControlState = (ActivationState)ConfigNode.ParseEnum(typeof(ActivationState), node.GetValue("controlState"));
        	Vector2 controlPosition = ConfigNode.ParseVector2(node.GetValue("controlPosition"));
        	isRollMode = bool.Parse(node.GetValue("rollMode"));
        	switch (savedControlState) {
        		case ActivationState.Active:
        		case ActivationState.Paused:
        		case ActivationState.TemporaryPaused:
        			controlState = ActivationState.Paused;
        			markerRect.center = controlPosition;
        		break;
        		default:
        			controlState = ActivationState.Inactive;
        		break;	
        	}
        	valuesLoaded = true;
        }
        
        public void OnDestroy()
        {
            //MonoBehaviour.Destroy(markerSpot);
            //MonoBehaviour.Destroy(target);
            actuatorOverride.restore(FlightGlobals.ActiveVessel);
            mouseWheelOverride.restore();
            config.saveConfig();
            GameEvents.onGameStateSave.Remove(onSave);
            GameEvents.onGameStatePostLoad.Remove(onLoad);
        }

        /// <summary>
        /// Handle interfacing in update
        /// </summary>
        public void Update()
        {
            config.dragWindow();
            if (GameSettings.MODIFIER_KEY.GetKey() && Input.GetKeyDown(config.windowKey.currentBind)) {
            	if (config.showWindow) {
            		config.saveConfig();
            	}
                config.showWindow = !config.showWindow;
            }
            if (Input.GetKeyDown(config.lockKey.currentBind) && GameSettings.MODIFIER_KEY.GetKey())
                lockInput = !lockInput;
            if (lockInput)
                return;
            if (Input.GetKeyDown(config.activate.currentBind)) {
            	if (controlState == ActivationState.Inactive) {
            		controlState = ActivationState.Paused;
            		actuatorOverride.apply(FlightGlobals.ActiveVessel, config.aeroActuatorScale, config.steeringSpeedScale, isRollMode);
            		mouseWheelOverride.pause();
            	} else {
            		controlState = ActivationState.Inactive;
            		actuatorOverride.restore(FlightGlobals.ActiveVessel);
            		mouseWheelOverride.restore();
            	}
            }
            
            if (GameSettings.MODIFIER_KEY.GetKey() && (Input.GetKeyDown(config.parkingBrakeKey.currentBind) || Input.GetKeyUp(config.parkingBrakeKey.currentBind))) {
            	FlightGlobals.ActiveVessel.ActionGroups.SetGroup(KSPActionGroup.Brakes, true);
            	/*List<ModuleWheelBrakes> modules = FlightGlobals.ActiveVessel.FindPartModulesImplementing<ModuleWheelBrakes>();
            	for (int i = 0; i < modules.Count; i++) {
            		modules[i].brakeInput = 1f;
            		
            	}*/
            }
            
            if (controlState == ActivationState.Inactive)
                return;
            
            if (controlState == ActivationState.Paused || controlState == ActivationState.TemporaryPaused) {
            	mouseWheelOverride.pause();
            } else {
            	mouseWheelOverride.unpause();
            	if (!config.mwThrottleActivate) {
            		mouseWheelOverride.activate();
            	}
            }

            if (Input.GetKeyDown(config.modeSwitch.currentBind)) {
            	actuatorOverride.restore(FlightGlobals.ActiveVessel);
                isRollMode = !isRollMode;
                actuatorOverride.apply(FlightGlobals.ActiveVessel, config.aeroActuatorScale, config.steeringSpeedScale, isRollMode);
            }
          
            if (config.mwThrottlePress) {
            	if (Input.GetKeyDown(config.mwThrottleKey.currentBind)) {
	            	mouseWheelOverride.toggle();
            	}
            } else {
            	if (config.mwThrottleActivate ^ Input.GetKey(config.mwThrottleKey.currentBind)) {
            		mouseWheelOverride.restore();
            	} else {
            		mouseWheelOverride.activate();
            	}
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
            	Color color = isRollMode ? Color.green : Color.yellow;
				color.a = config.transparency;
            	GUI.color = color; 
                GUI.DrawTexture(config.targetRect, target);
                color = controlState == ActivationState.Paused || controlState == ActivationState.TemporaryPaused ? Color.red : Color.green;
                color.a = config.transparency;
                GUI.color = color;
                GUI.DrawTexture(markerRect, markerSpot);
                Color thrColor = new Color(color.r, color.g, color.b, color.a / 2);
                GUI.color = thrColor;
                if (mouseWheelOverride.active) {
                	GUI.DrawTexture(new Rect(config.targetRect.center.x - 10, config.targetRect.yMax - 9, 20, 9), thr);
                }
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
                float wheelSteer = -response(hrztDisplacement, config.deadzone.x, state.wheelSteerTrim, STEERING_OUTPUT_GAIN);
                state.wheelSteer = wheelSteer;                
            }

            return state;
        }

        private float response(float displacement, float deadzone, float trim, float outputGain = 0f)
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
                response = outputGain + trim + response * (1 - trim);
            else
                response = -outputGain + trim + response * (1 + trim);

            return Mathf.Clamp(response, -1, 1);
        }

        /*private void setTransparency(Texture2D tex, float transparency)
        {
            Color32[] pixels = tex.GetPixels32();
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i].a = (byte)((float)pixels[i].a * Mathf.Clamp01(transparency));
            }
            tex.SetPixels32(pixels);
            tex.Apply();
        }*/
    }
}
