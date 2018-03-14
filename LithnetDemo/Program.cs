using Lithnet.ResourceManagement.Client;
using OCG.ResourceManagement.ObjectModel;
using OCG.ResourceManagement.PortalServiceModel;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.Caching;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace LithnetDemo
{
    public class PagedResultSet
    {
        public int TotalCount { get; set; }

        public bool HasMoreItems { get; set; }

        public string NextPage { get; set; }

        public string PreviousPage { get; set; }

        public IList<ResourceObject> Results { get; set; }
    }

    class Program
    {
        private static MemoryCache searchCache = new MemoryCache("seach-results");

        private static string buildCacheKey(string token)
        {
            //return token + ((WindowsIdentity)HttpContext.Current.User.Identity).Name;

            return token + WindowsIdentity.GetCurrent().Name;
        }

        private static SearchResultPager getSearchResultPager(ResourceManagementClient client, string filter, int pageSize, string token)
        {
            SearchResultPager p;

            if (token == null)
            {
                CultureInfo locale = null;
                IEnumerable<string> attributes = new string[] { "DisplayName" };

                if (attributes != null)
                {
                    p = client.GetResourcesPaged(filter, pageSize, attributes, locale);
                }
                else
                {
                    p = client.GetResourcesPaged(filter, pageSize, locale);
                }
            }
            else
            {
                p = (SearchResultPager)searchCache.Remove(buildCacheKey(token));

                if (p == null)
                {
                    throw new ArgumentException("Invalid token");
                }
            }
            return p;
        }

        static void Main(string[] args)
        {
            try
            {
                string filter = args.Length > 0 ? args[0] : "/*[starts-with(DisplayName,'%')]";
                int index = args.Length > 1 ? Convert.ToInt32(args[1]) : -1;
                string token = args.Length > 2 ? args[2] : null;
                
                int pageSize = 100;

                ResourceManagementClient client = new ResourceManagementClient();
                client.RefreshSchema();

                SearchResultPager p = getSearchResultPager(client, filter, pageSize, token);

                token = token ?? Guid.NewGuid().ToString();

                if (index >= 0)
                {
                    p.CurrentIndex = index;
                }

                p.PageSize = pageSize;

                int oldIndex = p.CurrentIndex;

                PagedResultSet results = new PagedResultSet
                {
                    Results = p.GetNextPage().ToList(),
                };
                results.TotalCount = p.TotalCount;
                results.HasMoreItems = index + pageSize < p.TotalCount;

                searchCache.Add(buildCacheKey(token), p, new CacheItemPolicy() { SlidingExpiration = new TimeSpan(0, 5, 0) });
                
                foreach (ResourceObject resource in results.Results)
                {
                    Console.WriteLine(resource.DisplayName);
                }
                Console.WriteLine("Total Count: " + results.TotalCount);
                Console.WriteLine("More Items to Load: " + results.HasMoreItems);
                
                //Uri nextPageUri;
                //Uri previousPageUri;

                //ResourceManagementWebServicev2.GetPageUris(context, oldIndex, pageSize, token, p, out previousPageUri, out nextPageUri);

                //results.NextPage = nextPageUri?.ToString();
                //results.PreviousPage = previousPageUri?.ToString();
                //results.TotalCount = p.TotalCount;
                //results.HasMoreItems = results.NextPage != null;

                //ResourceManagementWebServicev2.searchCache.Add(ResourceManagementWebServicev2.BuildCacheKey(token), p, new CacheItemPolicy() { SlidingExpiration = new TimeSpan(0, 5, 0) });

                //return WebResponseHelper.GetResponse(results, false);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}
