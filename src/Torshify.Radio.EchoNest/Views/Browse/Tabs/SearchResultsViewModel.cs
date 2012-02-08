﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.Threading.Tasks;
using Microsoft.Practices.Prism;
using Microsoft.Practices.Prism.Logging;
using Microsoft.Practices.Prism.Regions;
using Microsoft.Practices.Prism.ViewModel;

using Torshify.Radio.Framework;
using Torshify.Radio.Framework.Commands;

namespace Torshify.Radio.EchoNest.Views.Browse.Tabs
{
    [Export]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    public class SearchResultsViewModel : NotificationObject, IHeaderInfoProvider<HeaderInfo>, INavigationAware
    {
        #region Fields

        private ObservableCollection<Track> _results;

        #endregion Fields

        #region Constructors

        public SearchResultsViewModel()
        {
            _results = new ObservableCollection<Track>();

            HeaderInfo = new HeaderInfo
                         {
                             Title = "Results"
                         };

            GoToAlbumCommand = new StaticCommand<string>(ExecuteGoToAlbum);
            GoToArtistCommand = new StaticCommand<string>(ExecuteGoToArtist);
        }

        #endregion Constructors

        #region Properties

        public HeaderInfo HeaderInfo
        {
            get;
            private set;
        }

        public ObservableCollection<Track> Results
        {
            get
            {
                return _results;
            }
        }

        [Import]
        public IRegionManager RegionManager
        {
            get; 
            set;
        }

        [Import]
        public ILoadingIndicatorService LoadingIndicatorService
        {
            get;
            set;
        }

        [Import]
        public IRadio Radio
        {
            get;
            set;
        }

        [Import]
        public IToastService ToastService
        {
            get;
            set;
        }

        [Import]
        public ILoggerFacade Logger
        {
            get;
            set;
        }

        public StaticCommand<string> GoToArtistCommand
        {
            get;
            private set;
        }

        public StaticCommand<string> GoToAlbumCommand
        {
            get;
            private set;
        }

        #endregion Properties

        #region Methods

        void INavigationAware.OnNavigatedTo(NavigationContext context)
        {
            if (!string.IsNullOrEmpty(context.Parameters[SearchBar.IsFromSearchBarParameter]))
            {
                string value = context.Parameters[SearchBar.ValueParameter];
                ExecuteSearch(value);
            }
        }

        bool INavigationAware.IsNavigationTarget(NavigationContext context)
        {
            return true;
        }

        void INavigationAware.OnNavigatedFrom(NavigationContext context)
        {
        }

        private void ExecuteSearch(string query)
        {
            var ui = TaskScheduler.FromCurrentSynchronizationContext();
            Task
                .Factory
                .StartNew(state =>
                {
                    LoadingIndicatorService.Push();
                    return Radio.GetTracksByName(state.ToString());
                }, query)
                .ContinueWith(task =>
                {
                    if (task.Exception != null)
                    {
                        ToastService.Show("Error while search for " + task.AsyncState);
                        Logger.Log(task.Exception.ToString(), Category.Exception, Priority.Medium);
                    }
                    else
                    {
                        _results.Clear();

                        foreach (var track in task.Result)
                        {
                            _results.Add(track);
                        }
                    }

                    LoadingIndicatorService.Pop();
                }, ui);
        }

        private void ExecuteGoToArtist(string artistName)
        {
            UriQuery q = new UriQuery();
            q.Add("artistName", artistName);
            RegionManager.RequestNavigate(AppRegions.ViewRegion, typeof(ArtistView).FullName + q);
        }

        private void ExecuteGoToAlbum(string albumName)
        {
            UriQuery q = new UriQuery();
            q.Add("albumName", albumName);
            RegionManager.RequestNavigate(AppRegions.ViewRegion, typeof(AlbumView).FullName + q);
        }

        #endregion Methods
    }
}