using System;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Web.Http;
using System.Web.UI.WebControls;
using System.Xml;
using System.Xml.Schema;

namespace SqlFileStreamWebApiDemo.Controllers
{
    public class FastController : ApiController
    {
        [Route("")]
        public HttpResponseMessage GetResult()
        {
            var con = new SqlConnection("database=MyFastDB;server=(local);integrated security=sspi");
            var id = new Guid("1277453e-6894-4c57-95b3-8498b316d43a");
            var cmd = new SqlCommand
            {
                Connection = con,
                CommandText =
                    @"SELECT [ZippedXML].PathName() as filePath, GET_FILESTREAM_TRANSACTION_CONTEXT() AS txContext FROM [FASTTABLE] WHERE ID = '" +
                    id + "'"
            };
            con.Open();
            SqlTransaction transaction = con.BeginTransaction("ItemTran");
            cmd.Transaction = transaction;
            string filePath = "";
            SqlDataReader reader = cmd.ExecuteReader();
            byte[] txContext = null;
            while (reader.Read())
            {
                txContext = (reader["txContext"] as byte[]);
                filePath = reader["filePath"].ToString();
            }
            reader.Close();

            var sqlStream = new SqlFileStream(filePath, txContext, FileAccess.Read);
            
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {                   
                Content = new FilterStream(sqlStream, 1048576, cmd)
            };
            //response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/xml");
            //response.Content.Headers.ContentEncoding.Add("gzip");


            return response;
        }
    }
}