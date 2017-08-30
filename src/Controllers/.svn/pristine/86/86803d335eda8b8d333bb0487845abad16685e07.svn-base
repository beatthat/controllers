using BeatThat.App;

namespace BeatThat
{
	/// <summary>
	/// Marker interface for Controller sibling components that should be bound when the controller is bound.
	/// The motivating case is a property binding.
	/// </summary>
	public interface ISubcontroller : HasBinding
	{
		/// <summary>
		/// Binds the presenter to its Model and View, 
		/// e.g. binds Clicked events from any view buttons as well as events defined by the Model.
		/// View and Model (if any) must both be assigned/available before calling Bind
		/// </summary>
		void Bind();
	}
}
