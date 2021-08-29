using Files.Enums;
using Files.ViewModels;
using Microsoft.Toolkit.Uwp;
using System.Diagnostics;

namespace Files.Helpers
{
    public static class PostBannerHelpers
    {
        private static OngoingTasksViewModel OngoingTasksViewModel => OngoingTasksViewModel;

        public static void PostBanner_Delete(ReturnResult status, FileOperationType operation, Stopwatch sw, IShellPage associatedInstance)
        {
            if (status == ReturnResult.Failed ||
                status == ReturnResult.UnknownException ||
                status == ReturnResult.IntegrityCheckFailed ||
                status == ReturnResult.AccessUnauthorized)
            {
                if (status == ReturnResult.AccessUnauthorized)
                {
                    OngoingTasksViewModel.PostBanner(
                        "AccessDeniedDeleteDialog/Title".GetLocalized(),
                        "AccessDeniedDeleteDialog/Text".GetLocalized(),
                        0,
                        status,
                        operation);
                }
                else if (status == ReturnResult.IntegrityCheckFailed)
                {
                    OngoingTasksViewModel.PostBanner(
                        "FileNotFoundDialog/Title".GetLocalized(),
                        "FileNotFoundDialog/Text".GetLocalized(),
                        0,
                        status,
                        operation);
                }
                else if (status == ReturnResult.Failed || status == ReturnResult.UnknownException)
                {
                    OngoingTasksViewModel.PostBanner(
                        "StatusDeletionFailed".GetLocalized(),
                        "StatusUnknownError".GetLocalized(),
                        0,
                        status,
                        operation);
                }
                else if (sw.Elapsed.TotalSeconds >= 10)
                {
                    if (operation == FileOperationType.Delete)
                    {
                        OngoingTasksViewModel.PostBanner(
                        "StatusDeletionComplete".GetLocalized(),
                        "StatusOperationCompleted".GetLocalized(),
                        0,
                        ReturnResult.Success,
                        operation);
                    }
                    else if (operation == FileOperationType.Recycle)
                    {
                        OngoingTasksViewModel.PostBanner(
                        "StatusRecycleComplete".GetLocalized(),
                        "StatusOperationCompleted".GetLocalized(),
                        0,
                        ReturnResult.Success,
                        operation);
                    }
                }
            }
        }
    }
}