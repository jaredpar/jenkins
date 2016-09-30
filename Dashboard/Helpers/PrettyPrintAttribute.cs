using System;
using System.Net.Http.Formatting;
using System.Web;
using System.Web.Http.Filters;

namespace Dashboard.Helpers
{
    public class PrettyPrintFilterAttribute : ActionFilterAttribute
    {
        /// <summary>
        /// Constant for the query string key word
        /// </summary>
        public const string PrettyConstant = "pretty";

        /// <summary>
        /// Interceptor that parses the query string and pretty prints 
        /// </summary>
        public override void OnActionExecuted(HttpActionExecutedContext actionExecutedContext)
        {
            JsonMediaTypeFormatter jsonFormatter = actionExecutedContext.ActionContext.RequestContext.Configuration.Formatters.JsonFormatter;
            jsonFormatter.SerializerSettings.Formatting = Newtonsoft.Json.Formatting.None;

            var queryString = actionExecutedContext.ActionContext.Request.RequestUri.Query;
            if (!String.IsNullOrWhiteSpace(queryString))
            {
                string prettyPrint = HttpUtility.ParseQueryString(queryString.ToLower().Substring(1))[PrettyConstant];
                bool canPrettyPrint;
                if ((string.IsNullOrEmpty(prettyPrint) && queryString.ToLower().Contains(PrettyConstant)) ||
                    Boolean.TryParse(prettyPrint, out canPrettyPrint) && canPrettyPrint)
                {
                    jsonFormatter.SerializerSettings.Formatting = Newtonsoft.Json.Formatting.Indented;
                }
            }
            base.OnActionExecuted(actionExecutedContext);
        }
    }
}