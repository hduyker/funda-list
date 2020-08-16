using System;
using Xunit;

namespace FundaListAppTests
{
    /// <summary>
    /// Test the Funda API
    /// </summary>
    public class TestFundaApi
    {
        [Fact]
        public void RetrieveSingleItem()
        {
            // Arrange
            var target = new FundaApi();

            // Act
            var result = target.Query(searchType, "/amsterdam/tuin", pageSize);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue(result.Count() > 0);
        }

        [Fact]
        public void RetrieveSinglePage()
        {
            // Arrange
            var target = new FundaApi();

            // Act
            var result = target.Query(searchType, "/amsterdam/tuin", pageSize);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue(result.Count() > 0);
        }
    }
}
