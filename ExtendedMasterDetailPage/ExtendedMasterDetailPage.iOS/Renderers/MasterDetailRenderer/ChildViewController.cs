using UIKit;

namespace ExtendedMasterDetailPage.iOS.Renderers.MasterDetailRenderer
{
	internal class ChildViewController : UIViewController
	{
		public override void ViewDidLayoutSubviews()
		{
			foreach (var vc in ChildViewControllers)
				vc.View.Frame = View.Bounds;
		}
	}
}
