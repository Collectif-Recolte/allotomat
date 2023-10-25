using System;

namespace Sig.App.Backend.Utilities
{
    public readonly struct Page
    {
        public Page(int pageNumber, int pageSize)
        {
            if (pageNumber < 0)
                throw new ArgumentOutOfRangeException();

            PageNumber = pageNumber;
            PageSize = pageSize;
        }

        public int PageNumber { get; }
        public int PageSize { get; }

        public int Skip => (PageNumber - 1) * PageSize;
    }
}
