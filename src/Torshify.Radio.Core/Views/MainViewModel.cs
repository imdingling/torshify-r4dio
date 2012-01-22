﻿using System.ComponentModel.Composition;
using Microsoft.Practices.Prism.Regions;
using Microsoft.Practices.Prism.ViewModel;

namespace Torshify.Radio.Core.Views
{
    [Export(typeof(MainViewModel))]
    public class MainViewModel : NotificationObject, INavigationAware
    {
        void INavigationAware.OnNavigatedTo(NavigationContext navigationContext)
        {
        }

        bool INavigationAware.IsNavigationTarget(NavigationContext navigationContext)
        {
            return true;
        }

        void INavigationAware.OnNavigatedFrom(NavigationContext navigationContext)
        {
        }
    }
}