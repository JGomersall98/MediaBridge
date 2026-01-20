using MediaBridge.Configuration;
using MediaBridge.Database;
using MediaBridge.Database.DB_Models;
using MediaBridge.Models;
using MediaBridge.Models.Media.ExternalServices.Radarr;
using MediaBridge.Models.Media.ExternalServices.Sonarr;
using MediaBridge.Services.Helpers;
using MediaBridge.Services.Media;
using MediaBridge.Services.Media.ExternalServices.Radarr;
using MediaBridge.Services.Media.ExternalServices.Sonarr;
using MediaBridge.Tests.TestHelpers;
using Microsoft.Extensions.Options;
using Moq;

namespace MediaBridge.Tests.Integration
{
    [TestClass]
    public class MediaServiceIntegrationTests
    {
        private Mock<IHttpClientService> _mockHttpClient = null!;
        private Mock<MediaBridgeDbContext> _mockDbContext = null!;
        private Mock<IRadarrService> _mockRadarrService = null!;
        private Mock<ISonarrService> _mockSonarrService = null!;
        private Mock<IGetConfig> _mockConfig = null!;
        private MediaService _mediaService = null!;

        // Configuration options
        private IOptions<RadarrOptions> _radarrOptions = null!;
        private IOptions<SonarrOptions> _sonarrOptions = null!;

        [TestInitialize]
        public void Setup()
        {
            // Setup mocks
            _mockHttpClient = new Mock<IHttpClientService>();
            _mockDbContext = TestDbContextFactory.CreateMockDbContext();
            _mockRadarrService = new Mock<IRadarrService>();
            _mockSonarrService = new Mock<ISonarrService>();
            _mockConfig = new Mock<IGetConfig>();

            // Setup configuration options
            _radarrOptions = Options.Create(new RadarrOptions
            {
                BaseUrl = TestDataBuilder.TestConstants.RadarrBaseUrl,
                ApiKey = TestDataBuilder.TestConstants.TestRadarrApiKey,
                Endpoints = new RadarrEndpointsOptions
                {
                    GetMovie = TestDataBuilder.TestConstants.RadarrGetMovieEndpoint,
                    AddMovie = TestDataBuilder.TestConstants.RadarrAddMovieEndpoint
                }
            });

            _sonarrOptions = Options.Create(new SonarrOptions
            {
                BaseUrl = TestDataBuilder.TestConstants.SonarrBaseUrl,
                ApiKey = TestDataBuilder.TestConstants.TestSonarrApiKey,
                Endpoints = new SonarrEndpointsOptions
                {
                    GetSeries = TestDataBuilder.TestConstants.SonarrGetSeriesEndpoint,
                    AddSeries = TestDataBuilder.TestConstants.SonarrAddSeriesEndpoint
                }
            });

            // Create MediaService
            _mediaService = new MediaService(
                _mockConfig.Object,
                _mockHttpClient.Object,
                _mockDbContext.Object,
                _mockRadarrService.Object,
                _mockSonarrService.Object);
        }

        #region Movie Download Tests

        /// <summary>
        /// Tests the successful download flow for a movie.
        /// </summary>
        /// <returns></returns>
        [TestMethod]
        public async Task DownloadMedia_Movie_Success_CompletesFullFlow()
        {
            // Arrange
            var movieDetails = TestDataBuilder.MovieResponses.GetMovieDetailsObject;

            _mockRadarrService.Setup(x => x.GetMovieDetails(TestDataBuilder.TestConstants.TestMovieTmdbId))
                .ReturnsAsync(movieDetails);

            _mockRadarrService.Setup(x => x.SendMovieRequest(movieDetails.Title, movieDetails.TmdbId))
                .ReturnsAsync(true);

            // Act
            var result = await _mediaService.DownloadMedia(
                TestDataBuilder.TestConstants.TestMovieTmdbId,
                TestDataBuilder.TestConstants.TestUserId,
                TestDataBuilder.TestConstants.TestUsername,
                null,
                "movie");

            // Assert
            Assert.IsTrue(result.IsSuccess);
            Assert.IsNull(result.Reason);

            // Verify Radarr service calls
            _mockRadarrService.Verify(x => x.GetMovieDetails(TestDataBuilder.TestConstants.TestMovieTmdbId), Times.Once);
            _mockRadarrService.Verify(x => x.SendMovieRequest(movieDetails.Title, movieDetails.TmdbId), Times.Once);

            // Verify database save was attempted
            _mockDbContext.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.AtLeastOnce);
        }

        /// <summary>
        /// Tests that a failure in the Radarr service during movie request results in a handled failure.
        /// </summary>
        /// <returns></returns>
        [TestMethod]
        public async Task DownloadMedia_Movie_RadarrServiceFailure_ReturnsFailure()
        {
            // Arrange
            var movieDetails = TestDataBuilder.MovieResponses.GetMovieDetailsObject;

            _mockRadarrService.Setup(x => x.GetMovieDetails(TestDataBuilder.TestConstants.TestMovieTmdbId))
                .ReturnsAsync(movieDetails);

            _mockRadarrService.Setup(x => x.SendMovieRequest(movieDetails.Title, movieDetails.TmdbId))
                .ReturnsAsync(false);

            // Act
            var result = await _mediaService.DownloadMedia(
                TestDataBuilder.TestConstants.TestMovieTmdbId,
                TestDataBuilder.TestConstants.TestUserId,
                TestDataBuilder.TestConstants.TestUsername,
                null,
                "movie");

            // Assert
            Assert.IsFalse(result.IsSuccess);
            Assert.IsNotNull(result.Reason);
            Assert.Contains("Failed sending movie request", result.Reason);

            // Verify service calls were made
            _mockRadarrService.Verify(x => x.GetMovieDetails(TestDataBuilder.TestConstants.TestMovieTmdbId), Times.Once);
            _mockRadarrService.Verify(x => x.SendMovieRequest(movieDetails.Title, movieDetails.TmdbId), Times.Once);
        }

        /// <summary>
        /// Tests that exceptions thrown during the movie download process are handled gracefully.
        /// </summary>
        /// <returns></returns>
        [TestMethod]
        public async Task DownloadMedia_Movie_ExceptionThrown_HandlesGracefully()
        {
            // Arrange
            var exceptionMessage = "Radarr service unavailable";

            _mockRadarrService.Setup(x => x.GetMovieDetails(TestDataBuilder.TestConstants.TestMovieTmdbId))
           .ThrowsAsync(new InvalidOperationException(exceptionMessage));

            // Act
            var result = await _mediaService.DownloadMedia(
                TestDataBuilder.TestConstants.TestMovieTmdbId,
                TestDataBuilder.TestConstants.TestUserId,
                TestDataBuilder.TestConstants.TestUsername,
                null,
                "movie");

            // Assert
            Assert.IsFalse(result.IsSuccess);
            Assert.IsNotNull(result.Reason);
            Assert.Contains("Exception occurred while sending movie request", result.Reason);
            Assert.Contains(exceptionMessage, result.Reason);

            // Verify service call was attempted
            _mockRadarrService.Verify(x => x.GetMovieDetails(TestDataBuilder.TestConstants.TestMovieTmdbId), Times.Once);
        }

        #endregion

        #region TV Show Download Tests

        /// <summary>
        /// Tests the successful download flow for a TV show.
        /// </summary>
        /// <returns></returns>
        [TestMethod]
        public async Task DownloadMedia_Show_Success_CompletesFullFlow()
        {
            // Arrange
            var showDetails = TestDataBuilder.ShowResponses.GetShowDetailsObject;
            var seasonsRequested = TestDataBuilder.TestConstants.TestSeasons;

            _mockSonarrService.Setup(x => x.GetShowDetails(TestDataBuilder.TestConstants.TestShowTvdbId))
                .ReturnsAsync(showDetails);

            _mockSonarrService.Setup(x => x.SendShowRequest(showDetails.TvdbId, showDetails.Title!, seasonsRequested))
                .ReturnsAsync(true);

            // Act
            var result = await _mediaService.DownloadMedia(
                TestDataBuilder.TestConstants.TestShowTvdbId,
                TestDataBuilder.TestConstants.TestUserId,
                TestDataBuilder.TestConstants.TestUsername,
                seasonsRequested,
                "show");

            // Assert
            Assert.IsTrue(result.IsSuccess);
            Assert.IsNull(result.Reason);

            // Verify Sonarr service calls
            _mockSonarrService.Verify(x => x.GetShowDetails(TestDataBuilder.TestConstants.TestShowTvdbId), Times.Once);
            _mockSonarrService.Verify(x => x.SendShowRequest(showDetails.TvdbId, showDetails.Title!, seasonsRequested), Times.Once);

            // Verify database save was attempted
            _mockDbContext.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.AtLeastOnce);
        }

        /// <summary>
        /// Tests that missing seasons parameter for show download results in a handled failure.
        /// </summary>
        /// <returns></returns>
        [TestMethod]
        public async Task DownloadMedia_Show_MissingSeasonsParameter_ReturnsFailure()
        {
            // Act
            var result = await _mediaService.DownloadMedia(
                TestDataBuilder.TestConstants.TestShowTvdbId,
                TestDataBuilder.TestConstants.TestUserId,
                TestDataBuilder.TestConstants.TestUsername,
                null, // Missing seasons parameter
                "show");

            // Assert
            Assert.IsFalse(result.IsSuccess);
            Assert.IsNotNull(result.Reason);
            Assert.Contains("seasonsRequested must be provided when mediaType is 'show'", result.Reason);

            // Verify no service calls were made
            _mockSonarrService.Verify(x => x.GetShowDetails(It.IsAny<int>()), Times.Never);
            _mockSonarrService.Verify(x => x.SendShowRequest(It.IsAny<int?>(), It.IsAny<string>(), It.IsAny<int[]>()), Times.Never);
        }

        /// <summary>
        /// Tests that a failure in the Sonarr service during show request results in a handled failure.
        /// </summary>
        /// <returns></returns>
        [TestMethod]
        public async Task DownloadMedia_Show_SonarrServiceFailure_ReturnsFailure()
        {
            // Arrange
            var showDetails = TestDataBuilder.ShowResponses.GetShowDetailsObject;
            var seasonsRequested = TestDataBuilder.TestConstants.TestSeasons;

            _mockSonarrService.Setup(x => x.GetShowDetails(TestDataBuilder.TestConstants.TestShowTvdbId))
                .ReturnsAsync(showDetails);

            _mockSonarrService.Setup(x => x.SendShowRequest(showDetails.TvdbId, showDetails.Title!, seasonsRequested))
                .ReturnsAsync(false);

            // Act
            var result = await _mediaService.DownloadMedia(
                TestDataBuilder.TestConstants.TestShowTvdbId,
                TestDataBuilder.TestConstants.TestUserId,
                TestDataBuilder.TestConstants.TestUsername,
                seasonsRequested,
                "show");

            // Assert
            Assert.IsFalse(result.IsSuccess);
            Assert.IsNotNull(result.Reason);
            Assert.Contains("Failed sending show request", result.Reason);

            // Verify service calls were made
            _mockSonarrService.Verify(x => x.GetShowDetails(TestDataBuilder.TestConstants.TestShowTvdbId), Times.Once);
            _mockSonarrService.Verify(x => x.SendShowRequest(showDetails.TvdbId, showDetails.Title!, seasonsRequested), Times.Once);
        }

        /// <summary>
        /// Tests that exceptions thrown during the show download process are handled gracefully.
        /// </summary>
        /// <returns></returns>
        [TestMethod]
        public async Task DownloadMedia_Show_ExceptionThrown_HandlesGracefully()
        {
            // Arrange
            var exceptionMessage = "Sonarr service unavailable";
            var seasonsRequested = TestDataBuilder.TestConstants.TestSeasons;

            _mockSonarrService.Setup(x => x.GetShowDetails(TestDataBuilder.TestConstants.TestShowTvdbId))
                .ThrowsAsync(new InvalidOperationException(exceptionMessage));

            // Act
            var result = await _mediaService.DownloadMedia(
                TestDataBuilder.TestConstants.TestShowTvdbId,
                TestDataBuilder.TestConstants.TestUserId,
                TestDataBuilder.TestConstants.TestUsername,
                seasonsRequested,
                "show");

            // Assert
            Assert.IsFalse(result.IsSuccess);
            Assert.IsNotNull(result.Reason);
            Assert.Contains("Exception occurred while sending show request", result.Reason);
            Assert.Contains(exceptionMessage, result.Reason);

            // Verify service call was attempted
            _mockSonarrService.Verify(x => x.GetShowDetails(TestDataBuilder.TestConstants.TestShowTvdbId), Times.Once);
        }

        #endregion

        #region Validation Tests

        /// <summary>
        /// Tests that providing an invalid media type results in a handled failure.
        /// </summary>
        /// <param name="mediaType"></param>
        /// <returns></returns>
        [TestMethod]
        [DataRow(null)]
        [DataRow("")]
        [DataRow("invalid")]
        public async Task DownloadMedia_InvalidMediaType_CompletesWithoutCrashing(string? mediaType)
        {
            // Act
            var result = await _mediaService.DownloadMedia(
                TestDataBuilder.TestConstants.TestMovieTmdbId,
                TestDataBuilder.TestConstants.TestUserId,
                TestDataBuilder.TestConstants.TestUsername,
                null,
                mediaType!);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsFalse(result.IsSuccess);
            Assert.Contains("Invalid mediaType", result.Reason!);

        }

        #endregion

        #region End-to-End Integration Tests
        /// <summary>
        /// End-to-end test for movie download flow integrating all services.
        /// </summary>
        /// <returns></returns>
        [TestMethod]
        public async Task EndToEnd_Movie_Success_FullServiceIntegration()
        {
            // Arrange
            var movieDetails = TestDataBuilder.MovieResponses.GetMovieDetailsObject;

            _mockRadarrService.Setup(x => x.GetMovieDetails(TestDataBuilder.TestConstants.TestMovieTmdbId))
                .ReturnsAsync(movieDetails);

            _mockRadarrService.Setup(x => x.SendMovieRequest(movieDetails.Title, movieDetails.TmdbId))
                .ReturnsAsync(true);

            // Act
            var result = await _mediaService.DownloadMedia(
                TestDataBuilder.TestConstants.TestMovieTmdbId,
                TestDataBuilder.TestConstants.TestUserId,
                TestDataBuilder.TestConstants.TestUsername,
                null,
                "movie");

            // Assert
            Assert.IsTrue(result.IsSuccess);
            Assert.IsNull(result.Reason);

            // Verify the complete call chain
            _mockRadarrService.Verify(x => x.GetMovieDetails(TestDataBuilder.TestConstants.TestMovieTmdbId), Times.Once);
            _mockRadarrService.Verify(x => x.SendMovieRequest(movieDetails.Title, movieDetails.TmdbId), Times.Once);
            _mockDbContext.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.AtLeastOnce);
        }

        /// <summary>
        /// End-to-end test for TV show download flow integrating all services.
        /// </summary>
        /// <returns></returns>
        [TestMethod]
        public async Task EndToEnd_Show_Success_FullServiceIntegration()
        {
            // Arrange
            var showDetails = TestDataBuilder.ShowResponses.GetShowDetailsObject;
            var seasonsRequested = TestDataBuilder.TestConstants.TestSeasons;

            _mockSonarrService.Setup(x => x.GetShowDetails(TestDataBuilder.TestConstants.TestShowTvdbId))
                .ReturnsAsync(showDetails);

            _mockSonarrService.Setup(x => x.SendShowRequest(showDetails.TvdbId, showDetails.Title!, seasonsRequested))
                .ReturnsAsync(true);

            // Act
            var result = await _mediaService.DownloadMedia(
                TestDataBuilder.TestConstants.TestShowTvdbId,
                TestDataBuilder.TestConstants.TestUserId,
                TestDataBuilder.TestConstants.TestUsername,
                seasonsRequested,
                "show");

            // Assert
            Assert.IsTrue(result.IsSuccess);
            Assert.IsNull(result.Reason);

            // Verify the complete call chain
            _mockSonarrService.Verify(x => x.GetShowDetails(TestDataBuilder.TestConstants.TestShowTvdbId), Times.Once);
            _mockSonarrService.Verify(x => x.SendShowRequest(showDetails.TvdbId, showDetails.Title!, seasonsRequested), Times.Once);
            _mockDbContext.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.AtLeastOnce);
        }

        #endregion
    }
}