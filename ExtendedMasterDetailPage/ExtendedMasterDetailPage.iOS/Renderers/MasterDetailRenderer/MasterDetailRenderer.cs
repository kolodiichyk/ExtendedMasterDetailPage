using System;
using System.Collections;
using System.ComponentModel;
using System.Linq;
using ExtendedMasterDetailPage.iOS.Renderers.MasterDetailRenderer;
using ExtendedMasterDetailPage.Services;
using Pickpack.iOS.Renderers.MasterDetailRenderer;
using UIKit;
using Xamarin.Forms;
using Xamarin.Forms.Platform.iOS;
using PointF = CoreGraphics.CGPoint;

[assembly: ExportRenderer(typeof (MasterDetailPage), typeof (MasterDetailRenderer))]
namespace ExtendedMasterDetailPage.iOS.Renderers.MasterDetailRenderer
{
	public class MasterDetailRenderer : PhoneMasterDetailRenderer
	{
		private bool _disposed;

		private bool _isRightToLeft => DependencyService.Get<ILocalizeService>().IsRightToLeft;

		private readonly UIView _clickOffView;

		private readonly UIViewController _detailController;

		private readonly UIViewController _masterController;

		private UIPanGestureRecognizer _panGesture;

		private bool _presented;

		private UIGestureRecognizer _tapGesture;

		private MasterDetailPage page;

		public MasterDetailRenderer()
		{
			_masterController = new ChildViewController();
			_detailController = new ChildViewController();

			_clickOffView = new UIView();
			_clickOffView.BackgroundColor = new Color(0, 0, 0, 0).ToUIColor();
		}

		private bool Presented
		{
			get { return _presented; }
			set
			{
				if (_presented == value)
					return;
				_presented = value;
				LayoutChildren(true);
				if (value)
					AddClickOffView();
				else
					RemoveClickOffView();

				((IElementController) Element).SetValueFromRenderer(MasterDetailPage.IsPresentedProperty, value);
			}
		}

		public override void ViewDidLoad()
		{
			base.ViewDidLoad();
			if (!_isRightToLeft)
			{
				return;
			}

			Presented = ((MasterDetailPage) Element).IsPresented;

			((MasterDetailPage) Element).PropertyChanged += HandlePropertyChanged;

			_tapGesture = new UITapGestureRecognizer(() =>
			{
				if (Presented)
					Presented = false;
			});
			_clickOffView.AddGestureRecognizer(_tapGesture);

			PackContainers();

			UpdateMasterDetailContainers();

			UpdateBackground();

			UpdatePanGesture();

			var masterFrame = Element.Bounds.ToRectangleF();
			var frameWidth = Math.Min(masterFrame.Width, masterFrame.Height);
			masterFrame.Width = (int) (frameWidth*0.8);
			masterFrame.X = (int) (frameWidth - masterFrame.Width);
			_masterController.View.Frame = masterFrame;
		}

		private void HandlePropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == "Master" || e.PropertyName == "Detail")
				UpdateMasterDetailContainers();
			else if (e.PropertyName == MasterDetailPage.IsPresentedProperty.PropertyName)
				Presented = ((MasterDetailPage) Element).IsPresented;
			else if (e.PropertyName == MasterDetailPage.IsGestureEnabledProperty.PropertyName)
				UpdatePanGesture();
			else if (e.PropertyName == VisualElement.BackgroundColorProperty.PropertyName)
				UpdateBackground();
			else if (e.PropertyName == Page.BackgroundImageProperty.PropertyName)
				UpdateBackground();
		}

		private void UpdateBackground()
		{
			if (!string.IsNullOrEmpty(((Page) Element).BackgroundImage))
				View.BackgroundColor = UIColor.FromPatternImage(UIImage.FromBundle(((Page) Element).BackgroundImage));
			else if (Element.BackgroundColor == Color.Default)
				View.BackgroundColor = UIColor.White;
			else
				View.BackgroundColor = Element.BackgroundColor.ToUIColor();
		}

		private void EmptyContainers()
		{
			foreach (var child in _detailController.View.Subviews.Concat(_masterController.View.Subviews))
				child.RemoveFromSuperview();

			foreach (var vc in _detailController.ChildViewControllers.Concat(_masterController.ChildViewControllers))
				vc.RemoveFromParentViewController();
		}

		private void PackContainers()
		{
			_detailController.View.BackgroundColor = new UIColor(1, 1, 1, 1);
			View.AddSubview(_masterController.View);
			View.AddSubview(_detailController.View);

			AddChildViewController(_masterController);
			AddChildViewController(_detailController);
		}

		private void UpdateMasterDetailContainers()
		{
			((MasterDetailPage) Element).Master.PropertyChanged -= HandleMasterPropertyChanged;

			EmptyContainers();

			if (Platform.GetRenderer(((MasterDetailPage) Element).Master) == null)
				Platform.SetRenderer(((MasterDetailPage) Element).Master,
					Platform.CreateRenderer(((MasterDetailPage) Element).Master));
			if (Platform.GetRenderer(((MasterDetailPage) Element).Detail) == null)
				Platform.SetRenderer(((MasterDetailPage) Element).Detail,
					Platform.CreateRenderer(((MasterDetailPage) Element).Detail));

			var masterRenderer = Platform.GetRenderer(((MasterDetailPage) Element).Master);
			var detailRenderer = Platform.GetRenderer(((MasterDetailPage) Element).Detail);

			((MasterDetailPage) Element).Master.PropertyChanged += HandleMasterPropertyChanged;

			_masterController.View.AddSubview(masterRenderer.NativeView);
			_masterController.AddChildViewController(masterRenderer.ViewController);

			_detailController.View.AddSubview(detailRenderer.NativeView);
			_detailController.AddChildViewController(detailRenderer.ViewController);
		}

		private void HandleMasterPropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == Page.IconProperty.PropertyName || e.PropertyName == Page.TitleProperty.PropertyName)
				MessagingCenter.Send<IVisualElementRenderer>(this, "Xamarin.UpdateToolbarButtons");
		}

		private void UpdatePanGesture()
		{
			var model = (MasterDetailPage) Element;
			if (!model.IsGestureEnabled)
			{
				if (_panGesture != null)
					View.RemoveGestureRecognizer(_panGesture);
				return;
			}

			if (_panGesture != null)
			{
				View.AddGestureRecognizer(_panGesture);
				return;
			}

			UITouchEventArgs shouldRecieve = (g, t) => !(t.View is UISlider);
			var center = new PointF();
			_panGesture = new UIPanGestureRecognizer(g =>
			{
				switch (g.State)
				{
					case UIGestureRecognizerState.Began:
						center = g.LocationInView(g.View);
						break;
					case UIGestureRecognizerState.Changed:
						var currentPosition = g.LocationInView(g.View);
						var motion = -currentPosition.X + center.X;
						var detailView = _detailController.View;
						var targetFrame = detailView.Frame;
						if (Presented)
							targetFrame.X = -(nfloat) Math.Max(0, _masterController.View.Frame.Width + Math.Min(0, motion));
						else
							targetFrame.X = -(nfloat) Math.Min(_masterController.View.Frame.Width, Math.Max(0, motion));
						detailView.Frame = targetFrame;
						break;
					case UIGestureRecognizerState.Ended:
						var detailFrame = _detailController.View.Frame;
						var masterFrame = _masterController.View.Frame;
						if (Presented)
						{
							if (detailFrame.X > -masterFrame.Width*.75)
								Presented = false;
							else
								LayoutChildren(true);
						}
						else
						{
							if (detailFrame.X < -masterFrame.Width*.25)
								Presented = true;
							else
								LayoutChildren(true);
						}
						break;
				}
			});
			_panGesture.ShouldReceiveTouch = shouldRecieve;
			_panGesture.MaximumNumberOfTouches = 2;

			foreach (var gr in View.GestureRecognizers)
			{
				View.RemoveGestureRecognizer(gr);
			}

			View.AddGestureRecognizer(_panGesture);
		}

		private void LayoutChildren(bool animated)
		{
			var frame = Element.Bounds.ToRectangleF();
			var masterFrame = frame;

			var frameWidth = Math.Min(masterFrame.Width, masterFrame.Height);

			masterFrame.Width = (int) (frameWidth*0.8);
			masterFrame.X = (int) (frameWidth - masterFrame.Width);

			_masterController.View.Frame = masterFrame;

			var target = frame;
			if (Presented)
			{
				target.X -= masterFrame.Width;
			}

			if (animated)
			{
				UIView.BeginAnimations("Flyout");
				var view = _detailController.View;
				view.Frame = target;
				UIView.SetAnimationCurve(UIViewAnimationCurve.EaseOut);
				UIView.SetAnimationDuration(250);
				UIView.CommitAnimations();
			}
			else
				_detailController.View.Frame = target;

			if (Presented)
				_clickOffView.Frame = _detailController.View.Frame;
		}

		private void AddClickOffView()
		{
			View.Add(_clickOffView);
			_clickOffView.Frame = _detailController.View.Frame;
		}

		private void RemoveClickOffView()
		{
			_clickOffView.RemoveFromSuperview();
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing && !_disposed)
			{
				Element.PropertyChanged -= HandlePropertyChanged;

				if (_tapGesture != null)
				{
					if (_clickOffView != null && _clickOffView.GestureRecognizers.Contains(_panGesture))
					{
						((IList) _clickOffView.GestureRecognizers).Remove(_tapGesture);
						_clickOffView.Dispose();
					}
					_tapGesture.Dispose();
				}
				if (_panGesture != null)
				{
					if (View != null && View.GestureRecognizers.Contains(_panGesture))
						((IList) View.GestureRecognizers).Remove(_panGesture);
					_panGesture.Dispose();
				}

				EmptyContainers();

				_disposed = true;
			}

			base.Dispose(disposing);
		}
	}
}