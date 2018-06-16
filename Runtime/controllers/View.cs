using UnityEngine;

namespace BeatThat.Controllers
{
    public class View : MonoBehaviour, IView
	{
		public ViewEditorControls m_viewEditorControls; // exists just to make inspector draw controls for all view types

		virtual public IController controller
		{
			get {	
				if(m_controller == null) 
				{
					Transform t = this.controllerIsParent? this.transform.parent: this.transform;
					
					// search components manually, because unity make a fuss if you request a component that isn't defined
					foreach(Component c in t.GetComponents(typeof(Component))) {
						var p = c as IController;
						if(p != null) {
							m_controller = p;
							break;
						}
					}
					
				}
				return m_controller;
			}
		}
		
		virtual protected bool controllerIsParent { get { return false; } }
		
		virtual public void Reset() {}
		
		virtual public void Release()
		{
			this.gameObject.SetActive(false);
		}
		
		protected void SetPresenter(IController p)
		{
			m_controller = p;
		}
		
		protected IController m_controller;
	}
}

