using BeatThat;

namespace BeatThat
{
	public class Subcontroller : BindingBehaviour, ISubcontroller
	{
		/// <summary>
		/// There are two common/default set ups:
		/// 
		/// 1) This component is attached to a GameObject with a sibling controller, in which case that controller will call Bind() this component.
		/// 2) This component has NO controller sibling, in which case should usually call Bind() on itself when it enabled (and unbind on disable).
		/// 
		/// This property is to override the #2 behaviour, and NOT automatically bind on enable.
		/// </summary>
		public bool m_disableBindOnEnableWithNoControllerSibling;

		virtual protected void OnEnable()
		{
			if(this.didStart) {
				ConditionalBindOnEnable();
			}
		}

		virtual protected void OnDisable()
		{
			if(this.didBindOnEnable) {
				Unbind();
			}
		}

		virtual protected void Start()
		{
			ConditionalBindOnEnable();
			this.didStart = true;
		}
		private bool didStart { get; set; }

		private void ConditionalBindOnEnable()
		{
			if(this.isBound) {
				return;
			}

			if(m_disableBindOnEnableWithNoControllerSibling) {
				return;
			}

			if(GetComponent<IController>() == null) {
				Bind();
				this.didBindOnEnable = true;
			}
		}
		private bool didBindOnEnable { get; set; }
		/// <summary>
		/// Base implementation sets isBound property.
		/// overriding implementations should call base.Bind() as last step
		/// </summary>
		sealed override protected void BindAll()
		{
			_BindInternal();
			BindSubcontroller();
		}
		
		/// <summary>
		/// Base implementation sets isBound property false.
		/// overriding implementations should call base.Bind() as last step
		/// </summary>
		sealed override protected void UnbindAll()
		{
			UnbindSubcontroller();
		}

		/// <summary>
		/// Don't override this in subclasses. Internal only.
		/// </summary>
		virtual protected void _BindInternal() {}
		
		/// <summary>
		/// Put your subcontroller's custom Bind code here.
		/// </summary>
		virtual protected void BindSubcontroller() {}
		
		/// <summary>
		/// Put your subcontroller's custom Unbind code here.
		/// </summary>
		virtual protected void UnbindSubcontroller() {}

	}

	public class Subcontroller<T> : Subcontroller where T : class
	{
		protected T controller { get; private set; }

		sealed override protected void _BindInternal()
		{
			this.controller = GetComponent<T>();
		}
	}

}
