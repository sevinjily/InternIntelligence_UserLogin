using System.Net;

namespace Business.Results.Concrete.SuccessResult
{
   public class SuccessResult:Result
    {
        public SuccessResult(string message, HttpStatusCode statusCode) : base(message, true, statusCode)
        {

        }
        public SuccessResult(HttpStatusCode statusCode) : base(true, statusCode)
        {

        }
    }
}
