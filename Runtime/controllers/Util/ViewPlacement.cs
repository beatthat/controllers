using BeatThat.Placements;
using UnityEngine;

namespace BeatThat.Controllers
{
    public class ViewPlacement : ViewPlacement<View> {}

	public class ViewPlacement<T> : PrefabPlacement<T>, IViewPlacement
		where T : Component, IView
	{
		public IView view { get { return this.managedObject; } }


	}
}


