using UnityEngine;
using UnityEditor;

namespace BeatThat
{
	[CustomEditor(typeof(ViewPlacement))]
	public class ViewPlacementInspector : UnityEditor.Editor 
	{
		[MenuItem("Tools/MVP/Repair Broken ViewPlacement")]
		public static void ButtonToMultiGraphic()
		{
			var obj = Selection.activeGameObject;
			if(obj == null) {
				Debug.LogWarning("No object selected");
				return;
			}
			
			var vp = obj.GetComponent<ViewPlacement>();
			if(vp == null) {
				Debug.LogWarning("Selected object has no ViewPlacement");
				return;
			}

			var vpSerialized = new SerializedObject(vp);
			var prefab = vpSerialized.FindProperty("m_prefab");

			Debug.Log ("prefab=" + prefab);
		}

		private const string MI_EDIT_VIEW = "GameObject/UI/Ape - Edit View";
		[MenuItem(MI_EDIT_VIEW, true)]
		public static bool MenuItem_EditView_Validation(MenuCommand cmd)
		{
			var go = cmd.context as GameObject;
			return go != null && go.GetComponent<ViewPlacement>() != null;
		}

		[MenuItem(MI_EDIT_VIEW)]
		public static void MenuItem_EditView(MenuCommand cmd)
		{
			var vp = (cmd.context as GameObject).GetComponent<ViewPlacement> ();
			if (vp == null) {
				return;
			}

			vp.EnsureCreated ();
		}

		override public void OnInspectorGUI()
		{
			base.OnInspectorGUI();

			ViewPlacement v = (target as ViewPlacement);

			if (!v.isPrefabSet) {
				EditorGUILayout.HelpBox ("No prefab is assigned.", MessageType.Warning);
			}

			if(v.objectExists) {
				if(PrefabUtility.GetPrefabType(v.managedObject.gameObject) == PrefabType.PrefabInstance) {
					if(GUILayout.Button("Delete View")) {
						v.Delete(true);
					}
				}
				else {
					if(GUILayout.Button("Save View")) {
						v.m_prefab = PrefabUtility.ReplacePrefab(v.managedObject.gameObject, v.m_prefab.gameObject,
						                                         ReplacePrefabOptions.ConnectToPrefab).GetComponent<View>();

						v.Delete();
					}
				}
			}
			else {
				if(GUILayout.Button("Edit View")) {
					v.EnsureCreated();
				}
			}
		}
	}
}
