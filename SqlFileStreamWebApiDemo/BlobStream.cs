using System.Data.SqlClient;
using System.IO;
using System.Net.Http;

namespace SqlFileStreamWebApiDemo
{
    public class BlobStream : StreamContent
    {
        private readonly SqlCommand _command;

        public BlobStream(Stream content, SqlCommand cmd) : base(content)
        {
            _command = cmd;
        }

        public BlobStream(Stream content, int bufferSize, SqlCommand cmd) : base(content, bufferSize)
        {
            _command = cmd;
        }

        protected override void Dispose(bool disposing)
        {
            if (_command.Connection != null)
            {
                
                _command.Connection.Dispose();
            }
            if (_command != null)
            {
                _command.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}