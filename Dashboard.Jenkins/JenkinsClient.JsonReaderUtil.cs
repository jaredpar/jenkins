using Newtonsoft.Json;
using RestSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dashboard.Jenkins
{
    public sealed partial class JenkinsClient
    {
        private sealed class JsonReaderUtil<T>
        {
            internal Func<JsonReader, T> Func { get; }
            internal T Value { get; set; }
            internal Exception Exception { get; set;}
            internal IRestResponse Response { get; set; }

            internal bool Succeeded => Exception == null;

            internal JsonReaderUtil(Func<JsonReader, T> func)
            {
                Func = func;
            }

            internal void Run(Stream stream)
            {
                using (var streamReader = new StreamReader(stream))
                using (var jsonReader = new JsonTextReader(streamReader))
                {
                    try
                    {
                        Value = Func(jsonReader);
                    }
                    catch (Exception ex)
                    {
                        Exception = ex;
                    }
                }
            }
        }
    }
}
