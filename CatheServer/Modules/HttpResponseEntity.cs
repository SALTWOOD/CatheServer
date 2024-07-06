using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace CatheServer.Modules
{
    [JsonObject]
    public struct HttpResponseEntity
    {
        public HttpResponseEntity()
        {

        }

        [JsonProperty("code")]
        public int StatusCode { get; set; } = 200;

        [JsonProperty("message")]
        public string Message { get; set; } = "success";

        [JsonProperty("data")]
        public object? Data { get; set; } = null;

        [JsonProperty("$isUnknownError")]
        public bool unknownError { get; set; } = false;

        [JsonProperty("error")]
        public Error Error { get; set; } = new Error();
    }

    public struct Error
    {
        public Error() { }

        [JsonProperty("type")]
        public string? Type { get; set; } = null;

        [JsonProperty("message")]
        public string? Message { get; set; } = null;
    }
}
