/// <summary>
/// Core interface common to all presenters.
/// </summary>
using BeatThat.App;
using UnityEngine;


namespace BeatThat
{
	public interface IController : HasBinding
	{
		GameObject gameObject { get; }

		Transform transform { get; }

		/// <summary>
		/// Set up the presenter.
		/// </summary>
		void Reset();
		
		/// <summary>
		/// Binds the presenter to its Model and View, 
		/// e.g. binds Clicked events from any view buttons as well as events defined by the Model.
		/// View and Model (if any) must both be assigned/available before calling Bind
		/// </summary>
		void Bind();
		
		/// <summary>
		/// Default call to activate a presenter,
		/// e.g. in Go text available from the Model might be rendered in labels from the View
		/// </summary>
		void Go();
		
		/// <summary>
		/// Convenience function to activate a presenter, calls Reset(), Bind(), and Go() in sequence
		/// For IPresenters (that have a parameterized model type), generally use GoWith(ModelType) instead.
		/// </summary>
		void ResetBindGo();
		
		/// <summary>
		/// Unbind presenters View and Model and does any cleanup.
		/// </summary>
		void Unbind();

		bool isBound { get; }

		/// <summary>
		/// Is hidden (by a call to Hide)?
		/// </summary>
		bool isHidden { get; }

		/// <summary>
		/// Hide (or show) the presenter, typically by gameObject.SetActive(true|false), but not always.
		/// </summary>
		void Hide(bool hide);
	}

	public interface IController<ModelType> : IController, HasModel
		where ModelType : class
	{
		ModelType model { get; set; }

		void GoWith(ModelType model);
	}

	public interface IController<ModelType, ViewType> : IController<ModelType>, HasView
		where ModelType : class
		where ViewType : class, IView 
	{
		ViewType view { get; set; }
	}
}
