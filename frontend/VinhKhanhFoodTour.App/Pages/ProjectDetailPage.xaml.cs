using VinhKhanhFoodTour.App.Models;

namespace VinhKhanhFoodTour.App.Pages
{
    public partial class ProjectDetailPage : ContentPage
    {
        public ProjectDetailPage(ProjectDetailPageModel model)
        {
            InitializeComponent();
            BindingContext = model;
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            
            // Dừng phát âm thanh khi thoát khỏi trang để tránh audio chạy ngầm gây khó chịu
            if (BindingContext is ProjectDetailPageModel model)
            {
                model.StopAudioCommand.Execute(null);
            }
        }
    }
}
