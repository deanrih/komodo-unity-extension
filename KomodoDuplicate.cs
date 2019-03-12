#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public class KomodoDuplicate
{
	private const String COMMAND_DUPLICATE = "Duplicate";
	private const String COMMAND_PASTE = "Paste";

	private const String PREF_KEY_ENABLE = "Komodo.KomodoDuplicate.Enable";
	private const String PREF_KEY_INCREMENT = "Komodo.KomodoDuplicate.Increment";

	private static readonly List<Int32> ExistingIDs = new List<Int32> ();

	public static Action<GameObject> OnGameObjectDuplicated;

	private static Int32 lastCount;
	private static Int32 lastIndex;
	private static String lastCommand = String.Empty;

	static KomodoDuplicate ()
	{
		EditorApplication.hierarchyWindowItemOnGUI -= ItemUpdate;
		EditorApplication.hierarchyWindowItemOnGUI += ItemUpdate;

		EditorApplication.playModeStateChanged -= PlayModeChanged;
		EditorApplication.playModeStateChanged += PlayModeChanged;
	}

	[PreferenceItem ("Komodo Utilities")]
	private static void OnPreferenceGUI ()
	{
		var isEnabled = EditorPrefs.GetBool (PREF_KEY_ENABLE , true);
		var isIncrement = EditorPrefs.GetBool (PREF_KEY_INCREMENT , true);

		EditorGUILayout.LabelField (new GUIContent ($@"Komodo Duplicate") , EditorStyles.boldLabel);

		EditorGUI.BeginChangeCheck ();
		isEnabled = EditorGUILayout.Toggle (new GUIContent ($@"Enable" , $@"Allow the duplication name to be modified.") , isEnabled);
		if (EditorGUI.EndChangeCheck ())
		{
			EditorPrefs.SetBool (PREF_KEY_ENABLE , isEnabled);
		}

		if (isEnabled)
		{
			EditorGUI.BeginChangeCheck ();
			isIncrement = EditorGUILayout.Toggle (new GUIContent ($@"Enable komodo auto increment" , $@"Allow the duplication incrementation to be performed,") , isIncrement);
			if (EditorGUI.EndChangeCheck ())
			{
				EditorPrefs.SetBool (PREF_KEY_INCREMENT , isIncrement);
			}
		}
	}

	private static void Reset ()
	{
		lastCount = 0;
		lastIndex = 0;
		lastCommand = String.Empty;
		ExistingIDs.Clear ();
	}

	// TODO : Fix object Position, Scale or anything that got modified after duplicated
	private static void ItemUpdate (Int32 instance , Rect selection)
	{
		var isEnabled = EditorPrefs.GetBool (PREF_KEY_ENABLE , true);

		if (!isEnabled)
		{
			return;
		}

		var currentEventCommand = Event.current.commandName;
		var currentEventType = Event.current.type;
		var isCommandValid = String.CompareOrdinal (currentEventCommand , COMMAND_DUPLICATE) == 0 || String.CompareOrdinal (currentEventCommand , COMMAND_PASTE) == 0;

		if (currentEventType == EventType.ExecuteCommand && isCommandValid)
		{
			//EditorGUIUtility.PingObject (Selection.activeGameObject);
			//Debug.Log ($@"{Selection.activeGameObject.GetInstanceID ()}");
			//EditorGUIUtility.PingObject (Selection.activeGameObject.GetInstanceID());
			ExistingIDs.Clear ();

			var objects = GameObject.FindObjectsOfType<GameObject> ();
			lastCount = objects.Length;

			for (var c = 0 ; c < objects.Length ; c++)
			{
				ExistingIDs.Add (objects [c].GetInstanceID ());
			}

			lastCommand = currentEventCommand;
			lastIndex = Selection.activeGameObject.transform.GetSiblingIndex ();
		}
		else if (lastCount > 0)
		{
			var objects = GameObject.FindObjectsOfType<GameObject> ();

			if (lastCount != objects.Length)
			{
				lastCount = 0;
				var isLastCommandValid = String.CompareOrdinal (lastCommand , COMMAND_DUPLICATE) == 0 || String.CompareOrdinal (lastCommand , COMMAND_PASTE) == 0;

				for (var c = 0 ; c < objects.Length ; c++)
				{
					var currentObject = objects [c];//.GetInstanceID ();

					if (ExistingIDs.Contains (currentObject.GetInstanceID ()))
					{
						continue;
					}

					if (isLastCommandValid)
					{
						var regexValue = String.Empty;
						var failSafe = 10;

						do
						{
							regexValue = Regex.Match (currentObject.name , "\\([^\\d]*(\\d+)[^\\d]*\\).*$").Value;

							if (!String.IsNullOrEmpty (regexValue) && regexValue.EndsWith (")"))
							{
								currentObject.name = currentObject.name.Replace (regexValue , "").TrimEnd ();
							}

							failSafe--;
						}
						while (!String.IsNullOrEmpty (regexValue) && failSafe > 0);

						// IF fixed increment
						if (EditorPrefs.GetBool (PREF_KEY_INCREMENT , true))
						{
							regexValue = Regex.Match (currentObject.name , "\\d+$").Value;

							if (!String.IsNullOrEmpty (regexValue))
							{
								var digit = 0;

								if (Int32.TryParse (regexValue , out digit))
								{
									digit++;
									var digitString = digit.ToString ();
									var replaceIndex = currentObject.name.Length - digitString.Length;
									currentObject.name = currentObject.name.Remove (replaceIndex).Insert (replaceIndex , digitString);
								}
							}
						}

						currentObject.transform.SetSiblingIndex (lastIndex + 1);
						//Debug.Log ($@"Instance ID : {currentObject.GetInstanceID ()}");
						//EditorGUIUtility.PingObject (currentObject.GetInstanceID());
					}

					OnGameObjectDuplicated?.Invoke (currentObject);
				}

				//EditorGUIUtility.PingObject (objects [objects.Length - 1]);
				//objects [objects.Length - 1].transform.SetSiblingIndex (lastIndex - 1);
			}
			else
			{
				Reset ();
			}
		}
	}

	private static void PlayModeChanged (PlayModeStateChange state) => Reset ();
}
#endif