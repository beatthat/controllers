using BeatThat.OptionalComponents;
using UnityEditor;
using UnityEngine;
namespace BeatThat.Controllers
{
    /// <summary>
    /// Extend this class for concrete View inspectors to enable autowire feature in inspector
    /// 
    /// </summary>
    [CustomEditor(typeof(View), true)]
	public class ViewEditor : UnityEditor.Editor
	{
		
		public override void OnInspectorGUI()
		{
			base.OnInspectorGUI();
			EditorGUILayout.BeginHorizontal();
			if(GUILayout.Button(new GUIContent("AutoWire", "Find UI components and assign to view"))) {
				ControllerEditorUtils.AutoWireView(target.GetType(), (target as Component));
			}
			EditorGUILayout.EndHorizontal();

			(this.target as View).OnGUIProposeAddOptionalComponents ();
		}
		
	}

}

