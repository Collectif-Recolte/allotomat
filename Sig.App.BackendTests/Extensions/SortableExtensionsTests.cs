using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Xunit;
using Sig.App.Backend.DbModel;
using Sig.App.Backend.Extensions;

namespace Sig.App.BackendTests.Extensions;

public class SortableExtensionsTests
{
    private readonly IEnumerable<Item> unsortedItems = new Item[]
    {
        new("C", 3),
        new("A", 1),
        new("D", 4),
        new("B", 2)
    };

    [Fact]
    public void CanSortEnumerable()
    {
        unsortedItems.Sorted()
            .Should().BeAssignableTo<IOrderedEnumerable<Item>>()
            .And.BeInAscendingOrder();
    }
    
    [Fact]
    public void CanSortQueryable()
    {
        unsortedItems.AsQueryable().Sorted()
            .Should().BeAssignableTo<IOrderedQueryable<Item>>()
            .And.BeInAscendingOrder();
    }

    private class Item : ISortable, IComparable<Item>
    {
        public Item(string name, int sortOrder)
        {
            Name = name;
            SortOrder = sortOrder;
        }

        public string Name { get; }
        public int SortOrder { get; }
        

        public int CompareTo(Item other)
        {
            if (ReferenceEquals(this, other)) return 0;
            if (ReferenceEquals(null, other)) return 1;
            return string.Compare(Name, other.Name, StringComparison.Ordinal);
        }
    }
}