using System.Collections.Generic;

namespace Torshify.Radio.Framework
{
    public interface ITrackSource
    {
        #region Methods

        IEnumerable<Track> GetTracksByName(string name);

        IEnumerable<TrackContainer> GetAlbumsByArtist(string artist);

        #endregion Methods
    }
}