using System;
using UnityEngine;

namespace AnalogControl
{
	/// <summary>
	/// Provides everything needed for Analog Control configuration
	/// </summary>
	public class Configuration
	{
		
		private static Rect DEFAULT_CONTROL_ZONE = new Rect(Screen.width / 6, Screen.height / 6, Screen.width * 2 / 3, Screen.height * 2 / 3); 
		
		// settings
        public bool isPitchInverted = true;
        public float transparency = 1; // 0 == transparent, 1 == opaque
        public Vector2 deadzone;
        public Rect controlZone;
        public float aeroActuatorScale;
        public float steeringSpeedScale;
		public CustomKeybind activate, modeSwitch, windowKey, lockKey, pauseKey;        
        
        // config
        private KSP.IO.PluginConfiguration config;
        
        public Rect targetRect = new Rect();
        
		// window props        
        public bool showWindow = false;
        private bool draggingBottomRight;
        private Vector2 dragPos;
        private ActivationState controlState;
        
        public class CustomKeybind
        {
            public KeyCode currentBind { get; set; }
            public bool set { get; set; }
            public CustomKeybind(KeyCode defKey)
            {
                currentBind = defKey;
                set = true;
            }
        }
		
        public static Configuration loadConfig()
        {
        	
        	Configuration instance = new Configuration();
        	
            instance.config = KSP.IO.PluginConfiguration.CreateForType<AnalogControl>();
            instance.config.load();

            instance.isPitchInverted = instance.config.GetValue<bool>("pitchInvert", true);
            instance.transparency = (float)instance.config.GetValue<double>("transparency", 1f);
            instance.deadzone = instance.config.GetValue<Vector2>("deadzone", new Vector2(0.05f * Screen.height / Screen.width, 0.05f));
            instance.controlZone = instance.config.GetValue<Rect>("controlZone", DEFAULT_CONTROL_ZONE);
            instance.aeroActuatorScale = (float)instance.config.GetValue<double>("aeroActuatorScale", 10f);
            instance.steeringSpeedScale = (float)instance.config.GetValue<double>("steeringSpeedScale", 10f);
            instance.activate = new CustomKeybind(instance.config.GetValue<KeyCode>("activate", KeyCode.Return));
            instance.modeSwitch = new CustomKeybind(instance.config.GetValue<KeyCode>("modeSwitch", KeyCode.Tab));
            instance.windowKey = new CustomKeybind(instance.config.GetValue<KeyCode>("windowKey", KeyCode.O));
            instance.lockKey = new CustomKeybind(instance.config.GetValue<KeyCode>("lockKey", KeyCode.L));
            instance.pauseKey = new CustomKeybind(instance.config.GetValue<KeyCode>("pauseKey", KeyCode.O));
            
            instance.targetRect.width = instance.controlZone.width * instance.deadzone.x * 1.5f;
        	instance.targetRect.height = instance.controlZone.height * instance.deadzone.y * 1.5f;
        	instance.targetRect.center = instance.controlZone.center;
            
            return instance;
        }
        
        public void saveConfig()
        {
            config["pitchInvert"] = isPitchInverted;
            config["transparency"] = (double)transparency;
            config["deadzone"] = deadzone;
            config["controlZone"] = controlZone;
            config["aeroActuatorScale"] = (double)aeroActuatorScale;
            config["steeringSpeedScale"] = (double)steeringSpeedScale;
            config["activate"] = activate.currentBind;
            config["modeSwitch"] = modeSwitch.currentBind;
            config["windowKey"] = windowKey.currentBind;
            config["lockKey"] = lockKey.currentBind;
            config["pauseKey"] = pauseKey.currentBind;
            config.save();
        }
        
        public void onGUI(int instanceId, ActivationState controlState) {
        	if (showWindow) {
        		this.controlState = controlState;
                controlZone = GUILayout.Window(instanceId, controlZone, locationWindow, "", GUI.skin.box);
        	}
        }
        
        public void dragWindow()
        {          
            if (showWindow)
            {
                if (draggingBottomRight)
                {
                    if (!Input.GetMouseButton(0))
                        draggingBottomRight = false;
                    else
                    {
                        Vector2 diff = (Vector2)Input.mousePosition - dragPos;
                        controlZone.width += diff.x;
                        controlZone.height -= diff.y;
                        dragPos = Input.mousePosition;
                    }
                }
                targetRect.center = controlZone.center;
                targetRect.width = controlZone.width * deadzone.x * 1.5f;
                targetRect.height = controlZone.height * deadzone.y * 1.5f;
            }
        }
		
		private void locationWindow(int id)
        {
            if (controlState == ActivationState.Inactive)
            {
            	
            	GUILayout.BeginVertical();
            	
            	GUILayout.BeginHorizontal();
            	GUILayout.FlexibleSpace();
            	showWindow = !GUILayout.Button("✖");
            	if (!showWindow) {
            		saveConfig();
            	}
            	GUILayout.EndHorizontal();
            	
            	//GUILayout.FlexibleSpace(); //This centers settings vertically
            	
            	GUILayout.BeginHorizontal();
            	GUILayout.FlexibleSpace(); //This centers settings horizontally
            	
            	GUILayout.BeginVertical();
            	
                if (GUILayout.Button("Window Key: " + GameSettings.MODIFIER_KEY.primary.ToString() + " + => " + (windowKey.set ? windowKey.currentBind.ToString() : "Not Assigned"), GUILayout.Width(300)))
                    windowKey.set = !windowKey.set;
                if (!windowKey.set)
                {
                    if (Input.GetMouseButton(0) || Event.current.keyCode == KeyCode.Escape)
                        windowKey.set = true;
                    else if (Event.current.type == EventType.KeyDown)
                    {
                        windowKey.currentBind = Event.current.keyCode;
                        windowKey.set = true;
                    }
                }
                
                if (GUILayout.Button("Activate Key => " + (activate.set ? activate.currentBind.ToString() : "Not Assigned"), GUILayout.Width(300)))
                    activate.set = !activate.set;
                if (!activate.set)
                {
                    if (Input.GetMouseButton(0) || Event.current.keyCode == KeyCode.Escape)
                        activate.set = true;
                    else if (Event.current.type == EventType.KeyDown)
                    {
                        activate.currentBind = Event.current.keyCode;
                        activate.set = true;
                    }
                }

                if (GUILayout.Button("Mode Switch Key => " + (modeSwitch.set ? modeSwitch.currentBind.ToString() : "Not Assigned"), GUILayout.Width(300)))
                    modeSwitch.set = !modeSwitch.set;
                if (!modeSwitch.set)
                {
                    if (Input.GetMouseButton(0) || Event.current.keyCode == KeyCode.Escape)
                        modeSwitch.set = true;
                    else if (Event.current.type == EventType.KeyDown)
                    {
                        modeSwitch.currentBind = Event.current.keyCode;
                        modeSwitch.set = true;
                    }
                }

                if (GUILayout.Button("Lock Key: " + GameSettings.MODIFIER_KEY.primary.ToString() + " + => " + (lockKey.set ? lockKey.currentBind.ToString() : "Not Assigned"), GUILayout.Width(300)))
                    lockKey.set = !lockKey.set;
                if (!lockKey.set)
                {
                    if (Input.GetMouseButton(0) || Event.current.keyCode == KeyCode.Escape)
                        lockKey.set = true;
                    else if (Event.current.type == EventType.KeyDown)
                    {
                        lockKey.currentBind = Event.current.keyCode;
                        lockKey.set = true;
                    }
                }
                if (GUILayout.Button("Pause Key => " + (pauseKey.set ? pauseKey.currentBind.ToString() : "Not Assigned"), GUILayout.Width(300)))
                    pauseKey.set = !pauseKey.set;
                if (!pauseKey.set)
                {
                    if (Event.current.keyCode == KeyCode.Escape)
                        pauseKey.set = true;
                    else if (Event.current.type == EventType.KeyDown)
                    {
                        pauseKey.currentBind = Event.current.keyCode;
                        pauseKey.set = true;
                    }
                }
                isPitchInverted = GUILayout.Toggle(isPitchInverted, "Invert Pitch Control");
                GUILayout.BeginHorizontal();
                GUILayout.Label("Deadzone width percentage", GUILayout.Width(200));
                deadzone.x = float.Parse(GUILayout.TextField((deadzone.x * 100).ToString("0.00"), GUILayout.Width(100))) / 100;
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                GUILayout.Label("Deadzone height percentage", GUILayout.Width(200));
                deadzone.y = float.Parse(GUILayout.TextField((deadzone.y * 100).ToString("0.00"), GUILayout.Width(100))) / 100;
                GUILayout.EndHorizontal();
                
                GUILayout.BeginHorizontal();
                GUILayout.Label("Aero control surfaces response", GUILayout.Width(200));
                aeroActuatorScale = GUILayout.HorizontalSlider(aeroActuatorScale, 1, 10f, GUILayout.Width(100));
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                GUILayout.Label("Wheel steering response", GUILayout.Width(200));
                steeringSpeedScale = GUILayout.HorizontalSlider(steeringSpeedScale, 1, 10f, GUILayout.Width(100));
                GUILayout.EndHorizontal();
                
                /*GUILayout.BeginHorizontal();
                GUILayout.Label("HUD transparency", GUILayout.Width(200));
                transparency = GUILayout.HorizontalSlider(transparency, 0, 1, GUILayout.Width(100));
                GUILayout.EndHorizontal();*/
             
                GUILayout.EndVertical();
                GUILayout.FlexibleSpace();
                
                GUILayout.EndHorizontal();
                GUILayout.FlexibleSpace();
                
                GUILayout.BeginHorizontal();
                GUILayout.Label("Drag and resize the window to change control zone");
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Reset")) {
                	controlZone = DEFAULT_CONTROL_ZONE;
                }
                if (GUILayout.Button("Center")) {
                	controlZone.center = new Vector2(Screen.width / 2, Screen.height / 2);
                }
                GUILayout.Space(10);
                if (GUILayout.RepeatButton("*"))
                {
                    draggingBottomRight = true;
                    dragPos = Input.mousePosition;
                }
                GUILayout.EndHorizontal();
                
                GUILayout.EndVertical();
                GUI.DragWindow();
            }
            else
            {
            	GUILayout.BeginVertical();
            	GUILayout.FlexibleSpace();
            	GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                GUILayout.Label("To access settings deactivate control by pressing [" + activate.currentBind.ToString() + "]");
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
                GUILayout.FlexibleSpace();
                GUILayout.EndVertical();
            }
            
        }
		
	}
}
