using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public class CopyTransitions
{
	static AnimatorStateTransition buffer;
	[MenuItem ("CONTEXT/AnimatorStateTransition/Copy Transition Values")]
	static void CopyFromSource ()
	{
		var transition = Selection.activeObject as AnimatorStateTransition;
		if (transition == null)
			return;

		buffer = new AnimatorStateTransition();

		buffer.conditions = transition.conditions;
		buffer.canTransitionToSelf = transition.canTransitionToSelf;
		buffer.hasExitTime = transition.hasExitTime;
		buffer.duration = transition.duration;
		buffer.hasFixedDuration = transition.hasFixedDuration;
		buffer.interruptionSource = transition.interruptionSource;
		buffer.orderedInterruption = transition.orderedInterruption;
		buffer.exitTime = transition.exitTime;
		buffer.offset = transition.offset;
	}

	[MenuItem("CONTEXT/AnimatorStateTransition/Paste Transition Values")]
	static void CopyToDestination()
	{
		var transition = Selection.activeObject as AnimatorStateTransition;

		if (transition == null)
			return;

		if (buffer == null)
			return;

		transition.conditions = buffer.conditions;
		transition.canTransitionToSelf = buffer.canTransitionToSelf;
		transition.hasExitTime = buffer.hasExitTime;
		transition.duration = buffer.duration;
		transition.hasFixedDuration = buffer.hasFixedDuration;
		transition.interruptionSource = buffer.interruptionSource;
		transition.orderedInterruption = buffer.orderedInterruption;
		transition.exitTime= buffer.exitTime;
		transition.offset= buffer.offset;
	}
}

