using System;
using System.IO;
using System.Net;
using System.Threading;
using System.Timers;

using Microsoft.Practices.Prism.Logging;

using NAudio.Wave;
using Torshify.Radio.Framework;
using Torshify.Radio.Grooveshark.NAudio;

using Timer = System.Timers.Timer;

namespace Torshify.Radio.Grooveshark
{
    /// <summary>
    /// Class for streaming data with throttling support.
    /// </summary>
    public class ThrottledStream : Stream
    {
        /// <summary>
        /// A constant used to specify an infinite number of bytes that can be transferred per second.
        /// </summary>
        public const long Infinite = 0;

        #region Private members
        /// <summary>
        /// The base stream.
        /// </summary>
        private Stream _baseStream;

        /// <summary>
        /// The maximum bytes per second that can be transferred through the base stream.
        /// </summary>
        private long _maximumBytesPerSecond;

        /// <summary>
        /// The number of bytes that has been transferred since the last throttle.
        /// </summary>
        private long _byteCount;

        /// <summary>
        /// The start time in milliseconds of the last throttle.
        /// </summary>
        private long _start;

        #endregion

        #region Properties
        /// <summary>
        /// Gets the current milliseconds.
        /// </summary>
        /// <value>The current milliseconds.</value>
        protected long CurrentMilliseconds
        {
            get
            {
                return Environment.TickCount;
            }
        }

        /// <summary>
        /// Gets or sets the maximum bytes per second that can be transferred through the base stream.
        /// </summary>
        /// <value>The maximum bytes per second.</value>
        public long MaximumBytesPerSecond
        {
            get
            {
                return _maximumBytesPerSecond;
            }
            set
            {
                if (MaximumBytesPerSecond != value)
                {
                    _maximumBytesPerSecond = value;
                    Reset();
                }
            }
        }

        /// <summary>
        /// Gets a value indicating whether the current stream supports reading.
        /// </summary>
        /// <returns>true if the stream supports reading; otherwise, false.</returns>
        public override bool CanRead
        {
            get
            {
                return _baseStream.CanRead;
            }
        }

        /// <summary>
        /// Gets a value indicating whether the current stream supports seeking.
        /// </summary>
        /// <value></value>
        /// <returns>true if the stream supports seeking; otherwise, false.</returns>
        public override bool CanSeek
        {
            get
            {
                return _baseStream.CanSeek;
            }
        }

        /// <summary>
        /// Gets a value indicating whether the current stream supports writing.
        /// </summary>
        /// <value></value>
        /// <returns>true if the stream supports writing; otherwise, false.</returns>
        public override bool CanWrite
        {
            get
            {
                return _baseStream.CanWrite;
            }
        }

        /// <summary>
        /// Gets the length in bytes of the stream.
        /// </summary>
        /// <value></value>
        /// <returns>A long value representing the length of the stream in bytes.</returns>
        /// <exception cref="T:System.NotSupportedException">The base stream does not support seeking. </exception>
        /// <exception cref="T:System.ObjectDisposedException">Methods were called after the stream was closed. </exception>
        public override long Length
        {
            get
            {
                return _baseStream.Length;
            }
        }

        /// <summary>
        /// Gets or sets the position within the current stream.
        /// </summary>
        /// <value></value>
        /// <returns>The current position within the stream.</returns>
        /// <exception cref="T:System.IO.IOException">An I/O error occurs. </exception>
        /// <exception cref="T:System.NotSupportedException">The base stream does not support seeking. </exception>
        /// <exception cref="T:System.ObjectDisposedException">Methods were called after the stream was closed. </exception>
        public override long Position
        {
            get
            {
                return _baseStream.Position;
            }
            set
            {
                _baseStream.Position = value;
            }
        }
        #endregion

        #region Ctor
        /// <summary>
        /// Initializes a new instance of the <see cref="T:ThrottledStream"/> class with an
        /// infinite amount of bytes that can be processed.
        /// </summary>
        /// <param name="baseStream">The base stream.</param>
        public ThrottledStream(Stream baseStream)
            : this(baseStream, ThrottledStream.Infinite)
        {
            // Nothing todo.
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:ThrottledStream"/> class.
        /// </summary>
        /// <param name="baseStream">The base stream.</param>
        /// <param name="maximumBytesPerSecond">The maximum bytes per second that can be transferred through the base stream.</param>
        /// <exception cref="ArgumentNullException">Thrown when <see cref="baseStream"/> is a null reference.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when <see cref="maximumBytesPerSecond"/> is a negative value.</exception>
        public ThrottledStream(Stream baseStream, long maximumBytesPerSecond)
        {
            if (baseStream == null)
            {
                throw new ArgumentNullException("baseStream");
            }

            if (maximumBytesPerSecond < 0)
            {
                throw new ArgumentOutOfRangeException("maximumBytesPerSecond",
                    maximumBytesPerSecond, "The maximum number of bytes per second can't be negatie.");
            }

            _baseStream = baseStream;
            _maximumBytesPerSecond = maximumBytesPerSecond;
            _start = CurrentMilliseconds;
            _byteCount = 0;
        }
        #endregion

        #region Public methods
        /// <summary>
        /// Clears all buffers for this stream and causes any buffered data to be written to the underlying device.
        /// </summary>
        /// <exception cref="T:System.IO.IOException">An I/O error occurs.</exception>
        public override void Flush()
        {
            _baseStream.Flush();
        }

        /// <summary>
        /// Reads a sequence of bytes from the current stream and advances the position within the stream by the number of bytes read.
        /// </summary>
        /// <param name="buffer">An array of bytes. When this method returns, the buffer contains the specified byte array with the values between offset and (offset + count - 1) replaced by the bytes read from the current source.</param>
        /// <param name="offset">The zero-based byte offset in buffer at which to begin storing the data read from the current stream.</param>
        /// <param name="count">The maximum number of bytes to be read from the current stream.</param>
        /// <returns>
        /// The total number of bytes read into the buffer. This can be less than the number of bytes requested if that many bytes are not currently available, or zero (0) if the end of the stream has been reached.
        /// </returns>
        /// <exception cref="T:System.ArgumentException">The sum of offset and count is larger than the buffer length. </exception>
        /// <exception cref="T:System.ObjectDisposedException">Methods were called after the stream was closed. </exception>
        /// <exception cref="T:System.NotSupportedException">The base stream does not support reading. </exception>
        /// <exception cref="T:System.ArgumentNullException">buffer is null. </exception>
        /// <exception cref="T:System.IO.IOException">An I/O error occurs. </exception>
        /// <exception cref="T:System.ArgumentOutOfRangeException">offset or count is negative. </exception>
        public override int Read(byte[] buffer, int offset, int count)
        {
            Throttle(count);

            return _baseStream.Read(buffer, offset, count);
        }

        /// <summary>
        /// Sets the position within the current stream.
        /// </summary>
        /// <param name="offset">A byte offset relative to the origin parameter.</param>
        /// <param name="origin">A value of type <see cref="T:System.IO.SeekOrigin"></see> indicating the reference point used to obtain the new position.</param>
        /// <returns>
        /// The new position within the current stream.
        /// </returns>
        /// <exception cref="T:System.IO.IOException">An I/O error occurs. </exception>
        /// <exception cref="T:System.NotSupportedException">The base stream does not support seeking, such as if the stream is constructed from a pipe or console output. </exception>
        /// <exception cref="T:System.ObjectDisposedException">Methods were called after the stream was closed. </exception>
        public override long Seek(long offset, SeekOrigin origin)
        {
            return _baseStream.Seek(offset, origin);
        }

        /// <summary>
        /// Sets the length of the current stream.
        /// </summary>
        /// <param name="value">The desired length of the current stream in bytes.</param>
        /// <exception cref="T:System.NotSupportedException">The base stream does not support both writing and seeking, such as if the stream is constructed from a pipe or console output. </exception>
        /// <exception cref="T:System.IO.IOException">An I/O error occurs. </exception>
        /// <exception cref="T:System.ObjectDisposedException">Methods were called after the stream was closed. </exception>
        public override void SetLength(long value)
        {
            _baseStream.SetLength(value);
        }

        /// <summary>
        /// Writes a sequence of bytes to the current stream and advances the current position within this stream by the number of bytes written.
        /// </summary>
        /// <param name="buffer">An array of bytes. This method copies count bytes from buffer to the current stream.</param>
        /// <param name="offset">The zero-based byte offset in buffer at which to begin copying bytes to the current stream.</param>
        /// <param name="count">The number of bytes to be written to the current stream.</param>
        /// <exception cref="T:System.IO.IOException">An I/O error occurs. </exception>
        /// <exception cref="T:System.NotSupportedException">The base stream does not support writing. </exception>
        /// <exception cref="T:System.ObjectDisposedException">Methods were called after the stream was closed. </exception>
        /// <exception cref="T:System.ArgumentNullException">buffer is null. </exception>
        /// <exception cref="T:System.ArgumentException">The sum of offset and count is greater than the buffer length. </exception>
        /// <exception cref="T:System.ArgumentOutOfRangeException">offset or count is negative. </exception>
        public override void Write(byte[] buffer, int offset, int count)
        {
            Throttle(count);

            _baseStream.Write(buffer, offset, count);
        }

        /// <summary>
        /// Returns a <see cref="T:System.String"></see> that represents the current <see cref="T:System.Object"></see>.
        /// </summary>
        /// <returns>
        /// A <see cref="T:System.String"></see> that represents the current <see cref="T:System.Object"></see>.
        /// </returns>
        public override string ToString()
        {
            return _baseStream.ToString();
        }
        #endregion

        public long CurrentBytesPerSeconds
        {
            get
            {
                long elapsedMilliseconds = CurrentMilliseconds - _start;

                if (elapsedMilliseconds > 0)
                {
                    return _byteCount * 1000L / (elapsedMilliseconds);
                }

                return 0;
            }
        }

        #region Protected methods
        /// <summary>
        /// Throttles for the specified buffer size in bytes.
        /// </summary>
        /// <param name="bufferSizeInBytes">The buffer size in bytes.</param>
        protected void Throttle(int bufferSizeInBytes)
        {
            // Make sure the buffer isn't empty.
            if (_maximumBytesPerSecond <= 0 || bufferSizeInBytes <= 0)
            {
                return;
            }

            _byteCount += bufferSizeInBytes;
            long elapsedMilliseconds = CurrentMilliseconds - _start;

            if (elapsedMilliseconds > 0)
            {
                // Calculate the current bps.
                long bps = _byteCount * 1000L / elapsedMilliseconds;

                // If the bps are more then the maximum bps, try to throttle.
                if (bps > _maximumBytesPerSecond)
                {
                    // Calculate the time to sleep.
                    long wakeElapsed = _byteCount * 1000L / _maximumBytesPerSecond;
                    int toSleep = (int)(wakeElapsed - elapsedMilliseconds);

                    if (toSleep > 1)
                    {
                        try
                        {
                            // The time to sleep is more then a millisecond, so sleep.
                            Thread.Sleep(toSleep);
                        }
                        catch (ThreadAbortException)
                        {
                            // Eatup ThreadAbortException.
                        }

                        // A sleep has been done, reset.
                        Reset();
                    }
                }
            }
        }

        /// <summary>
        /// Will reset the bytecount to 0 and reset the start time to the current time.
        /// </summary>
        protected void Reset()
        {
            long difference = CurrentMilliseconds - _start;

            // Only reset counters when a known history is available of more then 1 second.
            if (difference > 1000)
            {
                _byteCount = 0;
                _start = CurrentMilliseconds;
            }
        }
        #endregion
    }

    public class GrooveSharkTrackPlayerHandler
    {
        #region Fields

        private readonly Action<bool> _isBuffering;
        private readonly Action<bool> _isPlaying;
        private readonly ILoggerFacade _log;
        private readonly GroovesharkTrack _track;
        private readonly Action<Track> _trackComplete;
        private readonly Action<double, double> _trackProgress;

        private BufferedWaveProvider _bufferedWaveProvider;
        private Thread _bufferThread;
        private TimeSpan _elapsedTimeSpan;
        private bool _fullyDownloaded;
        private StreamingPlaybackState _playbackState;
        private Timer _timer;
        private float _volume;
        private VolumeWaveProvider16 _volumeProvider;
        private WaveOut _waveOut;

        #endregion Fields

        #region Constructors

        public GrooveSharkTrackPlayerHandler(
            ILoggerFacade log,
            GroovesharkTrack track,
            Action<Track> trackComplete,
            Action<bool> isPlaying,
            Action<bool> isBuffering,
            Action<double, double> trackProgress)
        {
            _log = log;
            _track = track;
            _trackComplete = trackComplete;
            _isPlaying = isPlaying;
            _isBuffering = isBuffering;
            _trackProgress = trackProgress;

            Volume = 0.5f;
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

        #region Properties

        public float Volume
        {
            get
            {
                return _volume;
            }
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

        public void Initialize(Stream stream)
        {
            _timer = new Timer(250);
            _timer.Elapsed += OnTimerElapsed;
            _timer.Start();

            _bufferThread = new Thread(StreamMp3);
            _bufferThread.IsBackground = true;
            _bufferThread.Start(new ThrottledStream(stream, 1024 * 1024));
        }

        public void Pause()
        {
            if (_waveOut != null)
            {
                _waveOut.Pause();
                _isPlaying(false);
                _playbackState = StreamingPlaybackState.Paused;
            }
        }

        public void Play()
        {
            if (_playbackState == StreamingPlaybackState.Paused)
            {
                if (_waveOut != null)
                {
                    _waveOut.Play();
                    _isPlaying(true);
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
                _timer.Dispose();
                _playbackState = StreamingPlaybackState.Stopped;

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
                _log.Log("Grooveshark: Error while stopping player. " + e.Message, Category.Info, Priority.Medium);
            }
            finally
            {
                _isPlaying(false);
                _elapsedTimeSpan = TimeSpan.Zero;
            }
        }

        private void OnTimerElapsed(object sender, ElapsedEventArgs e)
        {
            if (_playbackState != StreamingPlaybackState.Stopped)
            {
                if (_waveOut == null && _bufferedWaveProvider != null)
                {
                    _log.Log("Grooveshark: Initializing", Category.Info, Priority.Medium);

                    try
                    {
                        _waveOut = new WaveOut();
                        _volumeProvider = new VolumeWaveProvider16(_bufferedWaveProvider);
                        _volumeProvider.Volume = Volume;
                        _waveOut.Init(_volumeProvider);
                    }
                    catch (Exception ex)
                    {
                        _log.Log("Grooveshark: " + ex.ToString(), Category.Exception, Priority.High);
                        _elapsedTimeSpan = TimeSpan.Zero;
                        _playbackState = StreamingPlaybackState.Stopped;
                        _timer.Stop();
                        _isPlaying(false);
                        _trackComplete(_track);
                    }
                }
                else if (_bufferedWaveProvider != null)
                {
                    var bufferedSeconds = _bufferedWaveProvider.BufferedDuration.TotalSeconds;
                    // make it stutter less if we buffer up a decent amount before playing
                    if (bufferedSeconds < 0.5 && _playbackState == StreamingPlaybackState.Playing && !_fullyDownloaded)
                    {
                        _isBuffering(true);

                        _log.Log("Grooveshark: Buffering..", Category.Info, Priority.Medium);

                        _playbackState = StreamingPlaybackState.Buffering;

                        if (_waveOut != null)
                        {
                            _waveOut.Pause();
                            _isPlaying(false);
                        }
                    }
                    else if (bufferedSeconds > 4 && _playbackState == StreamingPlaybackState.Buffering)
                    {
                        _log.Log("Grooveshark: Buffering complete", Category.Info, Priority.Medium);

                        if (_waveOut != null)
                        {
                            _waveOut.Play();
                            _playbackState = StreamingPlaybackState.Playing;
                            _isPlaying(true);
                        }

                        _isBuffering(false);
                    }
                    else if (_fullyDownloaded && bufferedSeconds < 0.5)
                    {
                        _log.Log("Grooveshark: Buffer empty and the stream is fully downloaded. Complete..", Category.Info, Priority.Medium);
                        _elapsedTimeSpan = TimeSpan.Zero;
                        _isPlaying(false);
                        _playbackState = StreamingPlaybackState.Stopped;
                        _timer.Stop();
                        _trackComplete(_track);
                    }
                }

                if (_playbackState == StreamingPlaybackState.Playing)
                {
                    _elapsedTimeSpan = _elapsedTimeSpan.Add(TimeSpan.FromMilliseconds(_timer.Interval));
                    _trackProgress(_track.TotalDuration.TotalMilliseconds, _elapsedTimeSpan.TotalMilliseconds);
                }
            }
        }

        private void StreamMp3(object state)
        {
            ThrottledStream responseStream = (ThrottledStream)state;
            byte[] buffer = new byte[16384 * 4]; // needs to be big enough to hold a decompressed frame

            IMp3FrameDecompressor decompressor = null;
            try
            {
                using (responseStream)
                {
                    _playbackState = StreamingPlaybackState.Buffering;

                    using (var readFullyStream = new ReadFullyStream(responseStream))
                    {
                        do
                        {
                            if (_bufferedWaveProvider != null &&
                                _bufferedWaveProvider.BufferLength - _bufferedWaveProvider.BufferedBytes <
                                _bufferedWaveProvider.WaveFormat.AverageBytesPerSecond / 4)
                            {
                                Thread.Sleep(500);
                            }

                            Mp3Frame frame;

                            try
                            {
                                frame = Mp3Frame.LoadFromStream(readFullyStream);

                                if (frame == null)
                                {
                                    _fullyDownloaded = true;
                                    break;
                                }
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
                                break;
                            }

                            if (decompressor == null)
                            {
                                WaveFormat waveFormat = new Mp3WaveFormat(44100,
                                                                          frame.ChannelMode == ChannelMode.Mono
                                                                              ? 1
                                                                              : 2, frame.FrameLength, frame.BitRate);
                                decompressor = new AcmMp3FrameDecompressor(waveFormat);
                                _bufferedWaveProvider = new BufferedWaveProvider(decompressor.OutputFormat);

                                if (_track.TotalDuration != TimeSpan.Zero)
                                {
                                    _bufferedWaveProvider.BufferDuration = _track.TotalDuration;
                                }
                                else
                                {
                                    _bufferedWaveProvider.BufferDuration = TimeSpan.FromSeconds(25);
                                }

                                responseStream.MaximumBytesPerSecond = _bufferedWaveProvider.WaveFormat.AverageBytesPerSecond / 4;
                            }

                            if (_bufferedWaveProvider != null)
                            {
                                try
                                {
                                    int decompressed = decompressor.DecompressFrame(frame, buffer, 0);
                                    _bufferedWaveProvider.AddSamples(buffer, 0, decompressed);
                                }
                                catch (Exception e)
                                {
                                    _fullyDownloaded = true;
                                    _log.Log("Grooveshark: Error decompressing frame: " + e.Message, Category.Exception, Priority.Medium);
                                    break;
                                }

                                _log.Log(_bufferedWaveProvider.BufferedDuration.TotalSeconds + " sec buff / " + responseStream.CurrentBytesPerSeconds + "bps", Category.Info, Priority.Low);
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
            }
            finally
            {
                if (decompressor != null)
                {
                    decompressor.Dispose();
                    decompressor = null;
                }
            }

            _log.Log("Grooveshark: Buffer thread exiting", Category.Info, Priority.Medium);
        }

        #endregion Methods
    }
}