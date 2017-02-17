using System;
using System.Reflection;

namespace AnalogControl
{
	/// <summary>
	/// Description of MouseWheelOverride.
	/// </summary>
	public class MouseWheelOverride
	{
		
		static AxisBinding_Single savedThrottleBinding;
		static AxisBinding savedMouseWheelBinding;
		
		public bool active = false;
		
		private bool paused = false;
		private Configuration conf;
		
		private MouseWheelOverride()
		{
		}
		
		public static MouseWheelOverride instance(Configuration conf) {
			MouseWheelOverride instance = new MouseWheelOverride();
			instance.conf = conf;
			savedThrottleBinding = GameSettings.AXIS_THROTTLE_INC.secondary;
			savedMouseWheelBinding = GameSettings.AXIS_MOUSEWHEEL;
			return instance;
		}
		
		public void activate() {
			if (!active && !paused) {
				activateAxis();
			}
			active = true;
		}
		
		public void restore() {
			if (active) {
				restoreAxis();
			}
			active = false;			
		}
		
		public void toggle() {
			if (active) {
				restore();
			} else {
				activate();
			}
		}
		
		public void pause() {
			if (!paused) {
				paused = true;
				restoreAxis();
			}
		}
		
		public void unpause() {
			 if (paused) {
				paused=false;
				if (active) {
					activateAxis();
				}
			}
		}
		
		private void activateAxis() {
			GameSettings.AXIS_THROTTLE_INC.secondary = cloneAxis(savedMouseWheelBinding.primary);
			GameSettings.AXIS_THROTTLE_INC.secondary.sensitivity = conf.mouseThrottleSensitivity;
			GameSettings.AXIS_MOUSEWHEEL = new AxisBinding();
		}
		
		private void restoreAxis() {
			GameSettings.AXIS_THROTTLE_INC.secondary = savedThrottleBinding;
			GameSettings.AXIS_MOUSEWHEEL = savedMouseWheelBinding;
		}
		
		//Clones an axis using reflection to get public attribute values
		private AxisBinding_Single cloneAxis(AxisBinding_Single source) {
			AxisBinding_Single dest = new AxisBinding_Single();
			FieldInfo[] fields = (typeof (AxisBinding_Single)).GetFields(BindingFlags.Public | BindingFlags.Instance);
			for (int i = 0; i < fields.Length; i++) {
				fields[i].SetValue(dest, fields[i].GetValue(source));
			}
			return dest;
		}
		
	}
}
