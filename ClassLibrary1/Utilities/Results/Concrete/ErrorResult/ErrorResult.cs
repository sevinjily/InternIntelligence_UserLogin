using Business.Utilities.Results.Concrete;
using System.Net;

namespace Business.Utilities.Results.Concrete.ErrorResult
{
   public class ErrorResult:Result
    {
        public ErrorResult(string message, HttpStatusCode statusCode) : base(message, false, statusCode)
        {

        }
        public ErrorResult(HttpStatusCode statusCode) : base(false, statusCode)
        {

        }
    }
}
