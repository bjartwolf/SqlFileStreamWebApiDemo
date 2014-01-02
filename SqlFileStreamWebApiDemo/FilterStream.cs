using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Runtime.Remoting.Messaging;
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
            _xmlStream = new XmlTextReader(new BufferedStream(new GZipStream(content, CompressionMode.Decompress),1000000));
        }

        protected override bool TryComputeLength(out long length)
        {
            length = -1;
            return false;
        }

        private Task<bool> ReadAsync(XmlTextReader reader)
        {
            var tcs = new TaskCompletionSource<bool>();
            tcs.SetResult(reader.Read());
            return tcs.Task;
            //return Task.Run(() => reader.Read()); // This is very slow due to threadstuffz
        }


        protected async override Task SerializeToStreamAsync(Stream stream, TransportContext context)
        {
            //var streamWriter = new StreamWriter(new GZipStream(stream, CompressionMode.Compress), new UTF8Encoding());
            var streamWriter = new StreamWriter(stream, new UTF8Encoding());
            try
            {
                while (await ReadAsync(_xmlStream))
                {
                    {
                        switch (_xmlStream.NodeType)
                        {
                            case XmlNodeType.Element:
                                if (_xmlStream.Name.Contains("Species"))
                                {
                                    var inner = _xmlStream.ReadInnerXml();
                                    if (inner.Contains("norvegicus"))
                                    {
                                        await streamWriter.WriteAsync(inner + "<br>");        
                                    }
                                    
                                }
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
                }

                streamWriter.Flush();
                streamWriter.Close();
            }
            catch (Exception ex)
            {
                Debug.Print(ex.Message);
            }

            if (_command.Connection != null)
            {
                _command.Connection.Dispose();
            }
            if (_command != null)
            {
                _command.Dispose();
            }
            Dispose();
        }
    }

}