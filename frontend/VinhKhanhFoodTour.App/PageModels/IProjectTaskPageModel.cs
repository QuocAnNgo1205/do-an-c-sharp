using CommunityToolkit.Mvvm.Input;
using VinhKhanhFoodTour.App.Models;

namespace VinhKhanhFoodTour.App.PageModels
{
    public interface IProjectTaskPageModel
    {
        IAsyncRelayCommand<ProjectTask> NavigateToTaskCommand { get; }
        bool IsBusy { get; }
    }
}