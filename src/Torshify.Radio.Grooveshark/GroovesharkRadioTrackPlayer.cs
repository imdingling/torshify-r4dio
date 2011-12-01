﻿using System;
using System.ComponentModel.Composition;
using System.IO;
using System.Net;
using System.Threading;
using System.Timers;

using Grooveshark_Sharp;

using Microsoft.Practices.Prism.Logging;

using NAudio.Wave;

using Torshify.Radio.Framework;
using Torshify.Radio.Grooveshark.NAudio;

using Timer = System.Timers.Timer;

namespace Torshify.Radio.Grooveshark
{
    [RadioTrackPlayerMetadata(Name = "Grooveshark", Icon = "pack://siteoforigin:,,,/Resources/Icons/Grooveshark_Logo.png")]
    public class GroovesharkRadioTrackPlayer : IRadioTrackPlayer
    {
        #region Fields

        private readonly ILoggerFacade _log;

        private BufferedWaveProvider _bufferedWaveProvider;
        private Thread _bufferThread;
        private GroovesharkRadioTrack _currentTrack;
        private TimeSpan _elapsedTimeSpan;
        private volatile bool _fullyDownloaded;
        private bool _isPlaying;
        private volatile StreamingPlaybackState _playbackState;
        private Timer _timer;
        private float _volume = 0.5f;
        private VolumeWaveProvider16 _volumeProvider;
        private IWavePlayer _waveOut;
        private HttpWebRequest _webRequest;

        #endregion Fields

        #region Constructors

        [ImportingConstructor]
        public GroovesharkRadioTrackPlayer(ILoggerFacade log)
        {
            _log = log;
            _timer = new Timer(250);
            _timer.Elapsed += OnTimerElapsed;
        }

        #endregion Constructors

        #region Enumerations

        enum StreamingPlaybackState
        {
            Stopped,
            Playing,
            Buffering,
            Paused
        }

        #endregion Enumerations

        #region Events

        public event EventHandler IsPlayingChanged;

        public event EventHandler<TrackEventArgs> TrackComplete;

        public event EventHandler<TrackProgressEventArgs> TrackProgress;

        #endregion Events

        #region Properties

        public bool IsPlaying
        {
            get { return _isPlaying; }
            set
            {
                if (_isPlaying != value)
                {
                    _isPlaying = value;
                    OnIsPlayingChanged();
                }
            }
        }

        public float Volume
        {
            get { return _volume; }
            set
            {
                _volume = value;

                if (_volumeProvider != null)
                {
                    _volumeProvider.Volume = value;
                }
            }
        }

        #endregion Properties

        #region Methods

        public bool CanPlay(RadioTrack track)
        {
            return track is GroovesharkRadioTrack;
        }

        public void Initialize()
        {
        }

        public void Load(RadioTrack track)
        {
            var currentTrack = track as GroovesharkRadioTrack;

            if (currentTrack != null && GroovesharkRadioTrackSource.Session != null)
            {
                try
                {
                    var streaming = new GroovesharkStreaming(GroovesharkRadioTrackSource.Session);
                    var key = streaming.GetStreamingKey(currentTrack.SongID);

                    if (key == null)
                    {
                        _elapsedTimeSpan = TimeSpan.Zero;
                        IsPlaying = false;
                        OnTrackComplete(_currentTrack);
                    }
                    else
                    {
                        var url = streaming.GetStreamingUrl(key);
                        _currentTrack = currentTrack;
                        _playbackState = StreamingPlaybackState.Buffering;
                        _bufferedWaveProvider = null;

                        _bufferThread = new Thread(StreamMp3);
                        _bufferThread.IsBackground = true;
                        _bufferThread.Start(url);
                    }
                }
                catch (Exception e)
                {
                    _log.Log(e.ToString(), Category.Exception, Priority.High);
                    _elapsedTimeSpan = TimeSpan.Zero;
                    IsPlaying = false;
                    OnTrackComplete(_currentTrack);
                }
            }
        }

        public void Pause()
        {
            if (_waveOut != null)
            {
                _waveOut.Pause();
                IsPlaying = false;
                _playbackState = StreamingPlaybackState.Paused;
            }
        }

        public void Play()
        {
            if (_currentTrack == null || IsPlaying)
                return;

            if (!IsPlaying)
            {
                if (_waveOut != null)
                {
                    _waveOut.Play();
                    IsPlaying = true;
                    _playbackState = StreamingPlaybackState.Playing;
                }

                _timer.Enabled = true;
            }
        }

        public void Stop()
        {
            try
            {
                _timer.Enabled = false;
                _playbackState = StreamingPlaybackState.Stopped;

                if (_fullyDownloaded)
                {
                    _webRequest.Abort();
                }

                if (_waveOut != null)
                {
                    _waveOut.Stop();
                    _waveOut.Dispose();
                    _waveOut = null;
                }

                if (_bufferThread != null && _bufferThread.IsAlive)
                {
                    _bufferThread.Join(1000);
                }
            }
            catch (Exception e)
            {
                _log.Log("Error while stopping Grooveshark player. " + e.Message, Category.Info, Priority.Medium);
            }
            finally
            {
                IsPlaying = false;
                _timer.Stop();
                _currentTrack = null;
                _elapsedTimeSpan = TimeSpan.Zero;
            }
        }

        protected virtual void OnTrackComplete(RadioTrack currentTrack)
        {
            var handler = TrackComplete;

            if (handler != null)
            {
                handler(this, new TrackEventArgs(this, currentTrack));
            }
        }

        protected virtual void OnTrackProgress(double totalMilliseconds, double elapsedMilliseconds)
        {
            var handler = TrackProgress;

            if (handler != null)
            {
                handler(this, new TrackProgressEventArgs(this, totalMilliseconds, elapsedMilliseconds));
            }
        }

        private void OnIsPlayingChanged()
        {
            var handler = IsPlayingChanged;

            if (handler != null)
            {
                handler(this, EventArgs.Empty);
            }
        }

        private void OnTimerElapsed(object sender, ElapsedEventArgs e)
        {
            if (_playbackState != StreamingPlaybackState.Stopped)
            {
                if (_waveOut == null && _bufferedWaveProvider != null)
                {
                    _log.Log("Initializing grooveshark player", Category.Info, Priority.Medium);

                    try
                    {
                        _waveOut = new WaveOut();
                        _volumeProvider = new VolumeWaveProvider16(_bufferedWaveProvider);
                        _volumeProvider.Volume = Volume;
                        _waveOut.Init(_volumeProvider);
                    }
                    catch (Exception ex)
                    {
                        _log.Log(ex.ToString(), Category.Exception, Priority.High);
                        _elapsedTimeSpan = TimeSpan.Zero;
                        IsPlaying = false;
                        OnTrackComplete(_currentTrack);
                    }
                }
                else if (_bufferedWaveProvider != null)
                {
                    var bufferedSeconds = _bufferedWaveProvider.BufferedDuration.TotalSeconds;
                    // make it stutter less if we buffer up a decent amount before playing
                    if (bufferedSeconds < 0.5 && _playbackState == StreamingPlaybackState.Playing && !_fullyDownloaded)
                    {
                        _log.Log("Buffering..", Category.Info, Priority.Medium);

                        _playbackState = StreamingPlaybackState.Buffering;

                        if (_waveOut != null)
                        {
                            _waveOut.Pause();
                            IsPlaying = false;
                        }
                    }
                    else if (bufferedSeconds > 4 && _playbackState == StreamingPlaybackState.Buffering)
                    {
                        _log.Log("Buffering complete", Category.Info, Priority.Medium);

                        if (_waveOut != null)
                        {
                            _waveOut.Play();
                            _playbackState = StreamingPlaybackState.Playing;
                            IsPlaying = true;
                        }
                    }
                    else if (_fullyDownloaded && bufferedSeconds < 0.5)
                    {
                        _log.Log("Buffer empty and the stream is fully downloaded. Complete..", Category.Info, Priority.Medium);
                        _elapsedTimeSpan = TimeSpan.Zero;
                        IsPlaying = false;
                        OnTrackComplete(_currentTrack);
                    }
                }

                if (_playbackState == StreamingPlaybackState.Playing)
                {
                    _elapsedTimeSpan = _elapsedTimeSpan.Add(TimeSpan.FromMilliseconds(_timer.Interval));
                    OnTrackProgress(_currentTrack.TotalDuration.TotalMilliseconds, _elapsedTimeSpan.TotalMilliseconds);
                }
            }
        }

        private void StreamMp3(object state)
        {
            string url = (string)state;

            _fullyDownloaded = false;
            _webRequest = (HttpWebRequest)WebRequest.Create(url);
            HttpWebResponse resp = null;

            try
            {
                resp = (HttpWebResponse)_webRequest.GetResponse();
            }
            catch (WebException e)
            {
                if (e.Status != WebExceptionStatus.RequestCanceled)
                {
                    Console.WriteLine(e);
                }
                return;
            }
            byte[] buffer = new byte[16384 * 4]; // needs to be big enough to hold a decompressed frame

            IMp3FrameDecompressor decompressor = null;
            try
            {
                using (var responseStream = resp.GetResponseStream())
                {
                    var readFullyStream = new ReadFullyStream(responseStream);
                    do
                    {
                        if (_bufferedWaveProvider != null && _bufferedWaveProvider.BufferLength - _bufferedWaveProvider.BufferedBytes < _bufferedWaveProvider.WaveFormat.AverageBytesPerSecond / 4)
                        {
                            Thread.Sleep(500);
                        }
                        else
                        {
                            Mp3Frame frame = null;
                            try
                            {
                                frame = Mp3Frame.LoadFromStream(readFullyStream);
                            }
                            catch (EndOfStreamException e)
                            {
                                _log.Log(e.Message, Category.Warn, Priority.Medium);
                                _fullyDownloaded = true;
                                // reached the end of the MP3 file / stream
                                break;
                            }
                            catch (WebException e)
                            {
                                _log.Log(e.Message, Category.Warn, Priority.Medium);
                                // probably we have aborted download from the GUI thread
                                break;
                            }
                            catch (Exception e)
                            {
                                _log.Log(e.Message, Category.Exception, Priority.High);
                            }

                            if (frame != null && decompressor == null)
                            {
                                // don't think these details matter too much - just help ACM select the right codec
                                // however, the buffered provider doesn't know what sample rate it is working at
                                // until we have a frame
                                //WaveFormat waveFormat = new Mp3WaveFormat(frame.SampleRate, frame.ChannelMode == ChannelMode.Mono ? 1 : 2, frame.FrameLength, frame.BitRate);
                                WaveFormat waveFormat = new Mp3WaveFormat(44100, frame.ChannelMode == ChannelMode.Mono ? 1 : 2, frame.FrameLength, frame.BitRate);
                                decompressor = new AcmMp3FrameDecompressor(waveFormat);
                                _bufferedWaveProvider = new BufferedWaveProvider(decompressor.OutputFormat);
                                //_bufferedWaveProvider.BufferDuration = TimeSpan.FromSeconds(20); // allow us to get well ahead of ourselves
                                _bufferedWaveProvider.BufferLength = (int)resp.ContentLength;

                            }

                            if (frame != null && _bufferedWaveProvider != null)
                            {
                                try
                                {
                                    int decompressed = decompressor.DecompressFrame(frame, buffer, 0);
                                    //Debug.WriteLine(String.Format("Decompressed a frame {0}", decompressed));
                                    _bufferedWaveProvider.AddSamples(buffer, 0, decompressed);
                                }
                                catch (Exception e)
                                {
                                    _fullyDownloaded = true;
                                    _log.Log("Error decompressing frame: " + e.Message, Category.Exception, Priority.Medium);
                                    break;
                                }
                            }
                            else
                            {
                                _fullyDownloaded = true;
                                break;
                            }
                        }

                    } while (_playbackState != StreamingPlaybackState.Stopped);

                    // was doing this in a finally block, but for some reason
                    // we are hanging on response stream .Dispose so never get there
                    if (decompressor != null)
                    {
                        decompressor.Dispose();
                        decompressor = null;
                    }
                }
            }
            finally
            {
                if (decompressor != null)
                {
                    decompressor.Dispose();
                    decompressor = null;
                }
            }

            _log.Log("Buffer thread exiting", Category.Info, Priority.Medium);

        }

        #endregion Methods
    }
}