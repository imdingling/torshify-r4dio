﻿using System;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading.Tasks;

using EchoNest;
using EchoNest.Artist;

using Microsoft.Practices.Prism.Logging;
using Microsoft.Practices.Prism.Regions;
using Microsoft.Practices.Prism.ViewModel;

using Torshify.Radio.Framework;

namespace Torshify.Radio.EchoNest.Views.Browse.Tabs
{
    [Export]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    [RegionMemberLifetime(KeepAlive = true)]
    public class AlbumViewModel : NotificationObject, INavigationAware
    {
        #region Fields

        private TrackContainer _currentTrackContainer;

        #endregion Fields

        #region Properties

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

        [Import]
        public ILoadingIndicatorService LoadingIndicatorService
        {
            get;
            set;
        }
        
        public TrackContainer CurrentTrackContainer
        {
            get
            {
                return _currentTrackContainer;
            }
            set
            {
                if (_currentTrackContainer != value)
                {
                    _currentTrackContainer = value;
                    RaisePropertyChanged("CurrentTrackContainer");
                }
            }
        }

        #endregion Properties

        #region Methods

        void INavigationAware.OnNavigatedTo(NavigationContext navigationContext)
        {
            string artist = navigationContext.Parameters["artistName"];
            string album = navigationContext.Parameters["albumName"];

            ExecuteSearchForAlbum(artist, album);
        }

        bool INavigationAware.IsNavigationTarget(NavigationContext navigationContext)
        {
            return true;
        }

        void INavigationAware.OnNavigatedFrom(NavigationContext navigationContext)
        {
            CurrentTrackContainer = null;
        }

        private void ExecuteSearchForAlbum(string artist, string album)
        {
            Task.Factory.StartNew(state => GetAlbumMatchingName(state), Tuple.Create(artist, album))
            .ContinueWith(task =>
            {
                var data = (Tuple<string, string>)task.AsyncState;

                if (task.Exception != null)
                {
                    ToastService.Show("Error occurred during fetching of album");
                    Logger.Log(task.Exception.ToString(), Category.Exception, Priority.Medium);
                }
                else
                {
                    CurrentTrackContainer = task.Result;
                    
                    if (task.Result == null)
                    {
                        ToastService.Show("Unable to find album " + data.Item2 + " by " + data.Item1);
                    }
                    else
                    {
                        Task childTask = new Task(state =>
                        {
                            var albumInfo = (Tuple<string, string>)task.AsyncState;
                            LookUpAlbumInformation(albumInfo.Item1, albumInfo.Item2);
                        }, data, TaskCreationOptions.AttachedToParent);
                        childTask.Start();
                    }
                }
            });
        }

        private TrackContainer GetAlbumMatchingName(object state)
        {
            var data = (Tuple<string, string>)state;

            using (LoadingIndicatorService.EnterLoadingBlock())
            {
                return Radio
                    .GetAlbumsByArtist(data.Item1)
                    .FirstOrDefault(a => a.Name.Equals(data.Item2, StringComparison.InvariantCultureIgnoreCase));
            }
        }

        private void LookUpAlbumInformation(string artistName, string albumName)
        {
            try
            {
                using(var session = new EchoNestSession(EchoNestModule.ApiKey))
                {
                    var response = session.Query<Reviews>().Execute(artistName, numberOfResults: 20);

                    if (response.Status.Code == ResponseCode.Success)
                    {
                        var reviews = response.Reviews.Where(r => r.Release.ToLower().Contains(albumName.ToLower()));
                        var review = reviews.FirstOrDefault();

                        if (review != null)
                        {
                            CurrentTrackContainer.ExtraData.Review = review;
                        }
                    }
                }
            }
            catch(Exception ex)
            {
                Logger.Log(ex.ToString(), Category.Exception, Priority.Medium);
            }
        }

        #endregion Methods
    }
}