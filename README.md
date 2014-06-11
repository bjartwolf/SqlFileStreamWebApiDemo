SqlFileStreamWebApiDemo
=======================

Here is a presentation of this topic from NDC Oslo 2014
https://vimeo.com/97507562

# Overview
The basic idea is to serve large JSON or XML files directly as streams of binary data. In a typical .NET Web API architecture with Entity Framwork and standard serializers are used, the web server must hold both large objects and the serialized result in memory for as long as the request is active. For responses around 100 MB this will lead to performance problems, also for the database. Standard caching strategies might not work very well in this case. There are strategies to avoid this, the one I am looking into in this demo is my favorite. 

I have not done very formal load testing, but some initial results have shown that where 10 requests in parallel would consume 10 GB of RAM in IIS we get around 300 MB with this strategy. Normal use was around 200 MB. Maybe one day I will do some proper testing, but it is hard on a single machine as real-life bottlenecks propably are greatly affected by the network, running databases on seperate instances and so forth. 

In strategy, someone has to gzip and store the data in SQL in advance. This could be done by a timer or triggered by a user request. It does not have to be done by the web application itself, so how we get the data into the database is out of the scope for this demo (except for getting some demo data in there). When a client makes a request, we ask SQL Server for a handle to the file that SQL Server is using as its underlying storage. This allows us to stream the raw data directly to the client, and simply by stating the encoding and content-type the client renders the data correctly. Gzip is unversially understood by web clients. I tried the example with PowerQuery in 64 bit Excel 2013 and it works very well to consume data directly from the web server, and I also got it working fine with Chrome.

              +-----------------+
              |                 |
              |     Browser     |
              |                 |
              |  Unpacking gzip |
              +--------+--------+
                       | Content-Encoding: gzip
                       | Content-Type: application/xml
              +--------+--------+
              |                 |
              |    Web API      | Web API serving a binary stream
              |                 | directly from SQL Server using
              +--------+--------+ SqlFileStream
                       |
              +--------+--------+
              |                 |
              |    SQL Server   | SQL Server is storing each entry as a file on disk
              |                 | SQL Server deals with transactions and so on.
              +--------+--------+
                       |
              +--------+--------+
              |                 |
              |    Filesystem   | We get a file handle directly from disk that we can 
              |                 | stream data from
              +-----------------+

# Simpler demos
Most of the complexity in this demo has to do with using SQL Server FILESTREAM. These two demos demonstrates the principle by streaming directly from file and is a lot easier to get running.

## In node.js
A simpler example that demonstrates the principle can be tested in node.js using a file directly.

```javascript      
var http = require('http');
var fs = require('fs');
http.createServer(function (req, res) {
    res.writeHead(200, {'Content-Type': 'application/xml',
                        'Content-Encoding':'gzip'});  
    fs.createReadStream('SwissProt.xml.gz').pipe(res);
}).listen(1337, '127.0.0.1');
```

## In C# directly from file

Or in Web API, reading directly from a file https://github.com/bjartwolf/FileStreamWebApiDemo

```c#
public class FastController : ApiController
{
    [Route("")]
    public HttpResponseMessage GetResult()
    {
        var fs = new FileStream(Path.Combine(HttpRuntime.AppDomainAppPath, "medline13n0701.xml.gz"), FileMode.Open);
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StreamContent(fs)
        };
        response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/xml");
        response.Content.Headers.ContentEncoding.Add("gzip");
        return response;
    }
}
```
              
# Getting the DB set up and testdata

Obviously you can use whatever huge XML or JSON file with Enterprise data that you happen to have, but if you don't have any data lying around then get the testdata from z available from http://www.cs.washington.edu/research/xmldatasets/www/repository.html
Direct download link: http://www.cs.washington.edu/research/xmldatasets/data/SwissProt/SwissProt.xml.gz

The gzip file is about 12% the size of the original XML file.

## Configuring SQL Server FILESTREAM
I think it is better to follow the instructions in "Appendix A: Configuring FILESTREAM on a SQL Server Instance" in
http://download.red-gate.com/ebooks/SQL/Art_of_SS_Filestream_Sebastian_and_Aelterman.pdf

There are some configurations that must be done both in SQL Server Configuration Manager and in SQL Server Management Studio. 


## The database
FILESTREAM fields can not be initialized from the user interface, but must be added by script.
I just dumped my create script...
 ```sql
USE [MyFastDB]
GO

/****** Object:  Table [dbo].[FASTTABLE]    Script Date: 29.12.2013 21:47:33 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

SET ANSI_PADDING ON
GO

CREATE TABLE [dbo].[FASTTABLE](
	[ID] [uniqueidentifier] ROWGUIDCOL  NOT NULL,
	[ZippedXML] [varbinary](max) FILESTREAM  NULL,
 CONSTRAINT [PK_FASTTABLE] PRIMARY KEY CLUSTERED 
(
	[ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY] FILESTREAM_ON [MyFastDBfs],
UNIQUE NONCLUSTERED 
(
	[ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] FILESTREAM_ON [MyFastDBfs]

GO

SET ANSI_PADDING OFF
GO
 ```



## Insert testdata in the database
I prefer to use LINQPad, so I simply insert the .gz file into the manually created table with a carefully chosen GUID already inserted in a row.
 ```cs
var guid = new Guid("1277453e-6894-4c57-95b3-8498b316d43a");
FASTTABLEs.Where(r => r.ID == guid).First().ZippedXML =
			File.ReadAllBytes(@"C:\Users\beb\Downloads\SwissProt.xml.gz");
SubmitChanges();
 ```

# Running the application
The Visual Studio 2013 project should fetch its dependencies using Nuget and run without problems...
I could only parse this large files in Chrome and using Excel PowerQuery, but try it in your favorite browser that you think is capable of handling it...


# Troubleshooting

I had a lot of issues that had to do with the gzip data being formatted correctly. For example I UTF-8 encoded the response due to some old code I hadn't changed. When the browser could not decode the response, it just closes the connection and shows no error message. IIS just sees that the response has been closed by the browser. I assumed I was closing the stream prematurely, whereas the real issue was the content in the stream. This is easily seen with Fiddler, which I of course should have used, but didn't and wasted a lot of time digging deep into Web API.
