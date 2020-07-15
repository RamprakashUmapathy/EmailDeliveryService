using EmailDeliveryService.ViewModel;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace EmailDeliveryService.Infrastructure
{
    /// <summary>
    /// Defines a template
    /// </summary>
    public interface ITemplate<T>
    {
        /// <summary>
        /// method to implement for loading data from various data sources
        /// </summary>
        /// <param name="connenctionManagers">pool of data source connection providers</param>
        Task LoadDataAsync(Dictionary<string, IDbConnection> connenctionManagers);

        /// <summary>
        /// method to implement for reading the data by pagination
        /// </summary>
        /// <param name="connection">Connection provider object</param>
        /// <param name="pageNumber">Page Number</param>
        /// <param name="pageSize">Page Size</param>
        /// /// <returns></returns>
        Task<PaginationViewModel<T>> GetDataAsync(IDbConnection connection, int pageNumber, int pageSize);
    }

}
