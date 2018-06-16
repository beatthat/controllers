using BeatThat.Bindings;
using BeatThat.CollectionsExt;
using BeatThat.GetComponentsExt;
using BeatThat.OptionalComponents;
using BeatThat.Pools;
using UnityEditor;
using UnityEngine;

namespace BeatThat.Controllers
{
    [CustomEditor(typeof(Controller), true)]
	public class ControllerEditor : UnityEditor.Editor 
	{
		private bool m_showController;
		private bool m_showTransitionOptions;
		private bool m_showAttachedBindings;

		override public void OnInspectorGUI()
		{
			var bkgColorSave = GUI.backgroundColor;

			base.OnInspectorGUI();
			AddControllerFoldout(this, ref m_showController, ref m_showAttachedBindings);

			var hasPanelTransitions = TypeUtils.Find("BeatThat.HasPanelTransitions");
			if(hasPanelTransitions != null && hasPanelTransitions.IsInstanceOfType (this.target)) {
				AddTransitionOptionsFoldout(this, ref m_showTransitionOptions);
			}

			var c = this.target as Controller;
			if (c is ViewController && c.GetComponent<IView> () == null
				&& c.GetComponent<IViewPlacement> () == null
				&& (c.transform.childCount != 1 || c.transform.GetChild(0).GetComponent<IView>() == null)) {

				EditorGUILayout.HelpBox ("Missing required View or ViewPlacement component", MessageType.Warning);
				GUI.backgroundColor = Color.yellow;
				if (GUILayout.Button ("Add a ViewPlacement")) {
					c.AddIfMissing<ViewPlacement> ();
				}
				if (GUILayout.Button ("Generate a View")) {
					EditorUtility.DisplayDialog ("Generate a View", "This would be a great feature to have!\nSomeone build it", "OK");
				}
				GUI.backgroundColor = bkgColorSave;
			}

			c.OnGUIProposeAddOptionalComponents ();

			this.serializedObject.ApplyModifiedProperties();
		}

		public static void AddControllerFoldout(UnityEditor.Editor editor, ref bool showControllerFoldout, ref bool showAttachedBindingsFoldout)
		{
			showControllerFoldout = EditorGUILayout.Foldout(showControllerFoldout, "Controller Properties");
			if(showControllerFoldout) {
				EditorGUI.indentLevel++;

				if(Application.isPlaying) {
					EditorGUILayout.LabelField("Is Bound: " + (editor.target as IController).isBound);
				}
					
				PresentBindSubcontrollersOption(editor);
				PresentAutoResetBindGoOption(editor);
				PresentEnsureUnbindOnDisableOption(editor);
				PresentForceLayerOption(editor);
				PresentHidesViewOnlyOption(editor);

				if((editor.target as Controller).isBound) {
					AddAttachedBindingsFoldout(editor, ref showAttachedBindingsFoldout);
				}

				EditorGUI.indentLevel--;
			}
		}

		public static void AddTransitionOptionsFoldout(UnityEditor.Editor editor, ref bool showFoldout)
		{
			showFoldout = EditorGUILayout.Foldout(showFoldout, "Transition Options");
			if(showFoldout) {
				EditorGUI.indentLevel++;
			
				PresentPanelProperties(editor);

				EditorGUI.indentLevel--;
			}
		}

		public static void AddAttachedBindingsFoldout(UnityEditor.Editor editor, ref bool showFoldout)
		{
			showFoldout = EditorGUILayout.Foldout(showFoldout, "Attached Bindings");
			if(showFoldout) {
				EditorGUI.indentLevel++;

				PresentAttachedBindings(editor);

				EditorGUI.indentLevel--;
			}
		}

		public static void PresentAttachedBindings(UnityEditor.Editor editor)
		{
			var ctl = editor.target as Controller;
			if(!ctl.isBound) {
				return;
			}

			using(var attached = ListPool<Binding>.Get()) {
				ctl.GetAttachedBindings(attached);

				foreach(var ab in attached) {
					EditorGUILayout.LabelField(ab.ToString());
				}
			}
		}

		public static void PresentPanelProperties(UnityEditor.Editor editor)
		{

			var iPanelController = TypeUtils.Find("BeatThat.IPanelController");
			if(iPanelController == null) {
				return;
			}

			if(!(iPanelController.IsInstanceOfType(editor.target))) {
				return;
			}

			EditorGUILayout.PropertyField(editor.serializedObject.FindProperty("m_setActiveOnTransitionIn"));
			EditorGUILayout.PropertyField(editor.serializedObject.FindProperty("m_setInactiveOnTransitionOut"));
			EditorGUILayout.PropertyField(editor.serializedObject.FindProperty("m_destroyOnTransitionOut"));
			EditorGUILayout.PropertyField(editor.serializedObject.FindProperty("m_ensureOutBeforeTransitionIn"));
			EditorGUILayout.PropertyField(editor.serializedObject.FindProperty("m_ensureResetBindGoOnTransitionIn"));
			EditorGUILayout.PropertyField(editor.serializedObject.FindProperty("m_ensureTransitionOutOnUnbind"));
			EditorGUILayout.PropertyField(editor.serializedObject.FindProperty("m_debugTransitions"));
			EditorGUILayout.PropertyField(editor.serializedObject.FindProperty("m_panelState"));

		}

		public static void PresentBindSubcontrollersOption(UnityEditor.Editor editor)
		{
			if(editor.target as ISubcontroller != null) { 
				// if a controller is marked subcontroller, it never binds other subcontrollers
				return;
			}

			var prop = editor.serializedObject.FindProperty("m_bindSubcontrollers");

			EditorGUILayout.PropertyField(prop, 
				new GUIContent("Bind Subcontrollers", 
					"If TRUE, then when this controller binds, binds all ISubcontrollers as well (usually property bindings)."));
		}

		public static void PresentEnsureUnbindOnDisableOption(UnityEditor.Editor editor)
		{

			var prop = editor.serializedObject.FindProperty("m_ensureUnbindOnDisable");

			EditorGUILayout.PropertyField(prop, 
				new GUIContent("Ensure Unbind On Disable", "Generally, you want any disabled controller to unbind. Set FALSE to disable this behaviour"));
		}

		public static void PresentAutoResetBindGoOption(UnityEditor.Editor editor)
		{
			var hasModel = editor.target as HasModel;
			if(hasModel != null && hasModel.GetModelType() != typeof(NoModel)) { 
				return;
			}

			var prop = editor.serializedObject.FindProperty("m_autoResetBindGoEvent");

			EditorGUILayout.PropertyField(prop, 
				new GUIContent("Ensure Bind On Event", "Ensure ResetBindGo is called on either Start or Enable"));
		}

		public static void PresentForceLayerOption(UnityEditor.Editor editor)
		{
			var hasView = editor.target as ViewController;
			if(hasView == null) { 
				return;
			}

			if(hasView.GetViewType().IsClass && !(typeof(Component).IsAssignableFrom(hasView.GetViewType()))) {
				// We only want to show this option if the view is a unity Component living on a child object of the presenter.
				// If the viewtype is a concrete class and *not* a subclass of Component, then assume view is some inner class of the presenter
				return;
			}

			var view = (editor.target as Component).GetComponent(hasView.GetViewType());
			if(view != null) { // view and presenter are on the same game object
				return;
			}

			var prop = editor.serializedObject.FindProperty("m_forceLayerTo");

			EditorGUILayout.PropertyField(prop, 
				new GUIContent("Force Layer To", "Override the layer of the (child) view component."));
		}

		public static void PresentHidesViewOnlyOption(UnityEditor.Editor editor)
		{
			var hasView = editor.target as HasView;
			if(hasView == null) { 
				return;
			}

			if(hasView.GetViewType().IsClass && !(typeof(Component).IsAssignableFrom(hasView.GetViewType()))) {
				// We only want to show this option if the view is a unity Component living on a child object of the presenter.
				// If the viewtype is a concrete class and *not* a subclass of Component, then assume view is some inner class of the presenter
				return;
			}

			var view = (editor.target as Component).GetComponent(hasView.GetViewType());
			if(view != null) { // view and presenter are on the same game object
				return;
			}

			var prop = editor.serializedObject.FindProperty("m_hidesViewOnly");

			EditorGUILayout.PropertyField(prop, 
				new GUIContent("Hides View Only", "Calls to Hide will disable the view's GameObject, rather than the presenter's."));
		}
	}
}





