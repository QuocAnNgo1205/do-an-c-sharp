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
    }
}
