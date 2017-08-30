
namespace BeatThat
{
	public interface IViewPlacement  
	{
		IView view { get; }
		
		void EnsureCreated();
		
		void Delete();
	}
}
