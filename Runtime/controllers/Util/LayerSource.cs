using UnityEngine;
using System.Collections;

namespace BeatThat.App
{
	/// <summary>
	/// If a presenter should copy and apply a layer to itself and or it's view,
	/// which layer should that be.
	/// </summary>
	public enum LayerSource 
	{
		NONE, SELF, PARENT
	}
}
