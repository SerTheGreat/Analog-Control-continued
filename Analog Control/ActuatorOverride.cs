using System;
using System.Collections.Generic;
using ModuleWheels;

namespace AnalogControl
{
	/// <summary>
	/// Description of ActuatorOverride.
	/// </summary>
	public class ActuatorOverride
	{
		
		private Dictionary<int, float> savedActuatorValues = new Dictionary<int, float>();
		
		public void apply(Vessel vessel, float aeroScale, float wheelScale, bool isRollMode) {
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
				List<ModuleWheelSteering> wheelModules = vessel.FindPartModulesImplementing<ModuleWheelSteering>();
				for (int i = 0; i < wheelModules.Count; i++) {
					if (savedActuatorValues.ContainsKey(wheelModules[i].GetInstanceID())) {
						continue;
					}
					savedActuatorValues.Add(wheelModules[i].GetInstanceID(), wheelModules[i].steeringResponse);
					wheelModules[i].steeringResponse *= wheelScale;
				}
			}
		}
		
		public void restore(Vessel vessel) {
			List<ModuleControlSurface> aeroModules = vessel.FindPartModulesImplementing<ModuleControlSurface>();
			for (int i = 0; i < aeroModules.Count; i++) {
				float savedValue;
				if (savedActuatorValues.TryGetValue(aeroModules[i].GetInstanceID(), out savedValue)) {
					aeroModules[i].actuatorSpeed = savedValue;
					aeroModules[i].actuatorSpeedNormScale = savedValue; //it's a shorcut but maybe it's wrong
					savedActuatorValues.Remove(aeroModules[i].GetInstanceID());
				}
			}
			
			List<ModuleWheelSteering> wheelModules = vessel.FindPartModulesImplementing<ModuleWheelSteering>();
			for (int i = 0; i < wheelModules.Count; i++) {
				float savedValue;
				if (savedActuatorValues.TryGetValue(wheelModules[i].GetInstanceID(), out savedValue)) {
					wheelModules[i].steeringResponse = savedValue;
					savedActuatorValues.Remove(wheelModules[i].GetInstanceID());
				}
			}
			
		}
		
		
	}
}
