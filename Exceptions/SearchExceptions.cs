using System;

namespace MVNFOEditor.Exceptions;

public class SearchExceptions
{
    public class ResultsEmptyException : Exception
    {
        public ResultsEmptyException() { }

        public ResultsEmptyException(string message)
            : base(message) { }

        public ResultsEmptyException(string message, Exception inner)
            : base(message, inner) { }
    }
}