#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;


namespace BeatThat.Controllers
{
    public static class EditorUtils 
	{


		static public Texture2D blankTexture
		{
			get
			{
				return EditorGUIUtility.whiteTexture;
			}
		}
		
		static public void DrawSeparator ()
		{
			GUILayout.Space(12f);
			
			if (Event.current.type == EventType.Repaint)
			{
				Texture2D tex = blankTexture;
				Rect rect = GUILayoutUtility.GetLastRect();
				GUI.color = new Color(0f, 0f, 0f, 0.25f);
				GUI.DrawTexture(new Rect(0f, rect.yMin + 6f, Screen.width, 4f), tex);
				GUI.DrawTexture(new Rect(0f, rect.yMin + 6f, Screen.width, 1f), tex);
				GUI.DrawTexture(new Rect(0f, rect.yMin + 9f, Screen.width, 1f), tex);
				GUI.color = Color.white;
			}
		}
		
		static public bool DrawHeader (string text) { return DrawHeader(text, text, false); }
		
		/// <summary>
		/// Draw a distinctly different looking header label
		/// </summary>
		
		static public bool DrawHeader (string text, string key) { return DrawHeader(text, key, false); }
		
		/// <summary>
		/// Draw a distinctly different looking header label
		/// </summary>
		
		static public bool DrawHeader (string text, bool forceOn) { return DrawHeader(text, text, forceOn); }
		
		/// <summary>
		/// Draw a distinctly different looking header label
		/// </summary>
		
		static public bool DrawHeader (string text, string key, bool forceOn)
		{
			bool state = EditorPrefs.GetBool(key, true);
			
			GUILayout.Space(3f);
			if (!forceOn && !state) GUI.backgroundColor = new Color(0.8f, 0.8f, 0.8f);
			GUILayout.BeginHorizontal();
			GUILayout.Space(3f);
			
			GUI.changed = false;
			#if UNITY_3_5
			if (state) text = "\u25B2 " + text;
			else text = "\u25BC " + text;
			if (!GUILayout.Toggle(true, text, "dragtab", GUILayout.MinWidth(20f))) state = !state;
			#else
			text = "<b><size=11>" + text + "</size></b>";
			if (state) text = "\u25B2 " + text;
			else text = "\u25BC " + text;
			if (!GUILayout.Toggle(true, text, "dragtab", GUILayout.MinWidth(20f))) state = !state;
			#endif
			if (GUI.changed) EditorPrefs.SetBool(key, state);
			
			GUILayout.Space(2f);
			GUILayout.EndHorizontal();
			GUI.backgroundColor = Color.white;
			if (!forceOn && !state) GUILayout.Space(3f);
			return state;
		}





		/// <summary>
		/// Searches assemblies in project for all static methods with a given attribute.
		/// Caches results for performance.
		/// </summary>
		public static MethodInfo[] FindStaticMethodsWithAttribute<T>(bool ignoreCache = false) where T : class
		{
			System.Type attrType = typeof(T);
			MethodInfo[] methods;
			if(ignoreCache || !m_staticMethodsByAttribute.TryGetValue(attrType, out methods)) {
				var methodList = new List<MethodInfo>();
				foreach(Assembly a in AppDomain.CurrentDomain.GetAssemblies()) {
					foreach(Type t in a.GetTypes()) {
						foreach(MethodInfo m in t.GetMethods(BindingFlags.Static | BindingFlags.Public)) {
							
							foreach(var attr in m.GetCustomAttributes(false)) {
								if(attr is T) {
									
									Debug.Log ("[" + Time.time + "] EditorUtils::FindStaticMethodsWithAttribute '" + attrType.Name + "' found "
									           + m.DeclaringType.Name + "::" + m.Name);
									
									methodList.Add(m);
								}
							}
						}
					}
				}

				methods = methodList.ToArray();

				m_staticMethodsByAttribute[attrType] = methods;
			}

			return methods;
		}

		/// <summary>
		/// Searches assemblies in project for all static fields with a given attribute whose type is assignable from the given type
		/// Caches results for performance.
		/// </summary>
		public static FieldInfo[] FindStaticFieldsWithAttrAndValType<AttrType,ValType>(bool ignoreCache = false)
		{
			System.Type attrType = typeof(AttrType);
			System.Type valType = typeof(ValType);
			AttrAndType key = new AttrAndType(attrType, valType);

			FieldInfo[] fields;
			if(ignoreCache || !m_staticFieldsByAttrAndType.TryGetValue(key, out fields)) {

				var fieldList = new List<FieldInfo>();
				foreach(Assembly a in AppDomain.CurrentDomain.GetAssemblies()) {
					foreach(Type t in a.GetTypes()) {
						foreach(FieldInfo f in t.GetFields()) {
							if(valType.IsAssignableFrom(f.FieldType)) {
								foreach(var attr in f.GetCustomAttributes(false)) {
									if(attr is AttrType) {
										Debug.Log ("[" + Time.time + "] EditorUtils::FindStaticFieldsWithAttrAndValType '" + attrType.Name + "' found "
										           + f.DeclaringType.Name + "::" + f.Name);

										fieldList.Add(f);
									}
								}
							}
						}
					}
				}

				fields = fieldList.ToArray();
				m_staticFieldsByAttrAndType[key] = fields;
			}

			return fields;
		}

		/// <summary>
		/// Searches assemblies in project for all static fields with a given attribute whose type is assignable from the given type
		/// Caches results for performance.
		/// </summary>
		public static ValType[] FindStaticValsWithAttrAndValType<AttrType,ValType>(bool ignoreCache = false)
		{
			FieldInfo[] fields = FindStaticFieldsWithAttrAndValType<AttrType, ValType>(ignoreCache);

			ValType[] vals = new ValType[fields.Length];
			for(int i = 0; i < fields.Length; i++) {
				vals[i] = (ValType)fields[i].GetRawConstantValue();
			}
			
			return vals;
		}

		public static string ToCamelCase(string s)
		{
			if(string.IsNullOrEmpty(s)) {
				return s;
			}
			
			return Char.ToLowerInvariant(s[0]) + s.Substring(1);
		}
		
		public static string UpperFirstChar(string s)
		{
			if(string.IsNullOrEmpty(s)) {
				return s;
			}
			
			return Char.ToUpperInvariant(s[0]) + s.Substring(1);
		}
		
		public static string RemoveSpaces(string s)
		{
			if(string.IsNullOrEmpty(s)) {
				return s;
			}
			
			return s.Replace(" ", "");
		}
		
		/// <summary>
		/// Checks the existance of a list of type names and returns true if all exist.
		/// Useful if you've generated a class and are waiting to see if unity compiled those classes.
		/// </summary>
		public static bool AllTypesExist(params string[] typeNames)
		{
			foreach(string name in typeNames) {
				if(Type.GetType(name, false) == null) {
					Debug.Log ("[" + Time.realtimeSinceStartup + "] ClassesExist " + name + " does NOT exist");
					return false;
				}
			}
			
			return true; // all exist
		}

		class AttrAndType
		{
			public AttrAndType(System.Type attrType, System.Type valType)
			{
				this.attrType = attrType;
				this.valType = valType;
			}

			public System.Type attrType
			{
				get; private set;
			}

			public System.Type valType
			{
				get; private set;
			}

			override public bool Equals(object o)
			{
				if(o == this) {
					return true;
				}
				else if(o == null) {
					return false;
				}
				else {
					AttrAndType thatObj = o as AttrAndType;
					if(thatObj == null) {
						return false;
					}
					else {
						return this.attrType == thatObj.attrType && this.valType == thatObj.valType;
					}
				}
			}

			override public int GetHashCode()
			{
				return this.attrType.GetHashCode() + (this.valType.GetHashCode() << 7);
			}
		}
		
		private static Dictionary<Type, MethodInfo[]> m_staticMethodsByAttribute = new Dictionary<Type, MethodInfo[]>();
		private static Dictionary<AttrAndType, FieldInfo[]> m_staticFieldsByAttrAndType = new Dictionary<AttrAndType, FieldInfo[]>();

	}
}
#endif

