using System;

namespace AnalogControl
{
	/// <summary>
	/// Enum representing current state of control
	/// </summary>
	public enum ActivationState
    {
        Inactive,
        Active,
        Paused,
        TemporaryPaused
    }
}
