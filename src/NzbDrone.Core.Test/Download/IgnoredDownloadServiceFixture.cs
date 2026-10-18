using System.Collections.Generic;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Core.Download;
using NzbDrone.Core.Download.TrackedDownloads;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.Music;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Qualities;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Test.Common;

namespace NzbDrone.Core.Test.Download
{
    [TestFixture]
    public class IgnoredDownloadServiceFixture : CoreTest<IgnoredDownloadService>
    {
        private TrackedDownload GivenTrackedDownload(RemoteAlbum remoteAlbum)
        {
            return new TrackedDownload
            {
                DownloadItem = new DownloadClientItem { Title = "Unmatched.Release-GROUP", DownloadId = "abc" },
                RemoteAlbum = remoteAlbum
            };
        }

        [Test]
        public void should_not_throw_when_the_download_was_not_matched_to_an_album()
        {
            Subject.IgnoreDownload(GivenTrackedDownload(null)).Should().BeFalse();

            Mocker.GetMock<IEventAggregator>().Verify(v => v.PublishEvent(It.IsAny<DownloadIgnoredEvent>()), Times.Never());
            ExceptionVerification.IgnoreWarns();
        }

        [Test]
        public void should_not_throw_when_albums_are_missing()
        {
            Subject.IgnoreDownload(GivenTrackedDownload(new RemoteAlbum { Artist = new Artist { Id = 1 }, Albums = null })).Should().BeFalse();

            ExceptionVerification.IgnoreWarns();
        }

        [Test]
        public void should_publish_ignored_event_for_a_matched_download()
        {
            var remoteAlbum = new RemoteAlbum
            {
                Artist = new Artist { Id = 1 },
                Albums = new List<Album> { new Album { Id = 2 } },
                ParsedAlbumInfo = new ParsedAlbumInfo { Quality = new QualityModel(Quality.FLAC) }
            };

            Subject.IgnoreDownload(GivenTrackedDownload(remoteAlbum)).Should().BeTrue();

            Mocker.GetMock<IEventAggregator>().Verify(v => v.PublishEvent(It.Is<DownloadIgnoredEvent>(e => e.ArtistId == 1 && e.AlbumIds.Contains(2))), Times.Once());
        }
    }
}
