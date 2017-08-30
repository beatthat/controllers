using System;
using UnityEngine;

namespace BeatThat
{
	/// <summary>
	/// A presenter that has a distinct view component
	/// </summary>
	public interface HasView : IController
	{
		Type GetViewType();

		GameObject GetViewGameObject(bool create = true);
	}
}
