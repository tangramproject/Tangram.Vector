using System;
namespace Core.API.Helper
{
    public class ApiException: Exception
    {
        public int StatusCode { get; set; }

        public string Content { get; set; }
    }
}
