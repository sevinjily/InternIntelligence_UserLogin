using System.Net;

namespace Business.Utilities.Results.Abstract
{
   public interface IResult
    {
        bool Success { get; }
        string Message { get; }
        HttpStatusCode StatusCode { get; }
    }
}
