using dotNETLemmy.API.Types;
using dotNETLemmy.API.Types.Forms;
using Moq;

namespace LemmyNanny.Tests
{
    [TestClass]
    public sealed class LemmyManagerTests
    {
        [TestMethod]
        public async Task GetNextPosts_Returns_Posts()
        {
            var mockLemmyClient = new Mock<ILemmyHttpClient>();
            mockLemmyClient.Setup(o => o.GetPosts(It.IsAny<GetPostsForm>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(new dotNETLemmy.API.Types.Responses.GetPostsResponse()));

            var lemmyManager = new LemmyManager(mockLemmyClient.Object, dotNETLemmy.API.Types.Enums.SortType.TopDay, dotNETLemmy.API.Types.Enums.ListingType.All);

            var posts = await lemmyManager.GetNextPosts();
            Assert.IsNotNull(posts);
        }

        [TestMethod]
        public async Task GetCommentsFromPost_Returns_Posts()
        {
            var mockLemmyClient = new Mock<ILemmyHttpClient>();
            mockLemmyClient.Setup(o => o.GetComments(It.IsAny<GetCommentsForm>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(new dotNETLemmy.API.Types.Responses.GetCommentsResponse()));

            var lemmyManager = new LemmyManager(mockLemmyClient.Object, dotNETLemmy.API.Types.Enums.SortType.TopDay, dotNETLemmy.API.Types.Enums.ListingType.All);

            var comments = await lemmyManager.GetCommentsFromPost(1);
            Assert.IsNotNull(comments);
        }
    }
}
