using CommunityToolkit.Mvvm.Input;
using VinhKhanhstreet.Models;

namespace VinhKhanhstreet.PageModels
{
    public interface IProjectTaskPageModel
    {
        IAsyncRelayCommand<ProjectTask> NavigateToTaskCommand { get; }
        bool IsBusy { get; }
    }
}