using Currency.Common.Pagination;

namespace Currency.Common.Tests.Pagination;

[TestFixture]
[Category("Unit")]
public class PagedListTests
{
    private List<int> _sampleData = null!;

    [SetUp]
    public void Setup()
    {
        _sampleData = Enumerable.Range(1, 50).ToList();
    }

    [Test]
    public void Create_ShouldContainAllItems_WithCorrectCountAndPagingProperties()
    {
        // Arrange
        int pageNumber = 0;
        int pageSize = 10;

        // Act
        var pagedList = PagedList<int>.Create(_sampleData, pageNumber, pageSize);

        // Assert
        Assert.That(pagedList.Items.Count, Is.EqualTo(_sampleData.Count));
        Assert.That(pagedList.TotalCount, Is.EqualTo(_sampleData.Count));
        Assert.That(pagedList.PageNumber, Is.EqualTo(pageNumber));
        Assert.That(pagedList.PageSize, Is.EqualTo(pageSize));
        Assert.That(pagedList.Items, Is.EquivalentTo(_sampleData));
    }

    [Test]
    public void CreateFromRaw_ShouldReturnCorrectPageItems_AndCorrectPagingMetadata()
    {
        // Arrange
        int pageNumber = 1;
        int pageSize = 10;

        // Act
        var pagedList = PagedList<int>.CreateFromRaw(_sampleData, pageNumber, pageSize);

        // Assert
        var expectedItems = _sampleData.Skip(pageNumber * pageSize).Take(pageSize).ToList();

        Assert.Multiple(() =>
        {
            Assert.That(pagedList.Items, Is.EquivalentTo(expectedItems));
            Assert.That(pagedList.TotalCount, Is.EqualTo(_sampleData.Count));
            Assert.That(pagedList.PageNumber, Is.EqualTo(pageNumber));
            Assert.That(pagedList.PageSize, Is.EqualTo(pageSize));
        });
    }

    [Test]
    public void CreateFromRaw_WhenPageNumberIsZero_ShouldReturnFirstPageItems()
    {
        // Arrange
        int pageNumber = 0;
        int pageSize = 10;

        // Act
        var pagedList = PagedList<int>.CreateFromRaw(_sampleData, pageNumber, pageSize);

        // Assert
        var expectedItems = _sampleData.Take(pageSize).ToList();

        Assert.That(pagedList.Items, Is.EquivalentTo(expectedItems));
    }

    [Test]
    public void CreateFromRaw_WhenPageSizeIsLargerThanCollection_ShouldReturnAllItems()
    {
        // Arrange
        int pageNumber = 0;
        int pageSize = 1000;

        // Act
        var pagedList = PagedList<int>.CreateFromRaw(_sampleData, pageNumber, pageSize);

        // Assert
        Assert.That(pagedList.Items.Count, Is.EqualTo(_sampleData.Count));
        Assert.That(pagedList.Items, Is.EquivalentTo(_sampleData));
    }

    [Test]
    public void CreateFromRaw_WhenPageNumberExceedsPages_ShouldReturnEmptyItems()
    {
        // Arrange
        int pageNumber = 10; // beyond last page
        int pageSize = 10;

        // Act
        var pagedList = PagedList<int>.CreateFromRaw(_sampleData, pageNumber, pageSize);

        // Assert
        Assert.That(pagedList.Items, Is.Empty);
        Assert.That(pagedList.TotalCount, Is.EqualTo(_sampleData.Count));
        Assert.That(pagedList.PageNumber, Is.EqualTo(pageNumber));
        Assert.That(pagedList.PageSize, Is.EqualTo(pageSize));
    }

    [Test]
    public void Create_WithEmptySource_ShouldReturnEmptyPagedList()
    {
        // Arrange
        var emptySource = Enumerable.Empty<int>();

        // Act
        var pagedList = PagedList<int>.Create(emptySource, 0, 10);

        // Assert
        Assert.That(pagedList.Items, Is.Empty);
        Assert.That(pagedList.TotalCount, Is.Zero);
        Assert.That(pagedList.PageNumber, Is.Zero);
        Assert.That(pagedList.PageSize, Is.EqualTo(10));
    }

    [Test]
    public void CreateFromRaw_WithEmptySource_ShouldReturnEmptyPagedList()
    {
        // Arrange
        var emptySource = Enumerable.Empty<int>();

        // Act
        var pagedList = PagedList<int>.CreateFromRaw(emptySource, 0, 10);

        // Assert
        Assert.That(pagedList.Items, Is.Empty);
        Assert.That(pagedList.TotalCount, Is.Zero);
        Assert.That(pagedList.PageNumber, Is.Zero);
        Assert.That(pagedList.PageSize, Is.EqualTo(10));
    }
}