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
using System.Text.Json;

namespace MediaBridge.Tests.Integration
{
    [TestClass]
    public class ExternalServicesIntegrationTests
    {
        private Mock<IHttpClientService> _mockHttpClient = null!;
        private Mock<MediaBridgeDbContext> _mockDbContext = null!;
        private Mock<IRadarrHttp> _mockRadarrHttp = null!;
        private Mock<ISonarrHttp> _mockSonarrHttp = null!;
        private Mock<IGetConfig> _mockConfig = null!;

        // Services
        private IRadarrService _radarrService = null!;
        private ISonarrService _sonarrService = null!;

        // Configuration options
        private IOptions<RadarrOptions> _radarrOptions = null!;
        private IOptions<SonarrOptions> _sonarrOptions = null!;

        [TestInitialize]
        public void Setup()
        {
            // Setup mocks
            _mockHttpClient = new Mock<IHttpClientService>();
            _mockDbContext = TestDbContextFactory.CreateMockDbContext();
            _mockRadarrHttp = new Mock<IRadarrHttp>();
            _mockSonarrHttp = new Mock<ISonarrHttp>();
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

            // Create services with mocked dependencies
            _radarrService = new RadarrService(_mockRadarrHttp.Object, _radarrOptions);
            _sonarrService = new SonarrService(_mockSonarrHttp.Object, _sonarrOptions);
        }

        #region Radarr Integration Tests

        /// <summary>
        /// Test to ensure that getting movie details is successful.
        /// </summary>
        /// <returns></returns>
        [TestMethod]
        public async Task RadarrService_GetMovieDetails_Success_ReturnsMovieDetails()
        {
            // Arrange
            var expectedResponse = TestDataBuilder.MovieResponses.GetMovieDetailsSuccess;

            _mockRadarrHttp.Setup(x => x.RadarrHttpGet(It.IsAny<string>(), TestDataBuilder.TestConstants.TestMovieTmdbId))
                .ReturnsAsync(TestDataBuilder.HttpResponses.SuccessResponse(expectedResponse));

            // Act
            var result = await _radarrService.GetMovieDetails(TestDataBuilder.TestConstants.TestMovieTmdbId);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("Test Movie", result.Title);
            Assert.AreEqual(12345, result.TmdbId);
            Assert.AreEqual("A test movie for integration testing", result.Description);
            Assert.AreEqual("https://example.com/poster.jpg", result.PosterUrl);
            Assert.AreEqual(2023, result.ReleaseYear);

            // Verify HTTP call was made
            _mockRadarrHttp.Verify(x => x.RadarrHttpGet(It.IsAny<string>(), TestDataBuilder.TestConstants.TestMovieTmdbId), Times.Once);
        }

        /// <summary>
        /// Test to ensure that sending a movie request is successful.
        /// </summary>
        /// <returns></returns>
        [TestMethod]
        public async Task RadarrService_SendMovieRequest_Success_ReturnsTrue()
        {
            // Arrange
            var addMovieResponse = TestDataBuilder.MovieResponses.AddMovieSuccess;

            _mockRadarrHttp.Setup(x => x.RadarrHttpPost(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(TestDataBuilder.HttpResponses.SuccessResponse(addMovieResponse));

            // Act
            var result = await _radarrService.SendMovieRequest(TestDataBuilder.TestConstants.TestMovieTitle, TestDataBuilder.TestConstants.TestMovieTmdbId);

            // Assert
            Assert.IsTrue(result);

            // Verify HTTP call was made
            _mockRadarrHttp.Verify(x => x.RadarrHttpPost(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }

        /// <summary>
        /// Test to ensure that sending a movie request handles HTTP failure gracefully.
        /// </summary>
        /// <returns></returns>
        [TestMethod]
        public async Task RadarrService_SendMovieRequest_HttpFailure_ReturnsFalse()
        {
            // Arrange
            _mockRadarrHttp.Setup(x => x.RadarrHttpPost(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(TestDataBuilder.HttpResponses.FailureResponse("Service unavailable"));

            // Act
            var result = await _radarrService.SendMovieRequest(TestDataBuilder.TestConstants.TestMovieTitle, TestDataBuilder.TestConstants.TestMovieTmdbId);

            // Assert
            Assert.IsFalse(result);
        }

        #endregion

        #region Sonarr Integration Tests

        /// <summary>
        /// Test to ensure that getting show details is successful.
        /// </summary>
        /// <returns></returns>
        [TestMethod]
        public async Task SonarrService_GetShowDetails_Success_ReturnsShowDetails()
        {
            // Arrange
            var expectedResponse = TestDataBuilder.ShowResponses.GetShowDetailsSuccess;

            _mockSonarrHttp.Setup(x => x.SonarrHttpGet(It.IsAny<string>(), TestDataBuilder.TestConstants.TestShowTvdbId))
                .ReturnsAsync(TestDataBuilder.HttpResponses.SuccessResponse(expectedResponse));

            // Act
            var result = await _sonarrService.GetShowDetails(TestDataBuilder.TestConstants.TestShowTvdbId);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("Test Show", result.Title);
            Assert.AreEqual(67890, result.TvdbId);
            Assert.AreEqual("A test TV show for integration testing", result.Description);
            Assert.AreEqual("https://example.com/show-poster.jpg", result.PosterUrl);
            Assert.AreEqual(2022, result.ReleaseYear);

            // Verify HTTP call was made
            _mockSonarrHttp.Verify(x => x.SonarrHttpGet(It.IsAny<string>(), TestDataBuilder.TestConstants.TestShowTvdbId), Times.Once);
        }

        /// <summary>
        /// Test to ensure that sending a show request is successful.
        /// </summary>
        /// <returns></returns>
        [TestMethod]
        public async Task SonarrService_SendShowRequest_Success_ReturnsTrue()
        {
            // Arrange
            var addShowResponse = TestDataBuilder.ShowResponses.AddShowSuccess;
            var seasonsRequested = TestDataBuilder.TestConstants.TestSeasons;

            _mockSonarrHttp.Setup(x => x.SonarrHttpPost(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(TestDataBuilder.HttpResponses.SuccessResponse(addShowResponse));

            // Act
            var result = await _sonarrService.SendShowRequest(TestDataBuilder.TestConstants.TestShowTvdbId, TestDataBuilder.TestConstants.TestShowTitle, seasonsRequested);

            // Assert
            Assert.IsTrue(result);

            // Verify HTTP call was made
            _mockSonarrHttp.Verify(x => x.SonarrHttpPost(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }

        /// <summary>
        /// Test to ensure that sending a show request handles HTTP failure gracefully.
        /// </summary>
        /// <returns></returns>
        [TestMethod]
        public async Task SonarrService_SendShowRequest_HttpFailure_ReturnsFalse()
        {
            // Arrange
            var seasonsRequested = TestDataBuilder.TestConstants.TestSeasons;

            _mockSonarrHttp.Setup(x => x.SonarrHttpPost(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(TestDataBuilder.HttpResponses.FailureResponse("Service unavailable"));

            // Act
            var result = await _sonarrService.SendShowRequest(TestDataBuilder.TestConstants.TestShowTvdbId, TestDataBuilder.TestConstants.TestShowTitle, seasonsRequested);

            // Assert
            Assert.IsFalse(result);
        }

        #endregion

        #region Validation Tests

        /// <summary>
        /// Test to ensure that getting movie details with null TMDB ID throws an ArgumentNullException.
        /// </summary>
        /// <returns></returns>
        [TestMethod]
        public async Task RadarrService_GetMovieDetails_NullTmdbId_ThrowsArgumentNullException()
        {
            // Act & Assert
            try
            {
                await _radarrService.GetMovieDetails(null);
                Assert.Fail("Expected ArgumentNullException was not thrown");
            }
            catch (ArgumentNullException)
            {
                Assert.IsTrue(true);
            }
            catch (Exception)
            {
                Assert.Fail("Expected ArgumentNullException was not thrown");
            }
        }

        /// <summary>
        /// Test to ensure that sending a movie request with null title throws an ArgumentNullException.
        /// </summary>
        /// <returns></returns>
        [TestMethod]
        public async Task RadarrService_SendMovieRequest_NullTitle_ThrowsArgumentNullException()
        {
            // Act & Assert
            try
            {
                await _radarrService.SendMovieRequest(null, TestDataBuilder.TestConstants.TestMovieTmdbId);
                Assert.Fail("Expected ArgumentNullException was not thrown");
            }
            catch (ArgumentNullException)
            {
                Assert.IsTrue(true);
            }
            catch (Exception)
            {
                Assert.Fail("Expected ArgumentNullException was not thrown");
            }
        }

        /// <summary>
        /// Test to ensure that sending a movie request with null TMDB ID throws an ArgumentNullException.
        /// </summary>
        /// <returns></returns>
        [TestMethod]
        public async Task RadarrService_SendMovieRequest_NullTmdbId_ThrowsArgumentNullException()
        {
            // Act & Assert
            try
            {
                await _radarrService.SendMovieRequest(TestDataBuilder.TestConstants.TestMovieTitle, null);
                Assert.Fail("Expected ArgumentNullException was not thrown");
            }
            catch (ArgumentNullException)
            {
                Assert.IsTrue(true);
            }
            catch (Exception)
            {
                Assert.Fail("Expected ArgumentNullException was not thrown");
            }
        }

        /// <summary>
        /// Test to ensure that getting show details with null TVDB ID throws an ArgumentNullException.
        /// </summary>
        /// <returns></returns>
        [TestMethod]
        public async Task SonarrService_GetShowDetails_NullTvdbId_ThrowsArgumentNullException()
        {
            // Act & Assert
            try
            {
                await _sonarrService.GetShowDetails(null);
                Assert.Fail("Expected ArgumentNullException was not thrown");
            }
            catch (ArgumentNullException)
            {
                Assert.IsTrue(true);
            }
            catch (Exception)
            {
                Assert.Fail("Expected ArgumentNullException was not thrown");
            }
        }

        /// <summary>
        /// Test to ensure that sending a show request with null TVDB ID throws an ArgumentNullException.
        /// </summary>
        /// <returns></returns>
        [TestMethod]
        public async Task SonarrService_SendShowRequest_NullTvdbId_ThrowsArgumentNullException()
        {
            // Act & Assert
            try
            {
                await _sonarrService.SendShowRequest(null, TestDataBuilder.TestConstants.TestShowTitle, TestDataBuilder.TestConstants.TestSeasons);
                Assert.Fail("Expected ArgumentNullException was not thrown");
            }
            catch (ArgumentNullException)
            {
                Assert.IsTrue(true);
            }
            catch (Exception)
            {
                Assert.Fail("Expected ArgumentNullException was not thrown");
            }
        }

        /// <summary>
        /// Test to ensure that sending a show request with null title throws an ArgumentNullException.
        /// </summary>
        /// <returns></returns>
        [TestMethod]
        public async Task SonarrService_SendShowRequest_NullTitle_ThrowsArgumentNullException()
        {
            // Act & Assert
            try
            {
                await _sonarrService.SendShowRequest(TestDataBuilder.TestConstants.TestShowTvdbId, null, TestDataBuilder.TestConstants.TestSeasons);
                Assert.Fail("Expected ArgumentNullException was not thrown");
            }
            catch (ArgumentNullException)
            {
                Assert.IsTrue(true);
            }
            catch (Exception)
            {
                Assert.Fail("Expected ArgumentNullException was not thrown");
            }
        }

        /// <summary>
        /// Test to ensure that sending a show request with null seasons throws an ArgumentNullException.
        /// </summary>
        /// <returns></returns>
        [TestMethod]
        public async Task SonarrService_SendShowRequest_NullSeasons_ThrowsArgumentNullException()
        {
            // Act & Assert
            try
            {
                await _sonarrService.SendShowRequest(TestDataBuilder.TestConstants.TestShowTvdbId, TestDataBuilder.TestConstants.TestShowTitle, null!);
                Assert.Fail("Expected ArgumentNullException was not thrown");
            }
            catch (ArgumentNullException)
            {
                Assert.IsTrue(true);
            }
            catch (Exception)
            {
                Assert.Fail("Expected ArgumentNullException was not thrown");
            }
        }

        #endregion
    }
}