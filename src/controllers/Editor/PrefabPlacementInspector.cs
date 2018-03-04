using UnityEngine;
using UnityEditor;

namespace BeatThat
{
	[CustomEditor(typeof(PrefabPlacement), true)]
	[CanEditMultipleObjects]
	public class PrefabPlacementInspector : UnityEditor.Editor 
	{

		override public void OnInspectorGUI()
		{
			base.OnInspectorGUI();

			PrefabPlacement placement = (target as PrefabPlacement);

			if (!placement.isPrefabSet) {
				EditorGUILayout.HelpBox ("No prefab is assigned.", MessageType.Warning);
			}

			if(placement.objectExists) {
				if(PrefabUtility.GetPrefabType(placement.managedGameObject) == PrefabType.PrefabInstance) {
					if(GUILayout.Button("Delete View")) {
						placement.Delete(true);
					}
				}
			}
			else {
				if(GUILayout.Button("Edit View")) {
					placement.EnsureCreated();
				}
			}
		}
	}
}
