using System;
using UnityEngine;

namespace AnalogControl
{
	/// <summary>
	/// Contains original values of properties modified in a ModuleWheelSteering instance
	/// </summary>
	public class SteeringProperties
	{

		public float steeringResponse;		
		public AnimationCurve steeringCurve;
		
		public SteeringProperties(float steeringResponse, AnimationCurve steeringCurve)
		{
			this.steeringResponse = steeringResponse;
			this.steeringCurve = steeringCurve;
		}
		
	}
}
