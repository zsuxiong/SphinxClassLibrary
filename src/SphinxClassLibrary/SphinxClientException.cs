namespace SphinxClassLibrary
{
    using System;

    public class SphinxClientException : Exception {
        public SphinxClientException(string _message) : base(_message) { }
    }
}
