using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Xml;

namespace SqlFileStreamWebApiDemo
{
    public class FilterStream: BlobStream
    {
        private XmlTextReader _xmlStream;
        public FilterStream(Stream content, SqlCommand cmd) : base(content, cmd)
        {
            _xmlStream = new XmlTextReader(new GZipStream(content, CompressionMode.Decompress));
        }

        public FilterStream(Stream content, int bufferSize, SqlCommand cmd) : base(content, bufferSize, cmd)
        {
            _xmlStream = new XmlTextReader(new GZipStream(content, CompressionMode.Decompress));
        }

        //protected override Task SerializeToStreamAsync(Stream stream, TransportContext context)
            protected override Task SerializeToStreamAsync(Stream stream, TransportContext context)
        {
            var streamWriter = new StreamWriter(stream, new UTF8Encoding());
            //while (await _xmlStream.ReadAsync())
            while (_xmlStream.Read())
            {
                switch (_xmlStream.NodeType)
                {
                    case XmlNodeType.Element:
                        //await streamWriter.WriteAsync(_xmlStream.Name);
                        streamWriter.Write(_xmlStream.Name);
                        break;
                    case XmlNodeType.Text:
                        break;
                    case XmlNodeType.XmlDeclaration:
                    case XmlNodeType.ProcessingInstruction:
                        break;
                    case XmlNodeType.Comment:
                        break;
                    case XmlNodeType.EndElement:
                        break;
                }
            }
            streamWriter.Flush();
            var tsk = new TaskCompletionSource<bool>();
            tsk.SetResult(true);
            return tsk.Task;
        }
    }

}