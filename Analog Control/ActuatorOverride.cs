using System;
using System.Collections.Generic;
using ModuleWheels;
using System.Reflection;
using UnityEngine;

namespace AnalogControl
{
	/// <summary>
	/// Description of ActuatorOverride.
	/// </summary>
	public class ActuatorOverride
	{

		private static FieldInfo farField = null; //Reflection is used to get/set FAR field value to avoid coupling
		
		private Dictionary<int, float> savedActuatorValues = new Dictionary<int, float>();
		private Dictionary<int, SteeringProperties> savedSteeringValues = new Dictionary<int, SteeringProperties>();
		private double savedFARValue;
		
		public void apply(Vessel vessel, float aeroScale, float wheelScale, bool isRollMode) {
			
			if (farField == null) {
				farField = getFARField("timeConstant");
			}
			
			if (farField != null) {
				//List<FARControllableSurface> farModules = vessel.FindPartModulesImplementing<FARControllableSurface>();
				savedFARValue = (double)farField.GetValue(null); 
				farField.SetValue(null, savedFARValue / aeroScale);
			}
			
			List<ModuleControlSurface> aeroModules = vessel.FindPartModulesImplementing<ModuleControlSurface>();
			
			if (isRollMode) {
				for (int i = 0; i < aeroModules.Count; i++) {
					if (savedActuatorValues.ContainsKey(aeroModules[i].GetInstanceID())) {
						continue;
					}
					if (!aeroModules[i].ignorePitch || !aeroModules[i].ignoreRoll) {
						savedActuatorValues.Add(aeroModules[i].GetInstanceID(), aeroModules[i].actuatorSpeed);
						aeroModules[i].actuatorSpeed *= aeroScale;
						aeroModules[i].actuatorSpeedNormScale *= aeroScale;
					}
				}
				
				/*for (int i = 0; i < farModules.Count; i++) {
					if (savedActuatorValues.ContainsKey(farModules[i].GetInstanceID())) {
						continue;
					}
					KSPLog.print("---------------- FAr module ");
					if (Math.Abs(farModules[i].pitchaxis) > 0.001f  || Math.Abs(farModules[i].rollaxis) > 0.001f) {
						KSPLog.print("---------------- setting " + FARControllableSurface.timeConstant);
						savedActuatorValues.Add(farModules[i].GetInstanceID(), (float)FARControllableSurface.timeConstant);
						FARControllableSurface.timeConstant /= aeroScale;
						KSPLog.print("---------------- after " + FARControllableSurface.timeConstant);
					}
				}*/
			} else {
				for (int i = 0; i < aeroModules.Count; i++) {
					if (savedActuatorValues.ContainsKey(aeroModules[i].GetInstanceID())) {
						continue;
					}
					if (!aeroModules[i].ignorePitch || !aeroModules[i].ignoreYaw) {
						savedActuatorValues.Add(aeroModules[i].GetInstanceID(), aeroModules[i].actuatorSpeed);
						aeroModules[i].actuatorSpeed *= aeroScale;
						aeroModules[i].actuatorSpeedNormScale *= aeroScale;
					}
				}
				
				/*for (int i = 0; i < farModules.Count; i++) {
					if (savedActuatorValues.ContainsKey(farModules[i].GetInstanceID())) {
						continue;
					}
					if (Math.Abs(farModules[i].pitchaxis) > 0.001f  || Math.Abs(farModules[i].yawaxis) > 0.001f) {
						savedActuatorValues.Add(farModules[i].GetInstanceID(), (float)FARControllableSurface.timeConstant);
						FARControllableSurface.timeConstant /= aeroScale;
					}
				}*/
				List<ModuleWheelSteering> wheelModules = vessel.FindPartModulesImplementing<ModuleWheelSteering>();
				for (int i = 0; i < wheelModules.Count; i++) {
					if (savedSteeringValues.ContainsKey(wheelModules[i].GetInstanceID())) {
						continue;
					}
					
					savedSteeringValues.Add(wheelModules[i].GetInstanceID(), new SteeringProperties(wheelModules[i].steeringResponse, wheelModules[i].steeringCurve.Curve));
					wheelModules[i].steeringResponse *= wheelScale;
					Keyframe[] keys = wheelModules[i].steeringCurve.Curve.keys; 
					if (keys.Length > 0) {
						wheelModules[i].steeringCurve.Curve = AnimationCurve.Linear(keys[0].time, keys[0].value, keys[keys.Length - 1].time, keys[keys.Length - 1].value);
					}
				}
			}
		}
		
		public void restore(Vessel vessel) {
			List<ModuleControlSurface> aeroModules = vessel.FindPartModulesImplementing<ModuleControlSurface>();
			//List<FARControllableSurface> farModules = vessel.FindPartModulesImplementing<FARControllableSurface>();
			for (int i = 0; i < aeroModules.Count; i++) {
				float savedValue;
				if (savedActuatorValues.TryGetValue(aeroModules[i].GetInstanceID(), out savedValue)) {
					aeroModules[i].actuatorSpeed = savedValue;
					aeroModules[i].actuatorSpeedNormScale = savedValue; //it's a shorcut but maybe it's wrong
					savedActuatorValues.Remove(aeroModules[i].GetInstanceID());
				}
			}
			
			if (farField != null) {
				farField.SetValue(null, savedFARValue);
				/*for (int i = 0; i < farModules.Count; i++) {
					float savedValue;
					if (savedActuatorValues.TryGetValue(farModules[i].GetInstanceID(), out savedValue)) {
						FARControllableSurface.timeConstant = savedValue;
						KSPLog.print("---------------- REstored " + FARControllableSurface.timeConstant);
						savedActuatorValues.Remove(farModules[i].GetInstanceID());
					}
				}*/
			}
			
			List<ModuleWheelSteering> wheelModules = vessel.FindPartModulesImplementing<ModuleWheelSteering>();
			for (int i = 0; i < wheelModules.Count; i++) {
				SteeringProperties savedValues;
				if (savedSteeringValues.TryGetValue(wheelModules[i].GetInstanceID(), out savedValues)) {
					wheelModules[i].steeringResponse = savedValues.steeringResponse;
					wheelModules[i].steeringCurve.Curve = savedValues.steeringCurve;
					savedSteeringValues.Remove(wheelModules[i].GetInstanceID());
				}
			}
			
		}
		
		private FieldInfo getFARField(string name) {
			Type type = AssemblyLoader.GetClassByName(typeof(PartModule), "FARControllableSurface");
			if (type == null) {
				return null;
			}
			return type.GetField(name, BindingFlags.Public | BindingFlags.Static);
		}
		
	}
}
