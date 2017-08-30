using System;

namespace BeatThat
{
	/// <summary>
	/// A presenter that has a model.
	/// </summary>
	public interface HasModel : IController
	{
		/// <summary>
		/// Sets the model. Will throw InvalidCastException if arg is wrong type for presenter
		/// </summary>
		void SetModel(object model);

		object GetModel();

		/// <summary>
		/// Sets the model then calls Reset,Bind,Go sequence. Will throw InvalidCastException if arg is wrong type for presenter
		/// </summary>
		void GoWithModel(object model);

		Type GetModelType();
	}
}
