﻿using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading.Tasks;

using EchoNest;
using EchoNest.Artist;

using Microsoft.Practices.Prism.Commands;
using Microsoft.Practices.Prism.Logging;
using Microsoft.Practices.Prism.Regions;
using Microsoft.Practices.Prism.ViewModel;

using Torshify.Radio.EchoNest.Views.Hot.Models;
using Torshify.Radio.Framework;

namespace Torshify.Radio.EchoNest.Views.Hot
{
    [Export]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    [RegionMemberLifetime(KeepAlive = false)]
    public class HotArtistsViewModel : NotificationObject, INavigationAware
    {
        #region Fields

        private readonly ILoadingIndicatorService _loadingIndicator;
        private readonly ILoggerFacade _logger;
        private readonly IRadio _radio;
        private readonly IToastService _toastService;

        private IEnumerable<HotArtistModel> _artists;

        #endregion Fields

        #region Constructors

        [ImportingConstructor]
        public HotArtistsViewModel(
            IRadio radio,
            IToastService toastService,
            ILoadingIndicatorService loadingIndicator,
            ILoggerFacade logger)
        {
            _radio = radio;
            _toastService = toastService;
            _loadingIndicator = loadingIndicator;
            _logger = logger;

            PlayAllTracksCommand = new DelegateCommand(ExecutePlayAllTracks);
            PlayTopTrackCommand = new DelegateCommand<HotArtistModel>(ExecutePlayTopTrack);
            QueueTopTrackCommand = new DelegateCommand<HotArtistModel>(ExecuteQueueTopTrack);
        }

        #endregion Constructors

        #region Properties

        public DelegateCommand PlayAllTracksCommand
        {
            get;
            private set;
        }

        public DelegateCommand<HotArtistModel> PlayTopTrackCommand
        {
            get;
            private set;
        }

        public DelegateCommand<HotArtistModel> QueueTopTrackCommand
        {
            get;
            private set;
        }

        public IEnumerable<HotArtistModel> Artists
        {
            get
            {
                return _artists;
            }
            set
            {
                _artists = value;
                RaisePropertyChanged("Artists");

            }
        }

        #endregion Properties

        #region Methods

        void INavigationAware.OnNavigatedTo(NavigationContext navigationContext)
        {
            var ui = TaskScheduler.FromCurrentSynchronizationContext();
            Task.Factory
                .StartNew(() =>
                {
                    using (_loadingIndicator.EnterLoadingBlock())
                    {
                        using (var session = new EchoNestSession(EchoNestModule.ApiKey))
                        {
                            var response = session.Query<TopHottt>().Execute(50,
                                                                             bucket:
                                                                                 ArtistBucket.Images |
                                                                                 ArtistBucket.Songs);

                            if (response != null)
                            {
                                return response;
                            }
                        }
                    }

                    return null;
                })
                .ContinueWith(task =>
                {
                    if (task.Exception != null)
                    {
                        _logger.Log(task.Exception.ToString(), Category.Exception, Priority.Medium);
                        _toastService.Show("Unable to fetch artists");
                    }
                    else
                    {
                        if (task.Result != null)
                        {
                            if (task.Result.Status.Code == ResponseCode.Success)
                            {
                                Artists = task.Result.Artists.Select((a, i) =>
                                {
                                    string name = a.Name;
                                    string image = a.Images.Count > 0 ? a.Images[0].Url : null;
                                    string song = a.Songs.Count > 0 ? a.Songs[0].Title : null;
                                    return new HotArtistModel((i + 1), name, image, song);
                                });
                            }
                            else
                            {
                                _toastService.Show(task.Result.Status.Message);
                            }
                        }
                    }
                }, ui);
        }

        bool INavigationAware.IsNavigationTarget(NavigationContext navigationContext)
        {
            return true;
        }

        void INavigationAware.OnNavigatedFrom(NavigationContext navigationContext)
        {
        }

        private void ExecutePlayAllTracks()
        {
            Task.Factory
                .StartNew(() => _radio.Play(new TopHotttTrackStream(_radio)))
                .ContinueWith(task =>
                {
                    if (task.Exception != null)
                    {
                        _logger.Log(task.Exception.ToString(), Category.Exception, Priority.Medium);
                    }
                });
        }

        private void ExecutePlayTopTrack(HotArtistModel artist)
        {
            GetTopTrackForArtistAsync(artist)
                .ContinueWith(task =>
                {
                    if (task.Result != null)
                    {
                        _radio.Play(task.Result.ToTrackStream(task.Result.Name, "Top hot artists"));
                    }
                    else
                    {
                        _toastService.Show("Unable to find track " + artist.HotSongTitle);
                    }
                });
        }

        private void ExecuteQueueTopTrack(HotArtistModel artist)
        {
            GetTopTrackForArtistAsync(artist)
                .ContinueWith(task =>
                {
                    if (task.Result != null)
                    {
                        _radio.Queue(task.Result.ToTrackStream(task.Result.Name, "Top hot artists"));
                        _toastService.Show(new ToastData
                        {
                            Message = "Queued " + task.Result.Name,
                            Icon = AppIcons.Add
                        });
                    }
                    else
                    {
                        _toastService.Show("Unable to find track " + artist.HotSongTitle);
                    }
                });
        }

        private Task<Track> GetTopTrackForArtistAsync(HotArtistModel artist)
        {
            return Task.Factory
                .StartNew(state =>
                {
                    HotArtistModel m = (HotArtistModel)state;
                    using (_loadingIndicator.EnterLoadingBlock())
                    {
                        var result = _radio.GetTracksByName(m.Name + " " + m.HotSongTitle);
                        var track = result.FirstOrDefault(
                            t => t.Artist.Equals(m.Name, StringComparison.InvariantCultureIgnoreCase));

                        if (track == null)
                        {
                            track = result.FirstOrDefault();
                        }

                        return track;
                    }
                }, artist)
                .ContinueWith(task =>
                {
                    if (task.Exception != null)
                    {
                        _logger.Log(task.Exception.ToString(), Category.Exception, Priority.Medium);
                        _toastService.Show("Unable to play track");
                    }

                    return task.Result;
                });
        }

        #endregion Methods
    }
}