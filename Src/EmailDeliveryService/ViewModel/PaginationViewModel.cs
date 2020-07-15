using System;
using System.Collections.Generic;
using System.Text;

namespace EmailDeliveryService.ViewModel
{
    /// <summary>
    /// Pagination class for returning the query results with page information 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class PaginationViewModel<T>
    {
        /// <summary>
        /// Total number of records
        /// </summary>
        public int RecordCount { get; set; }

        /// <summary>
        /// Currrent page size
        /// </summary>
        public int PageSize { get; set; }

        /// <summary>
        /// Current page
        /// </summary>
        public int CurrentPage { get; set; }

        /// <summary>
        /// Total number of pages available
        /// </summary>
        public int TotalPages { get; set; }

        /// <summary>
        /// The actual data
        /// </summary>
        public IEnumerable<T> Data { get; set; }

    }
}
