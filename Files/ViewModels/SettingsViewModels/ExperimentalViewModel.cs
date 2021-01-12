﻿using Microsoft.Toolkit.Mvvm.ComponentModel;

namespace Files.ViewModels.SettingsViewModels
{
    public class ExperimentalViewModel : ObservableObject
    {
        private bool showFileOwner = App.AppSettings.ShowFileOwner;

        public bool ShowFileOwner
        {
            get
            {
                return showFileOwner;
            }
            set
            {
                if (SetProperty(ref showFileOwner, value))
                {
                    App.AppSettings.ShowFileOwner = value;
                }
            }
        }

        private bool useFileListCache = App.AppSettings.UseFileListCache;

        public bool UseFileListCache
        {
            get
            {
                return useFileListCache;
            }
            set
            {
                if (SetProperty(ref useFileListCache, value))
                {
                    App.AppSettings.UseFileListCache = value;
                }
            }
        }

        private int preemptiveCacheParallelLimit = App.AppSettings.PreemptiveCacheParallelLimit;

        public int PreemptiveCacheParallelLimit
        {
            get
            {
                return preemptiveCacheParallelLimit;
            }
            set
            {
                if (SetProperty(ref preemptiveCacheParallelLimit, value))
                {
                    App.AppSettings.PreemptiveCacheParallelLimit = value;
                }
            }
        }
    }
}