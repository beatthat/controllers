using System;

using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace BeatThat.Controllers
{
    public static class ControllerEditorUtils 
	{

		private static MethodInfo[] FindAutoWireAddComponentImpls()
		{
			return EditorUtils.FindStaticMethodsWithAttribute<AutowireAddComponentImplAttribute>();
		}

		private static string ToCamelCase(string s)
		{
			return Char.ToLowerInvariant(s[0]) + s.Substring(1);
		}
		
		private static string UpperFirstChar(string s)
		{
			return Char.ToUpperInvariant(s[0]) + s.Substring(1);
		}
		
		private static void FindComponentFields(
			Transform t, Type[] compTypes, Dictionary<string, Type> fieldsByName, string parentName)
		{
			foreach(Type ctype in compTypes) {
				if(t.GetComponent(ctype) != null) {
					parentName += PolishTransformName(t);
					fieldsByName[parentName] = ctype;
					break;
				}
			}
			
			foreach(Transform child in t) {
				FindComponentFields(child, compTypes, fieldsByName, parentName);
			}
		}
		
		/// <summary>
		/// Attempts to set field properties of a view component by reflection, 
		/// matching field names from the view class to transform names in the passed-in component
		/// </summary>
		public static void AutoWireView(Type viewType, Component view)
		{
			foreach(MethodInfo m in FindAutoWireAddComponentImpls()) {
				m.Invoke(null, new object[] { view.transform });
			}

			Dictionary<string, FieldInfo> fieldsByName = GetFields(viewType);
			AutoWire(view, "", view.transform, fieldsByName);
		}
			
		private static string PolishFieldName(FieldInfo f)
		{
			string fname = f.Name.ToLower();
			if(fname.StartsWith("m_")) {
				fname = fname.Substring(2);
			}
			return fname;
		}
		
		private static string PolishTransformName(Transform t)
		{
			string name = t.name.Replace(" ", "");
			return name;
		}
		
		private static Dictionary<string, FieldInfo> GetFields(Type t)
		{
			Dictionary<string, FieldInfo> fieldsByName = new Dictionary<string, FieldInfo>();
			foreach(FieldInfo f in t.GetFields()) {
				fieldsByName[PolishFieldName(f)] = f;
			}
			return fieldsByName;
		}
		
		private static void AutoWire(object obj, string parentName, Transform t, Dictionary<string, FieldInfo> fieldsByName)
		{
			string fname = t.name.ToLower();
			
			if(!TrySetField(obj, fname, t, fieldsByName, ref parentName)) {
				TrySetField(obj, parentName + fname, t, fieldsByName, ref parentName);
			}
			
			foreach(Transform child in t) {
				AutoWire(obj, parentName, child, fieldsByName);
			}
		}
		
		private static bool TrySetField(object obj, string fname, Transform t, Dictionary<string, FieldInfo> fieldsByName, ref string parentName)
		{
			FieldInfo f;
			if(fieldsByName.TryGetValue(fname, out f)) { // does the name of this game object match a field?
				if(f.GetValue(obj) != null) { // if matching property is already set, then stop looking for it's value
					fieldsByName.Remove(fname);
					return true;
				}
				else {
					object val = t.GetComponent(f.FieldType); // see if the cur transform has a component that works as the field value
					if(val != null) { // if it does, use it
						f.SetValue(obj, val);
						fieldsByName.Remove(fname);
						parentName = parentName + fname; // store a compound name for the case of fields named like MyButton + Label = MyButtonLabel
						return true;
					}
				}
			}
			
			return false;
		}
	}

}

