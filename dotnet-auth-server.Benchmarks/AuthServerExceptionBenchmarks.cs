using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using DotnetAuthServer.Exceptions;

namespace DotnetAuthServer.Benchmarks
{
    [MemoryDiagnoser]
    public class AuthServerExceptionBenchmarks
    {
        // Number of detail entries to add to the exception
        [Params(10, 100, 1000)]
        public int DetailsCount;

        private AuthServerException _exceptionWithDetails;
        private Dictionary<string, object> _details;

        [GlobalSetup]
        public void GlobalSetup()
        {
            // Prepare a dictionary with the required number of detail entries
            _details = new Dictionary<string, object>(DetailsCount);
            for (int i = 0; i < DetailsCount; i++)
            {
                _details[$"key{i}"] = i;
            }

            // Create an exception and populate its Details dictionary
            _exceptionWithDetails = new AuthServerException(
                errorCode: "invalid_request",
                message: "Invalid request",
                statusCode: 400,
                errorDescription: "The request is invalid",
                errorUri: "https://example.com/error",
                innerException: null);

            foreach (var kvp in _details)
            {
                _exceptionWithDetails.Details.Add(kvp.Key, kvp.Value);
            }
        }

        // Benchmark creating an exception without any details
        [Benchmark]
        public AuthServerException CreateException()
        {
            return new AuthServerException(
                errorCode: "invalid_request",
                message: "Invalid request",
                statusCode: 400,
                errorDescription: "The request is invalid",
                errorUri: "https://example.com/error",
                innerException: null);
        }

        // Benchmark creating an exception and then adding a variable number of details
        [Benchmark]
        public AuthServerException CreateExceptionWithDetails()
        {
            var ex = new AuthServerException(
                errorCode: "invalid_request",
                message: "Invalid request",
                statusCode: 400,
                errorDescription: "The request is invalid",
                errorUri: "https://example.com/error",
                innerException: null);

            foreach (var kvp in _details)
            {
                ex.Details.Add(kvp.Key, kvp.Value);
            }

            return ex;
        }

        // Benchmark converting an exception (already populated with details) to an error response dictionary
        [Benchmark]
        public Dictionary<string, object> ToErrorResponse()
        {
            return _exceptionWithDetails.ToErrorResponse();
        }
    }
}
