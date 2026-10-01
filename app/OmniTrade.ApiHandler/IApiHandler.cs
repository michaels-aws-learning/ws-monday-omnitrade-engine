using System;
using System.Collections.Generic;
using System.Text;

namespace OmniTrade.ApiHandler
{
    public interface IApiHandler<T, T1>
    {
        public Task<T> HttpHandlerAsync(T1 requestBody, string url, HttpMethod method, Dictionary<string, string> headers, string methodType);
    }
}
