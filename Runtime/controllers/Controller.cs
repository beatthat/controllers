using System;
using BeatThat.Bindings;
using BeatThat.Commands;
using BeatThat.GameObjectUtil;
using BeatThat.GetComponentsExt;
using BeatThat.Notifications;
using BeatThat.Pools;
using BeatThat.Properties;
using BeatThat.SafeRefs;
using BeatThat.TransformPathExt;
using BeatThat.TypeUtil;
using UnityEngine;
using UnityEngine.Events;

namespace BeatThat.Controllers
{
    /// <summary>
    /// Basic Controller impl that you can subclass.
    /// 
    /// Includes the behaviour to find and manage any sibling ISubcontroller behaviours.
    /// </summary>
    public class Controller : BindingBehaviour, IController
	{
		public enum TriggerEvent { NONE = 0, START = 1, ENABLE = 2 }

		/// <summary>
		/// Generally, expected behaviour would be that any disabled Controller should get unbound.
		/// Having this automated saves some boilerplate code in cases where a parent controller has (levels of) child controllers
		/// and the parent gets disabled.
		/// </summary>
		[HideInInspector][SerializeField]private bool m_ensureUnbindOnDisable = true;

		/// <summary>
		/// This is only for presenters that don't have a model.
		///
		/// It's usually convenient if these presenters call their own ResetBindGo
		/// either when they Start or OnEnable.
		///
		/// This helps ensure that if they are owned by another Presenter and 'Attached'
		/// that they will Unbind when their owner Unbinds.
		///
		/// Example, a panel has a ScrollList of items attached
		/// and it's important that the ScrollList unbinds when its owner unbinds
		/// so that it will clear its items.
		///
		/// This field should be managed by a custom unity Editor
		/// so that it only displays in the inspector for presenters that have no model
		/// </summary>
		[HideInInspector][SerializeField]private TriggerEvent m_autoResetBindGoEvent = TriggerEvent.ENABLE;

		/// <summary>
		/// If TRUE, then when this controller binds,
		/// finds all sibling component ISubcontroller instances and binds them.
		/// This is useful for property bindings.
		/// </summary>
		[HideInInspector][SerializeField]private bool m_bindSubcontrollers = true;

		virtual protected void Start()
		{
			CheckAutoResetBindGo(TriggerEvent.START);
		}

		virtual protected void OnEnable()
		{
			CheckAutoResetBindGo(TriggerEvent.ENABLE);
		}

		virtual protected void OnDisable()
		{
			if(m_ensureUnbindOnDisable && this.isBound) {
				Unbind();
			}
		}

		protected void CheckAutoResetBindGo(TriggerEvent t)
		{
			// this behaviour should never apply to a presenter that has a model
			var hasModel = this as HasModel;
			if(hasModel != null && hasModel.GetModelType() != typeof(NoModel)) {
				return;
			}

			switch(m_autoResetBindGoEvent) {
			case TriggerEvent.START:
				if(t == TriggerEvent.START) {
					EnsureResetBindGo();
				}
				break;

				case TriggerEvent.ENABLE:
				if(t == TriggerEvent.ENABLE) {
					EnsureResetBindGo();
				}
				break;
			}
		}

		public void Reset()
		{
			if (!Application.isPlaying) {
				return;
			}
			m_gameObject = this.gameObject;
			if(this.isBound) {
				Unbind();
			}
			ResetController();
		}

		virtual protected void ResetController() {}

		virtual protected void ReleaseView() {}

		public void ResetBindGo()
		{
			Reset();
			Bind();
			Go();
		}

		protected void EnsureResetBindGo()
		{
			if(this.isBound) {
				return;
			}
			ResetBindGo();
		}

		virtual public bool isHidden
		{
			get {
				return this.gameObject != null && !this.gameObject.activeSelf;
			}
		}

		virtual public void Hide(bool hide)
		{
			if(this.isValid) {
				this.gameObject.SetActive(!hide);
			}
		}

		/// <summary>
		/// Determines whether this Controller will bind sibling subcontrollers.
		///
		/// Generally we only want to 'main' controller on a GameObject to bind subcontrollers,
		/// so by default, returns FALSE the implementation does NOT itself implement ISubcontroller,
		/// even if the bindSubcontrollers property is set to TRUE.
		/// </summary>
		virtual public bool bindSubcontrollers
		{
			get {
				return m_bindSubcontrollers && !(this is ISubcontroller);
			}
		}

		/// <summary>
		/// Base implementation sets isBound property.
		/// overriding implementations should call base.Bind() as last step
		/// </summary>
		sealed override protected void BindAll()
		{
			if(!this.isBound) {
				BindController();
				#pragma warning disable 618
				var cs = GetCommands(false);
				#pragma warning restore 618
				if(cs != null) {
					cs.RegisterCommands();
				}
				BindSubcontrollers();
			}
		}

		public void Go()
		{
			GoController ();
			GoSubcontrollers ();
		}

		/// <summary>
		/// Override to add behaviour Controller-specific behaviour to Controller::Go
		/// </summary>
		virtual protected void GoController() {}

		protected void GoSubcontrollers()
		{
			if(!this.bindSubcontrollers) {
				return;
			}
			using(var list = ListPool<ISubcontroller>.Get()) {
				this.GetSiblingComponents<ISubcontroller>(list, true);
				foreach(var s in list) {
					s.OnControllerDidGo ();
				}
			}
		}


	 	override protected void UnbindAll()
		{
			if(this.isBound) {
				UnbindController();
				#pragma warning disable 618
				UnregisterAllCommands();
				#pragma warning restore 618
				ReleaseView();
			}
		}

		/// <summary>
		/// Put your controller's custom Bind code here.
		/// </summary>
		virtual protected void BindController() {}

		/// <summary>
		/// Put your controller's custom Unbind code here.
		/// </summary>
		virtual protected void UnbindController() {}


		[System.Obsolete("use Bind instead")]
		protected NotificationBinding AddNotification(string type, Action callback)
		{
			return Bind(type, callback);
		}

		[System.Obsolete("use Bind instead")]
		protected NotificationBinding AddNotification<T>(string type, Action<T> callback)
		{
			return Bind<T>(type, callback);
		}

		[System.Obsolete("shouldn't need the ICommandSet any more, use NotificationCommand which binds/unbinds with the controller")]
		protected void AddCommand<T>(bool disableAutoRegistration = false) where T : Component, Command<Notification>, RegistersCommand
		{
			var cs = GetCommands(true);

			if(cs == null) {
				Debug.LogWarning("[" + Time.frameCount + "][" + this.Path() + "] unable to add command " + typeof(T).Name + " CommandSet is null");
				return;
			}

			cs.AddCommand<T>(disableAutoRegistration);
		}

		[System.Obsolete("shouldn't need the ICommandSet any more, use NotificationCommand which binds/unbinds with the controller")]
		protected ICommandSet GetCommands(bool create)
		{
			var cs = m_commands.value;
			if(cs != null) {
				return cs;
			}

			m_commands.value = cs = GetComponent<ICommandSet>();
			if(cs != null || !create) {
				return cs;
			}

			var csType = TypeUtils.Find("BeatThat.Notifications.CommandSet");
			if(csType == null) {
				#if UNITY_EDITOR || APE_DEBUG_UNSTRIP
				Debug.LogWarning("[" + Time.frameCount + "][" + this.Path()
					+ "] GetCommands(create:true) called and no CommandSet implementation found. Really code should be changed to not use deprecated CommandSet");
				#endif
				return null;
			}

			m_commands.value = cs = this.gameObject.AddComponent(csType) as ICommandSet;
			return cs;
		}
		virtual protected void BindSubcontrollers()
		{
			if(!this.bindSubcontrollers) {
				return;
			}
			using(var list = ListPool<ISubcontroller>.Get()) {
				this.GetSiblingComponents<ISubcontroller>(list, true);
				foreach(var s in list) {
					s.Bind();
					Attach(s.binding);
				}
			}
		}
		protected Action<T> IfBound<T>(Action<T> cb)
		{
			GameObject goRef = this.gameObject;

			return SafeCallback.Wrap<T>(cb, () => goRef != null && this.isBound);
		}

		protected Action IfBound(Action cb)
		{
			GameObject goRef = this.gameObject;

			return SafeCallback.Wrap(cb, () => goRef != null && this.isBound);
		}

		[System.Obsolete("shouldn't need the ICommandSet any more, use NotificationCommand which binds/unbinds with the controller")]
		protected void UnregisterAllCommands()
		{
			var cmds = GetCommands(false);
			if(cmds == null) {
				return;
			}
			cmds.UnregisterCommands();
		}

		protected bool IsValid()
		{
			return this.isValid;
		}

		/// <summary>
		/// Will return FALSE if the gameobject/owner of this presenter has been destroyed
		/// </summary>
		/// <value><c>true</c> if is valid; otherwise, <c>false</c>.</value>
		public bool isValid
		{
			get {
				return m_gameObject != null && !this.isDestroyed;
			}
		}

		#pragma warning disable 649
		private SafeRef<ICommandSet> m_commands;
		#pragma warning restore 649
		private GameObject m_gameObject;
	}

	public class Controller<ModelType> : Controller, IController<ModelType>, HasModel
		where ModelType : class
	{
        
        private ModelEvent m_onModelWillUpdate;
        /// <summary>
        /// Invoked immediately before the model is replaced
        /// </summary>
        public UnityEvent<ControllerModelUpdate<ModelType>> onModelWillUpdate
        {
            get
            {
                return m_onModelWillUpdate ?? (m_onModelWillUpdate = new ModelEvent());
            }
        }

        private ModelEvent m_onModelDidUpdate;
        /// <summary>
        /// Invoked immediately after the model is replaced. The old/prev model is passed as param
        /// </summary>
        public UnityEvent<ControllerModelUpdate<ModelType>> onModelDidUpdate
        {
            get
            {
                return m_onModelDidUpdate ?? (m_onModelDidUpdate = new ModelEvent());
            }
        }

        [Serializable] class ModelEvent : UnityEvent<ControllerModelUpdate<ModelType>> { }

		public bool hasModel
		{
			get {
				return this.model != null;
			}
		}

		public ModelType model { get; private set; }

		public void GoWithModel(object model)
		{
			GoWith((ModelType)model);
		}

		public void GoWith(ModelType m)
		{
			if(this.isBound) {
				if(this.model == m) {
					Go();
				}
				else {
					OnUpdateModel(this.model, m);
				}
				return;
			}
			Reset();
			this.model = m;
			Bind();
			Go();
		}

		virtual protected void OnUpdateModel(ModelType prev, ModelType cur)
		{
			Unbind();
			this.model = default(ModelType);
			GoWith(cur);
		}

        /// <summary>
        /// Set the model.
        /// Sends update events according to policy (default is SendOnChange)
        /// </summary>
        /// <param name="model">Model.</param>
        /// <param name="opts">Opts.</param>
        /// <param name="disposePreviousModel">If TRUE and the Model type implements IDisposable, 
        /// then calls Dispose on the previous model after set and all events.
        /// </param>
        public void SetModel(object model, 
                             PropertyEventOptions opts = PropertyEventOptions.SendOnChange, 
                             bool disposePreviousModel = true)
		{
            var m = model as ModelType;
            if (m == null && model != null)
            {
#if UNITY_EDITOR || DEBUG_UNSTRIP
                Debug.LogWarning("[" + Time.frameCount + "] SetModel called with invalid type: expected type="
                                 + typeof(ModelType) + " encountered type=" + model.GetType());
#endif
                return;
            }

            var update = new ControllerModelUpdate<ModelType>
            {
                controller = this,
                modelTo = m,
                modelFrom = this.model
            };

            if(m_onModelWillUpdate != null) {
                m_onModelDidUpdate.Invoke(update);
            }

            var prev = this.model;

			this.model = m;

            if(m_onModelDidUpdate != null) {
                m_onModelDidUpdate.Invoke(update);
            }

            var prevDispose = prev as IDisposable;
            if(prevDispose != null) {
                prevDispose.Dispose();
            }
		}

		public object GetModel()
		{
			return this.model;
		}

		public Type GetModelType() { return typeof(ModelType); }


	}

	public interface ViewController : HasView  {} // non generic marker interface to identify instances of Controller<M,V> without knowing M or V

	public abstract class Controller<ModelType, ViewType> : Controller<ModelType>, IController<ModelType, ViewType>, ViewController
		where ModelType : class
		where ViewType : class, IView
	{

		/// <summary>
		/// This is only for set ups where the view is a distinct component on a distinct gameobject from the presenter.
		///
		/// By default IController::Hide(bool) will disable the presenter's game object.
		/// If this is true, then will hide only the view.
		/// </summary>
		[HideInInspector][SerializeField]private bool m_hidesViewOnly;

		// identical to property hasView, for use as a function pointer
		public bool HasView()
		{
			return this.hasView;
		}

		public bool hasView
		{
			get {
				return this.isValid && (m_viewIsComponent)? m_viewGo != null && m_view != null: m_view != null;
			}
		}

		public GameObject GetViewGameObject(bool create = true)
		{
			if(!this.hasView && !create) {
				return null;
			}

			var view = this.view;
			return view == null ? null : view.transform.gameObject;
		}

		public Type GetViewType ()
		{
			return typeof(ViewType);
		}

		override public bool isHidden
		{
			get {
				return m_hidesViewOnly ? m_isViewHidden : base.isHidden;
			}
		}

		override public void Hide(bool hide)
		{
			if(m_hidesViewOnly) {
				m_isViewHidden = hide;
				if(this.hasView) {
					this.view.transform.gameObject.SetActive(!hide);
				}
			}
			else {
				base.Hide(hide);
			}
		}

		override protected void ReleaseView()
		{
			this.view = null;
			if(m_viewPlacement != null) {
				m_viewPlacement.Release();
			}
		}

		public ViewType view
		{
			get {
				if(!this.hasView) {
					this.Init();
				}
				return m_view;
			}
			set {
				m_view = value;
				m_viewIsComponent = (m_view is Component);
				m_viewGo = m_viewIsComponent ? (m_view as Component).gameObject : null;
			}
		}

		protected VT ViewAs<VT>() where VT : class
		{
			return this.view as VT;
		}

		override protected void ResetController()
		{
			base.ResetController();
			Init();
			if(this.hasView) {
				this.view.Reset();
			}
		}

		override protected void GoController()
		{
			// default behaviour is just to make sure view is visible on go
			if(this.hasView && this.view is Component) {
				var cview = this.view as Component;
				if(m_isViewHidden) {
					cview.gameObject.SetActive(false);
				}
				else if(!cview.gameObject.activeSelf) {
					cview.gameObject.SetActive(true);
				}
			}
		}

		private void Init()
		{
			if(!this.hasView) {
				this.view = CreateView();
				if(m_isViewHidden) {
					this.view.transform.gameObject.SetActive(false);
				}
			}

			if(!this.hasView) {
				Debug.LogWarning("[" + Time.time + "][" + this.Path() + "] " + GetType() + "::Init failed to create view!");
			}
		}

		private bool m_isViewHidden;

		[HideInInspector][SerializeField]private LayerSource m_forceLayerTo = LayerSource.NONE;


		public LayerSource forceLayerTo { get { return m_forceLayerTo; } set { m_forceLayerTo = value; } }

		virtual protected ViewType CreateView()
		{
			var v = FindView();

			if(v == null) {
				Debug.LogWarning("[" + Time.time + "][" + this.Path() + "] " + GetType()
					+ "::CreateView unable to create a view of any type. Is view placement component missing?");

				return null;
			}

			ApplyLayers(v);

			return v;
		}

		protected ViewType FindView()
		{
			m_viewPlacement = m_viewPlacement ?? GetComponent<IViewPlacement> ();
			return m_viewPlacement != null ? m_viewPlacement.view as ViewType : this.transform.GetComponentInDirectChildren<ViewType>(true);
		}

		protected void ApplyLayers(ViewType v)
		{
			switch(this.forceLayerTo) {
			case LayerSource.SELF:
				(v as Component).gameObject.SetLayerRecursively(this.gameObject.layer, true);
				break;
			case LayerSource.PARENT:
				if(this.transform.parent != null) {
					this.gameObject.SetLayerRecursively(this.transform.parent.gameObject.layer, true);
				}
				break;
			}
		}

		private IViewPlacement m_viewPlacement;

		private ViewType m_view;
		private GameObject m_viewGo;
		private bool m_viewIsComponent;
	}
}






